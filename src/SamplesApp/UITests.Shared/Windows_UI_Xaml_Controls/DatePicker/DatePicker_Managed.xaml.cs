using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample("Pickers", IgnoreInSnapshotTests = true)]
	public sealed partial class DatePicker_Managed : Page
	{
		public DatePicker_Managed()
		{
			this.InitializeComponent();
#if HAS_UNO
			SampleDatePicker.UseNativeStyle = false;
#endif
		}
	}
}
