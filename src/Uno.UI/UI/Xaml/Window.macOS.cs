using CoreGraphics;
using Foundation;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using AppKit;
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
		private RootViewController _mainController;
		private UIElement _content;
		private Grid _main;
		private Border _rootBorder;
		private Border _fullWindow;
		private object _windowResizeNotificationObject;

		/// <summary>
		/// A function to generate a custom view controller which inherits from <see cref="RootViewController"/>.
		/// This must be set before the <see cref="Window"/> is created (typically when <see cref="Window.Current"/> is called for the first time),
		/// otherwise it will have no effect.
		/// </summary>
		public static Func<RootViewController> ViewControllerGenerator { get; set; }

		public Window()
		{
			var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled | NSWindowStyle.Miniaturizable;
			var rect = new CoreGraphics.CGRect(100, 100, 1024, 768);
			_window = new Uno.UI.Controls.Window(rect, style, NSBackingStore.Buffered, false);

			_mainController = ViewControllerGenerator?.Invoke() ?? new RootViewController();

			ObserveOrientationAndSize();

			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow(_window);

			_window.CoreWindowEvents = CoreWindow;

			InitializeCommon();
		}

		private void ObserveOrientationAndSize()
		{
			_windowResizeNotificationObject = NSNotificationCenter.DefaultCenter.AddObserver(
				new NSString("NSWindowDidResizeNotification"), ResizeObserver, null);

			RaiseNativeSizeChanged(new CGSize(_window.Frame.Width, _window.Frame.Height));

		}

		private void ResizeObserver(NSNotification obj)
		{
			RaiseNativeSizeChanged(new CGSize(_window.Frame.Width, _window.Frame.Height));
		}

		partial void InternalActivate()
		{
			_window.ContentViewController = _mainController;
			_window.Display();
			_window.MakeKeyAndOrderFront(NSApplication.SharedApplication);
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

				_main = new Grid()
				{
					IsVisualTreeRoot = true,
					Children =
					{
						_rootBorder,
						_fullWindow
					}
				};

				_mainController.View = _main;
				_main.Frame = _window.Frame;
				_main.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
			}

			_rootBorder.Child?.RemoveFromSuperview();
			_rootBorder.Child = _content = value;

			// This is required to get the mouse move while not pressed!
			var options = NSTrackingAreaOptions.MouseEnteredAndExited
				| NSTrackingAreaOptions.MouseMoved
				| NSTrackingAreaOptions.ActiveInKeyWindow
				| NSTrackingAreaOptions.EnabledDuringMouseDrag // We want enter/leave events even if the button is pressed
				| NSTrackingAreaOptions.InVisibleRect; // Automagicaly syncs the bounds rect
			var trackingArea = new NSTrackingArea(Bounds, options, _main, null);

			_main.AddTrackingArea(trackingArea);
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

			if (Bounds != newBounds)
			{
				Bounds = newBounds;

				var applicationView = ApplicationView.GetForCurrentView();
				if (applicationView != null)
				{
					applicationView.SetCoreBounds(_window, newBounds);
				}

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

		private void UpdateCoreBounds()
		{
			var applicationView = ApplicationView.GetForCurrentView();
			if (applicationView != null)
			{
				applicationView.SetCoreBounds(_window, Bounds);
			}
		}
	}
}
