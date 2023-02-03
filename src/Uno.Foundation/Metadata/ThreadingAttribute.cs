namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the threading model of a Windows Runtime class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public partial class ThreadingAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="model">One of the enumeration values.</param>
	public ThreadingAttribute(ThreadingModel model) : base()
	{
	}
}
