using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.UI.Dispatching;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml;

internal static class NativeWindowHelpers
{
	/// <summary>
	/// Create a temporary view controller that replicates
	/// the launch storyboard, if any. As we're rendering using
	/// an metal view, a black screen may appear otherwise.
	///
	/// IMPORTANT: This method needs to be called synchronously with 
	/// `UnoSkiaAppDelegate.FinishedLaunching`. Asynchronously, a black 
	/// screen may appear.
	/// </summary>
	internal static bool TryCreateExtendedSplashScreen(UIWindow nativeWindow)
	{
		var launchName = NSBundle.MainBundle.ObjectForInfoDictionary("UILaunchStoryboardName") as NSString;

		if (!string.IsNullOrEmpty(launchName))
		{
			if (typeof(NativeWindowHelpers).Log().IsDebugEnabled())
			{
				typeof(NativeWindowHelpers).Log().Debug($"Using storyboard {launchName} as an extended Splash Screen");
			}

			var storyboard = UIStoryboard.FromName(launchName, null);
			var splashVC = storyboard.InstantiateInitialViewController();

			nativeWindow.RootViewController = splashVC;
			nativeWindow.MakeKeyAndVisible();
			return true;
		}

		return false;
	}

	internal static void TransitionFromSplashScreen(UIWindow nativeWindow, UIViewController targetViewController)
	{
		if (nativeWindow is null)
		{
			throw new InvalidOperationException("Native window needs to exist to transition from splash screen");
		}

		// Requeue after the current loaded event so we let all
		// controls render properly.
		Microsoft.UI.Dispatching.DispatcherQueue.Main.TryEnqueue(
			priority: DispatcherQueuePriority.High, () =>
			{
				if (typeof(NativeWindowHelpers).Log().IsDebugEnabled())
				{
					typeof(NativeWindowHelpers).Log().Debug($"ShowCore: Showing main content");
				}

				nativeWindow.MakeKeyAndVisible();

				// Fade the app's content over the extended splash screen
				UIView.Transition(
					nativeWindow,
					0.25f,
					UIViewAnimationOptions.TransitionCrossDissolve,
					() => nativeWindow.RootViewController = targetViewController,
					delegate { }
				);
			});
	}
}
