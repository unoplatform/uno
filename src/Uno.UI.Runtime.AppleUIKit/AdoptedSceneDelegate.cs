#nullable enable

using System;
using Foundation;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// Scene delegate Uno assigns to the application scene in <see cref="AppleUIKitLifecycleMode.AdoptedScene"/> mode,
/// where the app declares no scene manifest.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AdoptedSceneBinder"/> hosts the app's window in the scene. The callbacks UIKit sends to the scene rather
/// than to the application delegate under the scene lifecycle are forwarded to the application delegate, as UIKit
/// sends them under the app lifecycle: its overrides keep working, and the base implementations in
/// <see cref="UnoUIApplicationDelegate"/> report activations to AppInstance.
/// </para>
/// <para>
/// UIKit restores a scene session with the delegate class it was saved with, so the Objective-C name must not change.
/// Uno 6.8 registers its scene delegate under the same name.
/// </para>
/// </remarks>
[Register("UnoAdoptedSceneDelegate")]
internal sealed class AdoptedSceneDelegate : UIWindowSceneDelegate
{
	private static readonly Selector _openUrl = new("application:openURL:options:");
	private static readonly Selector _willContinueUserActivity = new("application:willContinueUserActivityWithType:");
	private static readonly Selector _continueUserActivity = new("application:continueUserActivity:restorationHandler:");
	private static readonly Selector _didFailToContinueUserActivity = new("application:didFailToContinueUserActivityWithType:error:");
	private static readonly Selector _didUpdateUserActivity = new("application:didUpdateUserActivity:");
	private static readonly Selector _didBecomeActive = new("applicationDidBecomeActive:");
	private static readonly Selector _willResignActive = new("applicationWillResignActive:");
	private static readonly Selector _didEnterBackground = new("applicationDidEnterBackground:");
	private static readonly Selector _willEnterForeground = new("applicationWillEnterForeground:");
#if !__TVOS__
	private static readonly Selector _performActionForShortcutItem = new("application:performActionForShortcutItem:completionHandler:");
#endif

	private enum ForwardResult
	{
		NotImplemented,
		Forwarded,
		Failed,
	}

	public override UIWindow? Window { get; set; }

	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (scene is UIWindowScene windowScene)
		{
			NativeCallbackGuard.Run(() => AdoptedSceneBinder.Connect(windowScene));
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Ignoring a non-window scene (role: {session.DescribeRole()}).");
		}

		// Once the window is hosted, so that app code failing on the activation cannot leave the app without it.
		NativeCallbackGuard.Run(() => ReportConnectionActivation(connectionOptions));
	}

	public override void DidDisconnect(UIScene scene)
		=> NativeCallbackGuard.Run(() => AdoptedSceneBinder.Disconnect(scene));

	// The application delegate gets each of these before Uno handles them, as under the app lifecycle, where UIKit
	// sends the delegate message before the notification Uno observes.

	public override void WillEnterForeground(UIScene scene)
	{
		if (!AdoptedSceneBinder.IsAppScene(scene))
		{
			return;
		}

		// The scene also enters the foreground as it connects at launch, which the app lifecycle doesn't report.
		if (AdoptedSceneBinder.IsInBackground)
		{
			Forward(_willEnterForeground, static d => UIApplicationDelegate_Extensions.WillEnterForeground(d, UIApplication.SharedApplication));
		}

		NativeCallbackGuard.Run(AdoptedSceneBinder.OnEnteredForeground);
	}

	public override void DidEnterBackground(UIScene scene)
	{
		if (!AdoptedSceneBinder.IsAppScene(scene))
		{
			return;
		}

		Forward(_didEnterBackground, static d => UIApplicationDelegate_Extensions.DidEnterBackground(d, UIApplication.SharedApplication));
		NativeCallbackGuard.Run(AdoptedSceneBinder.OnEnteredBackground);
	}

	public override void DidBecomeActive(UIScene scene)
	{
		if (!AdoptedSceneBinder.IsAppScene(scene))
		{
			return;
		}

		Forward(_didBecomeActive, static d => UIApplicationDelegate_Extensions.OnActivated(d, UIApplication.SharedApplication));
		NativeCallbackGuard.Run(static () => AdoptedSceneBinder.OnActivationChanged(CoreWindowActivationState.CodeActivated));
	}

	public override void WillResignActive(UIScene scene)
	{
		if (!AdoptedSceneBinder.IsAppScene(scene))
		{
			return;
		}

		Forward(_willResignActive, static d => UIApplicationDelegate_Extensions.OnResignActivation(d, UIApplication.SharedApplication));
		NativeCallbackGuard.Run(static () => AdoptedSceneBinder.OnActivationChanged(CoreWindowActivationState.Deactivated));
	}

	public override void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
	{
		foreach (var context in urlContexts)
		{
			ReportUrl(context, AppleUIKitActivation.CurrentExecutionState);
		}
	}

	public override void WillContinueUserActivity(UIScene scene, string userActivityType)
		=> Forward(_willContinueUserActivity, d => UIApplicationDelegate_Extensions.WillContinueUserActivity(d, UIApplication.SharedApplication, userActivityType));

	public override void ContinueUserActivity(UIScene scene, NSUserActivity userActivity)
		=> ReportUserActivity(userActivity, AppleUIKitActivation.CurrentExecutionState);

	public override void DidFailToContinueUserActivity(UIScene scene, string userActivityType, NSError error)
		=> Forward(_didFailToContinueUserActivity, d => UIApplicationDelegate_Extensions.DidFailToContinueUserActivity(d, UIApplication.SharedApplication, userActivityType, error));

	public override void DidUpdateUserActivity(UIScene scene, NSUserActivity userActivity)
		=> Forward(_didUpdateUserActivity, d => UIApplicationDelegate_Extensions.UserActivityUpdated(d, UIApplication.SharedApplication, userActivity));

