namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Provides info about a participant of an appointment in a calendar.
/// </summary>
public partial interface IAppointmentParticipant
{
	/// <summary>
	/// Gets or sets a string that communicates the address of a participant
	/// of an appointment. The address is required and is
	/// a Simple Mail Transfer Protocol (SMTP) e-mail address.
	/// It is also of type String and between 1 and 321 characters in length (non-empty).
	/// </summary>
	string Address { get; set; }

	/// <summary>
	/// Gets or sets a string that communicates the display name of a participant of an appointment.
	/// The display name is optional, of type String, and a maximum of 256 characters in length.
	/// </summary>
	string DisplayName { get; set; }
}
