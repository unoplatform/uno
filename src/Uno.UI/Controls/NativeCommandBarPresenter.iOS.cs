#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using System.IO;
using Windows.UI.ViewManagement;
using Uno.Disposables;
using Windows.Graphics.Display;
using UIKit;

namespace Uno.UI.Controls
{
	public partial class NativeCommandBarPresenter : ContentPresenter
	{
		private readonly SerialDisposable _statusBarSubscription = new SerialDisposable();
		private readonly SerialDisposable _orientationSubscription = new SerialDisposable();

		// See https://ivomynttinen.com/blog/ios-design-guidelines#nav-bar
		private static readonly double _defaultCommandBarHeight = 44;
		private static readonly double _landscapeSmallPhoneCommandBarHeight = 32;
		private static readonly double _iPad12CommandBarHeight = 50;

		protected override void OnLoaded()
		{
			base.OnLoaded();

			// TODO: Find a proper way to decide whether a CommandBar exists on canvas (within Page), or is mapped to the UINavigationController's NavigationBar.

			var commandBar = TemplatedParent as CommandBar;
			var navigationBar = commandBar?.GetRenderer(() => new CommandBarRenderer(commandBar)).Native;
			if (navigationBar.Superview == null) // Prevents the UINavigationController's NavigationBar instance from being moved to the Page
			{
				Content = navigationBar;
			}

			this.Height = GetCommandBarHeight();

			var statusBar = StatusBar.GetForCurrentView();

			statusBar.Showing += OnStatusBarChanged;
			statusBar.Hiding += OnStatusBarChanged;
			_statusBarSubscription.Disposable = Disposable.Create(() =>
			{
				statusBar.Showing -= OnStatusBarChanged;
				statusBar.Hiding -= OnStatusBarChanged;
			});

			// iOS doesn't automatically update the navigation bar position when the status bar visibility changes.
			void OnStatusBarChanged(StatusBar sender, object args)
			{
				navigationBar.SetNeedsLayout();
				navigationBar.Superview.SetNeedsLayout();
			}
			DisplayInformation.GetForCurrentView().OrientationChanged += OrientationChanged;
			_orientationSubscription.Disposable = Disposable.Create(() =>
			{
				DisplayInformation.GetForCurrentView().OrientationChanged -= OrientationChanged;
			});

			void OrientationChanged(DisplayInformation displayInformation, object args)
			{
				this.Height = GetCommandBarHeight();
			}
		}

		private double GetCommandBarHeight()
		{
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
			{
				// For phones with a screen size of 4.7 inches or less, the navigation bar size is different in portrait and landscape mode
				// Navigation bar height depending of device : https://kapeli.com/cheat_sheets/iOS_Design.docset/Contents/Resources/Documents/index
				// Devices screen specifications : https://www.idev101.com/code/User_Interface/sizes.html
				var bounds = UIScreen.MainScreen.Bounds;
				var height = Math.Max(bounds.Width, bounds.Height);
				var width = Math.Min(bounds.Width, bounds.Height);
				var isPhoneSmallScreen = height <= 667 && width <= 375;

				if (isPhoneSmallScreen || UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
				{
					var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

					switch (orientation)
					{
						case DisplayOrientations.Landscape:
						case DisplayOrientations.LandscapeFlipped:
							return _landscapeSmallPhoneCommandBarHeight;
						case DisplayOrientations.Portrait:
						case DisplayOrientations.PortraitFlipped:
						case DisplayOrientations.None:
						default:
							return _defaultCommandBarHeight;
					}
				}
				else
				{
					return _defaultCommandBarHeight;
				}
			}
			else
			{
				if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
				{
					return _iPad12CommandBarHeight;
				}
				else
				{
					return _defaultCommandBarHeight;
				}
			}
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_statusBarSubscription.Disposable = null;
			_orientationSubscription.Disposable = null;
		}
	}
}
#endif
