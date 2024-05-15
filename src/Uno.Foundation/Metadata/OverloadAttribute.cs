namespace Windows.Foundation.Metadata;

/// <summary>
/// Identifies the method as an overload in a language that supports overloading.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public partial class OverloadAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="method">The name that represents the method in the projected language.</param>
	public OverloadAttribute(string method) : base()
	{
	}
}
