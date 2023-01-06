using Uno;

namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Specifies the level of access granted to an AppointmentStore.
/// </summary>
public enum AppointmentStoreAccessType
{
	/// <summary>
	/// The appointment store has read and write access to appointment
	/// calendars created by the calling app.
	/// </summary>
	[NotImplemented]
	AppCalendarsReadWrite,

	/// <summary>
	/// The appointment store has read-only access to all calendars on the device.
	/// </summary>
	AllCalendarsReadOnly,

	/// <summary>
	/// The appointment store has read and write access to all calendars
	/// created by the calling app.
	/// </summary>
	[NotImplemented]
	AllCalendarsReadWrite,
}
