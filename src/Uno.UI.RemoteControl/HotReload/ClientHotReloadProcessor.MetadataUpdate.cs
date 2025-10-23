#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Diagnostics.UI;
using Uno.Threading;
using static Uno.UI.RemoteControl.HotReload.MetadataUpdater.ElementUpdateAgent;

#if HAS_UNO_WINUI
using _WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
#else
using _WindowActivatedEventArgs = Windows.UI.Core.WindowActivatedEventArgs;
#endif

namespace Uno.UI.RemoteControl.HotReload;

partial class ClientHotReloadProcessor
{
	private static readonly AsyncLock _uiUpdateGate = new(); // We can use the simple AsyncLock here as we don't need reentrancy.

	private static ElementUpdateAgent? _elementAgent;

	private static readonly Logger _log = typeof(ClientHotReloadProcessor).Log();

	private static ElementUpdateAgent ElementAgent
	{
		get
		{
			var log = _log.IsEnabled(LogLevel.Trace)
				? new Action<string>(_log.Trace)
				: static _ => { };

			_elementAgent ??= new ElementUpdateAgent(log, static (callback, error) => HotReloadClientOperation.GetForCurrentThread()?.ReportError(callback, error));

			return _elementAgent;
		}
	}

	internal static Window? CurrentWindow { get; private set; }

	private static (bool value, string reason) ShouldReload()
	{
		var isPaused = TypeMappings.IsPaused;
		return isPaused
			? (false, "type mapping prevent reload")
			: (true, string.Empty);
	}

	internal static void SetWindow(Window window, bool disableIndicator)
	{
#if HAS_UNO_WINUI
		if (CurrentWindow is not null)
		{
			CurrentWindow.Activated -= ShowDiagnosticsOnFirstActivation;
		}
#endif

		CurrentWindow = window;

#if HAS_UNO_WINUI
		if (CurrentWindow is not null && !disableIndicator)
		{
			CurrentWindow.Activated += ShowDiagnosticsOnFirstActivation;
		}
#endif
	}

#if HAS_UNO_WINUI // No diag to show currently on windows (so no WINUI)
	private static void ShowDiagnosticsOnFirstActivation(object snd, _WindowActivatedEventArgs windowActivatedEventArgs)
	{
		if (snd is Window { RootElement.XamlRoot: { } xamlRoot } window)
		{
			window.Activated -= ShowDiagnosticsOnFirstActivation;
			DiagnosticsOverlay.Get(xamlRoot).Show();
		}
	}
#endif

	/// <summary>
	/// Run on UI thread to reload the visual tree with updated types
	/// </summary>
	private static async Task ReloadWithUpdatedTypes(HotReloadClientOperation? hrOp, Window window, Type[] updatedTypes)
	{
		using var sequentialUiUpdateLock = await _uiUpdateGate.LockAsync(default);

		var handlerActions = ElementAgent?.ElementHandlerActions;
		var uiUpdating = true;
		try
		{
			hrOp?.SetCurrent();

			if (ShouldReload() is { value: false } prevent)
			{
				uiUpdating = false;
				hrOp?.ReportIgnored(prevent.reason);
				return;
			}

			UpdateGlobalResources(updatedTypes);

#if HAS_UNO_WINUI
			var rootElement = window.Content?.XamlRoot?.VisualTree.RootElement;
#else
			var rootElement = window.Content?.XamlRoot?.Content;
#endif
			if (rootElement is null)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error("Error doing UI Update - no visual root");
				}

				return;
			}

			// Action: BeforeVisualTreeUpdate
			// This is called before the visual tree is updated
			_ = handlerActions?.Do(h => h.Value.BeforeVisualTreeUpdate(updatedTypes)).ToArray();

			var capturedStates = new Dictionary<string, Dictionary<string, object>>();

			static int GetSubClassDepth(Type? type, Type baseType)
			{
				var count = 0;
				if (type == baseType)
				{
					return 0;
				}
				for (; type != null; type = type.BaseType)
				{
					count++;
					if (type == baseType)
					{
						return count;
					}
				}
				return -1;
			}

			var isCapturingState = true;
			var treeIterator = EnumerateHotReloadInstances(
				rootElement,
				async (fe, key) =>
				{
					// Get the original type of the element, in case it's been replaced
					var liveType = fe.GetType();
					var originalType = liveType.GetOriginalType() ?? fe.GetType();

					// Get the handler for the type specified
					// Since we're only interested in handlers for specific element types
					// we exclude those registered for "object". Handlers that want to run
					// for all element types should register for FrameworkElement instead
					ImmutableArray<ElementUpdateHandlerActions> handlers =
					[
						..from handler in handlerActions
						let depth = GetSubClassDepth(originalType, handler.Key)
						where depth is not -1 && handler.Key != typeof(object)
						orderby depth descending
						select handler.Value
					];

					// Get the replacement type, or null if not replaced
					var mappedType = originalType.GetMappedType();
					foreach (var handler in handlers)
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

						return (fe, [], liveType);
					}
					else
					{
						return (!handlers.IsDefaultOrEmpty || mappedType is not null)
							? (fe, handlers, mappedType)
							: (null, [], null);
					}
				},
				parentKey: default);

			// Forced iteration to capture all state before doing ui update
			var instancesToUpdate = await treeIterator.ToArrayAsync();

			// Iterate through the visual tree and either invoke ElementUpdate, 
			// or replace the element with a new one
			foreach (var (element, elementHandlers, elementMappedType) in instancesToUpdate)
			{
				if (element is null)
				{
					continue;
				}

				// Action: ElementUpdate
				// This is invoked for each existing element that is in the tree that needs to be replaced
				foreach (var elementHandler in elementHandlers)
				{
					elementHandler?.ElementUpdate(element, updatedTypes);
				}

				if (elementMappedType is not null)
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.Debug($"Updating element [{element}] to [{elementMappedType}]");
					}

					ReplaceViewInstance(element, elementMappedType, elementHandlers, updatedTypes);
				}
			}

			// Wait for the tree to be layouted before restoring state
			var tcs = new TaskCompletionSource();
