#nullable enable

using System;
using System.Globalization;
using Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// The lifecycle an Apple UIKit app runs.
/// </summary>
internal enum AppleUIKitLifecycleMode
{
	/// <summary>
	/// The app lifecycle, where the application delegate drives the app's single window.
	/// </summary>
	App,

	/// <summary>
	/// The scene lifecycle Uno adopts for an app without a scene manifest, which UIKit requires from the version 27
	/// SDKs on. The app's single window is created up front and hosted in the application scene.
	/// </summary>
	AdoptedScene,

	/// <summary>
	/// The scene lifecycle the app declares with a scene manifest, where each window gets a scene of its own.
	/// </summary>
	ManifestScene,
}

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

	private enum DecidedBy
	{
		SceneManifest,
		UseSceneLifecycleKey,
		SdkVersion,
		UnknownSdkVersion,
	}

	// Before the decision, which reads them.
	private static readonly bool _hasSceneManifest = GetInfoValue(SceneManifestKey) is not null;
	private static readonly NSObject? _useSceneLifecycle = GetInfoValue(UseSceneLifecycleKey);
	private static readonly string? _sdkVersion = GetInfoValue(SdkVersionKey)?.ToString();

	private static readonly (AppleUIKitLifecycleMode Mode, DecidedBy DecidedBy) _decision = Decide();

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
				DecidedBy.SceneManifest => "the scene manifest",
				DecidedBy.UseSceneLifecycleKey => UseSceneLifecycleKey,
				DecidedBy.SdkVersion => "the SDK version",
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

			if (_decision.DecidedBy == DecidedBy.UnknownSdkVersion)
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

	private static (AppleUIKitLifecycleMode, DecidedBy) Decide()
	{
		if (_hasSceneManifest)
		{
			return (AppleUIKitLifecycleMode.ManifestScene, DecidedBy.SceneManifest);
		}

		if (_useSceneLifecycle is NSNumber useSceneLifecycle)
		{
			return (useSceneLifecycle.BoolValue ? AppleUIKitLifecycleMode.AdoptedScene : AppleUIKitLifecycleMode.App, DecidedBy.UseSceneLifecycleKey);
		}

		if (!TryGetSdkMajorVersion(out var sdkMajorVersion))
		{
			return (AppleUIKitLifecycleMode.AdoptedScene, DecidedBy.UnknownSdkVersion);
		}

		// UIKit requires the scene lifecycle from the version 27 SDKs on, a version iOS and tvOS share. Apps built
		// with an older SDK keep the app lifecycle unless they opt in.
		return (sdkMajorVersion >= 27 ? AppleUIKitLifecycleMode.AdoptedScene : AppleUIKitLifecycleMode.App, DecidedBy.SdkVersion);
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
