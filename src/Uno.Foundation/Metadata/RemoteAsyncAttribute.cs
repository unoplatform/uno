namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that an interface can be delivered using an asynchronous form of the Async pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public partial class RemoteAsyncAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public RemoteAsyncAttribute() : base()
	{
	}
}
