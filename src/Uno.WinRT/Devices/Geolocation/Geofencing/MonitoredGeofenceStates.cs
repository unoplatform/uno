using System;

namespace Windows.Devices.Geolocation.Geofencing
{
	/// <summary>
	/// Indicates the state or states of the Geofences that are currently being monitored by the system.
	/// </summary>
	[Flags]
	public enum MonitoredGeofenceStates : uint
	{
		/// <summary>
		/// No flag is set.
		/// </summary>
		None = 0U,
		/// <summary>
		/// The device has entered a geofence area.
		/// </summary>
		Entered = 1U,
		/// <summary>
		/// The device has left a geofence area.
		/// </summary>
		Exited = 2U,
		/// <summary>
		/// The geofence has been removed.
		/// </summary>
		Removed = 4U
	}
}
