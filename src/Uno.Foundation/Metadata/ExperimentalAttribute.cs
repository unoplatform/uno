namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that a type or member should be marked in metadata as experimental, and consequently may not be present in the final, released version of an SDK or library.
/// </summary>
[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
public partial class ExperimentalAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public ExperimentalAttribute()
	{
	}
}
