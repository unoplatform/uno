namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that the interface contains overridable methods.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public partial class OverridableAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public OverridableAttribute() : base()
	{
	}
}
