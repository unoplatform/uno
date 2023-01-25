namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// Possible values for the persistence of a global qualifier value override.
/// </summary>
public enum ResourceQualifierPersistence
{
	/// <summary>
	/// The override value is not persistent.
	/// </summary>
	None = 0,
	
	/// <summary>
	/// The override value persists on the local machine.
	/// </summary>
	LocalMachine = 1,
}
