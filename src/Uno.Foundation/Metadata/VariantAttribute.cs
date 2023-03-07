namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that the item is an instance of a variant IInspectable. Applies to method parameters, properties, and return values of types.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
public partial class VariantAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public VariantAttribute() : base()
	{
	}
}
