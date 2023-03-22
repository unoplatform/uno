namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the number of array elements.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed partial class LengthIsAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	/// <param name="indexLengthParameter">The number of array elements.</param>
	public LengthIsAttribute(int indexLengthParameter) : base()
	{
	}
}
