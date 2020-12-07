using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample]
	public sealed partial class DatePicker_Features : Page
	{
		public DatePicker_Features()
		{
			this.InitializeComponent();
		}

		public DateTimeOffset Dt(object o)
		{
			if (o is SelectorItem item && item.Tag is string tag && !string.IsNullOrEmpty(tag))
			{
				var year = Convert.ToUInt16(tag);
				return new DateTime(year, 1, 1, 0, 0, 0);
			}
			return DateTimeOffset.Now;
		}
	}
}
