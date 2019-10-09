using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[SampleControlInfo("Date Picker", nameof(DatePickerFlyout_MinYear), typeof(DatePickerViewModel))]
	public sealed partial class DatePickerFlyout_MinYear : UserControl
	{
		public DatePickerFlyout_MinYear()
		{
			this.InitializeComponent();
		}
	}
}
