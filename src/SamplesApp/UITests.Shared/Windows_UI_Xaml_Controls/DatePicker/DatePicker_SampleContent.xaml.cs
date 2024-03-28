using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample("Pickers", ViewModelType = typeof(DatePickerViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class DatePicker_SampleContent : UserControl
	{
		public DatePicker_SampleContent()
		{
			this.InitializeComponent();

			theDatePicker.MinYear = new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero);
			theDatePicker.MaxYear = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
		}
	}
}
