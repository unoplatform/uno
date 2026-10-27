#nullable enable

using System;
using Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// Decides, once from the app's Info.plist, which lifecycle the app runs, and raises the app-level events every
/// lifecycle reports.
/// </summary>
internal static class AppleUIKitLifecycle
{
	/// <summary>
	/// Info.plist key that turns the scene lifecycle on or off for an app without a scene manifest, overriding the
	/// default picked from the SDK the app was built with.
	/// </summary>
	internal const string UseSceneLifecycleKey = "UnoUseSceneLifecycle";

	private const string SceneManifestKey = "UIApplicationSceneManifest";
	private const string SdkVersionKey = "DTPlatformVersion";

	// Before the decision, which reads them.
	private static readonly bool _hasSceneManifest = GetInfoValue(SceneManifestKey) is not null;
	private static readonly NSObject? _useSceneLifecycle = GetInfoValue(UseSceneLifecycleKey);
	private static readonly string? _sdkVersion = GetInfoValue(SdkVersionKey)?.ToString();

	private static readonly (AppleUIKitLifecycleMode Mode, AppleUIKitLifecycleDecidedBy DecidedBy) _decision =
		AppleUIKitLifecycleDecision.Decide(_hasSceneManifest, (_useSceneLifecycle as NSNumber)?.BoolValue, _sdkVersion);

	internal static AppleUIKitLifecycleMode Mode => _decision.Mode;

	internal static bool UsesSceneLifecycle => Mode != AppleUIKitLifecycleMode.App;

	/// <summary>
	/// Logs the lifecycle the app runs and what decided it. Called once logging is configured, which happens after
	/// <see cref="Mode"/> is read.
	/// </summary>
	internal static void LogMode()
	{
		var log = typeof(AppleUIKitLifecycle).Log();

		if (log.IsEnabled(LogLevel.Information))
		{
			var useSceneLifecycle = _useSceneLifecycle switch
			{
				null => "not set",
				NSNumber number => number.BoolValue ? "true" : "false",
				var value => value.ToString(),
			};

			var decidedBy = _decision.DecidedBy switch
			{
				AppleUIKitLifecycleDecidedBy.SceneManifest => "the scene manifest",
				AppleUIKitLifecycleDecidedBy.UseSceneLifecycleKey => UseSceneLifecycleKey,
				AppleUIKitLifecycleDecidedBy.SdkVersion => "the SDK version",
				_ => "an unknown SDK version",
			};

			log.Info(
				$"Running the {Mode} lifecycle, decided by {decidedBy} (scene manifest: {_hasSceneManifest}, " +
				$"{UseSceneLifecycleKey}: {useSceneLifecycle}, SDK version: {_sdkVersion ?? "unknown"}).");
		}

		if (log.IsEnabled(LogLevel.Warning))
		{
			if (!_hasSceneManifest && _useSceneLifecycle is not null and not NSNumber)
			{
				log.Warn($"{UseSceneLifecycleKey} is ignored, as it is neither a boolean nor a number.");
			}

			if (_decision.DecidedBy == AppleUIKitLifecycleDecidedBy.UnknownSdkVersion)
			{
				log.Warn($"The SDK version is unknown, so Uno adopts the scene lifecycle. Set {UseSceneLifecycleKey} to choose.");
			}
		}
	}

	/// <summary>
	/// Raises the app-level events of the app entering the background.
	/// </summary>
	internal static void RaiseAppEnteredBackground()
		=> Application.Current?.RaiseEnteredBackground(() => Application.Current?.RaiseSuspending());

	/// <summary>
	/// Raises the app-level events of the app leaving the background, then <paramref name="onLeftBackground"/>.
	/// </summary>
	internal static void RaiseAppLeavingBackground(Action onLeftBackground)
	{
		Application.Current?.RaiseResuming();
		Application.Current?.RaiseLeavingBackground(onLeftBackground);
	}

	private static NSObject? GetInfoValue(string key)
		=> NSBundle.MainBundle.ObjectForInfoDictionary(key);
}
