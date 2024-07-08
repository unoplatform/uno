using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics.Display;
using Foundation;
using UIKit;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Windows.Devices.Sensors;
using CoreGraphics;
using ObjCRuntime;
using Uno.Helpers.Theming;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Controls;

public class SkiaViewController : UINavigationController, IRotationAwareViewController
{
	internal event Action? VisibleBoundsChanged;

	public SkiaViewController()
	{
		Initialize();
	}

	public SkiaViewController(UIViewController rootViewController)
		: base(rootViewController)
	{
		Initialize();
	}

	public SkiaViewController(NSObjectFlag t)
		: base(t)
	{
		Initialize();
	}

	public SkiaViewController(NSCoder coder)
		: base(coder)
	{
		Initialize();
	}

	public SkiaViewController(NativeHandle handle)
		: base(handle)
	{
		Initialize();
	}

	private void Initialize()
	{
		// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #13847.

		// Dismiss on device rotation: this reproduces the windows behavior
		UIApplication.Notifications
			.ObserveDidChangeStatusBarOrientation((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));

		// Dismiss when the app is entering background
		UIApplication.Notifications
			.ObserveWillResignActive((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));

		// iOS 17+ only
		if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
		{
			((IUITraitChangeObservable)this).RegisterForTraitChanges<UITraitUserInterfaceStyle>((env, traits) => SystemThemeHelper.RefreshSystemTheme());
		}
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

	public override void MotionEnded(UIEventSubtype motion, UIEvent? evt)
	{
		if (motion == UIEventSubtype.MotionShake)
		{
			Accelerometer.HandleShake();
		}
		base.MotionEnded(motion, evt);
	}

	public bool CanAutorotate { get; set; } = true;

#pragma warning disable CA1422 // Validate platform compatibility
	public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();

	public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
	{
		base.TraitCollectionDidChange(previousTraitCollection);
		SystemThemeHelper.RefreshSystemTheme();
	}
#pragma warning restore CA1422 // Validate platform compatibility
}
