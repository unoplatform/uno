#nullable enable

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Foundation;
using Microsoft.UI.Xaml;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml;

/// <summary>
/// Scene delegate Uno assigns to the application scene, so that apps run the UIKit scene life cycle without
/// declaring a scene manifest. Apps built with the iOS 27 SDK fail to launch without it.
/// </summary>
/// <remarks>
/// <para>
/// It hosts the app's window in the scene, and forwards to the application delegate the callbacks UIKit only
/// sends to scene delegates under the scene life cycle, so that existing application delegates keep working.
/// </para>
/// <para>
/// UIKit restores a scene session with the delegate class it was saved with, so the Objective-C name must not change.
/// Later Uno versions register their scene delegate for apps without a scene manifest under the same name.
/// </para>
/// </remarks>
[Register("UnoAdoptedSceneDelegate")]
internal sealed class UnoUISceneDelegate : UIWindowSceneDelegate
{
	/// <summary>
	/// Info.plist key that turns the scene life cycle on or off, overriding the default picked from the SDK.
	/// </summary>
	private const string UseSceneLifecycleKey = "UnoUseSceneLifecycle";

	private static readonly Selector _getConfiguration = new("application:configurationForConnectingSceneSession:options:");
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

	// Before _isEnabled, which reads them.
	private static readonly bool _hasSceneManifest = GetInfoValue("UIApplicationSceneManifest") is not null;
	private static readonly NSObject? _useSceneLifecycle = GetInfoValue(UseSceneLifecycleKey);
	private static readonly string? _sdkVersion = GetInfoValue("DTPlatformVersion")?.ToString();
	private static readonly bool _isEnabled = ReadIsEnabled();

	private static UIWindow? _appWindow;
	private static UIWindowScene? _pendingScene;
	private static bool _isInBackground;

	private enum ForwardResult
	{
		NotImplemented,
		Forwarded,
		Failed,
	}

	public override UIWindow? Window { get; set; }

	/// <summary>
	/// Gets whether the app runs the scene life cycle, either declared by its own scene manifest or adopted by Uno.
	/// </summary>
	internal static bool UsesSceneLifecycle => _hasSceneManifest || _isEnabled;

	/// <summary>
	/// Gets whether an application delegate must hide <paramref name="selector"/>, as UIKit adopts the scene life
	/// cycle for any application delegate that implements GetConfiguration.
	/// </summary>
	internal static bool IsHiddenSelector(Selector? selector)
		=> !UsesSceneLifecycle && selector is not null && selector.Handle == _getConfiguration.Handle;

	/// <summary>
	/// Logs the life cycle the app runs and what decided it. Called once the app has configured logging.
	/// </summary>
	internal static void LogLifeCycle()
	{
		var log = typeof(UnoUISceneDelegate).Log();

		if (log.IsEnabled(LogLevel.Information))
		{
			var useSceneLifecycle = _useSceneLifecycle switch
			{
				null => "not set",
				NSNumber number => number.BoolValue ? "true" : "false",
				var value => value.ToString(),
			};

			log.Info(
				$"Running the {(UsesSceneLifecycle ? "scene" : "app")} life cycle (scene manifest: {_hasSceneManifest}, " +
				$"{UseSceneLifecycleKey}: {useSceneLifecycle}, SDK version: {_sdkVersion ?? "unknown"}).");
		}

		if (!_hasSceneManifest && log.IsEnabled(LogLevel.Warning))
		{
			if (_useSceneLifecycle is not null and not NSNumber)
			{
				log.Warn($"{UseSceneLifecycleKey} is ignored, as it is neither a boolean nor a number.");
			}
			else if (_useSceneLifecycle is null && !TryGetSdkMajorVersion(out _))
			{
				log.Warn($"The SDK version is unknown, so Uno adopts the scene life cycle. Set {UseSceneLifecycleKey} to choose.");
			}
		}
	}

