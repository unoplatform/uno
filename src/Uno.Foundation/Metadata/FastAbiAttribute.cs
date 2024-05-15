namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates if the type supports fast ABI.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public partial class FastAbiAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="version">The ABI version number.</param>
	public FastAbiAttribute(uint version) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="version">The ABI version number.</param>
	/// <param name="platform">The ABI platform name.</param>
	public FastAbiAttribute(uint version, Platform platform) : base()
	{
	}

	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="version">The ABI version number.</param>
	/// <param name="contractName">The ABI contractName.</param>
	public FastAbiAttribute(uint version, string contractName) : base()
	{
	}
}
