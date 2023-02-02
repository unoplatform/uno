using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates how a programming element is composed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public partial class ComposableAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="type">The type of the factory object that is used to create the programming element.</param>
	/// <param name="compositionType">One of the enumeration values.</param>
	/// <param name="version">The version.</param>
	public ComposableAttribute(Type type, CompositionType compositionType, uint version) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="type">The type of the factory object that is used to create the programming element.</param>
	/// <param name="compositionType">One of the enumeration values.</param>
	/// <param name="version">The version.</param>
	/// <param name="platform">A value of the enumeration. The default is Windows.</param>
	public ComposableAttribute(Type type, CompositionType compositionType, uint version, Platform platform) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="type">The type of the factory object that is used to create the programming element.</param>
	/// <param name="compositionType">One of the enumeration values.</param>
	/// <param name="version">The version.</param>
	/// <param name="contract">A string representing the type of the API contract implementing the class.</param>
	public ComposableAttribute(Type type, CompositionType compositionType, uint version, string contract) : base()
	{
	}
}