#if !__TVOS__
	public override void PerformAction(UIWindowScene windowScene, UIApplicationShortcutItem shortcutItem, Action<bool> completionHandler)
	{
		var isCompleted = false;

		// UIKit waits on this whatever the app's handler did, and it must run once.
		ReportShortcut(shortcutItem, succeeded =>
		{
			if (!isCompleted)
			{
				isCompleted = true;
				completionHandler?.Invoke(succeeded);
			}
		});
	}

	public override void DidUpdateCoordinateSpace(
		UIWindowScene windowScene,
		IUICoordinateSpace previousCoordinateSpace,
		UIInterfaceOrientation previousInterfaceOrientation,
		UITraitCollection previousTraitCollection)
		=> NativeCallbackGuard.Run(() => AdoptedSceneBinder.OnCoordinateSpaceChanged(windowScene));
#endif

	/// <summary>
	/// Reports the activation the scene connects with, which is how the scene lifecycle delivers what the app lifecycle
	/// puts in <c>FinishedLaunching</c>'s launch options.
	/// </summary>
	private static void ReportConnectionActivation(UISceneConnectionOptions connectionOptions)
	{
		try
		{
			var previousExecutionState = AppleUIKitActivation.CurrentExecutionState;

			if (connectionOptions.UrlContexts is { Count: > 0 } urlContexts)
			{
				foreach (var context in urlContexts)
				{
					ReportUrl(context, previousExecutionState);
				}

				return;
			}

#if !__TVOS__
			if (connectionOptions.ShortcutItem is { } shortcutItem)
			{
				ReportShortcut(shortcutItem, static _ => { });
				return;
			}
#endif

			if (connectionOptions.UserActivities is { } activities)
			{
				foreach (var activity in activities)
				{
					if (ReportUserActivity(activity, previousExecutionState))
					{
						return;
					}
				}
			}
		}
		finally
		{
			// Whether the scene carried an activation or not: the application delegate it went to may have handled it
			// without reporting it, and anything arriving later is a further activation.
			AppleUIKitActivation.MarkStartupActivationSettled();
		}
	}

	private static void ReportUrl(UIOpenUrlContext context, ApplicationExecutionState previousExecutionState)
	{
		var result = Forward(_openUrl, d =>
		{
			var options = new UIApplicationOpenUrlOptions
			{
				SourceApplication = context.Options.SourceApplication,
				Annotation = context.Options.Annotation,
				OpenInPlace = context.Options.OpenInPlace,
			};

			UIApplicationDelegate_Extensions.OpenUrl(d, UIApplication.SharedApplication, context.Url, options.Dictionary);
		});

		if (result == ForwardResult.NotImplemented)
		{
			NativeCallbackGuard.Run(() => AppleUIKitActivation.TryReportUrl(context.Url, previousExecutionState));
		}
	}

	/// <returns>Whether the activity was handled.</returns>
	private static bool ReportUserActivity(NSUserActivity userActivity, ApplicationExecutionState previousExecutionState)
	{
		var handled = false;
		var result = Forward(_continueUserActivity, d => handled = UIApplicationDelegate_Extensions.ContinueUserActivity(
			d,
			UIApplication.SharedApplication,
			userActivity,
			restorableObjects =>
			{
				foreach (var restorable in restorableObjects ?? [])
				{
					(restorable as UIResponder)?.RestoreUserActivityState(userActivity);
				}
			}));

		if (result == ForwardResult.NotImplemented)
		{
			NativeCallbackGuard.Run(() => handled = AppleUIKitActivation.TryReportUserActivity(userActivity, previousExecutionState));
		}

		return handled;
	}

#if !__TVOS__
	private static void ReportShortcut(UIApplicationShortcutItem shortcutItem, Action<bool> complete)
	{
		var result = Forward(
			_performActionForShortcutItem,
			d => UIApplicationDelegate_Extensions.PerformActionForShortcutItem(d, UIApplication.SharedApplication, shortcutItem, succeeded => complete(succeeded)));

		// Once forwarded, the application delegate completes it, as under the app lifecycle.
		if (result == ForwardResult.NotImplemented)
		{
			NativeCallbackGuard.Run(() => AppleUIKitActivation.ReportShortcut(shortcutItem));
			complete(true);
		}
		else if (result == ForwardResult.Failed)
		{
			complete(false);
		}
	}
#endif

	/// <summary>
	/// Sends <paramref name="selector"/> to the application delegate if it implements it, as UIKit does under the app
	/// lifecycle.
	/// </summary>
	/// <remarks>
	/// <paramref name="forward"/> must call the <see cref="UIApplicationDelegate_Extensions"/> methods, which send the
	/// message and so reach an override, rather than the <see cref="IUIApplicationDelegate"/> members. It runs guarded on
	/// its own, so that an override that throws doesn't skip the handling that follows.
	/// </remarks>
	private static ForwardResult Forward(Selector selector, Action<IUIApplicationDelegate> forward)
	{
		if (UIApplication.SharedApplication.Delegate is not NSObject applicationDelegate
			|| !applicationDelegate.RespondsToSelector(selector))
		{
			return ForwardResult.NotImplemented;
		}

		return NativeCallbackGuard.Run(() => forward((IUIApplicationDelegate)applicationDelegate), selector.Name)
			? ForwardResult.Forwarded
			: ForwardResult.Failed;
	}
}
