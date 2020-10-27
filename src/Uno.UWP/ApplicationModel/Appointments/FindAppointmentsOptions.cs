using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Appointments
{
	public partial class FindAppointmentsOptions
	{
		public uint MaxCount { get; set; }
		public bool IncludeHidden { get; set; }
		public IList<string> FetchProperties { get; }
		public FindAppointmentsOptions()
		{
			MaxCount = uint.MaxValue;
			IncludeHidden = false;
			FetchProperties = new List<string>();
		}
	}
}
