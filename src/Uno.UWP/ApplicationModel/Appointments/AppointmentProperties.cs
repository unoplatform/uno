
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Appointments
{
	public partial class AppointmentProperties
	{
		public static string AllDay => "Appointment.AllDay";
		public static string Location => "Appointment.Location";
		public static string StartTime => "Appointment.StartTime";
		public static string Duration => "Appointment.Duration";
		public static string Subject => "Appointment.Subject";
		public static string Organizer => "Appointment.Organizer";
		public static string Details => "Appointment.Details";
		public IList<string> DefaultProperties
		{
			get
			{
				var properties = new List<string>();
				properties.Add(AllDay);
				properties.Add(Location);
				properties.Add(StartTime);
				properties.Add(Duration);
				properties.Add(Subject);
				return properties;
				// UWP version returns this list:
				// "Appointment.Subject", "Appointment.Location", "Appointment.StartTime", "Appointment.Duration",
				// "Appointment.BusyStatus", "Appointment.AllDay", "Appointment.ParentFolderId", "Appointment.Recurrence"
				// "Appointment.RemoteId", "Appointment.OriginalStartTime", "Appointment.ChangeNumber"
				// but we define only some of them, that seems to be "a must" for every platform
			}

		}
	}
}
