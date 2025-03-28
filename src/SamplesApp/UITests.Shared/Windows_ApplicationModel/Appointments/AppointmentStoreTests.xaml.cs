using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Appointments;
using System.Collections.ObjectModel;

namespace UITests.Windows_ApplicationModel.Appointments;

[Sample("Windows.ApplicationModel", Name = "AppointmentStore", IgnoreInSnapshotTests = true)]
public sealed partial class AppointmentStoreTests : Page
{
	public AppointmentStoreTests()
	{
		this.InitializeComponent();
	}

	public ObservableCollection<Appointment> Results { get; } = new();

	private async void GetAppointments_Click(object sender, RoutedEventArgs e)
	{
		uiErrorMsg.Text = "";

		AppointmentStore appointmentStore;
		try
		{
			appointmentStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);
		}
		catch (Exception ex)
		{
			uiErrorMsg.Text = "Exception while RequestStoreAsync: " + ex.Message;
			return;
		}

		if (appointmentStore == null)
		{
			uiErrorMsg.Text = "Got null as store from RequestStoreAsync";
			return;
		}

		FindAppointmentsOptions calendarAppointmentOptions = new();
		calendarAppointmentOptions.IncludeHidden = true;
		calendarAppointmentOptions.FetchProperties.Add(AppointmentProperties.AllDay);
		calendarAppointmentOptions.FetchProperties.Add(AppointmentProperties.Location);
		calendarAppointmentOptions.FetchProperties.Add(AppointmentProperties.Reminder);
		calendarAppointmentOptions.FetchProperties.Add(AppointmentProperties.StartTime);
		calendarAppointmentOptions.FetchProperties.Add(AppointmentProperties.Duration);
		calendarAppointmentOptions.FetchProperties.Add(AppointmentProperties.Subject);
		calendarAppointmentOptions.FetchProperties.Add(AppointmentProperties.Details);
		calendarAppointmentOptions.MaxCount = 20;

		var appointments = await appointmentStore.FindAppointmentsAsync(DateTime.Now, TimeSpan.FromDays(7), calendarAppointmentOptions);
		if (appointments == null)
		{
			uiErrorMsg.Text = "Got null list from FindAppointmentsAsync - maybe empty calendar";
			return;
		}

		if (appointments.Count == 0)
		{
			uiErrorMsg.Text = "Seems like you have no events planned in next 7 days";
			return;
		}

		foreach (var appointment in appointments)
		{
			Results.Add(appointment);
		}
	}
}


