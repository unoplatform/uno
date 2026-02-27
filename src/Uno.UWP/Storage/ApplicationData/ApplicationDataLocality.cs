namespace Windows.Storage;

/// <summary>
/// Specifies the type of an application data store.
/// </summary>
public enum ApplicationDataLocality
{
	/// <summary>
	/// The data resides in the local application data store.
	/// </summary>
	Local = 0,

	/// <summary>
	/// The data resides in the roaming application data store.
	/// </summary>
	Roaming = 1,

	/// <summary>
	/// The data resides in the temporary application data store.
	/// </summary>
	Temporary = 2,

	/// <summary>
	/// The data resides in the local cache for the application data store.
	/// </summary>
	LocalCache = 3,

	/// <summary>
	/// The data resides in the shared local application data store.
	/// </summary>
	SharedLocal = 4,
}
