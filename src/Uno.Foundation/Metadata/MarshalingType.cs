namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies the marshaling type for the class.
/// </summary>
public enum MarshalingType 
{
	/// <summary>
	/// The class can't be marshaled.
	/// </summary>
	InvalidMarshaling = 0,

	/// <summary>
	/// The class prevents marshaling on all interfaces.
	/// </summary>
	None = 1,

	/// <summary>
	/// The class marshals and unmarshals to the same pointer value on all interfaces.
	/// </summary>
	Agile = 2,

	/// <summary>
	/// The class does not implement IMarshal or forwards to CoGetStandardMarshal on all interfaces.
	/// </summary>
	Standard = 3,
}
