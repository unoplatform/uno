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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.Appointments;


namespace UITests.Windows_Devices.Power
{

	[Sample("Windows.Devices.Power", Name = "Battery")]
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
