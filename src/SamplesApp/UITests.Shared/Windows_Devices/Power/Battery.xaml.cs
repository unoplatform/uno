using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Appointments;


namespace UITests.Windows_Devices.Power
{

	[SampleControlInfo("Windows.Devices.Power", "Battery")]
	public sealed partial class Battery : Page
	{
		public Battery()
		{
			this.InitializeComponent();
		}

		private void getBatteryInfo_Click(object sender, RoutedEventArgs e)
		{
			uiErrorMsg.Text = "";
			uiOkMsg.Text = "";
			uiStatus.Text = "";
			uiChargeRate.Text = "";
			uiFull.Text = "";
			uiRemaining.Text = "";

			Windows.Devices.Power.BatteryReport oBattRep;
			try
			{
				oBattRep = Windows.Devices.Power.Battery.AggregateBattery.GetReport();
			}
			catch (Exception ex)
			{
				uiErrorMsg.Text = "Exception reading battery data, maybe not implemented? " + ex.Message;
				return;
			}



			uiStatus.Text = "Battery status: " + oBattRep.Status.ToString();
			uiChargeRate.Text = "Charge Rate: " + oBattRep.ChargeRateInMilliwatts + " mW";
			uiFull.Text = "Full Charge Capacity: " + oBattRep.FullChargeCapacityInMilliwattHours + " mWh";
			uiRemaining.Text = "Remaining Capacity: " + oBattRep.RemainingCapacityInMilliwattHours + " mWh";
		}
	}
}
