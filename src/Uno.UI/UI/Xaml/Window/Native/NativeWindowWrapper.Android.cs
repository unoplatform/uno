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

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase, INativeWindowWrapper
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private readonly ActivationPreDrawListener _preDrawListener;
	private readonly DisplayInformation _displayInformation;

	private Rect _previousTrueVisibleBounds;

	public NativeWindowWrapper()
	{
		_preDrawListener = new ActivationPreDrawListener(this);
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += RaiseNativeSizeChanged;

		_displayInformation = DisplayInformation.GetForCurrentViewSafe() ?? throw new InvalidOperationException("DisplayInformation must be available when the window is initialized");
		_displayInformation.DpiChanged += (s, e) => DispatchDpiChanged();
		DispatchDpiChanged();
	}

	public override object NativeWindow => Microsoft.UI.Xaml.ApplicationActivity.Instance?.Window;

	internal static NativeWindowWrapper Instance => _instance.Value;

	private void DispatchDpiChanged() =>
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;

	public override string Title
	{
		get => Microsoft.UI.Xaml.ApplicationActivity.Instance.Title;
		set => Microsoft.UI.Xaml.ApplicationActivity.Instance.Title = value;
	}

	internal int SystemUiVisibility { get; set; }

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnActivityCreated() => AddPreDrawListener();

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();

	internal bool IsStatusBarTranslucent()
	{
		if (!(ContextHelper.Current is Activity activity))
		{
			throw new Exception("Cannot check NavigationBar translucent property. Activity is not defined yet.");
		}

		return activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.TranslucentStatus)
			|| activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.LayoutNoLimits)

			//  Both TranslucentStatus and LayoutNoLimits are false when EdgeToEdge is set (default mode in net9).
			|| FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled;
	}

	internal void RaiseNativeSizeChanged()
	{
		var (windowSize, visibleBounds) = GetVisualBounds();

		Bounds = new Rect(default, windowSize);
		VisibleBounds = visibleBounds;
		Size = new((int)(windowSize.Width * RasterizationScale), (int)(windowSize.Height * RasterizationScale));
		ApplySystemOverlaysTheming();

		if (_previousTrueVisibleBounds != visibleBounds)
		{
			_previousTrueVisibleBounds = visibleBounds;

			// TODO: Adjust when multiple windows are supported on Android #13827
			ApplicationView.GetForCurrentView()?.SetTrueVisibleBounds(visibleBounds);
		}
	}

	protected override void ShowCore()
	{
		ApplySystemOverlaysTheming();
		RemovePreDrawListener();
	}

	private (Size windowSize, Rect visibleBounds) GetVisualBounds()
	{
		if (ContextHelper.Current is not Activity activity)
		{
			return default;
		}

		var windowInsets = GetWindowInsets(activity);

		var insetsTypes = WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout(); // == WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars() | WindowInsets.Type.CaptionBar();
		Rect windowBounds;
		Rect visibleBounds;

		var decorView = activity.Window.DecorView;
		var fitsSystemWindows = decorView.FitsSystemWindows;

		if (FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled)
		{
			var insets = windowInsets?.GetInsets(insetsTypes).ToThickness() ?? default;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Insets: {insets}");
			}

			// Edge-to-edge is default on Android 15 and above
			windowBounds = new Rect(default, GetWindowSize());
			visibleBounds = windowBounds.DeflateBy(insets);
		}
		else
		{
			var opaqueInsetsTypes = insetsTypes;
			if (IsStatusBarTranslucent())
			{
				opaqueInsetsTypes &= ~WindowInsetsCompat.Type.StatusBars();
			}
			if (IsNavigationBarTranslucent())
			{
				opaqueInsetsTypes &= ~WindowInsetsCompat.Type.NavigationBars();
			}

			var insets = windowInsets?.GetInsets(insetsTypes).ToThickness() ?? default;
			var opaqueInsets = windowInsets?.GetInsets(opaqueInsetsTypes).ToThickness() ?? default;
			var translucentInsets = insets.Minus(opaqueInsets);

			// The native display size does not include any insets, so we remove the "opaque" insets under which we cannot draw anything
			windowBounds = new Rect(default, GetWindowSize().Subtract(opaqueInsets));

			// The visible bounds is the windows bounds on which we remove also translucentInsets
			visibleBounds = windowBounds.DeflateBy(translucentInsets);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"WindowBounds: {windowBounds}, VisibleBounds {visibleBounds}");
		}

		var windowBoundsLogical = windowBounds.PhysicalToLogicalPixels();
		var visibleBoundsLogical = visibleBounds.PhysicalToLogicalPixels();

		return (windowBoundsLogical.Size, visibleBoundsLogical);
	}

	private bool IsNavigationBarTranslucent()
	{
		if (!(ContextHelper.Current is Activity activity))
		{
			throw new Exception("Cannot check NavigationBar translucent property. Activity is not defined yet.");
		}

		var flags = activity.Window.Attributes.Flags;
		return flags.HasFlag(WindowManagerFlags.TranslucentNavigation)
			|| flags.HasFlag(WindowManagerFlags.LayoutNoLimits);
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
		if (FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled)
		{
			// In edge-to-edge experience we want to adjust the theming of status bar to match the app theme.
			if ((ContextHelper.TryGetCurrent(out var context)) &&
				context is Activity activity &&
				activity.Window?.DecorView is { FitsSystemWindows: false } decorView)
			{
				var requestedTheme = Microsoft.UI.Xaml.Application.Current.RequestedTheme;

				var insetsController = WindowCompat.GetInsetsController(activity.Window, decorView);

				// "appearance light" refers to status bar set to light theme == dark foreground
				insetsController.AppearanceLightStatusBars = requestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light;
			}
		}
	}

	private Size GetWindowSize()
	{
		if (ContextHelper.Current is not Activity activity)
		{
			return default;
		}

		Size displaySize = default;

		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
		{
			var windowMetrics = (ContextHelper.Current as Activity)?.WindowManager?.CurrentWindowMetrics;
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
#pragma warning disable 618
		var activity = ContextHelper.Current as Activity;
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

	private void AddPreDrawListener()
	{
		if (Uno.UI.ContextHelper.Current is Android.App.Activity activity &&
			activity.Window.DecorView is { } decorView)
		{
			decorView.ViewTreeObserver.AddOnPreDrawListener(_preDrawListener);
		}
	}

	private void RemovePreDrawListener()
	{
		if (Uno.UI.ContextHelper.Current is Android.App.Activity activity &&
			activity.Window.DecorView is { } decorView)
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

		public bool OnPreDraw() => _windowWrapper.IsVisible;
	}
}
