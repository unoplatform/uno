using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Appointments
{
	public partial class Appointment
	{
		public string Location { get; set; }
		public TimeSpan Duration { get; set; }
		public string Details { get; set; }
		public string Subject { get; set; }
		public DateTimeOffset StartTime { get; set; }
		public bool AllDay { get; set; }
		public string CalendarId { get; set; }
		public string LocalId { get; set; }
		public AppointmentOrganizer Organizer { get; set; } // Android: string = email; Win: Address (email), DisplayName (optional)
		public Appointment()
		{
			Location = "";
			Duration = TimeSpan.FromSeconds(0); // this is how UWP initialize this property
			Details = "";
			Subject = "";
			StartTime = DateTimeOffset.Now;  // this is how UWP initialize this property
			AllDay = false;
			CalendarId = "";
			LocalId = "";
			Organizer = null;
		}
	}
}
