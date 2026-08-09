#nullable enable

using System;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public bool SupportsClosingCancellation => false;

	// Secondary windows are backed by scenes, so the app must both declare a scene manifest and
	// opt into multiple scenes for them to be creatable.
	public bool SupportsMultipleWindows =>
		UnoUISceneDelegate.HasSceneManifest &&
		UIApplication.SharedApplication.SupportsMultipleScenes;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var wrapper = new NativeWindowWrapper(window, xamlRoot);

		if (wrapper.RequiresScene)
		{
			var token = SceneWindowRegistry.Register(wrapper);

			// UIKit connects the first scene on its own at launch; only secondary windows have to
			// ask for one.
			if (window != Window.InitialWindow)
			{
				RequestScene(token);
			}
		}

		return wrapper;
	}

	private static void RequestScene(string token)
	{
		// The token lets the connecting scene find the exact window that requested it, rather than
		// relying on scenes connecting in the order they were requested.
		var userActivity = new NSUserActivity(SceneWindowRegistry.ActivityType)
		{
			UserInfo = NSDictionary.FromObjectAndKey(
				new NSString(token),
				new NSString(SceneWindowRegistry.TokenKey))
		};

		void OnError(NSError error)
		{
			// Without this the wrapper would sit in the registry forever and mis-pair the next
			// scene that connects.
			SceneWindowRegistry.Remove(token);
			typeof(NativeWindowFactoryExtension).LogError()?.LogError($"Failed to create a new window: {error}");
		}

		if (OperatingSystem.IsIOSVersionAtLeast(17, 0) ||
			OperatingSystem.IsTvOSVersionAtLeast(17, 0) ||
			OperatingSystem.IsMacCatalystVersionAtLeast(17, 0))
		{
			var request = UISceneSessionActivationRequest.Create();
			request.UserActivity = userActivity;

			UIApplication.SharedApplication.ActivateSceneSession(request, OnError);
		}
		else
		{
			// UISceneSessionActivationRequest is 17.0+; earlier versions use the direct request.
#pragma warning disable CA1422 // Validate platform compatibility
			UIApplication.SharedApplication.RequestSceneSessionActivation(null, userActivity, null, OnError);
#pragma warning restore CA1422 // Validate platform compatibility
		}
	}
}
