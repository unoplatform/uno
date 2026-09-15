namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Represents the organizer of an appointment in a calendar.
/// </summary>
public partial class AppointmentOrganizer : IAppointmentParticipant
{
	/// <summary>
	/// Initializes a new instance of the AppointmentOrganizer class.
	/// </summary>
	public AppointmentOrganizer()
	{
	}

	/// <summary>
	/// Gets or sets a string that communicates the address of a participant
	/// of an appointment. The address is required and is
	/// a Simple Mail Transfer Protocol (SMTP) e-mail address.
	/// It is also of type String and between 1 and 321 characters in length (non-empty).
	/// </summary>
	public string Address { get; set; } = "";

	/// <summary>
	/// Gets or sets a string that communicates the display name of a participant of an appointment.
	/// The display name is optional, of type String, and a maximum of 256 characters in length.
	/// </summary>
	public string DisplayName { get; set; } = "";
}
