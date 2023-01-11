namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// Defines values that represent the type of resource that is encapsulated in a ResourceCandidate.
/// </summary>
public enum ResourceCandidateKind
{
	/// <summary>
	/// The resource is a string.
	/// </summary>
	String,

	/// <summary>
	/// The resource is a file located at the specified location.
	/// </summary>
	File,
	
	/// <summary>
	/// The resource is embedded data in some containing resource file (such as a .resw file).
	/// </summary>
	EmbeddedData,
}
