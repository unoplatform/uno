namespace Windows.Foundation.Metadata;

/// <summary>
/// Declares the platform that a type should be supported in, when platform-specific metadata is produced.
/// </summary>
public partial class PlatformAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="platform">A value of the enumeration. The default is Windows.</param>
	public PlatformAttribute(Platform platform) : base()
	{
	}
}
