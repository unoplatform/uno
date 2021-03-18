using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls
{
	internal static class DisplayRegionHelperTestApi
	{
		public static bool SimulateDisplayRegions
		{
			get => DisplayRegionHelper.SimulateDisplayRegions();
			set => DisplayRegionHelper.SimulateDisplayRegions(value);
		}

		public static TwoPaneViewMode SimulateMode
		{
			get => DisplayRegionHelper.SimulateMode();
			set => DisplayRegionHelper.SimulateMode(value);
		}

	}
}
