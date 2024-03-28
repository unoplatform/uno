using Uno;

namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// Placement of the Flyout relative to its target control.
/// </summary>
public enum FlyoutPlacementMode
{
	/// <summary>
	/// The preferred location of the flyout is above the target element.
	/// </summary>
	Top,
	/// <summary>
	/// The preferred location of the flyout is below the target element.
	/// </summary>
	Bottom,
	/// <summary>
	/// The preferred location of the flyout is to the left of the target element.
	/// </summary>
	Left,
	/// <summary>
	/// The preferred location of the flyout is to the right of the target element.
	/// </summary>
	Right,
	/// <summary>
	/// The preferred location of the flyout is centered on the screen.
	/// </summary>
	Full,

	/// <summary>
	/// Preferred location is above the target element, with the left edge of flyout aligned with left edge of the target element.
	/// </summary>
	TopEdgeAlignedLeft,

	/// <summary>
	/// Preferred location is above the target element, with the right edge of flyout aligned with right edge of the target element.
	/// </summary>
	TopEdgeAlignedRight,

	/// <summary>
	/// Preferred location is below the target element, with the left edge of flyout aligned with left edge of the target element.
	/// </summary>
	BottomEdgeAlignedLeft,

	/// <summary>
	/// Preferred location is below the target element, with the right edge of flyout aligned with right edge of the target element.
	/// </summary>
	BottomEdgeAlignedRight,

	/// <summary>
	/// Preferred location is to the left of the target element, with the top edge of flyout aligned with top edge of the target element.
	/// </summary>
	LeftEdgeAlignedTop,

	/// <summary>
	/// Preferred location is to the left of the target element, with the bottom edge of flyout aligned with bottom edge of the target element.
	/// </summary>
	LeftEdgeAlignedBottom,

	/// <summary>
	/// Preferred location is to the right of the target element, with the top edge of flyout aligned with top edge of the target element.
	/// </summary>
	RightEdgeAlignedTop,

	/// <summary>
	/// Preferred location is to the right of the target element, with the bottom edge of flyout aligned with bottom edge of the target element.
	/// </summary>
	RightEdgeAlignedBottom,

	/// <summary>
	/// Preferred location is determined automatically.
	/// </summary>
	Auto
}
