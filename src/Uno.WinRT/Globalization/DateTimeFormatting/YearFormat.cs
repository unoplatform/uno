namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Specifies the intended format for the year in a DateTimeFormatter object.
/// </summary>
public enum YearFormat
{
	/// <summary>
	/// Do not display the year.
	/// </summary>
	None,

	/// <summary>
	/// Display the year in the most natural way. It may be abbreviated or full depending 
	/// on the context, such as the language or calendar that is being used.
	/// </summary>
	Default,

	/// <summary>
	/// Display an abbreviated version of the year (for example, "11" for Gregorian 2011).
	/// </summary>
	Abbreviated,

	/// <summary>
	/// Display the year in its entirety (for example, "2011" for Gregorian 2011).
	/// </summary>
	Full,
}
