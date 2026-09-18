using System;

namespace Windows.Devices.Geolocation.Geofencing
{
	/// <summary>
	/// Indicates the current state of a Geofence.
	/// </summary>
	[Flags]
	public enum GeofenceState : uint
	{
		/// <summary>
		/// No flag is set.
		/// </summary>
		None = 0U,
		/// <summary>
		/// The device has entered the geofence area.
		/// </summary>
		Entered = 1U,
		/// <summary>
		/// The device has left the geofence area.
		/// </summary>
		Exited = 2U,
		/// <summary>
		/// The geofence was removed.
		/// </summary>
		Removed = 4U
	}
}
