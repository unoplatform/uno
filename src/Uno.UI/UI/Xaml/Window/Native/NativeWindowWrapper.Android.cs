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
using Size = Windows.Foundation.Size;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : INativeWindowWrapper
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private readonly ActivationPreDrawListener _preDrawListener;

	private Size _previousWindowSize = new Size(-1, -1);

	internal static NativeWindowWrapper Instance => _instance.Value; // TODO: Temporary until proper multi-window support is added.

	public NativeWindowWrapper()
	{
		_preDrawListener = new ActivationPreDrawListener(this);
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += RaiseNativeSizeChanged;
	}

	public event EventHandler<Size> SizeChanged;
	public event EventHandler<CoreWindowActivationState> ActivationChanged;
	public event EventHandler<bool> VisibilityChanged;
	public event EventHandler Closed;
	public event EventHandler Shown;

	public bool Visible { get; private set; }

	internal int SystemUiVisibility { get; set; }

	public void Activate()
	{
		// TODO: MZ: Bring window to the foreground?
	}

	internal void OnNativeVisibilityChanged(bool visible)
	{
		Visible = visible;
		VisibilityChanged?.Invoke(this, visible);
	}

	internal void OnNativeClosed() => Closed?.Invoke(this, EventArgs.Empty);

	internal void OnActivityCreated() => AddPreDrawListener();

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationChanged?.Invoke(this, state);

	internal bool IsStatusBarTranslucent()
	{
		if (!(ContextHelper.Current is Activity activity))
		{
			throw new Exception("Cannot check NavigationBar translucent property. Activity is not defined yet.");
		}

		return activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.TranslucentStatus)
			|| activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.LayoutNoLimits);
	}

	internal void RaiseNativeSizeChanged()
	{
		var (windowSize, visibleBounds, trueVisibleBounds) = GetVisualBounds();

		ApplicationView.GetForCurrentView()?.SetVisibleBounds(visibleBounds);
		ApplicationView.GetForCurrentView()?.SetTrueVisibleBounds(trueVisibleBounds);

		if (_previousWindowSize != windowSize)
		{
			_previousWindowSize = windowSize;

			SizeChanged?.Invoke(this, windowSize);
		}
	}

	public void Show()
	{
		RemovePreDrawListener();
		Shown?.Invoke(this, EventArgs.Empty);
	}

	private (Size windowSize, Rect visibleBounds, Rect trueVisibleBounds) GetVisualBounds()
	{
		if (ContextHelper.Current is not Activity activity)
		{
			return default;
		}

		var windowInsets = ViewCompat.GetRootWindowInsets(activity.Window.DecorView);

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
		if (!(ContextHelper.Current is Activity activity))
		{
			throw new Exception("Cannot check NavigationBar translucent property. Activity is not defined yet.");
		}

		var flags = activity.Window.Attributes.Flags;
		return flags.HasFlag(WindowManagerFlags.TranslucentNavigation)
			|| flags.HasFlag(WindowManagerFlags.LayoutNoLimits);
	}

	private Size GetDisplaySize()
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
			SetDisplaySizeLegacy();
		}

		return displaySize;

		void SetDisplaySizeLegacy()
		{
			using var realMetrics = new DisplayMetrics();

#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			activity.WindowManager?.DefaultDisplay.GetRealMetrics(realMetrics);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			displaySize = new Size(realMetrics.WidthPixels, realMetrics.HeightPixels);
		}
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

		public bool OnPreDraw() => _windowWrapper.Visible;
	}
}
