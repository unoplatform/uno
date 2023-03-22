namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that the specified type is exclusive to this type.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public partial class ExclusiveToAttribute : Attribute
{
	public ExclusiveToAttribute(Type typeName) : base()
	{
	}
}
