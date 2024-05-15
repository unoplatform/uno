namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the numeric range constraints for the value of a data field.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public partial class RangeAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute with the specified minimum and maximum values.
	/// </summary>
	/// <param name="minValue">The minimum value allowed.</param>
	/// <param name="maxValue">The maximum value allowed.</param>
	public RangeAttribute(int minValue, int maxValue) : base()
	{
	}
}
