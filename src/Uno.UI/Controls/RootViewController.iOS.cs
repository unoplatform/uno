using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics.Display;
using UIKit;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Uno.UI.Controls
{
    public class RootViewController : UINavigationController, IRotationAwareViewController
    {
		public RootViewController()
		{
			// Dismiss on device rotation: this reproduces the windows behavior
			UIApplication.Notifications
				.ObserveDidChangeStatusBarOrientation((sender, args) => VisualTreeHelper.CloseAllPopups());

			// Dismiss when the app is entering background
			UIApplication.Notifications
				.ObserveWillResignActive((sender, args) => VisualTreeHelper.CloseAllPopups());
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
		{
			return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
		}

		public bool CanAutorotate { get; set; } = true;

		public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();
	}
}
