#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

#if UIKIT_SKIA
using NativeWindow = Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml.AppleUIKitWindow;
#else
using NativeWindow = Uno.UI.Controls.Window;
#endif

namespace Microsoft.UI.Xaml;

[System.Runtime.Versioning.SupportedOSPlatform("ios13.0")]
[System.Runtime.Versioning.SupportedOSPlatform("tvos13.0")]
public class UnoUISceneDelegate : UISceneDelegate
{
	internal const string GetConfigurationSelectorName = "application:configurationForConnectingSceneSession:options:";
	internal const string UnoSceneConfigurationKey = "__UNO_DEFAULT_SCENE_CONFIGURATION__";
	internal const string UIApplicationSceneManifestKey = "UIApplicationSceneManifest";

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

		// Clear the window reference. The scene has already gone to background
		// before disconnecting, so visibility count is already updated.
		Window = null;
	}

	internal static bool HasSceneManifest() =>
		(OperatingSystem.IsIOSVersionAtLeast(13, 0) || OperatingSystem.IsTvOSVersionAtLeast(13, 0)) &&
		NSBundle.MainBundle.InfoDictionary.ContainsKey(new NSString(UIApplicationSceneManifestKey));
}
