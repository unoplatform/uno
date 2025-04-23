#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

[System.Runtime.Versioning.SupportedOSPlatform("ios13.0")]
[System.Runtime.Versioning.SupportedOSPlatform("tvos13.0")]
public class UnoSceneDelegate : UISceneDelegate
{
	[Export("window")]
	public UIWindow? Window { get; set; }

	[Export("scene:willConnectToSession:options:")]
	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		var windowScene = scene as UIWindowScene;

		// Always instantiate UIWindow within WillConnect
		var window = new Uno.UI.Controls.Window(windowScene!);
		Window = window;

		if (NativeWindowWrapper.AwaitingScene.Count == 0)
		{
			throw new InvalidOperationException("No window wrapper available for the scene.");
		}

		var wrapper = NativeWindowWrapper.AwaitingScene.Dequeue();
		wrapper.SetNativeWindow(window);
	}

	public override void DidDisconnect(UIScene scene)
	{

	}

	public override void WillEnterForeground(UIScene scene)
	{

	}

	public override void DidBecomeActive(UIScene scene) { }

	public override void WillResignActive(UIScene scene) { }

	public override void DidEnterBackground(UIScene scene)
	{

	}
}
