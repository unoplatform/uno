#if XAMARIN_IOS
using CoreGraphics;
using Foundation;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UIKit;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Uno.UI.Controls;
using System.Drawing;
using Windows.UI.ViewManagement;
using Uno.UI;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private Uno.UI.Controls.Window _window;

		private static Window _current;
		private Grid _main;
		private Border _rootBorder;
		private Border _fullWindow;
		private RootViewController _mainController;
		private UIElement _content;
		private NSObject _orientationRegistration;

		/// <summary>
		/// A function to generate a custom view controller which inherits from <see cref="RootViewController"/>.
		/// This must be set before the <see cref="Window"/> is created (typically when <see cref="Window.Current"/> is called for the first time),
		/// otherwise it will have no effect.
		/// </summary>
		public static Func<RootViewController> ViewControllerGenerator { get; set; }

		public Window()
		{
			_window = new Uno.UI.Controls.Window();

			_mainController = ViewControllerGenerator?.Invoke() ?? new RootViewController();
			_mainController.View.BackgroundColor = UIColor.White;
			_mainController.NavigationBarHidden = true;
			
			ObserveOrientationAndSize();

			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow(_window);

			InitializeCommon();
		}

		private void ObserveOrientationAndSize()
		{
			_orientationRegistration = UIApplication
				.Notifications
				.ObserveDidChangeStatusBarOrientation(
					(sender, args) => RaiseNativeSizeChanged(ViewHelper.GetScreenSize())
				);

			_orientationRegistration = UIApplication
				.Notifications
				.ObserveDidChangeStatusBarFrame(
					(sender, args) => RaiseNativeSizeChanged(ViewHelper.GetScreenSize())
				);

			_window.FrameChanged +=
				() => RaiseNativeSizeChanged(ViewHelper.GetScreenSize());

			_mainController.VisibleBoundsChanged +=
				() => RaiseNativeSizeChanged(ViewHelper.GetScreenSize());

			var statusBar = StatusBar.GetForCurrentView();
			statusBar.Showing += (o, e) => RaiseNativeSizeChanged(ViewHelper.GetScreenSize());
			statusBar.Hiding += (o, e) => RaiseNativeSizeChanged(ViewHelper.GetScreenSize());

			RaiseNativeSizeChanged(ViewHelper.GetScreenSize());
		}

		partial void InternalActivate()
		{
			_window.RootViewController = _mainController;
			_window.MakeKeyAndVisible();
		}

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
				FocusVisualLayer = new Canvas();

				_main = new Grid()
				{
					IsVisualTreeRoot = true,
					Children =
					{
						_rootBorder,
						_fullWindow,
						FocusVisualLayer
					}
				};
				
				_mainController.View.AddSubview(_main);
				_main.Frame = _mainController.View.Bounds;
				_main.AutoresizingMask = UIViewAutoresizing.All;
			}

			_rootBorder.Child?.RemoveFromSuperview();
			_rootBorder.Child = _content = value;
		}

		private UIElement InternalGetContent() => _content;

		private UIElement InternalGetRootElement() => _main;

		private static Window InternalGetCurrentWindow()
		{
			if (_current == null)
			{
				_current = new Window();
			}

			return _current;
		}

		internal void RaiseNativeSizeChanged(CGSize size)
		{
			var newBounds = new Rect(0, 0, size.Width, size.Height);

			ApplicationView.GetForCurrentView()?.SetVisibleBounds(_window, newBounds);

			if (Bounds != newBounds)
			{
				Bounds = newBounds;

				RaiseSizeChanged(
					new Windows.UI.Core.WindowSizeChangedEventArgs(
						new Windows.Foundation.Size((float)size.Width, (float)size.Height)
					)
				);
			}
		}

		internal void DisplayFullscreen(UIElement element)
		{
			if (element == null)
			{
				_fullWindow.Child = null;
				_rootBorder.Opacity = 1;
				_fullWindow.Visibility = Visibility.Collapsed;
			}
			else
			{
				_fullWindow.Visibility = Visibility.Visible;
				_rootBorder.Opacity = 0;
				_fullWindow.Child = element;
			}
		}
	}
}
#endif
