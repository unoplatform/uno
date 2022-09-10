using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.CalendarView
{
	[Sample("Pickers", IgnoreInSnapshotTests = true)]
	public sealed partial class CalendarView_Features : Page
	{
		public CalendarView_Features()
		{
			this.InitializeComponent();

			sut.SelectedDatesChanged += (snd, evt) =>
			{
				selected.ItemsSource = sut.SelectedDates.ToArray();
			};
		}

		private void SetDisplayDate(object sender, RoutedEventArgs args)
		{
			sut.SetDisplayDate(setDisplayDate.Date);
		}
	}
}
