using System;

namespace Microsoft.UI.Xaml.Controls
{
	internal class LifetimeHandler
	{
		private static DisplayRegionHelper _displayRegionHelper;

		internal static DisplayRegionHelper GetDisplayRegionHelperInstance()
			=> _displayRegionHelper = _displayRegionHelper ?? new DisplayRegionHelper();
	}
}
