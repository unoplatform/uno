namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values that are used by the IScrollProvider pattern to indicate the direction and distance to scroll.
/// </summary>
public enum ScrollAmount
{
	/// <summary>
	/// Specifies that scrolling is performed in large decrements, which is equivalent to pressing the PAGE UP key 
	/// or to clicking a blank part of a scrollbar. If the distance represented by the PAGE UP key is not a relevant 
	/// amount for the control, or if no scrollbar exists, the value represents an amount equal to the size of the 
	/// currently visible window.
	/// </summary>
	LargeDecrement,

	/// <summary>
	/// Specifies that scrolling is performed in small decrements, which is equivalent to pressing an arrow key or to
	/// clicking the arrow button on a scrollbar.
	/// </summary>
	SmallDecrement,

	/// <summary>
	/// Specifies that scrolling should not be performed.
	/// </summary>
	NoAmount,

	/// <summary>
	/// Specifies that scrolling is performed in small decrements, which is equivalent to pressing an arrow key or to
	/// clicking the arrow button on a scrollbar.
	/// </summary>
	LargeIncrement,

	/// <summary>
	/// Specifies that scrolling is performed in small increments, which is equivalent to pressing an arrow key or to 
	/// clicking the arrow button on a scrollbar.
	/// </summary>
	SmallIncrement
}
