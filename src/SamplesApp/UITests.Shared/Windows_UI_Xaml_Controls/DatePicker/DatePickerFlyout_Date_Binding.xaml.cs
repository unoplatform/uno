using System;
using Windows.UI.Xaml;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample("Pickers", IgnoreInSnapshotTests = true)]
	public sealed partial class DatePickerFlyout_Date_Binding : UserControl
	{
		public DatePickerFlyout_Date_Binding()
		{
			this.InitializeComponent();
		}

		public DateTimeOffset Date
		{
			get { return (DateTimeOffset)GetValue(DateProperty); }
			set { SetValue(DateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Date.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DateProperty =
			DependencyProperty.Register(
				name: "Date",
				propertyType: typeof(DateTimeOffset),
				ownerType: typeof(DatePickerFlyout_Date_Binding),
				typeMetadata: new PropertyMetadata(new DateTimeOffset(2019, 05, 04, 0, 0, 0, TimeSpan.Zero))
			);
	}
}
