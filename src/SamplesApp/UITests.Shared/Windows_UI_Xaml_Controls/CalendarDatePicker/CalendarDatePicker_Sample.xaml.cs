using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Windows_UI_Xaml_Controls.CalendarDatePicker.Models;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.CalendarDatePicker
{
	[SampleControlInfo(viewModelType: typeof(CalendarDatePickerViewModel))]
	[ActivePlatforms(Platform.Android)]
	public sealed partial class CalendarDatePicker_Sample : UserControl
	{
		public CalendarDatePicker_Sample()
		{
			this.InitializeComponent();

		}
	}
}
