namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that tools should create an entry for an interface when creating an application package.
/// </summary>
[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Class)]
[AttributeName("metadata_marshal")]
public partial class MetadataMarshalAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public MetadataMarshalAttribute() : base()
	{
	}
}
