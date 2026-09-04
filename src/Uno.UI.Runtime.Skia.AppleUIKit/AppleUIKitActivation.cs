#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Foundation;
using Microsoft.Windows.AppLifecycle;
using UIKit;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.Activation;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Translates UIKit activation payloads into <see cref="AppActivationArguments"/> and reports them to
/// <see cref="AppInstance"/>.
/// </summary>
/// <remarks>
/// Shared by <see cref="UnoUIApplicationDelegate"/> and <see cref="UnoUISceneDelegate"/>: UIKit routes
/// the same payloads through app-level or scene-level callbacks depending on whether the app declares a
/// scene manifest, so both delegates must produce identical activation arguments.
/// </remarks>
internal static class AppleUIKitActivation
{
	private static bool _startupActivationReported;

	/// <summary>
	/// Whether the activation the app started with has already been reported, so that a scene
	/// connecting later is treated as a further activation rather than the startup one.
	/// </summary>
	internal static bool StartupActivationReported => _startupActivationReported;

	/// <summary>
	/// The execution state to report for an activation arriving now.
	/// </summary>
	/// <remarks>
	/// Keyed off whether the startup activation has been consumed rather than off
	/// <see cref="Application.Current"/>, which UIKit has already created by the time the first
	/// scene connects and so can never distinguish a cold start.
	/// </remarks>
	internal static ApplicationExecutionState CurrentExecutionState
		=> _startupActivationReported ? ApplicationExecutionState.Running : ApplicationExecutionState.NotRunning;

	/// <summary>
	/// Reports a URL activation, whether from a custom scheme or a universal link.
	/// </summary>
	internal static bool TryReportUrl(NSUrl? url, ApplicationExecutionState previousExecutionState)
	{
		if (!TryParseUri(url, out var uri))
		{
			return false;
		}

		Report(AppActivationArguments.CreateProtocol(new ProtocolActivatedEventArgs(uri, previousExecutionState)));
		return true;
	}

	/// <summary>
	/// Reports a universal-link activation carried by an <see cref="NSUserActivity"/>.
	/// </summary>
	internal static bool TryReportUserActivity(NSUserActivity? userActivity, ApplicationExecutionState previousExecutionState)
	{
		if (userActivity?.ActivityType != NSUserActivityType.BrowsingWeb)
		{
			if (typeof(AppleUIKitActivation).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AppleUIKitActivation).Log().LogDebug(
					$"Ignoring a user activity of type '{userActivity?.ActivityType ?? "(null)"}'; only a browsing-web activity is a universal link.");
			}

			return false;
		}

		return TryReportUrl(userActivity.WebPageUrl, previousExecutionState);
	}

#if !__TVOS__
	/// <summary>
	/// Reports a home-screen shortcut (jump list) tap as a Launch activation.
	/// </summary>
	/// <remarks>
	/// <see cref="UIApplicationShortcutItem.Type"/> carries the
	/// <see cref="Windows.UI.StartScreen.JumpListItem.Arguments"/> the app registered the shortcut with.
	/// </remarks>
	internal static void ReportShortcut(UIApplicationShortcutItem shortcutItem)
		=> Report(AppActivationArguments.CreateLaunch(
			new LaunchActivatedEventArgs(ActivationKind.Launch, shortcutItem.Type)));
#endif

	private static void Report(AppActivationArguments args)
	{
		var instance = AppInstance.GetCurrent();

		if (_startupActivationReported)
		{
			instance.SetOrRaiseActivation(args);
		}
		else
		{
			// UIKit starts the app from didFinishLaunchingWithOptions and only then connects the
			// first scene, so under the scene lifecycle the startup activation always arrives after
			// OnLaunched has run. ReportStartupActivation stores it anyway, keeping
			// GetActivatedEventArgs correct, and still raises Activated.
			_startupActivationReported = true;
			instance.ReportStartupActivation(args);
		}
	}

	/// <summary>
	/// Marks the startup activation as settled for an app that launched without one, so a later
	/// URL or shortcut is reported as arriving into a running app.
	/// </summary>
	internal static void MarkStartupActivationSettled() => _startupActivationReported = true;

	private static bool TryParseUri(NSUrl? url, [NotNullWhen(true)] out Uri? uri)
	{
		uri = null;

		if (url is null)
		{
			return false;
		}

		if (Uri.TryCreate(url.ToString(), UriKind.Absolute, out uri))
		{
			return true;
		}

		// Scheme only: an activation URI routinely carries an OAuth code or token.
		typeof(AppleUIKitActivation).Log().LogError(
			$"An activation URL with scheme '{url.Scheme ?? "(none)"}' could not be parsed as an absolute URI.");
		return false;
	}
}
