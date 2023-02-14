namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the marshaling behavior of a Windows Runtime component.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed partial class MarshalingBehaviorAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="behavior">One of the enumeration values.</param>
	public MarshalingBehaviorAttribute(MarshalingType behavior) : base()
	{
	}
}
