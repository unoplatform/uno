#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWindowFactory
{
	public static bool SupportsClosingCancellation => false;

	public static bool SupportsMultipleWindows => true;

	private static INativeWindowWrapper? CreateWindowPlatform(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot)
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
