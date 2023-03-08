using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics.Display;
using Foundation;
using UIKit;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Windows.Devices.Sensors;
using CoreGraphics;
using ObjCRuntime;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if !NET6_0_OR_GREATER
using NativeHandle = System.IntPtr;
#endif

namespace Uno.UI.Controls
{
	public class RootViewController : UINavigationController, IRotationAwareViewController
	{
		internal event Action VisibleBoundsChanged;

		public RootViewController()
		{
			Initialize();
		}

		public RootViewController(UIViewController rootViewController)
			: base(rootViewController)
		{
			Initialize();
		}

		public RootViewController(NSObjectFlag t)
			: base(t)
		{
			Initialize();
		}

		public RootViewController(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}

		public RootViewController(NativeHandle handle)
			: base(handle)
		{
			Initialize();
		}

		private void Initialize()
		{
			// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #8341.

			// Dismiss on device rotation: this reproduces the windows behavior
			UIApplication.Notifications
				.ObserveDidChangeStatusBarOrientation((sender, args) =>
					VisualTreeHelper.CloseAllPopups(WinUICoreServices.Instance.ContentRootCoordinator.CoreWindowContentRoot.XamlRoot));

			// Dismiss when the app is entering background
			UIApplication.Notifications
				.ObserveWillResignActive((sender, args) =>
					VisualTreeHelper.CloseAllPopups(WinUICoreServices.Instance.ContentRootCoordinator.CoreWindowContentRoot.XamlRoot));
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

		public override void MotionEnded(UIEventSubtype motion, UIEvent evt)
		{
			if (motion == UIEventSubtype.MotionShake)
			{
				Accelerometer.HandleShake();
			}
			base.MotionEnded(motion, evt);
		}

		public bool CanAutorotate { get; set; } = true;

		public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();

		public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
		{
			base.TraitCollectionDidChange(previousTraitCollection);
			Windows.UI.Xaml.Application.Current.OnSystemThemeChanged();
		}
	}
}
