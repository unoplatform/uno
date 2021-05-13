using UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample("Date Picking", ViewModelType = typeof(DatePickerViewModel))]
	public sealed partial class DatePickerSample : UserControl
	{
		public DatePickerSample()
		{
			this.InitializeComponent();
		}
	}
}
