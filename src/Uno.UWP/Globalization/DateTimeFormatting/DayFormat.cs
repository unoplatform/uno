namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Specifies the intended format for the day in a DateTimeFormatter object.
/// </summary>
public enum DayFormat
{
	/// <summary>
	/// Do not display the day.
	/// </summary>
	None,

	/// <summary>
	/// Display the day in the most natural way. This will depend on the context, such as
	/// the language or calendar (for example, for the Hebrew calendar and Hebrew language, 
	/// use the Hebrew numbering system).
	/// </summary>
	Default,
}
