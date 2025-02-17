namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify the type of Scroll event that occurred.
/// </summary>
public enum ScrollEventType
{
	/// <summary>
	/// The Thumb moved a distance specified by the value of SmallChange. The Thumb moved to the left for a horizontal ScrollBar or upward for a vertical ScrollBar.
	/// </summary>
	SmallDecrement,

	/// <summary>
	/// The Thumb moved a distance specified by the value of SmallChange. The Thumb moved to the right for a horizontal ScrollBar or downward for a vertical ScrollBar.
	/// </summary>
	SmallIncrement,

	/// <summary>
	/// The Thumb moved a distance specified by the value of LargeChange. The Thumb moved to the left for a horizontal ScrollBar or upward for a vertical ScrollBar.
	/// </summary>
	LargeDecrement,

	/// <summary>
	/// The Thumb moved a distance specified by the value of LargeChange. The Thumb moved to the right for a horizontal ScrollBar or downward for a vertical ScrollBar.
	/// </summary>
	LargeIncrement,

	/// <summary>
	/// The Thumb moved to a new position because the user selected Scroll Here in the shortcut menu of the ScrollBar.
	/// </summary>
	ThumbPosition,

	/// <summary>
	/// The Thumb was dragged and caused a PointerMoved event. A Scroll event of this ScrollEventType may occur more than one time when the Thumb is dragged in the ScrollBar.
	/// </summary>
	ThumbTrack,

	/// <summary>
	/// The Thumb moved to the Minimum position of the ScrollBar.
	/// </summary>
	First,

	/// <summary>
	/// The Thumb moved to the Maximum position of the ScrollBar.
	/// </summary>
	Last,

	/// <summary>
	/// The Thumb was dragged to a new position and is now no longer being dragged by the user.
	/// </summary>
	EndScroll,
}
