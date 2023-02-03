namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that the interface contains protected methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public partial class ProtectedAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public ProtectedAttribute() : base()
	{
	}
}
