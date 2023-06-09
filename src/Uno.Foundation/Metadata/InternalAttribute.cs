namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that a type or member should be marked in metadata as internal to the SDK or framework, and for consumption by system components only.
/// </summary>
[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Module)]
public partial class InternalAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public InternalAttribute() : base()
	{
	}
}
