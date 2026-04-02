using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewDayItem
	{
		public void SetDensityColors(IEnumerable<Color> colors)
		{
			if (colors != null)
			{
				var c = new DirectUI.ValueTypeCollection<Color>();
				c.SetView(colors.ToList());
				base.SetDensityColors(c);
			}
			else
			{
				base.SetDensityColors(null);
			}
		}
	}
}
