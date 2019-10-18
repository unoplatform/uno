using System;
using Windows.UI.Xaml;
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

		public DateTimeOffset DtYear(object o)
		{
			if (o is SelectorItem item && item.Tag is string tag && !string.IsNullOrEmpty(tag))
			{
				var year = Convert.ToUInt16(tag);
				return new DateTime(year, 1, 1, 0, 0, 0);
			}
			return DateTimeOffset.Now;
		}

		public string DtString(object o)
		{
			if (o is DateTimeOffset dto)
			{
				return dto.ToString("R");
			}

			if(o is DateTime dt)
			{
				return dt.ToString("R");
			}

			return "none";
		}

		private async void PickClicked(object sender, RoutedEventArgs e)
		{
			var flyout = new DatePickerFlyout()
			{
				MinYear = DtYear(minYear.SelectedItem),
				MaxYear = DtYear(maxYear.SelectedItem),
				CalendarIdentifier = (calendarIdentifier.SelectedItem as ComboBoxItem)?.Content as string,
				DayVisible = dayVisible.IsChecked ?? false,
				MonthVisible = monthVisible.IsChecked ?? false,
				YearVisible = yearVisible.IsChecked ?? false,
			};

			var picked = await flyout.ShowAtAsync(flyoutTarget);
			pickedDate.Text = DtString(picked);
		}

	}
}
