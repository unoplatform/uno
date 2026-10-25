using System;
using System.Transactions;
using Foundation;
using Microsoft.UI.Xaml;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
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

		// After Start, which configures the app's logging.
		UnoUISceneDelegate.LogLifeCycle();

		return true;
	}

	// Implementing this opts the app into the scene life cycle even without a scene manifest.
	public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
		=> UnoUISceneDelegate.GetConfiguration(connectingSceneSession);

	public override bool RespondsToSelector(Selector? sel)
		=> !UnoUISceneDelegate.IsHiddenSelector(sel) && base.RespondsToSelector(sel);

	private void SubscribeBackgroundNotifications()
	{
		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			ObserveScene(UIScene.DidEnterBackgroundNotification, OnEnteredBackground);
			ObserveScene(UIScene.WillEnterForegroundNotification, OnLeavingBackground);
			ObserveScene(UIScene.DidActivateNotification, OnActivated);
			ObserveScene(UIScene.WillDeactivateNotification, OnDeactivated);
		}
		else
		{
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnEnteredBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnLeavingBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, OnActivated);
			NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, OnDeactivated);
		}
	}

	private static void ObserveScene(NSString name, Action<NSNotification> handler)
		=> NSNotificationCenter.DefaultCenter.AddObserver(name, notification =>
		{
			// Only the scene hosting the app's window drives it, not another of the app's scenes such as an external display.
			if (UnoUISceneDelegate.IsNotificationForWindow(notification, NativeWindowWrapper.Instance?.NativeWindow))
			{
				handler(notification);
			}
		});

	private void OnEnteredBackground(NSNotification notification)
	{
		NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(false);

		Application.Current?.RaiseEnteredBackground(() => Application.Current.RaiseSuspending());
	}

	private void OnLeavingBackground(NSNotification notification)
	{
		this.LogDebug()?.LogDebug($"Application leaving background");
		Application.Current?.RaiseResuming();
		Application.Current?.RaiseLeavingBackground(() => NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(true));
	}

	private void OnActivated(NSNotification notification)
	{
		this.LogDebug()?.LogDebug($"Application activated");
		NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.CodeActivated);
	}

	private void OnDeactivated(NSNotification notification)
	{
		NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.Deactivated);
	}
}
