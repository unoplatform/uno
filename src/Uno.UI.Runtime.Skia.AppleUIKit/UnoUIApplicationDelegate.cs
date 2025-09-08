using System.Transactions;
using Foundation;
using Microsoft.UI.Xaml;
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
