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

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase, INativeWindowWrapper
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private readonly ActivationPreDrawListener _preDrawListener;
	private readonly DisplayInformation _displayInformation;
	private bool _contentViewAttachedToWindow;

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

		SetBoundsAndVisibleBounds(new Rect(default, windowSize), visibleBounds);
		var size = new Windows.Graphics.SizeInt32((int)(windowSize.Width * RasterizationScale), (int)(windowSize.Height * RasterizationScale));
		SetSizes(size, size);
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
		MUX.Application.Current.RequestedThemeChanged += () =>
		{
			if (MUX.Application.Current.InitializationComplete)
			{
				ApplySystemOverlaysTheming();
			}
		};

		ApplicationActivity.Instance.ContentViewAttachedToWindow += Instance_ContentViewAttachedToWindow;
		ApplicationActivity.Instance.EnsureContentView();
		ApplySystemOverlaysTheming();
	}

	private void Instance_ContentViewAttachedToWindow(object sender, EventArgs e) =>
		_contentViewAttachedToWindow = true;

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
		// Only apply theming if the app hasn't explicitly set a foreground
		if (StatusBar.GetForCurrentView().ForegroundColor is null)
		{
			if (FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled)
			{
				// In edge-to-edge experience we want to adjust the theming of status bar to match the app theme.
				if (Microsoft.UI.Xaml.Application.Current is { } application &&
					(ContextHelper.TryGetCurrent(out var context)) &&
					context is Activity activity &&
					activity.Window?.DecorView is { FitsSystemWindows: false } decorView)
				{
					var requestedTheme = application.RequestedTheme;

					var insetsController = WindowCompat.GetInsetsController(activity.Window, decorView);

					// "appearance light" refers to status bar set to light theme == dark foreground
					insetsController.AppearanceLightStatusBars = requestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light;
				}
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

		public bool OnPreDraw()
		{
			if (_windowWrapper._contentViewAttachedToWindow)
			{
				_windowWrapper.RemovePreDrawListener();
				return true;
			}

			return false;
		}
	}
}
