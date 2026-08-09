#nullable enable

using System;
using Foundation;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

using NativeWindow = Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml.AppleUIKitWindow;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Default <see cref="UISceneDelegate"/> implementation used by Uno Platform on iOS and tvOS.
/// </summary>
/// <remarks>
/// <para>
/// Creates and attaches the native <see cref="UIWindow"/> to a <see cref="UIWindowScene"/> when a
/// scene connects, and releases it when the scene disconnects.
/// </para>
/// <para>
/// Apps using the scene-based lifecycle (iOS 13 / tvOS 13 and later) subclass this type and
/// register the subclass as the <c>UISceneDelegateClassName</c> in their scene manifest
/// (<c>UIApplicationSceneManifest</c> in Info.plist) to integrate with Uno Platform's
/// multi-window support.
/// </para>
/// </remarks>
[System.Runtime.Versioning.SupportedOSPlatform("ios13.0")]
[System.Runtime.Versioning.SupportedOSPlatform("tvos13.0")]
public class UnoUISceneDelegate : UISceneDelegate
{
	internal const string GetConfigurationSelectorName = "application:configurationForConnectingSceneSession:options:";
	internal const string UnoSceneConfigurationKey = "__UNO_DEFAULT_SCENE_CONFIGURATION__";
	internal const string UIApplicationSceneManifestKey = "UIApplicationSceneManifest";

	private NativeWindowWrapper? _wrapper;

	[Export("window")]
	public UIWindow? Window { get; set; }

	[Export("scene:willConnectToSession:options:")]
	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"WillConnect: Scene={scene.Session.PersistentIdentifier}, Role={session.Role}");
		}

		if (scene is not UIWindowScene windowScene)
		{
			throw new InvalidOperationException("WillConnect expected a UIWindowScene.");
		}

		// Always instantiate UIWindow within WillConnect
		var window = new NativeWindow(windowScene);
		Window = window;

		if (!NativeWindowWrapper.AwaitingScene.TryDequeue(out var wrapper))
		{
			this.Log().Error(
				$"No window wrapper available for scene. " +
				$"Scene={scene.Session.PersistentIdentifier}, Role={session.Role}. " +
				$"Ensure a Window is created before the scene connects.");
			throw new InvalidOperationException(
				$"No window wrapper available for the scene (PersistentIdentifier={scene.Session.PersistentIdentifier}). " +
				$"Ensure a Window is created before the scene connects.");
		}
		_wrapper = wrapper;
		wrapper.SetNativeWindow(window);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"WillConnect: Window attached to scene successfully");
		}
	}

	public override void DidDisconnect(UIScene scene)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"DidDisconnect: Scene={scene.Session.PersistentIdentifier}");
		}

		// The scene has already gone to background before disconnecting, so the visible
		// window count is already updated; only the observers and references are released here.
		_wrapper?.UnsubscribeBackgroundNotifications();
		_wrapper = null;
		Window = null;
	}

	internal static bool HasSceneManifest() =>
		(OperatingSystem.IsIOSVersionAtLeast(13, 0) || OperatingSystem.IsTvOSVersionAtLeast(13, 0)) &&
		NSBundle.MainBundle.InfoDictionary.ContainsKey(new NSString(UIApplicationSceneManifestKey));
}
