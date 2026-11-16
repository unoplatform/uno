#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.AppleUIKit;

[Register("UnoUIApplicationDelegate")]
public partial class UnoUIApplicationDelegate : UIApplicationDelegate
{
	private static readonly NativeHandle _getConfigurationSelector = Selector.GetHandle(UnoUISceneDelegate.GetConfigurationSelectorName);

	private static Class? _adoptedSceneDelegateClass;
	private static Class? _sceneDelegateClass;

	private bool _preventSecondaryActivationHandling;

	public UnoUIApplicationDelegate()
	{
		// Under the scene lifecycle the scene delegates track each window's state, and the app-level
		// events with it.
		if (!AppleUIKitLifecycle.UsesSceneLifecycle)
		{
			SubscribeBackgroundNotifications();
		}
	}

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		// Under the scene lifecycle UIKit delivers the launch activation through
		// UISceneConnectionOptions instead, so these keys are never present.
		if (launchOptions != null && !AppleUIKitLifecycle.UsesSceneLifecycle)
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

		// After Start, which configures the app's logging.
		AppleUIKitLifecycle.LogMode();

		return true;
	}

	public override bool RespondsToSelector(Selector? sel)
	{
		// Apps staying on the app lifecycle must not appear to support scene configuration, otherwise
		// UIKit switches them to the scene lifecycle.
		if (!AppleUIKitLifecycle.UsesSceneLifecycle &&
			sel?.Handle == _getConfigurationSelector)
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

		try
		{
			if (configuration is null && connectingSceneSession.TryGetRole(out var role))
			{
				configuration = new UISceneConfiguration(UnoUISceneDelegate.UnoSceneConfigurationKey, role);
			}

			// An app without a scene manifest, or whose manifest names no delegate class, declares none. Other roles,
			// such as an external display, get none so that UIKit keeps mirroring them.
			if (configuration is { DelegateClass: null } && connectingSceneSession.IsApplicationRole())
			{
				if (AppleUIKitLifecycle.Mode == AppleUIKitLifecycleMode.AdoptedScene)
				{
					configuration.DelegateClass = _adoptedSceneDelegateClass ??= new Class(typeof(AdoptedSceneDelegate));
				}
				else if (AppleUIKitLifecycle.Mode == AppleUIKitLifecycleMode.ManifestScene)
				{
					configuration.DelegateClass = _sceneDelegateClass ??= new Class(typeof(UnoUISceneDelegate));
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Resolving a scene configuration failed.", ex);
			}

			// UIKit can't do without a configuration, and terminating with the managed exception beats a null one.
			if (configuration is null)
			{
				throw;
			}

			// An exception reaching UIKit would terminate the app.
			Application.Current?.RaiseRecoverableUnhandledException(ex);
		}

		return configuration ?? throw new InvalidOperationException(
			$"UIKit provided no configuration for a scene session of an unknown role ({connectingSceneSession.DescribeRole()}).");
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
			AppleUIKitLifecycle.RaiseAppEnteredBackground();
		});

	private void OnLeavingBackground(NSNotification notification) =>
		Guarded(() =>
		{
			this.LogDebug()?.LogDebug($"Application leaving background");
			AppleUIKitLifecycle.RaiseAppLeavingBackground(() => NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(true));
		});

	private void OnActivated(NSNotification notification) =>
		Guarded(() =>
		{
			this.LogDebug()?.LogDebug($"Application activated");
			NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.CodeActivated);
		});

	private void OnDeactivated(NSNotification notification) =>
		Guarded(() => NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.Deactivated));

	private static void Guarded(Action action, [CallerMemberName] string callback = "") => NativeCallbackGuard.Run(action, callback);

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
