#if HAS_UNO_WINUI
namespace Microsoft.Windows.ApplicationModel.Resources;

/// <summary>
/// Defines values that represent the type of resource that is encapsulated in a ResourceCandidate.
/// </summary>
public enum ResourceCandidateKind
{
	/// <summary>
	/// The ResourceCandidate is of an unknown type.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// The resource is a string.
	/// </summary>
	String = 1,

	/// <summary>
	/// The resource is a file located at the specified location.
	/// </summary>
	FilePath = 2,

	/// <summary>
	/// The resource is embedded data in some containing resource file (such as a .resw file).
	/// </summary>
	EmbeddedData = 3,
}
#endif
