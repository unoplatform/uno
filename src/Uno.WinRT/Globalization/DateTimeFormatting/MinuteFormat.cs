namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Specifies the intended format for the minute in a DateTimeFormatter object.
/// </summary>
public enum MinuteFormat
{
	/// <summary>
	/// Do not display the minute.
	/// </summary>
	None,

	/// <summary>
	/// Display the minute in the most natural way. This will depend on the context, 
	/// such as the language or clock that is being used.
	/// </summary>
	Default,
}
