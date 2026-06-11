#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml;

partial class Window
{
	private static readonly ConditionalWeakTable<object, SecondaryAlcMarker> _secondaryAlcMarkers = new();

	private bool _isWindowFromSecondaryAlc;

	// Window-local storage to detect secondary ALC content
	private object? _secondaryAlcContent;

	// Lazily allocated ALC window state - null when not in ALC mode
	private AlcWindowState? _alcState;

	partial void InitializeAlcState(Assembly callingAssembly)
	{
		_isWindowFromSecondaryAlc = IsAssemblyFromSecondaryAlc(callingAssembly);
	}

	/// <summary>
	/// Encapsulates all ALC window lifecycle state to avoid memory overhead when ALC is not used.
	/// </summary>
	private sealed class AlcWindowState
	{
		public bool IsClosed;
		public bool IsVisible;

		// Subscription handlers for cleanup
		public SizeChangedEventHandler? HostSizeChangedHandler;
		public RoutedEventHandler? HostLoadedHandler;
		public RoutedEventHandler? HostUnloadedHandler;

	}

	internal partial bool TryGetContentFromSecondaryAlc(out UIElement? content)
	{
		if (IsSecondaryAlcContent(_secondaryAlcContent))
		{
			content = ContentHostOverride?.Content as UIElement;
			return true;
		}

		content = default;
		return false;
	}

	private partial bool TrySetContentFromSecondaryAlc(UIElement? value, ContentControl host, Assembly callingAssembly)
	{
		if (!ShouldRedirectToContentHost(value, callingAssembly))
		{
			return false;
		}

#if DEBUG
		if (callingAssembly.FullName?.StartsWith("System.", StringComparison.Ordinal) == true)
		{
			global::System.Diagnostics.Debug.WriteLine("Window.Content was set via reflection or framework code; secondary ALC detection may be inaccurate.");
		}
#endif

		InitializeAlcWindowMode();

		UnmarkSecondaryAlcContent(_secondaryAlcContent);
		host.Content = value;
		_secondaryAlcContent = value;
		MarkContentAsSecondaryAlc(value);
		return true;
	}

	/// <summary>
	/// Initialize ALC mode when content is first set from a secondary ALC.
	/// </summary>
	private void InitializeAlcWindowMode()
	{
		if (_alcState is not null)
		{
			return;
		}

		_alcState = new AlcWindowState();
		ApplicationHelper.RemoveWindow(this); // Don't block app closure

		// Close the native window if it was created - ALC windows don't need a native window
		// since they render into ContentHostOverride. This also unregisters from platform
		// window tracking (e.g., X11XamlRootHost) so the app can exit when the main window closes.
		_windowImplementation.NativeWindowWrapper?.Close();

		SubscribeToContentHostEvents();
	}

	/// <summary>
	/// Subscribe to ContentHostOverride for event forwarding.
	/// </summary>
	private void SubscribeToContentHostEvents()
	{
		var host = ContentHostOverride;
		var state = _alcState;
		if (host is null || state is null)
		{
			return;
		}

		state.HostSizeChangedHandler = OnContentHostSizeChanged;
		host.SizeChanged += state.HostSizeChangedHandler;

		state.HostLoadedHandler = OnContentHostLoaded;
		state.HostUnloadedHandler = OnContentHostUnloaded;
		host.Loaded += state.HostLoadedHandler;
		host.Unloaded += state.HostUnloadedHandler;

		state.IsVisible = host.IsLoaded;
	}

