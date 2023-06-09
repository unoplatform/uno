namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that the type is an instance of a variant object. Applies to runtime classes, interfaces, and parameterized interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
[AttributeName("hasvariant")]
public partial class HasVariantAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public HasVariantAttribute() : base()
	{
	}
}
