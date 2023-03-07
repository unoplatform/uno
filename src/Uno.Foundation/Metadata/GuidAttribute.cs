namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the GUID for the interface or delegate.
/// </summary>
[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Interface)]
public partial class GuidAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="a">The first 4 bytes of the GUID.</param>
	/// <param name="b">The next 2 bytes of the GUID.</param>
	/// <param name="c">The next 2 bytes of the GUID.</param>
	/// <param name="d">The next byte of the GUID.</param>
	/// <param name="e">The next byte of the GUID.</param>
	/// <param name="f">The next byte of the GUID.</param>
	/// <param name="g">The next byte of the GUID.</param>
	/// <param name="h">The next byte of the GUID.</param>
	/// <param name="i">The next byte of the GUID.</param>
	/// <param name="j">The next byte of the GUID.</param>
	/// <param name="k">The next byte of the GUID.</param>
	public GuidAttribute(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) : base()
	{
	}
}
