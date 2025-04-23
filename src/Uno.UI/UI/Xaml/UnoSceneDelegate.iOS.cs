#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;

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
	}

	public override void DidDisconnect(UIScene scene)
	{

	}

	public override void WillEnterForeground(UIScene scene)
	{

	}

	public override void DidBecomeActive(UIScene scene) => base.DidBecomeActive(scene);

	public override void WillResignActive(UIScene scene) => base.WillResignActive(scene);

	public override void DidEnterBackground(UIScene scene)
	{

	}
}
