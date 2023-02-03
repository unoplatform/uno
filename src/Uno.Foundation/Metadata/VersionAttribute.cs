namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the version of the type.
/// </summary>
[AttributeUsage(
	AttributeTargets.Delegate |
	AttributeTargets.Enum |
	AttributeTargets.Event |
	AttributeTargets.Field |
	AttributeTargets.Interface |
	AttributeTargets.Method |
	AttributeTargets.Property |
	AttributeTargets.Class |
	AttributeTargets.Struct,
	AllowMultiple = true)]
public partial class VersionAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="version">The version to associate with the marked object.</param>
	public VersionAttribute(uint version) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="version">The version to associate with the marked object.</param>
	/// <param name="platform">A value of the enumeration. The default is Windows.</param>
	public VersionAttribute(uint version, Platform platform) : base()
	{
	}
}
