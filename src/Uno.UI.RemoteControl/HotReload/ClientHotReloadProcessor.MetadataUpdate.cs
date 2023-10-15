#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DirectUI;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;
#if !WINUI
using Windows.System;
#else
using Microsoft.UI.Dispatching;
#endif
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RemoteControl.HotReload;

partial class ClientHotReloadProcessor
{
	private static int _isReloading;

	private static ElementUpdateAgent? _elementAgent;

	private static Logger _log = typeof(ClientHotReloadProcessor).Log();

	private static ElementUpdateAgent ElementAgent
	{
		get
		{
			_elementAgent ??= new ElementUpdateAgent(s =>
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace(s);
					}
				});

			return _elementAgent;
		}
	}

	private static async Task<bool> ShouldReload()
	{
		if (Interlocked.CompareExchange(ref _isReloading, 1, 0) == 1)
		{
			return false;
		}
		try
		{
			var waiter = TypeMappings.WaitForResume();
			if (!waiter.IsCompleted)
			{
				return false;
			}
			return await waiter;
		}
		finally
		{
			Interlocked.Exchange(ref _isReloading, 0);
		}
	}

	internal static Window? CurrentWindow { get; set; }

	private static async Task ReloadWithUpdatedTypes(Type[] updatedTypes)
	{
		var handlerActions = ElementAgent?.ElementHandlerActions;

		var uiUpdating = true;
		try
		{
			if (!await ShouldReload())
			{
				uiUpdating = false;
				return;
			}

			UpdateGlobalResources(updatedTypes);

			// Action: BeforeVisualTreeUpdate
			// This is called before the visual tree is updated
			_ = handlerActions?.Do(h => h.Value.BeforeVisualTreeUpdate(updatedTypes)).ToArray();

			var capturedStates = new Dictionary<string, Dictionary<string, object>>();

			var isCapturingState = true;
			var treeIterator = EnumerateHotReloadInstances(
					CurrentWindow?.Content,
					async (fe, key) =>
					{
						// Get the original type of the element, in case it's been replaced
						var liveType = fe.GetType();
						var originalType = liveType.GetOriginalType() ?? fe.GetType();

						// Get the handler for the type specified
						// Since we're only interested in handlers for specific element types
						// we exclude those registered for "object". Handlers that want to run
						// for all element types should register for FrameworkElement instead
						var handler = (from h in handlerActions
									   where (originalType == h.Key ||
											originalType.IsSubclassOf(h.Key)) &&
											h.Key != typeof(object)
									   select h.Value).FirstOrDefault();

						// Get the replacement type, or null if not replaced
						var mappedType = originalType.GetMappedType();

						if (handler is not null)
						{
							if (!capturedStates.TryGetValue(key, out var dict))
							{
								dict = new();
							}
							if (isCapturingState)
							{
								handler.CaptureState(fe, dict, updatedTypes);
								if (dict.Any())
								{
									capturedStates[key] = dict;
								}
							}
							else
							{
								await handler.RestoreState(fe, dict, updatedTypes);
							}
						}

						if (updatedTypes.Contains(liveType))
						{
							// This may happen if one of the nested types has been hot reloaded, but not the type itself.
							// For instance, a DataTemplate in a resource dictionary may mark the type as updated in `updatedTypes`
							// but it will not be considered as a new type even if "CreateNewOnMetadataUpdate" was set.

							return (fe, null, liveType);
						}
						else
						{
							return (handler is not null || mappedType is not null) ? (fe, handler, mappedType) : default;
						}
					},
					parentKey: default);

			// Forced iteration to capture all state before doing ui update
			var instancesToUpdate = await treeIterator.ToArrayAsync();

			// Iterate through the visual tree and either invoke ElementUpdate, 
			// or replace the element with a new one
			foreach (var (element, elementHandler, elementMappedType) in instancesToUpdate)
			{
				// Action: ElementUpdate
				// This is invoked for each existing element that is in the tree that needs to be replaced
				elementHandler?.ElementUpdate(element, updatedTypes);

				if (elementMappedType is not null)
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Error($"Updating element [{element}] to [{elementMappedType}]");
					}

					ReplaceViewInstance(element, elementMappedType, elementHandler);
				}
			}

			isCapturingState = false;
			// Forced iteration again to restore all state after doing ui update
			_ = await treeIterator.ToArrayAsync();

			// Action: AfterVisualTreeUpdate
			_ = handlerActions?.Do(h => h.Value.AfterVisualTreeUpdate(updatedTypes)).ToArray();
		}
		catch (Exception ex)
		{
			if (_log.IsEnabled(LogLevel.Error))
			{
				_log.Error($"Error doing UI Update - {ex.Message}", ex);
			}
			uiUpdating = false;
			throw;
		}
		finally
		{
			// Action: ReloadCompleted
			_ = handlerActions?.Do(h => h.Value.ReloadCompleted(updatedTypes, uiUpdating)).ToArray();
		}
	}

	/// <summary>
	/// Updates App-level resources (from app.xaml) using the provided updated types list.
	/// </summary>
	private static void UpdateGlobalResources(Type[] updatedTypes)
	{
		var globalResourceTypes = updatedTypes
			.Where(t => t?.FullName is { Length: > 0 } name
				&& !name.Contains('+') // Ignore nested types
				&& (name.IndexOf('#') switch
				{
					< 0 => name,
					{ } sharp => name[..sharp],
				}).EndsWith("GlobalStaticResources", StringComparison.OrdinalIgnoreCase)
			)
			.ToArray();

		if (globalResourceTypes.Length != 0)
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.Debug($"Updating app resources");
			}

			MethodInfo? GetInitMethod(Type type, string name)
			{
				if (type.GetMethod(
					name,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, Array.Empty<Type>()) is { } initializeMethod)
				{
					return initializeMethod;
				}
				else
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.Debug($"{name} method not found on {type}");
					}

					return null;
				}
			}

			// First, register all dictionaries
			foreach (var globalResourceType in globalResourceTypes)
			{
				// Follow the initialization sequence implemented by
				// App.InitializeComponent (Initialize then RegisterResourceDictionariesBySourceLocal).

				if (GetInitMethod(globalResourceType, "Initialize") is { } initializeMethod)
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Debug($"Initializing resources for {globalResourceType}");
					}

					// Invoke initializers so default types and other resources get updated.
					initializeMethod.Invoke(null, null);
				}

				// This should be called by the init of root app GlobalStaticResource.Initialize(), but here as we are reloading only the StaticResources of the dependency library,
				// we have to invoke it to make sure that all registrations are updated (including the "ms-appx://[NAME_OF_MY_LIBRARY]/...").
				if (GetInitMethod(globalResourceType, "RegisterResourceDictionariesBySource") is { } registerResourceDictionariesBySourceMethod)
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Debug($"Initializing resources sources for {globalResourceType}");
					}

					// Invoke initializers so default types and other resources get updated.
					registerResourceDictionariesBySourceMethod.Invoke(null, null);
				}

				// This is needed for the head only (causes some extra invalid registration ins the ResourceResolver, but it has no negative impact)
				if (GetInitMethod(globalResourceType, "RegisterResourceDictionariesBySourceLocal") is { } registerResourceDictionariesBySourceLocalMethod)
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Debug($"Initializing local resources sources for {globalResourceType}");
					}

					// Invoke initializers so default types and other resources get updated.
					registerResourceDictionariesBySourceLocalMethod.Invoke(null, null);
				}
			}


