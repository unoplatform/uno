#if XAMARIN_ANDROID
using System;
using Android.App;
using Android.Util;
using Android.Views;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Uno.Extensions.ValueType;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private RootVisual _rootVisual;
		private Border _rootBorder;
		private UIElement _content;

		public Window()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = CoreWindow.GetOrCreateForCurrentThread();

			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged
				+= RaiseNativeSizeChanged;

			InitializeCommon();
		}

		internal Thickness Insets { get; set; }

		internal int SystemUiVisibility { get; set; }

		private void InternalSetContent(UIElement value)
		{
			if (_rootVisual == null)
			{
				_rootBorder = new Border();
				CoreServices.Instance.PutVisualRoot(_rootBorder);
				_rootVisual = CoreServices.Instance.MainRootVisual;

				if (_rootVisual == null)
				{
					throw new InvalidOperationException("The root visual could not be created.");
				}

				ApplicationActivity.Instance?.SetContentView(_rootVisual);
			}
			_rootBorder.Child = _content = value;
		}

		private UIElement InternalGetContent() => _content;

		private UIElement InternalGetRootElement() => _rootVisual;

		internal UIElement MainContent => _rootVisual;

		private static Window InternalGetCurrentWindow()
		{
			if (_current == null)
			{
				_current = new Window();
			}

			return _current;
		}

		internal void RaiseNativeSizeChanged()
		{
#if __ANDROID_30__
			var (windowBounds, visibleBounds, trueVisibleBounds) = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R
				? GetVisualBounds()
				: GetVisualBoundsLegacy();
#else
			var (windowBounds, visibleBounds, trueVisibleBounds) = GetVisualBoundsLegacy();
#endif

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

#if __ANDROID_30__
		private (Rect windowBounds, Rect visibleBounds, Rect trueVisibleBounds) GetVisualBounds()
		{
			var metrics = (ContextHelper.Current as Activity)?.WindowManager?.CurrentWindowMetrics;

			var insetsTypes = WindowInsets.Type.SystemBars(); // == WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars() | WindowInsets.Type.CaptionBar();
			var opaqueInsetsTypes = insetsTypes;
			if (IsStatusBarTranslucent())
			{
				opaqueInsetsTypes &= ~WindowInsets.Type.StatusBars();
			}
			if (IsNavigationBarTranslucent())
			{
				opaqueInsetsTypes &= ~WindowInsets.Type.NavigationBars();
			}

			var insets = metrics.WindowInsets.GetInsets(insetsTypes).ToThickness();
			var opaqueInsets = metrics.WindowInsets.GetInsets(opaqueInsetsTypes).ToThickness();
			var translucentInsets = insets.Minus(opaqueInsets);

			// The 'metric.Bounds' does not include any insets, so we remove the "opaque" insets under which we cannot draw anything
			var windowBounds = new Rect(default, ((Rect)metrics.Bounds).DeflateBy(opaqueInsets).Size);

			// The visible bounds is the windows bounds on which we remove also translucentInsets
			var visibleBounds = windowBounds.DeflateBy(translucentInsets);

			return (windowBounds.PhysicalToLogicalPixels(), visibleBounds.PhysicalToLogicalPixels(), visibleBounds.PhysicalToLogicalPixels());
		}
#endif

		private (Rect windowBounds, Rect visibleBounds, Rect trueVisibleBounds) GetVisualBoundsLegacy()
		{
			using var display = (ContextHelper.Current as Activity)?.WindowManager?.DefaultDisplay;
			using var fullScreenMetrics = new DisplayMetrics();

#pragma warning disable 618
			display?.GetMetrics(outMetrics: fullScreenMetrics);
#pragma warning restore 618

			var newBounds = ViewHelper.PhysicalToLogicalPixels(new Rect(0, 0, fullScreenMetrics.WidthPixels, fullScreenMetrics.HeightPixels));

			var statusBarSize = GetLogicalStatusBarSize();

			var statusBarSizeExcluded = IsStatusBarTranslucent()
				// The real metrics excluded the StatusBar only if it is plain.
				// We want to subtract it if it is translucent. Otherwise, it will be like we subtract it twice.
				? statusBarSize
				: 0;
			var navigationBarSizeExcluded = GetLogicalNavigationBarSizeExcluded();

			// Actually, we need to check visibility of nav bar and status bar since the insets don't
			UpdateInsetsWithVisibilities();

			var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

			Rect CalculateVisibleBounds(double excludedStatusBarHeight)
			{
				var topHeightExcluded = Math.Max(Insets.Top, excludedStatusBarHeight);
				var newVisibleBounds = new Rect();

				switch (orientation)
				{
					// StatusBar on top, NavigationBar on right
					case DisplayOrientations.Landscape:
						newVisibleBounds = new Rect(
							x: newBounds.X + Insets.Left,
							y: newBounds.Y + topHeightExcluded,
							width: newBounds.Width - (Insets.Left + Math.Max(Insets.Right, navigationBarSizeExcluded)),
							height: newBounds.Height - topHeightExcluded - Insets.Bottom
						);
						break;
					// StatusBar on top, NavigationBar on left
					case DisplayOrientations.LandscapeFlipped:
						newVisibleBounds = new Rect(
							x: newBounds.X + Math.Max(Insets.Left, navigationBarSizeExcluded),
							y: newBounds.Y + topHeightExcluded,
							width: newBounds.Width - (Math.Max(Insets.Left, navigationBarSizeExcluded) + Insets.Right),
							height: newBounds.Height - topHeightExcluded - Insets.Bottom
						);
						break;
					// StatusBar on top, NavigationBar on bottom
					default:
						newVisibleBounds = new Rect(
							x: newBounds.X + Insets.Left,
							y: newBounds.Y + topHeightExcluded,
							width: newBounds.Width - (Insets.Left + Insets.Right),
							height: newBounds.Height - topHeightExcluded - Math.Max(Insets.Bottom, navigationBarSizeExcluded)
						);
						break;
				}

				return newVisibleBounds;
			}

			var visibleBounds = CalculateVisibleBounds(statusBarSizeExcluded);
			var trueVisibleBounds = CalculateVisibleBounds(statusBarSize);

			return (newBounds, visibleBounds, trueVisibleBounds);
		}

		internal void UpdateInsetsWithVisibilities()
		{
			var newInsets = new Thickness();
			var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

			// Navigation bar check (depending of the orientation
			switch (orientation)
			{
				// StatusBar on top, NavigationBar on bottom
				case DisplayOrientations.Portrait:
					newInsets.Top = IsStatusBarVisible() ? Insets.Top : 0d;
					newInsets.Bottom = IsNavigationBarVisible() ? Insets.Bottom : 0d;
					newInsets.Left = Insets.Left;
					newInsets.Right = Insets.Right;
					break;
				// StatusBar on top, NavigationBar on right
				case DisplayOrientations.Landscape:
					newInsets.Top = IsStatusBarVisible() ? Insets.Top : 0d;
					newInsets.Right = IsNavigationBarVisible() ? Insets.Right : 0d;
					newInsets.Left = Insets.Left;
					newInsets.Bottom = Insets.Bottom;
					break;
				// StatusBar on top, NavigationBar on bottom
				case DisplayOrientations.PortraitFlipped:
					newInsets.Top = IsStatusBarVisible() ? Insets.Top : 0d;
					newInsets.Bottom = IsNavigationBarVisible() ? Insets.Bottom : 0d;
					newInsets.Left = Insets.Left;
					newInsets.Right = Insets.Right;
					break;
				// StatusBar on top, NavigationBar on left
				case DisplayOrientations.LandscapeFlipped:
					newInsets.Top = IsStatusBarVisible() ? Insets.Top : 0d;
					newInsets.Left = IsNavigationBarVisible() ? Insets.Left : 0d;
					newInsets.Bottom = Insets.Bottom;
					newInsets.Right = Insets.Right;
					break;
				default:
					break;
			}

			Insets = newInsets;
		}

		private double GetLogicalStatusBarSize()
		{
			var logicalStatusBarHeight = 0d;

			if (IsStatusBarVisible())
			{
				var resourceId = Android.Content.Res.Resources.System.GetIdentifier("status_bar_height", "dimen", "android");
				if (resourceId > 0)
				{
					logicalStatusBarHeight = ViewHelper.PhysicalToLogicalPixels(Android.Content.Res.Resources.System.GetDimensionPixelSize(resourceId));
				}
			}

			return logicalStatusBarHeight;
		}

		// Used by legacy visual bounds calculation on <API 30 devices
		private double GetLogicalNavigationBarSizeExcluded()
		{
			var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

			var navigationBarSize = orientation == DisplayOrientations.Landscape || orientation == DisplayOrientations.LandscapeFlipped
				? ApplicationActivity.Instance.LayoutProvider.NavigationBarRect.Width()
				: ApplicationActivity.Instance.LayoutProvider.NavigationBarRect.Height();

			// The real metrics excluded the NavigationBar only if it is plain.
			// We want to subtract it if it is translucent. Otherwise, it will be like we subtract it twice.
			return IsNavigationBarVisible() && IsNavigationBarTranslucent()
				? ViewHelper.PhysicalToLogicalPixels(navigationBarSize)
				: 0;
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
		private bool IsStatusBarVisible()
		{
			var decorView = (ContextHelper.Current as Activity)?.Window?.DecorView;

			if (decorView == null)
			{
				throw new global::System.Exception("Cannot check NavigationBar visibility property. DecorView is not defined yet.");
			}

#pragma warning disable 618
			return ((int)decorView.SystemUiVisibility & (int)SystemUiFlags.Fullscreen) == 0;
#pragma warning restore 618
		}

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
		private bool IsNavigationBarVisible()
		{
			var decorView = (ContextHelper.Current as Activity)?.Window?.DecorView;
			if (decorView == null)
			{
				throw new global::System.Exception("Cannot check NavigationBar visibility property. DecorView is not defined yet.");
			}

#pragma warning disable 618
			var uiFlags = (int)decorView.SystemUiVisibility;
#pragma warning restore 618
			return (uiFlags & (int)SystemUiFlags.HideNavigation) == 0
				|| (uiFlags & (int)SystemUiFlags.LayoutHideNavigation) == 0;
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
#endregion

		internal IDisposable OpenPopup(Controls.Primitives.Popup popup)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating popup");
			}

			if (PopupRoot == null)
			{
				throw new InvalidOperationException("PopupRoot is not initialized yet.");
			}

			var popupPanel = popup.PopupPanel;
			PopupRoot.Children.Add(popupPanel);

			return new CompositeDisposable(
				Disposable.Create(() =>
				{

					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Closing popup");
					}

					PopupRoot.Children.Remove(popupPanel);
				}),
				VisualTreeHelper.RegisterOpenPopup(popup)
			);
		}
	}
}
#endif
