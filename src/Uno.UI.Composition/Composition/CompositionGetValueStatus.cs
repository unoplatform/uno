namespace Windows.UI.Composition;

/// <summary>
/// Indicates the outcome of an attempt to retrieve the value of a key-value pair.
/// </summary>
public enum CompositionGetValueStatus
{
	/// <summary>
	/// The value successfully retrieved.
	/// </summary>
	Succeeded = 0,

	/// <summary>
	/// The value type of the key-value pair is different than the value type requested.
	/// </summary>
	TypeMismatch = 1,

	/// <summary>
	/// The key-value pair does not exist.
	/// </summary>
	NotFound = 2,
}
