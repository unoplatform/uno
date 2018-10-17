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

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private Uno.UI.Controls.Window _window;

		private static Window _current;
		private RootViewController _mainController;
		private UIElement _content;

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

			ObserveOrientationAndSize();

			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow(_window);
		}

		private void ObserveOrientationAndSize()
		{
			//_window.FrameChanged +=
			//	() => RaiseNativeSizeChanged(ViewHelper.GetScreenSize());

			var statusBar = StatusBar.GetForCurrentView();
			statusBar.Showing += (o, e) => UpdateCoreBounds();
			statusBar.Hiding += (o, e) => UpdateCoreBounds();

			RaiseNativeSizeChanged(ViewHelper.GetScreenSize());
		}

		private void InternalActivate()
		{
			_window.ContentViewController = _mainController;
			_window.MakeKeyWindow();
		}

		private void InternalSetContent(UIElement value)
		{
			_content?.RemoveFromSuperview();

			_content = value;
			_mainController.View.AddSubview(value);
			value.Frame = _mainController.View.Bounds;
			value.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		}

		private UIElement InternalGetContent() => _content;

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
					new WindowSizeChangedEventArgs(
						new Windows.Foundation.Size((float)size.Width, (float)size.Height)
					)
				);
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