	/// <summary>
	/// Resolves the configuration of a connecting scene, assigning this delegate to an application scene that
	/// has none. A delegate class declared by the app's own scene manifest is kept.
	/// </summary>
	internal static UISceneConfiguration GetConfiguration(UISceneSession session)
	{
		var configuration = session.Configuration;

		try
		{
			// Other roles, such as an external display, get no delegate so that UIKit keeps mirroring them.
			if (configuration is not null && IsApplicationRole(session))
			{
				if (configuration.DelegateClass is null)
				{
					configuration.DelegateClass = new Class(typeof(UnoUISceneDelegate));
				}
				else if (typeof(UnoUISceneDelegate).Log().IsEnabled(LogLevel.Information))
				{
					typeof(UnoUISceneDelegate).Log().Info(
						$"Keeping the scene delegate {configuration.DelegateClass.Name} declared by the app, which must show the app's window in its scene.");
				}
			}
		}
		catch (Exception e)
		{
			if (typeof(UnoUISceneDelegate).Log().IsEnabled(LogLevel.Error))
			{
				typeof(UnoUISceneDelegate).Log().Error("Resolving a scene configuration failed.", e);
			}

			// An exception reaching UIKit would terminate the app.
			Application.Current?.RaiseRecoverableUnhandledException(e);
		}

		// UIKit can't do without a configuration, and terminating with a managed exception beats returning none.
		return configuration ?? throw new InvalidOperationException("UIKit provided no configuration for the connecting scene.");
	}

	/// <summary>
	/// Hosts the app's window in the application scene, now or once that scene connects.
	/// </summary>
	internal static void SetAppWindow(UIWindow window)
	{
		if (_appWindow is not null)
		{
			return;
		}

		_appWindow = window;

		if (_pendingScene is { } scene)
		{
			_pendingScene = null;
			Host(window, scene);
		}
	}

	/// <summary>
	/// Gets whether a <see cref="UIScene"/> notification concerns the scene hosting <paramref name="window"/>,
	/// rather than another of the app's scenes such as an external display.
	/// </summary>
	internal static bool IsNotificationForWindow(NSNotification notification, UIWindow? window)
	{
		if (!UsesSceneLifecycle || notification.Object is not UIScene scene)
		{
			return true;
		}

		return window?.WindowScene is { } windowScene
			? scene.Handle == windowScene.Handle
			: scene.Session is { } session && IsApplicationRole(session);
	}

	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Scene connecting (window created: {_appWindow is not null}).");
		}

		if (scene is UIWindowScene windowScene)
		{
			Guard(() =>
			{
				if (_appWindow is { } window)
				{
					Host(window, windowScene);
				}
				else
				{
					_pendingScene = windowScene;
				}
			});
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"The application scene is a {scene.GetType().Name}, not a window scene: the window cannot be shown in it.");
		}

		// The app launched from FinishedLaunching, as under the app life cycle, but UIKit now delivers the URL, user
		// activity or shortcut the app was launched with here, so it arrives as an activation into the running app.
		ForwardConnectionOptions(connectionOptions);
	}

	public override void DidDisconnect(UIScene scene)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Scene disconnected, the window is hosted again if the application scene reconnects.");
		}

		if (_pendingScene is { } pendingScene && pendingScene.Handle == scene.Handle)
		{
			_pendingScene = null;
		}

		Window = null;
	}

	public override void DidBecomeActive(UIScene scene)
		=> ForwardToApplicationDelegate(_didBecomeActive, d => UIApplicationDelegate_Extensions.OnActivated(d, UIApplication.SharedApplication));

	public override void WillResignActive(UIScene scene)
		=> ForwardToApplicationDelegate(_willResignActive, d => UIApplicationDelegate_Extensions.OnResignActivation(d, UIApplication.SharedApplication));

	public override void DidEnterBackground(UIScene scene)
	{
		_isInBackground = true;
		ForwardToApplicationDelegate(_didEnterBackground, d => UIApplicationDelegate_Extensions.DidEnterBackground(d, UIApplication.SharedApplication));
	}

	public override void WillEnterForeground(UIScene scene)
	{
		// The scene also enters the foreground as it connects at launch, which the app life cycle doesn't report.
		if (_isInBackground)
		{
			_isInBackground = false;
			ForwardToApplicationDelegate(_willEnterForeground, d => UIApplicationDelegate_Extensions.WillEnterForeground(d, UIApplication.SharedApplication));
		}
	}

	public override void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
	{
		foreach (var context in urlContexts)
		{
			ForwardOpenUrl(context);
		}
	}

	public override void WillContinueUserActivity(UIScene scene, string type)
		=> ForwardToApplicationDelegate(_willContinueUserActivity, d => UIApplicationDelegate_Extensions.WillContinueUserActivity(d, UIApplication.SharedApplication, type));

	public override void ContinueUserActivity(UIScene scene, NSUserActivity userActivity)
		=> ForwardContinueUserActivity(userActivity);

	public override void DidFailToContinueUserActivity(UIScene scene, string type, NSError error)
		=> ForwardToApplicationDelegate(_didFailToContinueUserActivity, d => UIApplicationDelegate_Extensions.DidFailToContinueUserActivity(d, UIApplication.SharedApplication, type, error));

	public override void DidUpdateUserActivity(UIScene scene, NSUserActivity userActivity)
		=> ForwardToApplicationDelegate(_didUpdateUserActivity, d => UIApplicationDelegate_Extensions.UserActivityUpdated(d, UIApplication.SharedApplication, userActivity));

