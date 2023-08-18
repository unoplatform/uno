using System;
using Android.App;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.View;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using static System.Net.Mime.MediaTypeNames;
using Size = Windows.Foundation.Size;

namespace Microsoft.UI.Xaml
{
	public sealed partial class Window
	{
		private readonly ActivationPreDrawListener _preDrawListener = new();
		private Border _rootBorder;

		partial void InitPlatform()
		{
			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged
				+= RaiseNativeSizeChanged;
		}

		internal Android.Views.Window NativeWindow => ApplicationActivity.Instance?.Window;

		internal void OnActivityCreated() => AddPreDrawListener();

		partial void ShowPartial() => RemovePreDrawListener();

		internal int SystemUiVisibility { get; set; }

		internal UIElement MainContent => _rootVisual;

		internal void RaiseNativeSizeChanged()
		{
			var (windowBounds, visibleBounds, trueVisibleBounds) = GetVisualBounds();

			ApplicationView.GetForCurrentView()?.SetVisibleBounds(visibleBounds);
			ApplicationView.GetForCurrentView()?.SetTrueVisibleBounds(trueVisibleBounds);

			if (Bounds != windowBounds)
			{
				Bounds = windowBounds;

				RaiseSizeChanged(
					new Windows.UI.Core.WindowSizeChangedEventArgs(
						new Windows.Foundation.Size(Bounds.Width, Bounds.Height)
					)
				);
			}
		}

		private (Rect windowBounds, Rect visibleBounds, Rect trueVisibleBounds) GetVisualBounds()
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

			return (windowBounds.PhysicalToLogicalPixels(), visibleBounds.PhysicalToLogicalPixels(), visibleBounds.PhysicalToLogicalPixels());
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

		internal void DisplayFullscreen(UIElement element)
		{
			if (element == null)
			{
				FullWindowMediaRoot.Child = null;
				_rootBorder.Visibility = Visibility.Visible;
				FullWindowMediaRoot.Visibility = Visibility.Collapsed;
			}
			else
			{
				FullWindowMediaRoot.Visibility = Visibility.Visible;
				_rootBorder.Visibility = Visibility.Collapsed;
				FullWindowMediaRoot.Child = element;
			}
		}

		#region StatusBar properties
		public bool IsStatusBarTranslucent()
		{
			if (!(ContextHelper.Current is Activity activity))
			{
				throw new Exception("Cannot check NavigationBar translucent property. Activity is not defined yet.");
			}

			return activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.TranslucentStatus)
				|| activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.LayoutNoLimits);
		}
		#endregion

		#region NavigationBar properties
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
		#endregion

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
			public ActivationPreDrawListener()
			{
			}

			public ActivationPreDrawListener(IntPtr handle, JniHandleOwnership transfer)
				: base(handle, transfer)
			{
			}

			public bool OnPreDraw() => Window.IShouldntUseCurrentWindow.Visible;
		}
	}
}
