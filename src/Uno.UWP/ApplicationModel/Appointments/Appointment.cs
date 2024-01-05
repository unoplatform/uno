using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Represents an appointment in a calendar. This class is used when an app
/// is activated using the AppointmentsProvider value for ActivationKind,
/// as a value for AppointmentInformation properties.
/// </summary>
public partial class Appointment
{
	/// <summary>
	/// Initializes a new instance of the Appointment class.
	/// </summary>
	public Appointment()
	{
	}

	internal Appointment(string calendarId, string localId)
	{
		CalendarId = calendarId;
		LocalId = localId;
	}

	/// <summary>
	/// Gets or sets a Boolean value that indicates whether the appointment will last all day.
	/// The default is false for won't last all day.
	/// </summary>
	public bool AllDay { get; set; }

	/// <summary>
	/// Gets the unique identifier for the calendar associated with the appointment.
	/// </summary>
	public string CalendarId { get; } = "";

	/// <summary>
	/// Gets or sets a string value. The string contains extended details
	/// that describe the appointment. Details is of type String and a maximum
	/// of 1,073,741,823 characters in length, which is the maximum length
	/// of a JET database string.
	/// </summary>
	public string Details { get; set; } = "";

	/// <summary>
	/// Gets or sets a time span that represents the time duration of the appointment.
	/// Duration is of type TimeSpan and must be non-negative.
	/// </summary>
	public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0);

	/// <summary>
	/// Gets a string that uniquely identifies the appointment on the local device.
	/// </summary>
	public string LocalId { get; } = "";

	/// <summary>
	/// Gets or sets a string that communicates the physical location of the appointment.
	/// Location is of type String and a maximum of 32,768 characters in length.
	/// </summary>
	public string Location { get; set; } = "";

	/// <summary>
	/// Gets or sets the organizer of the appointment. Organizer is of type AppointmentOrganizer.
	/// The number of invitees is unlimited.
	/// </summary>
	public AppointmentOrganizer? Organizer { get; set; } // Android: string = email; Win: Address (email), DisplayName (optional)

	/// <summary>
	/// Gets or sets a time span value. The value declares the amount of time
	/// to subtract from the StartTime, and that time used as the issue time
	/// for a reminder for an appointment. A null value indicates that the appointment
	/// will not issue a reminder.
	/// </summary>
	public TimeSpan? Reminder { get; set; }

	/// <summary>
	/// Gets or sets the starting time for the appointment. StartTime is of type DateTime.
	/// </summary>
	public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;

	/// <summary>
	/// Gets or sets a string that communicates the subject of the appointment.
	/// Subject is of type String and a maximum of 255 characters in length.
	/// </summary>
	public string Subject { get; set; } = "";
}
