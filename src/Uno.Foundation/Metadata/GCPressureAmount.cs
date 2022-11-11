namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies the amount of memory for garbage collection.
/// </summary>
public enum GCPressureAmount 
{
	/// <summary>
	/// Less than 10k of memory pressure.
	/// </summary>
	Low = 0,

	/// <summary>
	/// Between 10k and 100k of memory pressure.
	/// </summary>
	Medium = 1,

	/// <summary>
	/// Over 100k of memory pressure.
	/// </summary>
	High = 2,
}

