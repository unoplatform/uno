using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Uno.UI
{
	public static class UWPViewHelper
	{
		public static Point LogicalToPhysicalPixels(this Point point)
#if XAMARIN
			=> Uno.UI.ViewHelper.LogicalToPhysicalPixels(point);
#else
			=> point;
#endif
	}
}
