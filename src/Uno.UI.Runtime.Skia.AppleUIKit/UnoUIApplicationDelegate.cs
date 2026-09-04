#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

[Register("UnoUIApplicationDelegate")]
public partial class UnoUIApplicationDelegate : UIApplicationDelegate
{
	private bool _preventSecondaryActivationHandling;

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
		// Under the scene lifecycle UIKit delivers the launch activation through
		// UISceneConnectionOptions instead, so these keys are never present.
		if (launchOptions != null && !UnoUISceneDelegate.HasSceneManifest)
		{
			if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUrlKey, out var urlObject))
			{
				_preventSecondaryActivationHandling = true;
				NativeCallbackGuard.Run(() => AppleUIKitActivation.TryReportUrl(urlObject as NSUrl, ApplicationExecutionState.NotRunning));
			}
#if !__TVOS__
			else if (launchOptions.TryGetValue(UIApplication.LaunchOptionsShortcutItemKey, out var shortcutItemObject)
				&& shortcutItemObject is UIApplicationShortcutItem shortcutItem)
			{
				_preventSecondaryActivationHandling = true;
				NativeCallbackGuard.Run(() => AppleUIKitActivation.ReportShortcut(shortcutItem));
			}
#endif
			else if (TryGetUserActivityFromLaunchOptions(launchOptions, out var userActivity))
			{
				_preventSecondaryActivationHandling =
					AppleUIKitActivation.TryReportUserActivity(userActivity, ApplicationExecutionState.NotRunning);
			}
		}

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

	private static void Guarded(Action action) => NativeCallbackGuard.Run(action);

	public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
	{
		var handled = false;
		NativeCallbackGuard.Run(() => handled = TryHandleUniversalLinkFromUserActivity(userActivity));
		return handled;
	}

	public override void UserActivityUpdated(UIApplication application, NSUserActivity userActivity) =>
		NativeCallbackGuard.Run(() => TryHandleUniversalLinkFromUserActivity(userActivity));

	public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
	{
		var handled = true;

		// UIKit calls this straight after a URL cold start too, where FinishedLaunching already
		// reported the activation from launchOptions.
		if (!_preventSecondaryActivationHandling)
		{
			NativeCallbackGuard.Run(() => handled = AppleUIKitActivation.TryReportUrl(url, AppleUIKitActivation.CurrentExecutionState));
		}

		_preventSecondaryActivationHandling = false;
		return handled;
	}

#if !__TVOS__
	public override void PerformActionForShortcutItem(UIApplication application, UIApplicationShortcutItem shortcutItem, UIOperationHandler completionHandler)
	{
		try
		{
			// Not called for a cold start from a shortcut - FinishedLaunching handles that one.
			NativeCallbackGuard.Run(() => AppleUIKitActivation.ReportShortcut(shortcutItem));
		}
		finally
		{
			// UIKit waits on this regardless of what the app's handler did.
			completionHandler?.Invoke(true);
		}
	}
#endif

	/// <summary>
	/// This method enables UI Tests to get the output path
	/// of the current application, in the context of the simulator.
	/// </summary>
	/// <returns>The host path to get the container</returns>
	[Export("getApplicationDataPath")]
	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	public NSString GetWorkingFolder() => new NSString(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

	private bool TryHandleUniversalLinkFromUserActivity(NSUserActivity userActivity)
	{
		// UIKit calls this straight after a universal-link cold start too, where FinishedLaunching
		// already reported the activation from launchOptions.
		if (_preventSecondaryActivationHandling)
		{
			_preventSecondaryActivationHandling = false;
			return true;
		}

		return AppleUIKitActivation.TryReportUserActivity(userActivity, AppleUIKitActivation.CurrentExecutionState);
	}

	private bool TryGetUserActivityFromLaunchOptions(NSDictionary launchOptions, out NSUserActivity? userActivity)
	{
		userActivity = null;

		if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUserActivityDictionaryKey, out var userActivityObject) &&
			userActivityObject is NSDictionary userActivityDictionary)
		{
			userActivity = userActivityDictionary.Values.OfType<NSUserActivity>().FirstOrDefault();
		}

		return userActivity != null;
	}
}
