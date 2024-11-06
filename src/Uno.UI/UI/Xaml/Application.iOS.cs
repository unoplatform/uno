using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, shortcutItem.Type));
		}
		_preventSecondaryActivationHandling = false;
	}

	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, [Transient] UIWindow forWindow)
	{
		return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
	}
}