#if !__TVOS__
	public override void PerformAction(UIWindowScene windowScene, UIApplicationShortcutItem shortcutItem, Action<bool> completionHandler)
	{
		var isCompleted = false;

		// UIKit waits on this whatever the app's handler did, and it must run once.
		ForwardShortcut(shortcutItem, succeeded =>
		{
			if (!isCompleted)
			{
				isCompleted = true;
				completionHandler(succeeded);
			}
		});
	}
#endif

	private static void Host(UIWindow window, UIWindowScene scene)
	{
		if (typeof(UnoUISceneDelegate).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(UnoUISceneDelegate).Log().Debug($"Hosting the window in scene {scene.Session?.PersistentIdentifier}.");
		}

		window.WindowScene = scene;
		window.Frame = scene.CoordinateSpace.Bounds;

		if (scene.Delegate is UnoUISceneDelegate sceneDelegate)
		{
			sceneDelegate.Window = window;
		}

		if (!window.Hidden)
		{
			// It was made key and visible before the scene connected, during launch or in a scene that has since
			// disconnected.
			window.MakeKeyAndVisible();
		}
	}

	/// <summary>
	/// Forwards the activation a scene connects with, which the app life cycle passes in the launch options.
	/// </summary>
	private static void ForwardConnectionOptions(UISceneConnectionOptions connectionOptions)
	{
		if (connectionOptions.UrlContexts is { } urlContexts)
		{
			foreach (var context in urlContexts)
			{
				ForwardOpenUrl(context);
			}
		}

		if (connectionOptions.UserActivities is { } userActivities)
		{
			foreach (var userActivity in userActivities)
			{
				ForwardContinueUserActivity(userActivity);
			}
		}

#if !__TVOS__
		if (connectionOptions.ShortcutItem is { } shortcutItem)
		{
			ForwardShortcut(shortcutItem, _ => { });
		}
#endif
	}

	private static void ForwardOpenUrl(UIOpenUrlContext context)
		=> ForwardToApplicationDelegate(_openUrl, d =>
		{
			var options = new UIApplicationOpenUrlOptions
			{
				SourceApplication = context.Options.SourceApplication,
				Annotation = context.Options.Annotation,
				OpenInPlace = context.Options.OpenInPlace,
			};

			UIApplicationDelegate_Extensions.OpenUrl(d, UIApplication.SharedApplication, context.Url, options.Dictionary);
		});

	private static void ForwardContinueUserActivity(NSUserActivity userActivity)
		=> ForwardToApplicationDelegate(_continueUserActivity, d => UIApplicationDelegate_Extensions.ContinueUserActivity(
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

#if !__TVOS__
	private static void ForwardShortcut(UIApplicationShortcutItem shortcutItem, Action<bool> complete)
	{
		var result = ForwardToApplicationDelegate(
			_performActionForShortcutItem,
			d => UIApplicationDelegate_Extensions.PerformActionForShortcutItem(d, UIApplication.SharedApplication, shortcutItem, succeeded => complete(succeeded)));

		// Once forwarded, the application delegate completes it, as under the app life cycle.
		if (result != ForwardResult.Forwarded)
		{
			complete(false);
		}
	}
#endif

	/// <summary>
	/// Sends a message to the application delegate if it implements it, as UIKit does for optional methods.
	/// </summary>
	/// <remarks>
	/// <paramref name="forward"/> must call the <see cref="UIApplicationDelegate_Extensions"/> methods, which send the
	/// message. The <see cref="IUIApplicationDelegate"/> members call the managed methods instead, which miss an
	/// implementation exported under another name or added at runtime by an SDK, and whose base implementations throw.
	/// </remarks>
	private static ForwardResult ForwardToApplicationDelegate(Selector selector, Action<IUIApplicationDelegate> forward)
	{
		if (UIApplication.SharedApplication.Delegate is not NSObject applicationDelegate
			|| !applicationDelegate.RespondsToSelector(selector))
		{
			return ForwardResult.NotImplemented;
		}

		return Guard(() => forward((IUIApplicationDelegate)applicationDelegate), selector.Name)
			? ForwardResult.Forwarded
			: ForwardResult.Failed;
	}

	/// <summary>
	/// Runs work invoked from a UIKit callback, reporting an exception rather than letting it terminate the app.
	/// </summary>
	/// <returns>Whether <paramref name="action"/> completed without throwing.</returns>
	private static bool Guard(Action action, [CallerMemberName] string callback = "")
	{
		try
		{
			action();
			return true;
		}
		catch (Exception e)
		{
			if (typeof(UnoUISceneDelegate).Log().IsEnabled(LogLevel.Error))
			{
				typeof(UnoUISceneDelegate).Log().Error($"The {callback} scene life cycle callback failed.", e);
			}

			Application.Current?.RaiseRecoverableUnhandledException(e);
			return false;
		}
	}

	private static bool IsApplicationRole(UISceneSession session)
	{
		try
		{
			return session.Role == UIWindowSceneSessionRole.Application;
		}
		catch (NotSupportedException e)
		{
			// A role the bindings don't know, introduced by a newer OS.
			if (typeof(UnoUISceneDelegate).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(UnoUISceneDelegate).Log().Debug($"Ignoring a scene with an unknown role: {e.Message}");
			}

			return false;
		}
	}

	private static bool ReadIsEnabled()
	{
		if (_useSceneLifecycle is NSNumber useSceneLifecycle)
		{
			return useSceneLifecycle.BoolValue;
		}

		// UIKit requires the scene life cycle from the version 27 SDKs on, a version iOS, tvOS and Mac Catalyst share.
		// Apps built with an older SDK keep the app life cycle unless they opt in.
		return !TryGetSdkMajorVersion(out var sdkMajorVersion) || sdkMajorVersion >= 27;
	}

	private static bool TryGetSdkMajorVersion(out int sdkMajorVersion)
	{
		sdkMajorVersion = 0;

		return _sdkVersion is not null
			&& int.TryParse(_sdkVersion.Split('.')[0], NumberStyles.None, CultureInfo.InvariantCulture, out sdkMajorVersion);
	}

	private static NSObject? GetInfoValue(string key)
		=> NSBundle.MainBundle.ObjectForInfoDictionary(key);
}
