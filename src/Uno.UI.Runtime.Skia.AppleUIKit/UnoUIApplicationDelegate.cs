#nullable enable

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
		// Under the scene lifecycle each window tracks its own state through UnoUISceneDelegate,
		// which also aggregates the app-level events.
		if (!UnoUISceneDelegate.HasSceneManifest)
		{
			SubscribeBackgroundNotifications();
		}
	}

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		this.LogDebug()?.LogDebug($"Application finished launching");
		Application.Start(AppleUIKitHost.CreateAppAction);

		return true;
	}

	public override bool RespondsToSelector(Selector? sel)
	{
		// Apps without a scene manifest must not appear to support scene configuration, otherwise
		// UIKit switches them to the scene lifecycle.
		if (!UnoUISceneDelegate.HasSceneManifest &&
			sel?.Name == UnoUISceneDelegate.GetConfigurationSelectorName)
		{
			return false;
		}

		return base.RespondsToSelector(sel);
	}

	public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
	{
		// Honour the configuration UIKit resolved from the app's scene manifest, so apps declaring
		// their own configuration name and delegate class keep working.
		var configuration = connectingSceneSession.Configuration;

		return configuration is not null
			? configuration
			: new UISceneConfiguration(UnoUISceneDelegate.UnoSceneConfigurationKey, connectingSceneSession.Role);
	}

	private void SubscribeBackgroundNotifications()
	{
		NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnEnteredBackground);
		NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnLeavingBackground);
		NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, OnActivated);
		NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, OnDeactivated);
	}

	private void OnEnteredBackground(NSNotification notification) =>
		Guarded(() =>
		{
			NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(false);
			Application.Current?.RaiseEnteredBackground(() => Application.Current?.RaiseSuspending());
		});

	private void OnLeavingBackground(NSNotification notification) =>
		Guarded(() =>
		{
			this.LogDebug()?.LogDebug($"Application leaving background");
			Application.Current?.RaiseResuming();
			Application.Current?.RaiseLeavingBackground(() => NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(true));
		});

	private void OnActivated(NSNotification notification) =>
		Guarded(() =>
		{
			this.LogDebug()?.LogDebug($"Application activated");
			NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.CodeActivated);
		});

	private void OnDeactivated(NSNotification notification) =>
		Guarded(() => NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.Deactivated));

	private static void Guarded(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			// A managed exception must never escape into a native callback.
			Application.Current?.RaiseRecoverableUnhandledException(ex);
		}
	}
}
