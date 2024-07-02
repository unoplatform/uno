using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewDayItem
	{
		// We are not supporting rendering of those density bars yet
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void SetDensityColors(IEnumerable<Color> colors)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.CalendarViewDayItem", "void CalendarViewDayItem.SetDensityColors(IEnumerable<Color> colors)");

			// UNO TODO
			// As the code for density bars has not been tested yet, we prefer to just do nothing with provided colors!

			//var c = new ValueTypeCollection<Color>();
			//c.SetView(colors.ToList());
			//base.SetDensityColors(c);
		}
	}
}
