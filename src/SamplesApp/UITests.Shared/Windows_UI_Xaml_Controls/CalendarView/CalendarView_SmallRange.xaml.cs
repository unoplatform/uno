using System;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.CalendarView
{
	[Sample("Pickers", IgnoreInSnapshotTests = true)]
	public sealed partial class CalendarView_SmallRange : Page
	{
		public CalendarView_SmallRange()
		{
			this.InitializeComponent();

			var today = DateTime.Now.Date;
			var sundayThisWeek = today.AddDays(-(int)today.DayOfWeek);
			var tuesday = sundayThisWeek.AddDays((int)DayOfWeek.Tuesday);
			var friday = sundayThisWeek.AddDays((int)DayOfWeek.Friday);

			sut.MinDate = tuesday;
			sut.MaxDate = friday;
		}
	}
}
