namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Specifies the intended format for the hour in a DateTimeFormatter object.
/// </summary>
public enum HourFormat
{
	/// <summary>
	/// Do not display the hour.
	/// </summary>
	None,

	/// <summary>
	/// Display the hour in the most natural way. This will depend on the context, 
	/// such as the language or clock that is being used.
	/// </summary>
	Default,
}