#if !(WINUI || WINAPPSDK || WINDOWS_UWP)
			// Then find over updated types to find the ones that are implementing IXamlResourceDictionaryProvider
			List<Uri> updatedDictionaries = new();

			foreach (var updatedType in updatedTypes)
			{
				if (updatedType.GetInterfaces().Contains(typeof(IXamlResourceDictionaryProvider)))
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Debug($"Updating resources for {updatedType}");
					}

					// This assumes that we're using an explicit implementation of IXamlResourceDictionaryProvider, which
					// provides an instance property that returns the new dictionary.
					var staticDictionaryProperty = updatedType
						.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

					if (staticDictionaryProperty?.GetMethod is { } getMethod)
					{
						if (getMethod.Invoke(null, null) is IXamlResourceDictionaryProvider provider
							&& provider.GetResourceDictionary() is { Source: not null } dictionary)
						{
							updatedDictionaries.Add(dictionary.Source);
						}
					}
				}
			}

			// Traverse the current app's tree to replace dictionaries matching the source property
			// with the updated ones.
			UpdateResourceDictionaries(updatedDictionaries, Application.Current.Resources);

			// Force the app reevaluate global resources changes
			Application.Current.UpdateResourceBindingsForHotReload();
#endif
		}
	}

#if !(WINUI || WINAPPSDK || WINDOWS_UWP)
	/// <summary>
	/// Refreshes ResourceDictionary instances that have been detected as updated
	/// </summary>
	/// <param name="updatedDictionaries"></param>
	/// <param name="root"></param>
	private static void UpdateResourceDictionaries(List<Uri> updatedDictionaries, ResourceDictionary root)
	{
		var dictionariesToRefresh = root
			.MergedDictionaries
			.Where(merged => updatedDictionaries.Any(d => d == merged.Source))
			.ToArray();

		foreach (var merged in dictionariesToRefresh)
		{
			root.RefreshMergedDictionary(merged);
		}
	}
