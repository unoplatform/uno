namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the name of the attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public partial class AttributeNameAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="A_0">One or more of the enumeration values.</param>
	public AttributeNameAttribute(string A_0) : base()
	{
	}
}
