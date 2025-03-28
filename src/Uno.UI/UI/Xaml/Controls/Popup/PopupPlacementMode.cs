namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify the preferred location for positioning a popup relative to a visual element.
/// </summary>
public enum PopupPlacementMode
{
	/// <summary>
	/// Preferred location is determined automatically.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// Preferred location is above the target element.
	/// </summary>
	Top = 1,

	/// <summary>
	/// Preferred location is below the target element.
	/// </summary>
	Bottom = 2,

	/// <summary>
	/// Preferred location is to the left of the target element.
	/// </summary>
	Left = 3,

	/// <summary>
	/// Preferred location is to the right of the target element.
	/// </summary>
	Right = 4,

	/// <summary>
	/// Preferred location is above the target element, with the left edge of popup aligned with left edge of the target element.
	/// </summary>
	TopEdgeAlignedLeft = 5,

	/// <summary>
	/// Preferred location is above the target element, with the right edge of popup aligned with right edge of the target element.
	/// </summary>
	TopEdgeAlignedRight = 6,

	/// <summary>
	/// Preferred location is below the target element, with the left edge of popup aligned with left edge of the target element.
	/// </summary>
	BottomEdgeAlignedLeft = 7,

	/// <summary>
	/// Preferred location is below the target element, with the right edge of popup aligned with right edge of the target element.
	/// </summary>
	BottomEdgeAlignedRight = 8,

	/// <summary>
	/// Preferred location is to the left of the target element, with the top edge of popup aligned with top edge of the target element.
	/// </summary>
	LeftEdgeAlignedTop = 9,

	/// <summary>
	/// Preferred location is to the left of the target element, with the bottom edge of popup aligned with bottom edge of the target element.
	/// </summary>
	LeftEdgeAlignedBottom = 10,

	/// <summary>
	/// Preferred location is to the right of the target element, with the top edge of popup aligned with top edge of the target element.
	/// </summary>
	RightEdgeAlignedTop = 11,

	/// <summary>
	/// Preferred location is to the right of the target element, with the bottom edge of popup aligned with bottom edge of the target element.
	/// </summary>
	RightEdgeAlignedBottom = 12,
}
