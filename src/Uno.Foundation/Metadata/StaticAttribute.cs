namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates an interface that contains only static methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public partial class StaticAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="type">The type that contains the static methods for the runtime class.</param>
	/// <param name="version">The version of the API Contract in which the static factory was added to the runtime class's activation factory.</param>
	public StaticAttribute(Type type, uint version) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="type">The type that contains the static methods for the runtime class.</param>
	/// <param name="version">The version of the API Contract in which the static factory was added to the runtime class's activation factory.</param>
	/// <param name="platform">A value of the enumeration. The default is Windows.</param>
	public StaticAttribute(Type type, uint version, Platform platform) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="type">The type that contains the static methods for the runtime class.</param>
	/// <param name="version">The version of the API Contract in which the static factory was added to the runtime class's activation factory.</param>
	/// <param name="contractName">A string representing the type of the API contract implementing the class.</param>
	public StaticAttribute(Type type, uint version, string contractName) : base()
	{
	}
}
