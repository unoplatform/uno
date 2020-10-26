using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Appointments
{
	public partial class AppointmentOrganizer : IAppointmentParticipant
	{
		public string DisplayName { get; set; }
		public string Address { get; set; }
		public AppointmentOrganizer()
		{
			DisplayName = "";
			Address = "";
		}
	}
}
