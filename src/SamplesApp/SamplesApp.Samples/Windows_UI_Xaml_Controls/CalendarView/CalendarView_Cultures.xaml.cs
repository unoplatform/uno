using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.CalendarView;

[Sample("Pickers", IgnoreInSnapshotTests = true, IsManualTest = true, Description = """
		Showing CalendarView with different cultures.
		""")]
public sealed partial class CalendarView_Cultures : Page
{
	public CalendarView_Cultures()
	{
		this.InitializeComponent();
	}
}
