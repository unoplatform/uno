using UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample("Pickers", ViewModelType = typeof(DatePickerViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class DatePickerFlyout_MinYear : UserControl
	{
		public DatePickerFlyout_MinYear()
		{
			this.InitializeComponent();
		}
	}
}
