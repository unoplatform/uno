#nullable enable

using System.Globalization;

namespace Uno.UI.Xaml;

/// <summary>
/// The life cycle an Apple UIKit app runs.
/// </summary>
internal enum AppleUIKitLifecycleMode
{
	/// <summary>
	/// The app life cycle, where the application delegate drives the app's window.
	/// </summary>
	App,

	/// <summary>
	/// The scene life cycle Uno adopts for an app without a scene manifest, which UIKit requires from the version 27
	/// SDKs on.
	/// </summary>
	AdoptedScene,

	/// <summary>
	/// The scene life cycle the app declares with a scene manifest.
	/// </summary>
	ManifestScene,
}

/// <summary>
/// What decided the life cycle an Apple UIKit app runs.
/// </summary>
internal enum AppleUIKitLifecycleDecidedBy
{
	SceneManifest,
	UseSceneLifecycleKey,
	SdkVersion,
	UnknownSdkVersion,
}

/// <summary>
/// The pure part of <c>UnoUISceneDelegate</c>'s life cycle choice, split out so it is unit-testable without an app bundle.
/// </summary>
internal static class AppleUIKitLifecycleDecision
{
	/// <summary>
	/// Decides which life cycle the app runs from its Info.plist.
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

		// UIKit requires the scene life cycle from the version 27 SDKs on, a version iOS, tvOS and Mac Catalyst share.
		// Apps built with an older SDK keep the app life cycle unless they opt in.
		return (sdkMajorVersion >= 27 ? AppleUIKitLifecycleMode.AdoptedScene : AppleUIKitLifecycleMode.App, AppleUIKitLifecycleDecidedBy.SdkVersion);
	}

	private static bool TryGetMajorVersion(string? version, out int majorVersion)
	{
		majorVersion = 0;

		return version is not null
			&& int.TryParse(version.Split('.')[0], NumberStyles.None, CultureInfo.InvariantCulture, out majorVersion);
	}
}
