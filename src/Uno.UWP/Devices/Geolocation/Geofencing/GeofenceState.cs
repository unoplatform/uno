

using System;

namespace Windows.Devices.Geolocation.Geofencing
{
	[Flags]
	public enum GeofenceState
	{
		None =0,
		Entered=1,
		Exited=2,
		Removed=4,
	}
}
