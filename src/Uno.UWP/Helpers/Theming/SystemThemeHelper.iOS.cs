using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private ApplicationTheme GetDefaultSystemTheme()
		{
			//Ensure the current device is running 12.0 or higher, because `TraitCollection.UserInterfaceStyle` was introduced in iOS 12.0
			if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
			{
				if (UIScreen.MainScreen.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark)
				{
					return ApplicationTheme.Dark;
				}
			}
			return ApplicationTheme.Light;
		}
	}
}
