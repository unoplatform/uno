using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
﻿using System.Transactions;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
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
		SubscribeBackgroundNotifications();
	}

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		var currentInstance = AppInstance.GetCurrent();
		if (launchOptions != null)
		{
			if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUrlKey, out var urlObject))
			{
				_preventSecondaryActivationHandling = true;
				var url = (NSUrl)urlObject;
				if (TryParseUri(url, out var uri))
				{
					currentInstance.SetActivatedEventArgs(AppActivationArguments.CreateProtocol(new(uri, ApplicationExecutionState.NotRunning)));
				}
			}
#if !__TVOS__
			else if (launchOptions.TryGetValue(UIApplication.LaunchOptionsShortcutItemKey, out var shortcutItemObject))
			{
				_preventSecondaryActivationHandling = true;
				var shortcutItem = (UIApplicationShortcutItem)shortcutItemObject;
				currentInstance.SetActivatedEventArgs(AppActivationArguments.CreateLaunch(new(ActivationKind.Launch, shortcutItem.Type)));
			}
#endif
			else if (
				TryGetUserActivityFromLaunchOptions(launchOptions, out var userActivity) &&
				userActivity.ActivityType == NSUserActivityType.BrowsingWeb)
			{
				_preventSecondaryActivationHandling = true;
				if (TryParseUri(userActivity.WebPageUrl, out var uri))
				{
					currentInstance.SetActivatedEventArgs(AppActivationArguments.CreateProtocol(new(uri, ApplicationExecutionState.NotRunning)));
				}
			}
		}

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

	public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler) =>
		TryHandleUniversalLinkFromUserActivity(userActivity);

	public override void UserActivityUpdated(UIApplication application, NSUserActivity userActivity) =>
		TryHandleUniversalLinkFromUserActivity(userActivity);

	public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
	{
		// If the application was not running, URL was already handled by FinishedLaunching
		if (!_preventSecondaryActivationHandling)
		{
			if (TryParseUri(url, out var uri))
			{
				var args = AppActivationArguments.CreateProtocol(new(uri, ApplicationExecutionState.Running));
				AppInstance.GetCurrent().RaiseActivatedEvent(args);
			}
		}
		_preventSecondaryActivationHandling = false;
		return true;
	}

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
		// If the application was not running, universal link was already handled by FinishedLaunching
		if (_preventSecondaryActivationHandling)
		{
			_preventSecondaryActivationHandling = false;
			return true;
		}

		if (userActivity.ActivityType == NSUserActivityType.BrowsingWeb)
		{
			if (TryParseUri(userActivity.WebPageUrl, out var uri))
			{
				var args = AppActivationArguments.CreateProtocol(new(uri, ApplicationExecutionState.Running));
				AppInstance.GetCurrent().RaiseActivatedEvent(args);
				return true;
			}
		}

		return false;
	}

	private bool TryParseUri(NSUrl? url, [NotNullWhen(true)] out Uri? uri)
	{
		if (url is null)
		{
			uri = null;
			return false;
		}

		if (Uri.TryCreate(url.ToString(), UriKind.Absolute, out uri))
		{
			return true;
		}
		else
		{
			this.Log().LogError($"Activation URI {url} could not be parsed");
			return false;
		}
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
