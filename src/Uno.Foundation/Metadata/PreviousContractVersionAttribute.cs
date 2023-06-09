namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that the type was previously associated with a different API contract.
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
public partial class PreviousContractVersionAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="contract">The name of the previous contract.</param>
	/// <param name="versionLow">The first version of the previous contract to which the type was associated.</param>
	/// <param name="versionHigh">The last version of the previous contract to which the type was associated.</param>
	/// <param name="newContract">The name of the new contract to which the type is associated.</param>
	public PreviousContractVersionAttribute(string contract, uint versionLow, uint versionHigh, string newContract) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="contract">The name of the previous contract.</param>
	/// <param name="versionLow">The first version of the previous contract to which the type was associated.</param>
	/// <param name="versionHigh">The last version of the previous contract to which the type was associated.</param>
	public PreviousContractVersionAttribute(string contract, uint versionLow, uint versionHigh) : base()
	{
	}
}
