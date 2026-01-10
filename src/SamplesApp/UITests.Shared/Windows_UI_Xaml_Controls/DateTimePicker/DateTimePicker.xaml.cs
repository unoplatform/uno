using SamplesApp.UITests.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.DateTimePicker
{
	[Sample("Pickers", "DateTimePicker", typeof(DateTimePickerViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class DateTimePicker : UserControl
	{
		public DateTimePicker()
		{
			this.InitializeComponent();
		}
	}
}
