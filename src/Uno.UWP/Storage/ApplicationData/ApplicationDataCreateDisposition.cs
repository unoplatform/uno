namespace Windows.Storage;

/// <summary>
/// Specifies options for creating application data containers or returning existing containers.
/// This enumeration is used by the ApplicationDataContainer.CreateContainer method.
/// </summary>
public enum ApplicationDataCreateDisposition
{
	/// <summary>
	/// Always returns the specified container. Creates the container if it does not exist.	
	/// </summary>
	Always = 0,

	/// <summary>
	/// Returns the specified container only if it already exists. Raises an exception 
	/// of type System.Exception if the specified container does not exist.
	/// </summary>
	Existing = 1,
}
