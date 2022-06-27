using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.TimePicker
{

	[SampleControlInfo("Pickers", nameof(TimePicker_Flyout_Automated))]
	public sealed partial class TimePicker_Flyout_Automated : UserControl
	{
		public TimePicker_Flyout_Automated()
		{
			this.InitializeComponent();
			this.TestTimePicker.Time = new TimeSpan(3, 12, 0);
		}
	}
}
