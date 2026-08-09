using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public bool SupportsClosingCancellation => false;

	// Additional windows are backed by UIScenes, which requires the app to declare
	// a scene manifest and to opt into multiple scenes.
	public bool SupportsMultipleWindows =>
		UnoUISceneDelegate.HasSceneManifest() &&
		UIApplication.SharedApplication.SupportsMultipleScenes;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var wrapper = new NativeWindowWrapper(window, xamlRoot);

		if (window != Window.InitialWindow && SupportsMultipleWindows)
		{
			RequestScene();
		}

		return wrapper;
	}

	private static void RequestScene()
	{
		var request = UISceneSessionActivationRequest.Create();

		UIApplication.SharedApplication.ActivateSceneSession(
			request,
			err => typeof(NativeWindowFactoryExtension).LogError()?.LogError($"Failed to create new window: {err}"));
	}
}
