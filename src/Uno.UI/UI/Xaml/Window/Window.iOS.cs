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
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml
{
	public sealed partial class Window
	{
		private Uno.UI.Controls.Window _nativeWindow;

		private Border _rootBorder;
		private RootViewController _mainController;
		private NSObject _orientationRegistration;

		/// <summary>
		/// A function to generate a custom view controller which inherits from <see cref="RootViewController"/>.
		/// This must be set before the <see cref="Window"/> is created (typically when <see cref="Window.Current"/> is called for the first time),
		/// otherwise it will have no effect.
		/// </summary>
		public static Func<RootViewController> ViewControllerGenerator { get; set; }

		partial void InitPlatform()
		{
			_nativeWindow = new Uno.UI.Controls.Window();

			_mainController = ViewControllerGenerator?.Invoke() ?? new RootViewController();
			_mainController.View.BackgroundColor = UIColor.Clear;
			_mainController.NavigationBarHidden = true;

			ObserveOrientationAndSize();

			CoreWindow = new CoreWindow(_nativeWindow);

			_nativeWindow.SetOwner(CoreWindow);
		}

		internal Uno.UI.Controls.Window NativeWindow => _nativeWindow;

		private void ObserveOrientationAndSize()
		{
			_orientationRegistration = UIApplication
				.Notifications
				.ObserveDidChangeStatusBarOrientation(
					(sender, args) => RaiseNativeSizeChanged(ViewHelper.GetMainWindowSize())
				);

			_orientationRegistration = UIApplication
				.Notifications
				.ObserveDidChangeStatusBarFrame(
					(sender, args) => RaiseNativeSizeChanged(ViewHelper.GetMainWindowSize())
				);

			_nativeWindow.FrameChanged +=
				() => RaiseNativeSizeChanged(ViewHelper.GetMainWindowSize());

			_mainController.VisibleBoundsChanged +=
				() => RaiseNativeSizeChanged(ViewHelper.GetMainWindowSize());

			var statusBar = StatusBar.GetForCurrentView();
			statusBar.Showing += (o, e) => RaiseNativeSizeChanged(ViewHelper.GetMainWindowSize());
			statusBar.Hiding += (o, e) => RaiseNativeSizeChanged(ViewHelper.GetMainWindowSize());

			RaiseNativeSizeChanged(ViewHelper.GetWindowSize(this));
		}

		partial void ShowPartial()
		{
			_nativeWindow.RootViewController = _mainController;
			_nativeWindow.MakeKeyAndVisible();
		}

		internal void RaiseNativeSizeChanged(CGSize size)
		{
			var newBounds = new Rect(0, 0, size.Width, size.Height);

			ApplicationView.GetForCurrentView()?.SetVisibleBounds(_nativeWindow, newBounds);

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
				FullWindowMediaRoot.Child = null;
				_rootBorder.Opacity = 1;
				FullWindowMediaRoot.Visibility = Visibility.Collapsed;
			}
			else
			{
				FullWindowMediaRoot.Visibility = Visibility.Visible;
				_rootBorder.Opacity = 0;
				FullWindowMediaRoot.Child = element;
			}
		}
	}
}
