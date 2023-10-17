#nullable enable

using System;
using Android.App;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.View;
using Uno.UI.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase
{
	private static Lazy<NativeWindowWrapper> _instanceLazy = new Lazy<NativeWindowWrapper>(() => new NativeWindowWrapper(default!, default!));

	private readonly ActivationPreDrawListener _preDrawListener;
	private Activity? _activity;
	private Rect _previousTrueVisibleBounds;

	public NativeWindowWrapper(Windows.UI.Xaml.Window window, XamlRoot xamlRoot)
	{
		_preDrawListener = new ActivationPreDrawListener(this);
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += RaiseNativeSizeChanged;
	}

	public static NativeWindowWrapper Instance => _instanceLazy.Value; // TODO:MZ: Remove this

	internal void SetActivity(Activity activity)
	{
		_activity = activity;
	}

	internal int SystemUiVisibility { get; set; }

	internal void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	internal void OnActivityCreated()
	{
		var decorView = _activity!.Window!.DecorView;

#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
		NativeWindowWrapper.Instance.SystemUiVisibility = (int)decorView.SystemUiVisibility;
		decorView.SetOnSystemUiVisibilityChangeListener(new OnSystemUiVisibilityChangeListener());
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

		AddPreDrawListener();
	}

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosed();

	internal bool IsStatusBarTranslucent()
	{
		if (_activity?.Window?.Attributes is null)
		{
			return false;
		}

		return _activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.TranslucentStatus)
			|| _activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.LayoutNoLimits);
	}

	internal void RaiseNativeSizeChanged()
	{
		var (windowSize, visibleBounds, trueVisibleBounds) = GetVisualBounds();

		Bounds = new Rect(default, windowSize);
		VisibleBounds = visibleBounds;

		if (_previousTrueVisibleBounds != trueVisibleBounds)
		{
			_previousTrueVisibleBounds = trueVisibleBounds;

			// TODO: Adjust when multiple windows are supported on Android #13827
			ApplicationView.GetForCurrentView()?.SetTrueVisibleBounds(trueVisibleBounds);
		}
	}

	protected override void ShowCore() => RemovePreDrawListener();

	private (Size windowSize, Rect visibleBounds, Rect trueVisibleBounds) GetVisualBounds()
	{
		var activity = _activity!;
		var window = activity.Window!;
		var windowInsets = ViewCompat.GetRootWindowInsets(window.DecorView);

		var insetsTypes = WindowInsetsCompat.Type.SystemBars(); // == WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars() | WindowInsets.Type.CaptionBar();

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
		var windowBounds = new Rect(default, GetDisplaySize().Subtract(opaqueInsets));

		// The visible bounds is the windows bounds on which we remove also translucentInsets
		var visibleBounds = windowBounds.DeflateBy(translucentInsets);

		return (windowBounds.PhysicalToLogicalPixels().Size, visibleBounds.PhysicalToLogicalPixels(), visibleBounds.PhysicalToLogicalPixels());
	}

	private bool IsNavigationBarTranslucent()
	{
		var activity = _activity!;
		var window = activity.Window!;
		var flags = window.Attributes!.Flags;
		return flags.HasFlag(WindowManagerFlags.TranslucentNavigation)
			|| flags.HasFlag(WindowManagerFlags.LayoutNoLimits);
	}

	private Size GetDisplaySize()
	{
		if (_activity?.WindowManager is not { } windowManager)
		{
			return default;
		}

		Size displaySize = default;

		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			displaySize = new Size(windowMetrics.Bounds.Width(), windowMetrics.Bounds.Height());
		}
		else
		{
			SetDisplaySizeLegacy();
		}

		return displaySize;

		void SetDisplaySizeLegacy()
		{
			using var realMetrics = new DisplayMetrics();

#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			windowManager.DefaultDisplay?.GetRealMetrics(realMetrics);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			displaySize = new Size(realMetrics.WidthPixels, realMetrics.HeightPixels);
		}
	}

	private void AddPreDrawListener()
	{
		if (_activity?.Window is not { } window)
		{
			return;
		}

		if (window.DecorView?.ViewTreeObserver is { } viewTreeObserver)
		{
			viewTreeObserver.AddOnPreDrawListener(_preDrawListener);
		}
	}

	private void RemovePreDrawListener()
	{
		if (_activity?.Window is not { } window)
		{
			return;
		}

		if (window.DecorView?.ViewTreeObserver is { } viewTreeObserver)
		{
			viewTreeObserver.RemoveOnPreDrawListener(_preDrawListener);
		}
	}

	private sealed class ActivationPreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
	{
		private readonly NativeWindowWrapper? _windowWrapper;

		public ActivationPreDrawListener(NativeWindowWrapper windowWrapper)
		{
			_windowWrapper = windowWrapper;
		}

		public ActivationPreDrawListener(IntPtr handle, JniHandleOwnership transfer)
			: base(handle, transfer)
		{
		}

		public bool OnPreDraw() => _windowWrapper?.Visible ?? false;
	}
}
