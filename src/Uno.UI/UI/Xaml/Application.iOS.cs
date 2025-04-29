using Foundation;
using System;
using System.Linq;
using UIKit;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel;
using ObjCRuntime;
using Windows.Globalization;
using Windows.Graphics.Display;
using Uno.Extensions;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Globalization;
using System.Threading;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

public partial class Application
{
	public override void PerformActionForShortcutItem(
		UIApplication application,
		UIApplicationShortcutItem shortcutItem,
		UIOperationHandler completionHandler)
	{
		if (!_preventSecondaryActivationHandling)
		{
			InvokeOnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, shortcutItem.Type));
		}
		_preventSecondaryActivationHandling = false;
	}

	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, [Transient] UIWindow forWindow)
	{
		return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
	}
}
