using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample("Pickers", IgnoreInSnapshotTests = true)]
	public sealed partial class DatePicker_Features : Page
	{
		public static readonly DependencyProperty PickedDateProperty =
			DependencyProperty.Register(
				"PickedDate",
				typeof(DateTimeOffset),
				typeof(DatePicker_Features),
				new PropertyMetadata(new DateTimeOffset(2021, 2, 2, 12, 0, 0, TimeSpan.Zero)));

		public DateTimeOffset PickedDate
		{
			get => (DateTimeOffset)GetValue(PickedDateProperty);
			set => SetValue(PickedDateProperty, value);
		}

		public DatePicker_Features()
		{
			this.InitializeComponent();
		}

		public DateTimeOffset DtYear(object o)
		{
			var now = DateTimeOffset.Now;
			if (o is SelectorItem item && item.Tag is string tag && !string.IsNullOrEmpty(tag))
			{
				switch (tag)
				{
					case "lastweek": return now.AddDays(-7);
					case "nextweek": return now.AddDays(7);
					case "today": return now;
				}

				var year = Convert.ToUInt16(tag);
				return new DateTime(year, 1, 1, 0, 0, 0);
			}

			return now;
		}

		public string DtString(object o)
		{
			if (o is DateTimeOffset dto)
			{
				return dto.ToString("R");
			}

			if (o is DateTime dt)
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

		private void AddYear(object sender, RoutedEventArgs e)
		{
			switch ((sender as FrameworkElement)?.Tag)
			{
				case DatePickerFlyout flyout:
					flyout.Date = flyout.Date.AddYears(1);
					break;
				case Windows.UI.Xaml.Controls.DatePicker picker:
					picker.Date = picker.Date.AddYears(1);
					break;
			}
		}

		private void AddMonth(object sender, RoutedEventArgs e)
		{
			switch ((sender as FrameworkElement)?.Tag)
			{
				case DatePickerFlyout flyout:
					flyout.Date = flyout.Date.AddMonths(1);
					break;
				case Windows.UI.Xaml.Controls.DatePicker picker:
					picker.Date = picker.Date.AddMonths(1);
					break;
			}
		}

		private void AddDay(object sender, RoutedEventArgs e)
		{
			switch ((sender as FrameworkElement)?.Tag)
			{
				case DatePickerFlyout flyout:
					flyout.Date = flyout.Date.AddDays(1);
					break;
				case Windows.UI.Xaml.Controls.DatePicker picker:
					picker.Date = picker.Date.AddDays(1);
					break;
			}
		}
	}
}
