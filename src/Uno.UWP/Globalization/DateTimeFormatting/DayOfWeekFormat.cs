namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Specifies the intended format for the day of the week in a DateTimeFormatter object.
/// </summary>
public enum DayOfWeekFormat
{
	/// <summary>
	/// Do not display the day of the week.
	/// </summary>
	None,

	/// <summary>
	/// Display the day of the week in the most natural way. It may be abbreviated 
	/// or full depending on the context, such as the language or calendar that is being used.
	/// </summary>
	Default,

	/// <summary>
	/// Display an abbreviated version of the day of the week (for example, "Thur" for Thursday).
	/// </summary>
	Abbreviated,

	/// <summary>
	/// Display the day of the week in its entirety (for example, "Thursday").
	/// </summary>
	Full,
}
