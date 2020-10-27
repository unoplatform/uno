using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Geolocation.Geofencing
{
	[Flags]
	public enum MonitoredGeofenceStates
	{
		None =0,
		Entered=1,
		Exited=2,
		Removed=4,
	}
}
