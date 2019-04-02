#if XAMARIN_ANDROID
using Android.App;
using Android.Util;
using Android.Views;
using Uno.UI;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private Grid _main;
		private Border _rootBorder;
		private Border _fullWindow;
		private UIElement _content;

		public Window()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow();
			InitializeCommon();
		}

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

				_main = new Grid()
				{
					Children =
					{
						_rootBorder,
						_fullWindow
					}
				};

				ApplicationActivity.Instance?.SetContentView(_main);
			}

			_rootBorder.Child = _content = value;
		}

		private UIElement InternalGetContent()
		{
			return _content;
		}

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

			// To get the real size of the screen, we should use GetRealMetrics
			// GetMetrics or Resources.DisplayMetrics return the usable metrics, ignoring the bottom rounded space on device like LG G7 ThinQ for example
			display?.GetRealMetrics(outMetrics: fullScreenMetrics);

			var newBounds = ViewHelper.PhysicalToLogicalPixels(new Rect(0, 0, fullScreenMetrics.WidthPixels, fullScreenMetrics.HeightPixels));

			var statusBarHeightExcluded = GetLogicalStatusBarHeightExcluded();
			var navigationBarHeightExcluded = GetLogicalNavigationBarHeightExcluded();

			var newVisibleBounds = new Rect(
				x: newBounds.X,
				y: newBounds.Y + statusBarHeightExcluded,
				width: newBounds.Width,
				height: newBounds.Height - statusBarHeightExcluded - navigationBarHeightExcluded
			);

			ApplicationView.GetForCurrentView()?.SetVisibleBounds(newVisibleBounds);

			if (Bounds != newBounds)
			{
				Bounds = newBounds;

				RaiseSizeChanged(
					new WindowSizeChangedEventArgs(
						new Windows.Foundation.Size(Bounds.Width, Bounds.Height)
					)
				);
			}
		}

		private double GetLogicalStatusBarHeightExcluded()
		{
			var logicalStatusBarHeight = 0d;
			var activity = ContextHelper.Current as Activity;
			var decorView = activity.Window.DecorView;
			var isStatusBarVisible = ((int)decorView.SystemUiVisibility & (int)SystemUiFlags.Fullscreen) == 0;

			var isStatusBarTranslucent =
				activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.TranslucentStatus)
				|| activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.LayoutNoLimits);

			// The real metrics excluded the StatusBar only if it is plain.
			// We want to substract it if it is translucent. Otherwise, it will be like we substract it twice.
			if (isStatusBarVisible && isStatusBarTranslucent)
			{
				int resourceId = Android.Content.Res.Resources.System.GetIdentifier("status_bar_height", "dimen", "android");
				if (resourceId > 0)
				{
					logicalStatusBarHeight = ViewHelper.PhysicalToLogicalPixels(Android.Content.Res.Resources.System.GetDimensionPixelSize(resourceId));
				}
			}

			return logicalStatusBarHeight;
		}

		private double GetLogicalNavigationBarHeightExcluded()
		{
			// The real metrics excluded the NavigationBar only if it is plain.
			// We want to substract it if it is translucent. Otherwise, it will be like we substract it twice.
			return ContextHelper.Current is Activity activity && ShouldBeExcluded()
				? ViewHelper.PhysicalToLogicalPixels(LayoutProvider.Instance.NavigationBarLayout.Height())
				: 0;

			bool ShouldBeExcluded() => false
				|| activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.TranslucentNavigation)
				|| activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.LayoutNoLimits);
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
	}
}
#endif
