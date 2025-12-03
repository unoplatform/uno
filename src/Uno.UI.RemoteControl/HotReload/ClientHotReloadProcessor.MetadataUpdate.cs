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
using System.Runtime.CompilerServices;
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
using Microsoft.VisualBasic;
using static Microsoft.UI.Xaml.Controls.CollectionChangedOperation;
using System.Xml.Linq;
using Uno.UI.HotReload;

#if HAS_UNO_WINUI
using _WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
#else
using _WindowActivatedEventArgs = Windows.UI.Core.WindowActivatedEventArgs;
#endif

namespace Uno.UI.RemoteControl.HotReload;

partial class ClientHotReloadProcessor
{
	private static readonly Logger _log = typeof(ClientHotReloadProcessor).Log();
	private static readonly AsyncLock _uiUpdateGate = new(); // We can use the simple AsyncLock here as we don't need reentrancy.
	private static Window? _mainWindow;

	internal static void SetWindow(Window window, bool disableIndicator)
	{
#if HAS_UNO_WINUI
		if (_mainWindow is not null)
		{
			_mainWindow.Activated -= ShowDiagnosticsOnFirstActivation;
		}
#endif

		_mainWindow = window;

#if HAS_UNO_WINUI
		if (window is not null && !disableIndicator)
		{
			window.Activated += ShowDiagnosticsOnFirstActivation;
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

	private readonly ElementUpdateAgent? _elementAgent = new(
		_log.IsEnabled(LogLevel.Trace) ? new Action<string>(_log.Trace) : static _ => { },
		static (callback, error) => HotReloadClientOperation.GetForCurrentThread()?.ReportError(callback, error));

	/// <summary>
	/// Updates the UI of all windows
	/// </summary>
	internal async Task UpdateUI(HotReloadClientOperation hrOp, Type[] updatedTypes, CancellationToken ct)
	{
		using var sequentialUiUpdateLock = await _uiUpdateGate.LockAsync(ct);

		var updateHandlers = new ElementUpdateHandlerCollection(_elementAgent?.ElementHandlerActions ?? ImmutableDictionary<Type, ElementUpdateHandlerActions>.Empty);
		var uiUpdating = true;
#if HAS_UNO
		var windows = ApplicationHelper.Windows;
#else
		IReadOnlyList<Window> windows = CurrentWindow is { } cw ? [cw] : [];
#endif

		try
		{
			hrOp.SetCurrent();

			if (HotReloadService.Instance?.ShouldReloadUi() is { value: false } prevent)
			{
				uiUpdating = false;
				hrOp.ReportIgnored(prevent.reason);

				return;
			}

			if (windows is null or { Count: 0 })
			{
				var errorMsg = "Cannot update UI as no window has been found. Make sure you have enabled hot-reload (Window.UseStudio()) in app startup. See https://aka.platform.uno/hot-reloa";
				hrOp.ReportError(new InvalidOperationException(errorMsg));

				if (_log.IsEnabled(LogLevel.Warning))
				{
					_log.Warn(errorMsg);
				}

				return;
			}

			var isFirstWindow = true;
			foreach (var window in windows)
			{
				try
				{
					var tcs = new TaskCompletionSource();
					await using var _ = ct.Register(() => tcs.TrySetCanceled());
					if (window.DispatcherQueue.TryEnqueue(async () =>
						{
							try
							{
								if (isFirstWindow)
								{
									isFirstWindow = false;

									// Note : For backward compatibility we run this on the UI thread (on the first window), but this should be moved out of it.
									UpdateGlobalResources(updatedTypes);
								}

								await UpdateWindow(updatedTypes, window, updateHandlers, ct);
								tcs.TrySetResult();
							}
							catch (Exception ex)
							{
								tcs.TrySetException(ex);
							}
						}))
					{
						await tcs.Task;
					}
					else if (_log.IsEnabled(LogLevel.Warning))
					{
						_log.Warn($"Cannot update window '{window.Title}' as dispatcher queue has been aborted.");
					}
				}
				catch (Exception ex)
				{
					hrOp.ReportError(ex);
					if (_log.IsEnabled(LogLevel.Error))
					{
						_log.Error($"Failed to update window '{window.Title}'.", ex);
					}
				}
			}
		}
		catch (Exception ex)
		{
			hrOp.ReportError(ex);

			if (_log.IsEnabled(LogLevel.Error))
			{
				_log.Error($"Error doing UI Update - {ex.Message}", ex);
			}
			uiUpdating = false;
			throw;
		}
		finally
		{
			foreach (var handler in updateHandlers)
			{
				handler.ReloadCompleted(updatedTypes, uiUpdating);
			}

			hrOp.ResignCurrent();
			hrOp.ReportCompleted();
		}
	}

	private static async Task UpdateWindow(Type[] updatedTypes, Window window, ElementUpdateHandlerCollection updateHandlers, CancellationToken ct)
	{
		var rootElement = window.Content?.XamlRoot?.VisualTree.RootElement
			?? throw new ArgumentException("Cannot update window, no visual root to update", nameof(window));

		// This is called before the visual tree is updated
		foreach (var handler in updateHandlers)
		{
			handler.BeforeVisualTreeUpdate(updatedTypes);
		}

		// Walk the whole visual tree to capture state.
		var capturedStates = new Dictionary<string, Dictionary<string, object>>();
		var elementState = new Dictionary<string, object>();
		foreach (var (element, key) in EnumerateVisualTree(rootElement, "root"))
		{
			var liveType = element.GetType();
			var originalType = liveType.GetOriginalType();
			var handlers = updateHandlers.Get(originalType ?? liveType);

			foreach (var handler in handlers)
			{
				handler.CaptureState(element, elementState, updatedTypes);
				if (elementState.Any())
				{
					capturedStates[key] = elementState;
					elementState = new(); // Prepare a new dic for next element.
				}
			}
		}

		// Iterate again the visual tree to update elements (either handler.ElementUpdate, either by replacing it).
		IterateVisualTree(rootElement, element =>
		{
			var liveType = element.GetType();
			var originalType = liveType.GetOriginalType();
			var updatedType = originalType?.GetMappedType();
			var handlers = updateHandlers.Get(originalType ?? liveType);

			if (updatedTypes.Contains(liveType))
			{
				// The type has been updated, but not "replaced" (i.e. not flagged with CreateNewOnMetadataUpdate)
				// This is the standard case since we no longer flag UIElement with the CreateNewOnMetadataUpdate.
				// But this could also occur with a type flagged with the CreateNewOnMetadataUpdate if one of its nested
				// types has been updated, but not the type itself.
				// For instance, a DataTemplate in a resource dictionary may mark the type as updated in `updatedTypes`
				// but it will not be considered as a new type even if "CreateNewOnMetadataUpdate" was set.

				return ReplaceViewInstance(element, liveType, handlers, updatedTypes); // Stop drill down when we successfully replaced the view
			}
			else if (updatedType is not null && liveType != updatedType /* Correct check should `updatedTypes.Contains(updatedType)` but this allows to catch any non-updated type (like when pausing UI updates)*/)
			{
				return ReplaceViewInstance(element, updatedType, handlers, updatedTypes); // Stop drill down when we successfully replaced the view
			}
			else if (handlers is { Length: > 0 })
			{
				// Instance will be kept in the visual tree (unless a parent removes it, but we ignore that case and process anyway).
				// We inform it that an update has occurred for the given `updatedTypes` so it can refresh it's internal state.

				foreach (var handler in handlers)
				{
					handler.ElementUpdate(element, updatedTypes);
				}

				return false;
			}
			else
			{
				return false;
			}
		});

		// Wait for the tree to be layouted before restoring state
		var tcs = new TaskCompletionSource();
		if (window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => tcs.TrySetResult()))
		{
			await tcs.Task;
		}

		// Then attempt to restore the state
		foreach (var (fe, key) in EnumerateVisualTree(rootElement, "root"))
		{
			var liveType = fe.GetType();
			var originalType = liveType.GetOriginalType();
			var handlers = updateHandlers.Get(originalType ?? liveType);

			foreach (var handler in handlers)
			{
				if (!capturedStates.TryGetValue(key, out var dict))
				{
					dict = new();
				}

				await handler.RestoreState(fe, dict, updatedTypes);
			}
		}

		// Finally notify handlers that the visual tree update is completed
		foreach (var handler in updateHandlers)
		{
			handler.AfterVisualTreeUpdate(updatedTypes);
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

		if (globalResourceTypes.Length == 0)
		{
			return;
		}

		if (_log.IsEnabled(LogLevel.Debug))
		{
			_log.Debug($"Updating app resources");
		}

		MethodInfo? GetInitMethod(Type type, string name)
		{
			if (type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, []) is { } initializeMethod)
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

	private static bool ReplaceViewInstance(UIElement instance, Type replacementType, in ImmutableArray<ElementUpdateHandlerActions> handlers, Type[] updatedTypes)
	{
		if (_log.IsEnabled(LogLevel.Debug))
		{
			_log.Debug($"Updating element [{instance}] to [{replacementType}]");
		}

		if (replacementType.GetConstructor([]) is { } creator)
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"Creating instance of type {replacementType}");
			}

			var newInstance = Activator.CreateInstance(replacementType);
			var instanceFE = instance as FrameworkElement;
			var newInstanceFE = newInstance as FrameworkElement;
			if (instanceFE is not null && newInstanceFE is not null)
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

				return true;
			}
		}
		else
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.LogDebug($"Type [{instance.GetType()}] has no parameterless constructor, skipping reload");
			}
		}

		return false;
	}

	/// <summary>
	/// Forces a hot reload update
	/// </summary>
	public static void ForceHotReloadUpdate()
		=> UpdateApplicationCore([], HotReloadSource.Manual);

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

	private static void UpdateApplicationCore(Type[] types, HotReloadSource? explicitSource = null)
	{
		var instance = _instance;
		var hr = instance?._status.ReportLocalStarting(types, explicitSource);

		UpdateTypeMappingFromMetadataUpdate(types, hr);

		if (instance is null)
		{
			if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.Warn("Cannot process updated types, hot reload processor is not yet initialized.");
			}
			return;
		}

		if (_log.IsEnabled(LogLevel.Trace))
		{
			_log.Trace($"UpdateApplication (changed types: {string.Join(", ", types.Select(s => s.ToString()))})");
		}

		_ = instance.UpdateUI(hr!, types, CancellationToken.None);
	}

	private static void UpdateTypeMappingFromMetadataUpdate(Type[] types, HotReloadClientOperation? hr)
	{
		var typesByOriginal = types
			.Select(type => (original: GetOriginalFromMetadataAttribute(type), updated: type))
			.Where(pair => pair.original is not null)
			.ToImmutableDictionary(pair => pair.original!, pair => pair.updated);

		TypeMappings.RegisterMappings(typesByOriginal);

		Type? GetOriginalFromMetadataAttribute(Type type)
		{
			try
			{
				// Look up the attribute by name rather than by type.
				// This would allow netstandard targeting libraries to define their own copy without having to cross-compile.
				var attr = type.GetCustomAttributesData().FirstOrDefault(data => data is { AttributeType.FullName: "System.Runtime.CompilerServices.MetadataUpdateOriginalTypeAttribute" });
				if (attr is { ConstructorArguments: [{ Value: Type originalType }] })
				{
					return originalType;
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

			return null;
		}
	}
}
