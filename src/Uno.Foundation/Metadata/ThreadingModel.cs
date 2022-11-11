namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies the threading model.
/// </summary>
public enum ThreadingModel
{
	/// <summary>
	/// Single-threaded apartment.
	/// </summary>
	STA = 1,
	
	/// <summary>
	/// Multithreaded apartment.
	/// </summary>
	MTA = 2,
	
	/// <summary>
	/// Both single-threaded and multithreaded apartments.
	/// </summary>
	Both = 3,

	/// <summary>
	/// No valid threading model applies.
	/// </summary>
	InvalidThreading = 0,
}
