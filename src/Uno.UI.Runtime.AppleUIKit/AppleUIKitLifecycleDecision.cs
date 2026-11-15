#nullable enable

using System.Globalization;

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
/// What decided the lifecycle an Apple UIKit app runs.
/// </summary>
internal enum AppleUIKitLifecycleDecidedBy
{
	SceneManifest,
	UseSceneLifecycleKey,
	SdkVersion,
	UnknownSdkVersion,
}

/// <summary>
/// The pure part of <c>AppleUIKitLifecycle</c>, split out so it is unit-testable without an app bundle.
/// </summary>
internal static class AppleUIKitLifecycleDecision
{
	/// <summary>
	/// Decides which lifecycle the app runs from its Info.plist.
	/// </summary>
	/// <param name="hasSceneManifest">Whether the app declares a scene manifest.</param>
	/// <param name="useSceneLifecycle">The UnoUseSceneLifecycle value, or null if it is unset or neither a boolean nor a number.</param>
	/// <param name="sdkVersion">The version of the SDK the app was built with, if known.</param>
	internal static (AppleUIKitLifecycleMode Mode, AppleUIKitLifecycleDecidedBy DecidedBy) Decide(
		bool hasSceneManifest,
		bool? useSceneLifecycle,
		string? sdkVersion)
	{
		if (hasSceneManifest)
		{
			return (AppleUIKitLifecycleMode.ManifestScene, AppleUIKitLifecycleDecidedBy.SceneManifest);
		}

		if (useSceneLifecycle is { } useScene)
		{
			return (useScene ? AppleUIKitLifecycleMode.AdoptedScene : AppleUIKitLifecycleMode.App, AppleUIKitLifecycleDecidedBy.UseSceneLifecycleKey);
		}

		if (!TryGetMajorVersion(sdkVersion, out var sdkMajorVersion))
		{
			return (AppleUIKitLifecycleMode.AdoptedScene, AppleUIKitLifecycleDecidedBy.UnknownSdkVersion);
		}

		// UIKit requires the scene lifecycle from the version 27 SDKs on, a version iOS and tvOS share. Apps built
		// with an older SDK keep the app lifecycle unless they opt in.
		return (sdkMajorVersion >= 27 ? AppleUIKitLifecycleMode.AdoptedScene : AppleUIKitLifecycleMode.App, AppleUIKitLifecycleDecidedBy.SdkVersion);
	}

	private static bool TryGetMajorVersion(string? version, out int majorVersion)
	{
		majorVersion = 0;

		return version is not null
			&& int.TryParse(version.Split('.')[0], NumberStyles.None, CultureInfo.InvariantCulture, out majorVersion);
	}
}
