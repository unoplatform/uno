using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Provides strings that identify the properties of an appointment.
/// </summary>
public partial class AppointmentProperties
{
	/// <summary>
	/// Gets the name of the AllDay property.
	/// </summary>
	public static string AllDay => GetAppointmentProperty();

	/// <summary>
	/// Gets the name of the Location property.
	/// </summary>
	public static string Location => GetAppointmentProperty();

	/// <summary>
	/// Gets the name of the StartTime property.
	/// </summary>
	public static string StartTime => GetAppointmentProperty();

	/// <summary>
	/// Gets the name of the Duration property.
	/// </summary>
	public static string Duration => GetAppointmentProperty();

	/// <summary>
	/// Gets the name of the Subject property.
	/// </summary>
	public static string Subject => GetAppointmentProperty();

	/// <summary>
	/// Gets the name of the Organizer property.
	/// </summary>
	public static string Organizer => GetAppointmentProperty();

	/// <summary>
	/// Gets the name of the Details property.
	/// </summary>
	public static string Details => GetAppointmentProperty();

	/// <summary>
	/// Gets the name of the Reminder property.
	/// </summary>
	public static string Reminder => GetAppointmentProperty();

	/// <summary>
	/// Gets a list of names for the default appointment properties.
	/// </summary>
	/// <remarks>The list of properties is different from WinUI/UWP.</remarks>
	public static IList<string> DefaultProperties => new List<string>
	{
		Subject,
		Location,
		StartTime,
		Duration,
		AllDay
	};

	private static string GetAppointmentProperty([CallerMemberName] string? propertyName = null) =>
		$"Appointment.{propertyName}";
}
