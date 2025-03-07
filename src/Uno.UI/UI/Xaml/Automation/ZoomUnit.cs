namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains possible values for the ZoomByUnit method, which zooms the viewport of a control by the specified unit.
/// </summary>
public enum ZoomUnit
{
	/// <summary>
	/// No increase or decrease in zoom.
	/// </summary>
	NoAmount = 0,

	/// <summary>
	/// Decrease zoom by a large decrement.
	/// </summary>
	LargeDecrement = 1,

	/// <summary>
	/// Decrease zoom by a small decrement.
	/// </summary>
	SmallDecrement = 2,

	/// <summary>
	/// Increase zoom by a large increment.
	/// </summary>
	LargeIncrement = 3,

	/// <summary>
	/// Increase zoom by a small increment.
	/// </summary>
	SmallIncrement = 4,
}
