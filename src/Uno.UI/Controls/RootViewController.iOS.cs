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
		internal event Action VisibleBoundsChanged;

		public RootViewController()
		{
			// Dismiss on device rotation: this reproduces the windows behavior
			UIApplication.Notifications
				.ObserveDidChangeStatusBarOrientation((sender, args) => VisualTreeHelper.CloseAllPopups());

			// Dismiss when the app is entering background
			UIApplication.Notifications
				.ObserveWillResignActive((sender, args) => VisualTreeHelper.CloseAllPopups());
		}

		// This will handle when the status bar is showed / hidden by the system on iPhones
		public override void ViewSafeAreaInsetsDidChange()
		{
			base.ViewSafeAreaInsetsDidChange();
			VisibleBoundsChanged?.Invoke();
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
		{
			return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
		}

		public bool CanAutorotate { get; set; } = true;

		public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();
	}
}
