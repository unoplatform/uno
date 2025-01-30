using System;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal class LifetimeHandler
	{
		private static DisplayRegionHelper _displayRegionHelper;

		internal static DisplayRegionHelper GetDisplayRegionHelperInstance()
			=> _displayRegionHelper = _displayRegionHelper ?? new DisplayRegionHelper();
	}
}
