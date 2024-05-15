namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies that the type represents an API contract.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public partial class ApiContractAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public ApiContractAttribute() : base()
	{
	}
}
