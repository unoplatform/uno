namespace Windows.Foundation.Metadata;

/// <summary>
/// Microsoft internal use only.
/// </summary>
public partial class GCPressureAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public GCPressureAttribute() : base()
	{
	}

	/// <summary>
	/// GC pressure amount.
	/// </summary>
	public GCPressureAmount amount;
}