#endif

	private static void ReplaceViewInstance(UIElement instance, Type replacementType, ElementUpdateAgent.ElementUpdateHandlerActions? handler = default, Type[]? updatedTypes = default)
	{
		if (replacementType.GetConstructor(Array.Empty<Type>()) is { } creator)
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"Creating instance of type {replacementType}");
			}

			var newInstance = Activator.CreateInstance(replacementType);
			var instanceFE = instance as FrameworkElement;
			var newInstanceFE = newInstance as FrameworkElement;
			if (instanceFE is not null &&
				newInstanceFE is not null)
			{
				handler?.BeforeElementReplaced(instanceFE, newInstanceFE, updatedTypes);

				SwapViews(instanceFE, newInstanceFE);

				handler?.AfterElementReplaced(instanceFE, newInstanceFE, updatedTypes);
			}
		}
		else
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.LogDebug($"Type [{instance.GetType()}] has no parameterless constructor, skipping reload");
			}
		}
	}

	public static void UpdateApplication(Type[] types)
	{
		foreach (var t in types)
		{
			if (t.GetCustomAttribute<System.Runtime.CompilerServices.MetadataUpdateOriginalTypeAttribute>() is { } update)
			{
				TypeMappings.RegisterMapping(t, update.OriginalType);
			}
		}

		if (_log.IsEnabled(LogLevel.Trace))
		{
			_log.Trace($"UpdateApplication (changed types: {string.Join(", ", types.Select(s => s.ToString()))})");
		}

#if WINUI
		var dispatcherQueue = CurrentWindow?.DispatcherQueue;
		if (dispatcherQueue is not null)
		{
			dispatcherQueue.TryEnqueue(async () => await ReloadWithUpdatedTypes(types));
		}
#else
		var dispatcher = CurrentWindow?.Dispatcher;
		if (dispatcher is not null)
		{
			_ = dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await ReloadWithUpdatedTypes(types));
		}
#endif
		else if (_log.IsEnabled(LogLevel.Warning))
		{
			_log.Warn($"Unable to access Dispatcher/DispatcherQueue in order to invoke {nameof(ReloadWithUpdatedTypes)}. Make sure you have enabled hot-reload (Window.EnableHotReload()) in app startup. See https://aka.platform.uno/hot-reload");
		}
	}
}

public static class AsyncEnumerableExtensions
{
	public async static Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> enumerable)
	{
		var list = new List<T>();
		await foreach (var item in enumerable)
		{
			list.Add(item);
		}
		return list.ToArray();
	}
}
