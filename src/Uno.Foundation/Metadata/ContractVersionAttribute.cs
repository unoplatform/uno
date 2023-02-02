using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the version of the API contract.
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
public partial class ContractVersionAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="version">The version of the API contract.</param>
	public ContractVersionAttribute(uint version) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="contract">The type to associate with the API contract.</param>
	/// <param name="version">The version of the API contract.</param>
	public ContractVersionAttribute(Type contract, uint version) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="contract">The type to associate with the API contract.</param>
	/// <param name="version">The version of the API contract.</param>
	public ContractVersionAttribute(string contract, uint version) : base()
	{
	}
}
