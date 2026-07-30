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

	// Weak so a tagged window never keeps a collectible (secondary) ALC alive after its app unloads.
	private WeakReference<AssemblyLoadContext>? _ownerAlc;

	// Window-local storage to detect secondary ALC content
	private object? _secondaryAlcContent;

	// Lazily allocated ALC window state - null when not in ALC mode
	private AlcWindowState? _alcState;

	partial void InitializeAlcState(Assembly? callingAssembly)
	{
		_isWindowFromSecondaryAlc = callingAssembly is not null && IsAssemblyFromSecondaryAlc(callingAssembly);
	}

	partial void CaptureOwnerAssemblyLoadContext(Assembly? callingAssembly)
	{
		if (callingAssembly is not null
			&& AssemblyLoadContext.GetLoadContext(callingAssembly) is { } alc
			&& !ReferenceEquals(alc, AssemblyLoadContext.Default))
		{
			_ownerAlc = new WeakReference<AssemblyLoadContext>(alc);
		}
	}

	/// <summary>
	/// The non-default <see cref="AssemblyLoadContext"/> of the code that constructed this window,
	/// or null for windows created by default-ALC (host) code — or whose ALC has been collected.
	/// Unlike inferring ownership from the window content's concrete type, this stays correct when
	/// a secondary-ALC app roots a shared framework type (e.g. a plain <c>Frame</c>) or when its
	/// content was redirected to an <see cref="Uno.UI.Xaml.Controls.AlcContentHost"/> (leaving the
	/// window's own root content null). Used by <c>Application.GetOwningApplication</c> to map a
	/// content root back to the application that owns it.
	/// </summary>
	internal AssemblyLoadContext? OwnerAssemblyLoadContext
		=> _ownerAlc is { } weak && weak.TryGetTarget(out var alc) ? alc : null;

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

		// Pin the owning app's explicit ApplicationTheme (if any) at the host boundary so the
		// secondary app's theme governs its subtree without touching the shared FrameworkTheming —
		// the element-level RequestedTheme mechanism WinUI uses for per-island theming
		// (CFrameworkElement::GetRequestedThemeOverride, framework.cpp:3399-3418).
		if (ResolveOwningAlcApplication(value) is { } owningApp)
		{
			host.RequestedTheme = owningApp.AlcElementTheme;
		}

		return true;
	}

	/// <summary>
	/// Resolves the application owning this ALC window's content: the window's
	/// <see cref="OwnerAssemblyLoadContext"/> is authoritative (correct even when the content is a
	/// shared default-ALC type such as a plain <c>Frame</c>); the content's own ALC is the fallback
	/// for windows created by host code.
	/// </summary>
	private Application? ResolveOwningAlcApplication(object? content)
		=> (OwnerAssemblyLoadContext is { } ownerAlc ? Application.GetForAssemblyLoadContext(ownerAlc) : null)
			?? Application.GetForInstance(content);

	/// <summary>
	/// Re-applies <paramref name="app"/>'s explicit-theme pin to the content-host boundary of each
	/// window owned by that secondary-ALC application. Invoked when the app's explicit theme changes
	/// (<c>Application.SetAlcRequestedTheme</c>); the matching pull happens when content attaches in
	/// <see cref="TrySetContentFromSecondaryAlc"/>.
	/// </summary>
	internal static void ApplyAlcRequestedTheme(Application app, ElementTheme theme)
	{
		foreach (var kvp in _appWindowMap)
		{
			var window = kvp.Value;
			if (window._alcState is { IsClosed: false }
				&& window._secondaryAlcContent is { } content
				&& ReferenceEquals(window.ResolveOwningAlcApplication(content), app)
				&& ContentHostOverride is { } host
				&& ReferenceEquals(host.Content, content))
			{
				host.RequestedTheme = theme;
			}
		}
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

		// Capture the dying ALC BEFORE the content reference is cleared: at window-close time the
		// secondary app's Unload() has not been initiated yet, so the delegate prune below cannot
		// discriminate by unload state and needs the ALC identity explicitly. OwnerAssemblyLoadContext
		// is authoritative — it stays correct even when the secondary app rooted a shared framework
		// type (Frame, Grid, …) whose own type resolves to the default ALC. Fall back to the content
		// type's ALC only when the owner was never captured.
		var dyingAlc = OwnerAssemblyLoadContext;
		if (dyingAlc is null
			&& _secondaryAlcContent is { } secondaryContent
			&& AssemblyLoadContext.GetLoadContext(secondaryContent.GetType().Assembly) is { IsCollectible: true } contentAlc
			&& contentAlc != AssemblyLoadContext.Default)
		{
			dyingAlc = contentAlc;
		}

		// Clear content from host
		var host = ContentHostOverride;
		if (host is not null && ReferenceEquals(host.Content, _secondaryAlcContent))
		{
			host.Content = null;

			// Clear the secondary app's theme pin so the next hosted app starts from the host theme.
			host.RequestedTheme = ElementTheme.Default;
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
		// (e.g. a designer client's Closed handler) — for the process lifetime, pinning
		// the ALC: DisplayInformation → native wrapper → window implementation → Closed → client.
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

		// Remove this window's ContentRoot from the process-wide ContentRootCoordinator.
		// This is the only path that removes a window-owned content root — the teardown sweep
		// in Application.PruneCollectibleAlcEventSubscriptions deliberately skips window roots —
		// so an ALC window's content root otherwise stays registered forever, and its
		// Window.Closed subscribers (e.g. Hot Design's client host) keep the collectible ALC
		// pinned via the coordinator's list:
		// CoreServices → ContentRootCoordinator → ContentRoot → XamlIslandRoot → Window →
		// Closed handlers → per-ALC subscriber → LoaderAllocator.
		var alcContentRoot = _windowImplementation.XamlRoot?.VisualTree?.ContentRoot;
		if (alcContentRoot is not null)
		{
			Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator.RemoveContentRoot(alcContentRoot);
		}

		// WindowId-keyed statics (DisplayInformation, CoreDragDropManager's map, input managers,
		// …) can retain the closed window's implementation graph. Rather than chasing every
		// registry, strip the window-event subscriptions whose targets live in the collectible
		// ALC (Closed/Activated/SizeChanged/VisibilityChanged on the implementation object).
		// This window was removed from ApplicationHelper at InitializeAlcWindowMode, so the
		// ALC-teardown window sweep in Application.CleanupNonDefaultAlcCaches never visits it —
		// prune here, after RaiseClosed.
		try
		{
			Application.PruneCollectibleDelegateFields(this, dyingAlc);
			Application.PruneCollectibleDelegateFields(_windowImplementation, dyingAlc);
		}
		catch (Exception ex)
		{
			if (typeof(Window).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(Window).Log().Debug($"[ALC-CLEANUP] window delegate prune error: {ex.GetType().Name}: {ex.Message}");
			}
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
	/// True for a hosted secondary-ALC window on macOS. <see cref="Uno.UI.Xaml.Controls.DesktopWindow.Initialize"/>
	/// skips native window creation in this case (it would collide with MacOSWindowHost's native-window
	/// registration), so the window has no native backing — callers that depend on one must skip their work
	/// too (the native window itself, and the DisplayInformation registration whose macOS extension subscribes
	/// to <c>MacOSWindowNative.NativeWindowReady</c> and would never unsubscribe).
	/// </summary>
	internal bool IsMacOSHostedAlcWindow
		=> OperatingSystem.IsMacOS() && ContentHostOverride is not null && IsAlcWindow;

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
