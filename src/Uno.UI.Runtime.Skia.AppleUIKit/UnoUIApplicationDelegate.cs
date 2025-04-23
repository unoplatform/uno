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

	private DateTimeOffset GetSuspendingOffset() => DateTimeOffset.Now.AddSeconds(10);

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
