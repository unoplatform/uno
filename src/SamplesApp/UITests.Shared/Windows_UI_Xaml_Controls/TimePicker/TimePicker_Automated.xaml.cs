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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TimePicker
{
	[SampleControlInfo("TimePicker", nameof(TimePicker_Automated))]
	public sealed partial class TimePicker_Automated : UserControl
	{

		public TimePicker_Automated()
		{
			this.InitializeComponent();
		}

		private void MyTimePickerLoaded(object sender, RoutedEventArgs e)
		{
			myTimePicker.Time = new TimeSpan(14, 50, 0);
		}

		private void changeSelectedClockIdentifier(object sender, RoutedEventArgs e)
		{
			var myBtn = sender as RadioButton;

			if (myBtn.Tag.ToString() == "12HourClock")
			{
				myTimePicker.ClockIdentifier = "12HourClock";
			}
			else
			{
				myTimePicker.ClockIdentifier = "24HourClock";
			}
		}

		private void applyNewTime(object sender, RoutedEventArgs e)
		{
			var newHour = txtNewHour.Text != "" ? int.Parse(txtNewHour.Text) : myTimePicker.Time.Hours;
			var newMinute = txtNewMinute.Text != "" ? int.Parse(txtNewMinute.Text) : myTimePicker.Time.Minutes;

			myTimePicker.Time = new TimeSpan(newHour, newMinute, 0);
		}
	}
}