#if HAS_UNO_WINUI || WINDOWS_WINUI
			window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => tcs.TrySetResult());
#else
			_ = window.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => tcs.TrySetResult());
#endif
			await tcs.Task;

			isCapturingState = false;
			// Forced iteration again to restore all state after doing ui update
			_ = await treeIterator.ToArrayAsync();

			// Action: AfterVisualTreeUpdate
			_ = handlerActions?.Do(h => h.Value.AfterVisualTreeUpdate(updatedTypes)).ToArray();
		}
		catch (Exception ex)
		{
			hrOp?.ReportError(ex);

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

			hrOp?.ResignCurrent();
			hrOp?.ReportCompleted();
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

	private static void ReplaceViewInstance(UIElement instance, Type replacementType, in ImmutableArray<ElementUpdateHandlerActions> handlers, Type[] updatedTypes)
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
#if HAS_UNO
				var oldStore = ((IDependencyObjectStoreProvider)instanceFE).Store;
				var newStore = ((IDependencyObjectStoreProvider)newInstanceFE).Store;
				oldStore.ClonePropertiesToAnotherStoreForHotReload(newStore);

				if (instanceFE.DebugParseContext is { } debugParseContext)
				{
					newInstanceFE.SetBaseUri(instanceFE.BaseUri.OriginalString, debugParseContext.LocalFileUri, debugParseContext.LineNumber, debugParseContext.LinePosition);
				}
#endif

				foreach (var handler in handlers)
				{
					handler.BeforeElementReplaced(instanceFE, newInstanceFE, updatedTypes);
				}

				SwapViews(instanceFE, newInstanceFE);

				foreach (var handler in handlers)
				{
					handler.AfterElementReplaced(instanceFE, newInstanceFE, updatedTypes);
				}
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

	/// <summary>
	/// Forces a hot reload update
	/// </summary>
	public static void ForceHotReloadUpdate()
	{
		try
		{
			Instance?._status.ConfigureSourceForNextOperation(HotReloadSource.Manual);
			UpdateApplicationCore([]);
		}
		finally
		{
			Instance?._status.ConfigureSourceForNextOperation(default);
		}
	}

	/// <summary>
	/// Entry point for .net MetadataUpdateHandler, do not use directly.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void UpdateApplication(Type[] types)
	{
		if (types is { Length: > 0 })
		{
			UpdateApplicationCore(types);
		}
		else
		{
			// https://github.com/dotnet/aspnetcore/issues/52937
			// Explicitly ignore to avoid flicker on WASM
			_log.Trace("Invalid metadata update, ignore it.");
		}
	}

	private static void UpdateApplicationCore(Type[] types)
	{
		var hr = Instance?._status.ReportLocalStarting(types);

		foreach (var type in types)
		{
			try
			{
				// Look up the attribute by name rather than by type.
				// This would allow netstandard targeting libraries to define their own copy without having to cross-compile.
				var attr = type.GetCustomAttributesData().FirstOrDefault(data => data is { AttributeType.FullName: "System.Runtime.CompilerServices.MetadataUpdateOriginalTypeAttribute" });
				if (attr is { ConstructorArguments: [{ Value: Type originalType }] })
				{
					TypeMappings.RegisterMapping(type, originalType);
				}
				else if (attr is not null && _log.IsEnabled(LogLevel.Warning))
				{
					_log.Warn($"Found invalid MetadataUpdateOriginalTypeAttribute for {type}");
				}
			}
			catch (TypeLoadException error)
			{
				if (_log.IsEnabled(LogLevel.Warning))
				{
					_log.Warn($"Type load error while processing MetadataUpdateOriginalTypeAttribute for {type}", error);
				}
				hr?.ReportWarning(error);
			}
			catch (Exception error)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error($"Error while processing MetadataUpdateOriginalTypeAttribute for {type}", error);
				}
				hr?.ReportError(error);
			}
		}

		if (_log.IsEnabled(LogLevel.Trace))
		{
			_log.Trace($"UpdateApplication (changed types: {string.Join(", ", types.Select(s => s.ToString()))})");
		}

#if WINUI
		if (CurrentWindow is { DispatcherQueue: { } dispatcherQueue } window)
		{
			dispatcherQueue.TryEnqueue(async () => await ReloadWithUpdatedTypes(hr, window, types));
		}
#else
		if (CurrentWindow is { Dispatcher: { } dispatcher } window)
		{
			_ = dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await ReloadWithUpdatedTypes(hr, window, types));
		}
#endif
		else
		{
			var errorMsg = $"Unable to access Dispatcher/DispatcherQueue in order to invoke {nameof(ReloadWithUpdatedTypes)}. Make sure you have enabled hot-reload (Window.UseStudio()) in app startup. See https://aka.platform.uno/hot-reload";
			hr?.ReportError(new InvalidOperationException(errorMsg));
			if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.Warn(errorMsg);
			}
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
