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
		=> userActivity?.ActivityType == NSUserActivityType.BrowsingWeb
			&& TryReportUrl(userActivity.WebPageUrl, previousExecutionState);

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
		=> AppInstance.GetCurrent().SetOrRaiseActivation(args);

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

		typeof(AppleUIKitActivation).Log().LogError($"Activation URI {url} could not be parsed");
		return false;
	}
}
