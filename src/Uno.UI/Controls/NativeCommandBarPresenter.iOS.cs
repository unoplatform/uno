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

		private static readonly double _defaultCommandBarHeight = 44;
		private static readonly double _landscapePhoneCommandBarHeight = 32;

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

			// if device is iOS 10+ and a iPhone, we need to adapt the size of the bar based on the orientation
			if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0)
				&& UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
			{
				// Set height based on current orientation and listen to orientation changes
				this.Height = GetCommandBarHeight(DisplayInformation.GetForCurrentView().CurrentOrientation);
				DisplayInformation.GetForCurrentView().OrientationChanged += OrientationChanged;
			}
			else
			{
				// Below iOS 10 or on iPads, status bar is always the same size
				this.Height = _defaultCommandBarHeight;
			}

			_orientationSubscription.Disposable = Disposable.Create(() =>
			{
				DisplayInformation.GetForCurrentView().OrientationChanged -= OrientationChanged;
			});

			void OrientationChanged(DisplayInformation displayInformation, object args)
			{
				this.Height = GetCommandBarHeight(displayInformation.CurrentOrientation);
			}
		}

		private double GetCommandBarHeight(DisplayOrientations orientation)
		{
			switch (orientation)
			{
				case DisplayOrientations.Landscape:
				case DisplayOrientations.LandscapeFlipped:
					return _landscapePhoneCommandBarHeight;
				case DisplayOrientations.Portrait:
				case DisplayOrientations.PortraitFlipped:
				case DisplayOrientations.None:
				default:
					return _defaultCommandBarHeight;
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