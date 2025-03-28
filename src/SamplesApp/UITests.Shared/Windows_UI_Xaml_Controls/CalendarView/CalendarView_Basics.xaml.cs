using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.CalendarViewTests
{
	[Sample("Pickers", IgnoreInSnapshotTests = true, IsManualTest = true, Description = """
		1) Make sure to test this twice. Once with the time zone set to UTC - 2 and one more time with UTC + 2
		2) Also, make sure that the time is set to "edge" values, e.g, 11:00 PM and 01:00 AM
		""")]
	public sealed partial class CalendarView_Basics : Page
	{
		public CalendarView_Basics()
		{
			this.InitializeComponent();
		}
	}
}
