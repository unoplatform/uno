namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies the platforms that a specified type should be supported in,
/// as used by Windows Runtime attributes and metadata.
/// </summary>
public enum Platform
{
	/// <summary>
	/// For use by Windows metadata.
	/// </summary>
	Windows = 0,

	/// <summary>
	/// For use by Windows Phone metadata.
	/// </summary>
	WindowsPhone = 1,
}
