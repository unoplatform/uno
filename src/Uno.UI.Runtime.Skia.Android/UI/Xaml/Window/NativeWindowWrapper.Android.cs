#nullable disable

using System;
using Android.App;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.View;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Size = Windows.Foundation.Size;
using MUX = Microsoft.UI.Xaml;
using Microsoft.UI.Xaml;
using Uno.UI.Runtime.Skia.Android;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase, INativeWindowWrapper
{
	private ApplicationActivity _activity;
	private bool _showPending;
	private readonly ActivationPreDrawListener _preDrawListener;
	private readonly DisplayInformation _displayInformation;
	private bool _contentViewAttachedToWindow;

	// Armed by the Skia render path so splash dismissal waits for the first Skia frame; cleared once that frame is
	// on screen. Native Android never arms it, so its splash keeps dismissing as soon as content is attached.
	private volatile bool _awaitingFirstFrame;

	private Rect _previousTrueVisibleBounds;

	/// <summary>
	/// Creates a wrapper for the window an existing activity already drives.
	/// </summary>
	public NativeWindowWrapper(ApplicationActivity activity)
		: this()
		=> _activity = activity;

	/// <summary>
	/// Creates a wrapper for a window whose hosting activity does not exist yet. <see cref="ShowCore"/>
	/// launches one, and <see cref="CurrentActivity"/> is set once it adopts this window.
	/// </summary>
	public NativeWindowWrapper()
	{
		_preDrawListener = new ActivationPreDrawListener(this);
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += RaiseNativeSizeChanged;

		_displayInformation = DisplayInformation.GetForCurrentViewSafe() ?? throw new InvalidOperationException("DisplayInformation must be available when the window is initialized");
		_displayInformation.DpiChanged += (s, e) => DispatchDpiChanged();
		DispatchDpiChanged();
	}

	public override object NativeWindow => _activity?.Window;

	/// <summary>
	/// The activity currently driving this window. Updated on activity re-creation, since the
	/// managed Window (and this wrapper) outlive individual activities on Android.
	/// </summary>
	internal ApplicationActivity CurrentActivity
	{
		get => _activity;
		set => _activity = value;
	}

	// Per-window input sources, resolved by each window's InputManager via its IXamlRootHost
	// and fed by the driving activity's native event dispatch.
	internal AndroidCorePointerInputSource PointerSource { get; } = new();

	internal AndroidKeyboardInputSource KeyboardSource { get; } = new();

	private void DispatchDpiChanged() =>
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;

	public override string Title
	{
		get => _activity?.Title ?? string.Empty;
		set
		{
			if (_activity is { } activity)
			{
				activity.Title = value;
			}
		}
	}

	internal int SystemUiVisibility { get; set; }

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnActivityCreated() => AddPreDrawListener();

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();

	/// <summary>
	/// Closing a window means finishing the task hosting it. The main window's activity is left
	/// alone: finishing it would close the whole app rather than a window.
	/// </summary>
	protected override void CloseCore()
	{
		if (_activity is { } activity && !ReferenceEquals(Window, MUX.Window.CurrentSafe))
		{
			activity.FinishAndRemoveTask();
		}
	}

	internal void RaiseNativeSizeChanged()
	{
		var (windowSize, visibleBounds) = GetVisualBounds();

		SetBoundsAndVisibleBounds(new Rect(default, windowSize), visibleBounds);
		var size = new Windows.Graphics.SizeInt32((int)(windowSize.Width * RasterizationScale), (int)(windowSize.Height * RasterizationScale));
		SetSizes(size, size);
		ApplySystemOverlaysTheming();

		if (_previousTrueVisibleBounds != visibleBounds)
		{
			_previousTrueVisibleBounds = visibleBounds;

			// Per window: GetForCurrentView() resolves the main window, which would let a
			// secondary window overwrite the main window's visible bounds.
			if (Window?.AppWindow is { } appWindow)
			{
				ApplicationView.GetOrCreateForWindowId(appWindow.Id).SetTrueVisibleBounds(visibleBounds);
			}
		}
	}

	protected override void ShowCore()
	{
		// Skip attaching content to the shared Activity window when a secondary ALC
		// is being hosted (Window.ContentHostOverride != null). EnsureContentView()
		// swaps the Activity's content view, so an ALC window driving ShowCore would
		// take over the host's content. ALC-mode windows normally never reach ShowCore
		// (Window.Activate routes to ActivateAlcWindow once _alcState is set), so this
		// is defensive hardening that mirrors the gating on the iOS window wrappers.
		if (MUX.Window.ContentHostOverride is not null)
		{
			return;
		}

		if (_activity is not { } activity)
		{
			// A secondary window has no activity until Android hands us one. Ask for a task to host
			// it and stop here: the activity that adopts this window calls CompleteDeferredShow from
			// its OnStart, once it has built the render stack there is nothing to attach to before.
			_showPending = true;
			ApplicationActivity.LaunchForWindow(this);
			return;
		}

		ShowForActivity(activity);
	}

	/// <summary>
	/// Runs a show that <see cref="ShowCore"/> deferred because the window had no activity yet.
	/// Called by the adopting activity once its render stack exists; a no-op otherwise.
	/// </summary>
	internal void CompleteDeferredShow()
	{
		if (_showPending && _activity is { } activity)
		{
			_showPending = false;
			ShowForActivity(activity);
		}
	}

	private void ShowForActivity(ApplicationActivity activity)
	{
		MUX.Application.Current.RequestedThemeChanged += (_, _) =>
		{
			if (MUX.Application.Current.InitializationComplete)
			{
				ApplySystemOverlaysTheming();
			}
		};

		AttachContentView(activity);
	}

	private void AttachContentView(ApplicationActivity activity)
	{
		activity.ContentViewAttachedToWindow += Instance_ContentViewAttachedToWindow;
		activity.EnsureContentView();

		// The activity attaches its own surface in OnStart when it adopts an existing window, so the
		// attach can already have happened before this subscription and the event fired with nobody
		// listening. The pre-draw listener holds back every draw pass until this flag is set -- and a
		// window that never draws also never gets its SurfaceView z-ordered behind it -- so seed the
		// flag from the activity's current state rather than relying on the event alone.
		_contentViewAttachedToWindow |= activity.IsContentViewAttachedToWindow;

		ApplySystemOverlaysTheming();
	}

	private void Instance_ContentViewAttachedToWindow(object sender, EventArgs e) =>
		_contentViewAttachedToWindow = true;

	private (Size windowSize, Rect visibleBounds) GetVisualBounds()
	{
		if (_activity is not { Window: not null } activity)
		{
			return default;
		}

		var windowInsets = GetWindowInsets(activity);

		var insetsTypes = WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout(); // == WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars() | WindowInsets.Type.CaptionBar();
		Rect windowBounds;
		Rect visibleBounds;

		var decorView = activity.Window.DecorView;
		var fitsSystemWindows = decorView.FitsSystemWindows;

		var insets = windowInsets?.GetInsets(insetsTypes).ToThickness() ?? default;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Insets: {insets}");
		}

		if (StatusBar.GetForCurrentView().BackgroundColor is { } && (int)Android.OS.Build.VERSION.SdkInt >= 35)
		{
			// quick refresher:
			// - windowBounds: size of the rendered area, its location is ignored/unused
			// - visibleBounds: area WITHIN windowBounds that isn't occluded/blocked by system overlays (status-bar, navigation-bar, etc.)
			//		^ since VB calculated from WB, it is important that WB doesn't have an location/offset to be inherited by VB.

			// see: StatusBar.SetStatusBarBackgroundColor (StatusBar.Android.cs)
			// Setting a non-null StatusBar.Background in v35, will add a padding to the decor-view.
			// This will move down the coordinates system for both windowBounds and visibleBounds,
			// their zero(0,0) will be (0, inset.top) on the physical display.

			var size = GetWindowSize();
			windowBounds = new Rect(0, 0, size.Width, size.Height - insets.Top); // exclude top inset from rendering area
			visibleBounds = windowBounds.DeflateBy(insets with { Top = 0 }); // apply the rest of the insets, skipping Top that is already excluded
		}
		else
		{
			windowBounds = new Rect(default, GetWindowSize());
			visibleBounds = windowBounds.DeflateBy(insets);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"WindowBounds: {windowBounds}, VisibleBounds {visibleBounds}");
		}

		var windowBoundsLogical = windowBounds.PhysicalToLogicalPixels();
		var visibleBoundsLogical = visibleBounds.PhysicalToLogicalPixels();

		return (windowBoundsLogical.Size, visibleBoundsLogical);
	}

	private WindowInsetsCompat GetWindowInsets(Activity activity)
	{
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
		{
			return WindowInsetsCompat.ToWindowInsetsCompat(activity.WindowManager?.CurrentWindowMetrics.WindowInsets);
		}

		var decorView = activity.Window.DecorView;
		if (decorView.IsAttachedToWindow)
		{
			return ViewCompat.GetRootWindowInsets(decorView);
		}

		return null;
	}

	internal void ApplySystemOverlaysTheming()
	{
		// Only apply theming if the app hasn't explicitly set a foreground
		if (StatusBar.GetForCurrentView().ForegroundColor is null)
		{
			// In edge-to-edge experience we want to adjust the theming of status bar to match the app theme.
			if (Microsoft.UI.Xaml.Application.Current is { } application &&
				_activity?.Window is { DecorView: { FitsSystemWindows: false } decorView } nativeWindow)
			{
				var requestedTheme = application.RequestedTheme;

				var insetsController = WindowCompat.GetInsetsController(nativeWindow, decorView);

				// "appearance light" refers to status bar set to light theme == dark foreground
				insetsController.AppearanceLightStatusBars = requestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light;
			}
		}
	}

	private Size GetWindowSize()
	{
		if (_activity is not { Window: not null } activity)
		{
			return default;
		}

		Size displaySize = default;

		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
		{
			var windowMetrics = activity.WindowManager?.CurrentWindowMetrics;
			if (windowMetrics is null)
			{
				return default;
			}

			displaySize = new Size(windowMetrics.Bounds.Width(), windowMetrics.Bounds.Height());
		}
		else
		{
			using var realMetrics = new DisplayMetrics();

#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			activity.WindowManager?.DefaultDisplay.GetRealMetrics(realMetrics);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			displaySize = new Size(realMetrics.WidthPixels, realMetrics.HeightPixels);
		}

		return displaySize;
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		UpdateFullScreenMode(true);
		return Disposable.Create(() => UpdateFullScreenMode(false));
	}

	private void UpdateFullScreenMode(bool isFullscreen)
	{
		if (_activity is not { Window: not null } activity)
		{
			return;
		}

#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
		var uiOptions = (int)activity.Window.DecorView.SystemUiVisibility;
#pragma warning restore CA1422 // Validate platform compatibility

		if (isFullscreen)
		{
			uiOptions |= (int)SystemUiFlags.Fullscreen;
			uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
			uiOptions |= (int)SystemUiFlags.HideNavigation;
			uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
		}
		else
		{
			uiOptions &= ~(int)SystemUiFlags.Fullscreen;
			uiOptions &= ~(int)SystemUiFlags.ImmersiveSticky;
			uiOptions &= ~(int)SystemUiFlags.HideNavigation;
			uiOptions &= ~(int)SystemUiFlags.LayoutHideNavigation;
		}

#pragma warning disable CA1422 // Validate platform compatibility
		activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618
	}

	// Called on the Skia path (in ApplicationActivity.OnCreate) so the splash is held until the first Skia frame.
	internal void ArmFirstFrameGate() => _awaitingFirstFrame = true;

	// Called on the GL/Vulkan render thread once the first Skia frame has been presented.
	internal void NotifyFirstFrameRendered() => _awaitingFirstFrame = false;

	private void AddPreDrawListener()
	{
		if (_activity?.Window?.DecorView is { } decorView)
		{
			decorView.ViewTreeObserver.AddOnPreDrawListener(_preDrawListener);
		}
	}

	private void RemovePreDrawListener()
	{
		if (_activity?.Window?.DecorView is { } decorView)
		{
			decorView.ViewTreeObserver.RemoveOnPreDrawListener(_preDrawListener);
		}
	}

	private sealed class ActivationPreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
	{
		private readonly NativeWindowWrapper _windowWrapper;

		public ActivationPreDrawListener(NativeWindowWrapper windowWrapper)
		{
			_windowWrapper = windowWrapper;
		}

		public ActivationPreDrawListener(IntPtr handle, JniHandleOwnership transfer)
			: base(handle, transfer)
		{
		}

		public bool OnPreDraw()
		{
			if (_windowWrapper._contentViewAttachedToWindow
				&& !_windowWrapper._awaitingFirstFrame)
			{
				_windowWrapper.RemovePreDrawListener();
				return true;
			}

			return false;
		}
	}
}
