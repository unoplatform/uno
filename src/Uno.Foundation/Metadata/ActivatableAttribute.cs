using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that the class is an activatable runtime class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public partial class ActivatableAttribute : Attribute
{
	/// <summary>
	/// Indicates that the runtime class can be activated with no parameters,
	/// starting in a particular version.
	/// </summary>
	/// <param name="version"></param>
	public ActivatableAttribute(uint version) : base()
	{
	}

	/// <summary>
	/// Indicates that the runtime class can be activated with no parameters,
	/// starting in a particular version of a particular API contract.
	/// </summary>
	/// <param name="version">The minimum version that can activate the runtime class with the specified interface.</param>
	/// <param name="type">The name of the API contract that can activate the runtime class with no parameters.</param>
	public ActivatableAttribute(uint version, string type) : base()
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="version">The version of the platform that can activate the runtime class with the specified interface.</param>
	/// <param name="platform">The platform that can activate the runtime class with the specified interface.</param>
	public ActivatableAttribute(uint version, Platform platform) : base()
	{
	}

	/// <summary>
	/// Indicates that the runtime class can be activated with parameters, starting in a particular version.
	/// </summary>
	/// <param name="type">The type of the interface that is used to activate objects.</param>
	/// <param name="version">The minimum version that can activate the runtime class with the specified interface.</param>
	public ActivatableAttribute(Type type, uint version) : base()
	{
	}

	/// <summary>
	/// Indicates that the runtime class can be activated with parameters, starting in a particular version of a particular API contract.
	/// </summary>
	/// <param name="type">The type of the interface that is used to activate objects.</param>
	/// <param name="version">The minimum version of the API contract that can activate the runtime class with the specified interface. The major version is in the high-order 16-bits and the minor version is in the low-order 16 bits.</param>
	/// <param name="contractName">The name of the API contract that can activate the runtime class with the specified interface.</param>
	public ActivatableAttribute(Type type, uint version, string contractName) : base()
	{
	}

	/// <summary>
	/// Indicates that the runtime class can be activated with parameters,
	/// starting in a particular version of a particular platform.
	/// </summary>
	/// <param name="type">The type of the interface that is used to activate objects.</param>
	/// <param name="version">The version of the platform that can activate the runtime class with the specified interface.</param>
	/// <param name="platform">The platform that can activate the runtime class with the specified interface.</param>
	public ActivatableAttribute(Type type, uint version, Platform platform) : base()
	{
	}
}
