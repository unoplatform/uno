
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

namespace UITests.Windows_ApplicationModel.Appointments
{
	[SampleControlInfo("Windows.ApplicationModel.Appointments", "AppointmentStore")]

	public sealed partial class AppointmentStore : Page
	{

		public AppointmentStore()
		{
			this.InitializeComponent();
		}

		private async void getCalendarEntry_Click(object sender, RoutedEventArgs e)
		{
			uiErrorMsg.Text = "";
			uiOkMsg.Text = "";
			uiSubject.Text = "";
			uiStartTime.Text = "";

			Windows.ApplicationModel.Appointments.AppointmentStore oCalendarStore;
			try
			{
				oCalendarStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);
			}
			catch (Exception ex)
			{
				uiErrorMsg.Text = "Exception while RequestStoreAsync: " + ex.Message;
				return;
			}

			if (oCalendarStore == null)
			{
				uiErrorMsg.Text = "Got null as store from RequestStoreAsync";
				return;
			}

			FindAppointmentsOptions oCalOpt = new();
			oCalOpt.IncludeHidden = true;
			oCalOpt.FetchProperties.Add(AppointmentProperties.AllDay);
			oCalOpt.FetchProperties.Add(AppointmentProperties.Location);
			oCalOpt.FetchProperties.Add(AppointmentProperties.Reminder);
			oCalOpt.FetchProperties.Add(AppointmentProperties.StartTime);
			oCalOpt.FetchProperties.Add(AppointmentProperties.Duration);
			oCalOpt.FetchProperties.Add(AppointmentProperties.Subject);
			oCalOpt.MaxCount = 20;

			var oBatch = await oCalendarStore.FindAppointmentsAsync(DateTime.Now, TimeSpan.FromDays(7), oCalOpt);
			if (oBatch == null)
			{
				uiErrorMsg.Text = "Got null list from FindAppointmentsAsync - maybe empty calendar";
				return;
			}

			if (oBatch.Count < 1)
			{
				uiErrorMsg.Text = "Seems like you have no events planned in next 7 days";
				return;
			}

			uiOkMsg.Text = "You have " + oBatch.Count.ToString() + " event(s) in the next 7 days, data from first:";

			var entry = oBatch[0];

			uiSubject.Text = entry.Subject;
			uiStartTime.Text = entry.StartTime.ToString();

		}

	}
}


