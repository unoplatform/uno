namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Specifies the intended format for the month in a DateTimeFormatter object.
/// </summary>
public enum MonthFormat
{
	/// <summary>
	/// Do not display the month.
	/// </summary>
	None,

	/// <summary>
	/// Display the month in the most natural way. It may be abbreviated, full, or numeric 
	/// depending on the context, such as the language or calendar that is being used.
	/// </summary>
	Default,

	/// <summary>
	/// Display an abbreviated version of the month (for example, "Sep" for September).
	/// </summary>
	Abbreviated,

	/// <summary>
	/// Display the month in its entirety (for example, "September").
	/// </summary>
	Full,

	/// <summary>
	/// Display the month as a number (for example, "9" for September).
	/// </summary>
	Numeric,
}
