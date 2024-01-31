using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TimePicker
{
	[Sample("Pickers", IgnoreInSnapshotTests = true)]
	public sealed partial class TimePicker_Managed : Page
	{
		public TimePicker_Managed()
		{
			this.InitializeComponent();
#if HAS_UNO
			SampleTimePicker.UseNativeStyle = false;
#endif
		}
	}
}
