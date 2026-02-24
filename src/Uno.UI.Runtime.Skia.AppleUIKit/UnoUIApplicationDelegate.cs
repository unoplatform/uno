using System;
using Foundation;
using Microsoft.UI.Xaml;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

[Register("UnoUIApplicationDelegate")]
public partial class UnoUIApplicationDelegate : UIApplicationDelegate
{
	public UnoUIApplicationDelegate()
	{
		SubscribeBackgroundNotifications();
	}

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		this.LogDebug()?.LogDebug($"Application finished launching");
		Application.Start(AppleUIKitHost.CreateAppAction);

		return true;
	}

	public override bool RespondsToSelector(Selector? sel)
	{
		// if the app is not a multi-window app, then we cannot override the GetConfiguration method
		if (sel?.Name == UnoUISceneDelegate.GetConfigurationSelectorName && !UnoUISceneDelegate.HasSceneManifest())
		{
			return false;
		}

		return base.RespondsToSelector(sel);
	}

	public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options) =>
		new(UnoUISceneDelegate.UnoSceneConfigurationKey, connectingSceneSession.Role);

	private void SubscribeBackgroundNotifications()
	{
		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.DidEnterBackgroundNotification, OnEnteredBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.WillEnterForegroundNotification, OnLeavingBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.DidActivateNotification, OnActivated);
			NSNotificationCenter.DefaultCenter.AddObserver(UIScene.WillDeactivateNotification, OnDeactivated);
		}
		else
		{
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnEnteredBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnLeavingBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, OnActivated);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, OnDeactivated);
		}
	}

	// For non-scene-manifest apps, these handlers manage app lifecycle (same as master).
	// For scene-manifest apps, per-window handlers in NativeWindowWrapper track visible
	// window count and raise app events on last background / first foreground.

	private void OnEnteredBackground(NSNotification notification)
	{
		if (!UnoUISceneDelegate.HasSceneManifest())
		{
			Application.Current?.RaiseEnteredBackground(() => Application.Current?.RaiseSuspending());
		}
	}

	private void OnLeavingBackground(NSNotification notification)
	{
		this.LogDebug()?.LogDebug($"Application leaving background");
		if (!UnoUISceneDelegate.HasSceneManifest())
		{
			Application.Current?.RaiseResuming();
			Application.Current?.RaiseLeavingBackground(null);
		}
	}

	private void OnActivated(NSNotification notification)
	{
		this.LogDebug()?.LogDebug($"Application activated");
	}

	private void OnDeactivated(NSNotification notification)
	{
	}
}
