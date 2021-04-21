#if XAMARIN_ANDROID
using System;
using Android.App;
using Android.Util;
using Android.Views;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private Grid _main;
		private Border _rootBorder;
		private Border _fullWindow;
		private UIElement _content;
		private PopupRoot _popupRoot;

		public Window()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow();

			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged
				+= RaiseNativeSizeChanged;

			InitializeCommon();
		}

		internal Thickness Insets { get; set; }

		internal int SystemUiVisibility { get; set; }

		private void InternalSetContent(UIElement value)
		{
			if (_main == null)
			{
				_rootBorder = new Border();
				_fullWindow = new Border()
				{
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					Visibility = Visibility.Collapsed
				};
				_popupRoot = new PopupRoot();
				FocusVisualLayer = new Canvas();

				_main = new Grid()
				{
					IsVisualTreeRoot = true,
					Children =
					{
						_rootBorder,
						_fullWindow,
						_popupRoot,
						FocusVisualLayer
					}
				};

				ApplicationActivity.Instance?.SetContentView(_main);
			}

			_rootBorder.Child = _content = value;
		}

		private UIElement InternalGetContent() => _content;

		private UIElement InternalGetRootElement() => _main;

		internal UIElement MainContent => _main;

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
			var display = (ContextHelper.Current as Activity)?.WindowManager?.DefaultDisplay;
			var fullScreenMetrics = new DisplayMetrics();

#pragma warning disable 618
			display?.GetMetrics(outMetrics: fullScreenMetrics);
#pragma warning restore 618

			var newBounds = ViewHelper.PhysicalToLogicalPixels(new Rect(0, 0, fullScreenMetrics.WidthPixels, fullScreenMetrics.HeightPixels));

			var statusBarSize = GetLogicalStatusBarSize();

			var statusBarSizeExcluded = IsStatusBarTranslucent() ?
				// The real metrics excluded the StatusBar only if it is plain.
				// We want to subtract it if it is translucent. Otherwise, it will be like we subtract it twice.
				statusBarSize :
				0;
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
			ApplicationView.GetForCurrentView()?.SetVisibleBounds(visibleBounds);
			ApplicationView.GetForCurrentView()?.SetTrueVisibleBounds(trueVisibleBounds);

			if (Bounds != newBounds)
			{
				Bounds = newBounds;

				RaiseSizeChanged(
					new Windows.UI.Core.WindowSizeChangedEventArgs(
						new Windows.Foundation.Size(Bounds.Width, Bounds.Height)
					)
				);
			}
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
				_fullWindow.Child = null;
				_rootBorder.Visibility = Visibility.Visible;
				_fullWindow.Visibility = Visibility.Collapsed;
			}
			else
			{
				_fullWindow.Visibility = Visibility.Visible;
				_rootBorder.Visibility = Visibility.Collapsed;
				_fullWindow.Child = element;
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
				|| activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.LayoutNoLimits); ;
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

		internal IDisposable OpenPopup(Popup popup)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating popup");
			}

			var popupPanel = popup.PopupPanel;
			_popupRoot.Children.Add(popupPanel);

			return new CompositeDisposable(
				Disposable.Create(() => {

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Closing popup");
					}

					_popupRoot.Children.Remove(popupPanel);
				}),
				VisualTreeHelper.RegisterOpenPopup(popup)
			);
		}

	}
}
#endif
