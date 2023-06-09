namespace Windows.Foundation.Metadata;

/// <summary>
/// Identifies the type as one whose functionality is not projected into the specified target language.
/// </summary>
[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed partial class WebHostHiddenAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public WebHostHiddenAttribute()
	{
	}
}
