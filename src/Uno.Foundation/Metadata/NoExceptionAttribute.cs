namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates if the type raises exceptions.
/// </summary>
[AttributeName("noexcept")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed partial class NoExceptionAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public NoExceptionAttribute() : base()
	{
	}
}