	/// <summary>
	/// Forward SizeChanged from host.
	/// </summary>
	private void OnContentHostSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return;
		}

		_windowImplementation.RaiseSizeChanged(new WindowSizeChangedEventArgs(e.NewSize));
	}

	/// <summary>
	/// Forward visibility from host Loaded event.
	/// </summary>
	private void OnContentHostLoaded(object sender, RoutedEventArgs e)
	{
		var state = _alcState;
		if (state is null || state.IsClosed || state.IsVisible)
		{
			return;
		}

		state.IsVisible = true;
		_windowImplementation.RaiseVisibilityChanged(new VisibilityChangedEventArgs { Visible = true });
	}

	/// <summary>
	/// Forward visibility from host Unloaded event.
	/// </summary>
	private void OnContentHostUnloaded(object sender, RoutedEventArgs e)
	{
		var state = _alcState;
		if (state is null || state.IsClosed || !state.IsVisible)
		{
			return;
		}

		state.IsVisible = false;
		_windowImplementation.RaiseVisibilityChanged(new VisibilityChangedEventArgs { Visible = false });
	}

	/// <summary>
	/// Get visibility for ALC window based on ContentHostOverride state.
	/// </summary>
	private bool GetAlcWindowVisible()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return false;
		}

		var host = ContentHostOverride;
		return host is not null && host.IsLoaded && host.Visibility == Visibility.Visible;
	}

	/// <summary>
	/// Get bounds for ALC window based on ContentHostOverride dimensions.
	/// </summary>
	private Rect GetAlcWindowBounds()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return default;
		}

		var host = ContentHostOverride;
		return host is null ? default : new Rect(0, 0, host.ActualWidth, host.ActualHeight);
	}

	/// <summary>
	/// Activate ALC window - raises Activated event.
	/// </summary>
	private void ActivateAlcWindow()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			throw new InvalidOperationException("Cannot activate a closed window.");
		}

		_windowImplementation.RaiseActivated(new WindowActivatedEventArgs(CoreWindowActivationState.CodeActivated));
	}

	/// <summary>
	/// Close ALC window - cleanup and raise event.
	/// </summary>
	private bool CloseAlcWindow()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return false;
		}

		var closedArgs = new WindowEventArgs();
		_windowImplementation.RaiseClosed(closedArgs);
		if (closedArgs.Handled)
		{
			return false; // Handled, cancel close
		}

		state.IsClosed = true;

		// Clear content from host
		var host = ContentHostOverride;
		if (host is not null && ReferenceEquals(host.Content, _secondaryAlcContent))
		{
			host.Content = null;
		}

		// Raise visibility changed if was visible
		if (state.IsVisible)
		{
			state.IsVisible = false;
			_windowImplementation.RaiseVisibilityChanged(new VisibilityChangedEventArgs { Visible = false });
		}

		// Cleanup subscriptions
		UnsubscribeFromContentHostEvents();
		UnmarkSecondaryAlcContent(_secondaryAlcContent);
		_secondaryAlcContent = null;

		// Remove from the static window map so the Window object can be collected.
		// The native window was already closed during InitializeAlcWindowMode().
		_appWindowMap.TryRemove(AppWindow, out _);

		// The Window registered a DisplayInformation for its WindowId at construction; that
		// static map has no other removal path, so it retains the closed window's
		// implementation graph — including window-event subscribers from the secondary ALC
		// (e.g. the Hot Design client's Closed handler) — for the process lifetime, pinning
		// the ALC: DisplayInformation → native wrapper → DesktopWindow → Closed → client.
		try
		{
			global::Windows.Graphics.Display.DisplayInformation.DestroyForWindowId(AppWindow.Id);
		}
		catch (Exception ex)
		{
			if (typeof(Window).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(Window).Log().Debug($"[ALC-CLEANUP] DisplayInformation.DestroyForWindowId error: {ex.GetType().Name}: {ex.Message}");
			}
		}

		// Other WindowId-keyed statics (CoreDragDropManager's map, input managers, …) retain the
		// closed window's implementation graph too. Rather than chasing every registry, strip the
		// window-event subscriptions whose targets live in the collectible ALC (Closed/Activated/
		// SizeChanged/VisibilityChanged on the implementation object). This window was removed
		// from ApplicationHelper at InitializeAlcWindowMode, so the ALC-teardown window sweep in
		// Application.CleanupNonDefaultAlcCaches never visits it — prune here, after RaiseClosed.
		try
		{
			Application.PruneCollectibleDelegateFields(this);
			Application.PruneCollectibleDelegateFields(_windowImplementation);
		}
		catch (Exception ex)
		{
			if (typeof(Window).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(Window).Log().Debug($"[ALC-CLEANUP] window delegate prune error: {ex.GetType().Name}: {ex.Message}");
			}
		}

		// Remove this window's ContentRoot from the process-wide ContentRootCoordinator.
		// RemoveContentRoot has no other caller, so an ALC window's content root otherwise
		// stays registered forever — and its Window.Closed subscribers (e.g. Hot Design's
		// client host) keep the collectible ALC pinned via the coordinator's list:
		// CoreServices → ContentRootCoordinator → ContentRoot → XamlIslandRoot → Window →
		// Closed handlers → per-ALC subscriber → LoaderAllocator.
		var alcContentRoot = _windowImplementation.XamlRoot?.VisualTree?.ContentRoot;
		if (alcContentRoot is not null)
		{
			Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator.RemoveContentRoot(alcContentRoot);
		}

		// Purge Type-keyed caches (DependencyProperty registry, Style caches, etc.)
		// that hold references to types from the ALC being torn down. Without this,
		// these statics prevent the GC from collecting the ALC after Unload().
		try
		{
			Application.CleanupNonDefaultAlcCaches();
		}
		catch (Exception ex)
		{
			if (typeof(Window).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(Window).Log().Debug($"[ALC-CLEANUP] CleanupNonDefaultAlcCaches error in CloseAlcWindow: {ex.GetType().Name}: {ex.Message}");
			}
		}

		return true;
	}

	/// <summary>
	/// Closes all <see cref="Window"/> instances that belong to a non-default (secondary) ALC.
	/// This replaces reflection-based window cleanup with a proper internal API.
	/// </summary>
	/// <summary>
	/// Optional callback invoked during <see cref="CloseAlcWindows"/> so that platform hosts
	/// (e.g. X11XamlRootHost) can remove their own static entries for ALC windows.
	/// Set by the platform host at startup.
	/// </summary>
	internal static Action? AlcWindowCleanupCallback { get; set; }

	internal static void CloseAlcWindows()
	{
		foreach (var kvp in _appWindowMap)
		{
			var window = kvp.Value;
			if (window._alcState is not null)
			{
				window.CloseAlcWindow();
			}
		}

		// Allow platform hosts to clean their own static window maps.
		AlcWindowCleanupCallback?.Invoke();
	}

	/// <summary>
	/// Unsubscribe from ContentHostOverride events.
	/// </summary>
	private void UnsubscribeFromContentHostEvents()
	{
		var host = ContentHostOverride;
		var state = _alcState;
		if (host is null || state is null)
		{
			return;
		}

		if (state.HostSizeChangedHandler is not null)
		{
			host.SizeChanged -= state.HostSizeChangedHandler;
		}

		if (state.HostLoadedHandler is not null)
		{
			host.Loaded -= state.HostLoadedHandler;
		}

		if (state.HostUnloadedHandler is not null)
		{
			host.Unloaded -= state.HostUnloadedHandler;
		}
	}

	/// <summary>
	/// Gets whether this window is operating in ALC mode.
	/// </summary>
	internal bool IsAlcWindow => _isWindowFromSecondaryAlc || _alcState is not null;

	/// <summary>
	/// Checks if the given content element is from a secondary AssemblyLoadContext.
	/// When value is null, returns false to allow clearing content.
	/// </summary>
	private bool IsContentFromSecondaryAlc(object? value)
	{
		// Explicitly handle null: a null assignment should clear content, not be detected as secondary ALC
		if (value == null)
		{
			return false;
		}

		return !ReferenceEquals(
			AssemblyLoadContext.Default,
			AssemblyLoadContext.GetLoadContext(value.GetType().Assembly));
	}

	private bool IsSecondaryAlcContent(object? value)
		=> IsContentFromSecondaryAlc(value) || IsContentMarkedAsSecondaryAlc(value);

	private static bool IsContentMarkedAsSecondaryAlc(object? value)
		=> value is not null && _secondaryAlcMarkers.TryGetValue(value, out _);

	private static void MarkContentAsSecondaryAlc(object? value)
	{
		if (value is null)
		{
			return;
		}

		_secondaryAlcMarkers.GetValue(value, static _ => new SecondaryAlcMarker());
	}

	private static void UnmarkSecondaryAlcContent(object? value)
	{
		if (value is null)
		{
			return;
		}

		_secondaryAlcMarkers.Remove(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool ShouldRedirectToContentHost(object? value, Assembly callingAssembly)
	{
		if (IsContentFromSecondaryAlc(value))
		{
			return true;
		}

		if (IsAssemblyFromSecondaryAlc(callingAssembly))
		{
			return true;
		}

		return _isWindowFromSecondaryAlc;
	}

	private static bool IsAssemblyFromSecondaryAlc(Assembly assembly)
	{
		var loadContext = AssemblyLoadContext.GetLoadContext(assembly);
		return loadContext is not null && !ReferenceEquals(loadContext, AssemblyLoadContext.Default);
	}

	private sealed class SecondaryAlcMarker
	{
	}
}
