namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Specifies the intended format for the second in a DateTimeFormatter object.
/// </summary>
public enum SecondFormat
{
	/// <summary>
	/// Do not display the second.
	/// </summary>
	None,

	/// <summary>
	/// Display the second in the most natural way. This will depend on the context, 
	/// such as the language or clock that is being used.
	/// </summary>
	Default,
}
