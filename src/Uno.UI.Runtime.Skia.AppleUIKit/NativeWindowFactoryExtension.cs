using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public bool SupportsClosingCancellation => false;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var wrapper = new NativeWindowWrapper(window, xamlRoot);

		if (window != Window.InitialWindow)
		{
			// Request scene for the new window
			var userActivity = new NSUserActivity(UnoUISceneDelegate.UIApplicationSceneManifestKey);
			var request = UISceneSessionActivationRequest.Create();
			request.UserActivity = userActivity;
			Action<NSError> errorAction = err => typeof(NativeWindowFactory).LogError()?.LogError($"Failed to create new window: {err}");
			UIApplication.SharedApplication.ActivateSceneSession(
				request,
				errorAction);
		}

		return wrapper;
	}
}
