using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[SampleControlInfo("DatePicker", "DatePicker_SampleContent", typeof(DatePickerViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class DatePicker_SampleContent : UserControl
    {
        public DatePicker_SampleContent()
        {
            this.InitializeComponent();
        }
    }
}
