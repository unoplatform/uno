namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// Structure that determines version and counts of resources returned for the app package.
/// </summary>
public partial struct ResourceLayoutInfo
{
	/// <summary>
	/// Major version of resources to be returned.
	/// </summary>
	public uint MajorVersion;

	/// <summary>
	/// Minor version of resources to be returned.
	/// </summary>
	public uint MinorVersion;

	/// <summary>
	/// Number of resource subtrees to be returned.
	/// </summary>
	public uint ResourceSubtreeCount;

	/// <summary>
	/// Number of named resources to be returned.
	/// </summary>
	public uint NamedResourceCount;

	/// <summary>
	/// Framework-generated checksum.
	/// </summary>
	public int Checksum;
}
