using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using AppKit;
using CoreGraphics;
using Foundation;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml
{
	public sealed partial class Window
	{
		private Uno.UI.Controls.Window _window;

		private RootViewController _mainController;
		private Border _rootBorder;
		private object _windowResizeNotificationObject;

		/// <summary>
		/// A function to generate a custom view controller which inherits from <see cref="RootViewController"/>.
		/// This must be set before the <see cref="Window"/> is created (typically when <see cref="Window.Current"/> is called for the first time),
		/// otherwise it will have no effect.
		/// </summary>
		public static Func<RootViewController> ViewControllerGenerator { get; set; }

		partial void InitPlatform()
		{
			var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled | NSWindowStyle.Miniaturizable;

			var preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
			if (preferredWindowSize != Windows.Foundation.Size.Empty)
			{
				var rect = new CoreGraphics.CGRect(100, 100, (int)preferredWindowSize.Width, (int)preferredWindowSize.Height);
				_window = new Uno.UI.Controls.Window(rect, style, NSBackingStore.Buffered, false);
			}
			else
			{
				var rect = new CoreGraphics.CGRect(100, 100, 1024, 768);
				_window = new Uno.UI.Controls.Window(rect, style, NSBackingStore.Buffered, false);
			}

			_mainController = ViewControllerGenerator?.Invoke() ?? new RootViewController();

			ObserveOrientationAndSize();

			Dispatcher = CoreDispatcher.Main;
			CoreWindow = CoreWindow.GetOrCreateForCurrentThread();
			CoreWindow.SetWindow(_window);
		}

		internal NSWindow NativeWindow => _window;

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

		partial void ShowPartial()
		{
			_window.ContentViewController = _mainController;
			_window.Display();
			_window.MakeKeyAndOrderFront(NSApplication.SharedApplication);
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
	}
}
