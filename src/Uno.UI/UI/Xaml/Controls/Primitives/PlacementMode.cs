namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify the preferred location for positioning a ToolTip relative to a visual element.
/// </summary>
public enum PlacementMode
{
	/// <summary>
	/// The preferred location of the ToolTip is below the target element when element
	/// receives keyboard focus, at the bottom of the mouse pointer when element is hovered over with pointer.
	/// </summary>
	Bottom = 2,

	/// <summary>
	/// The preferred location of the ToolTip is to the left of the target element when element receives
	/// keyboard focus, to the left of the mouse pointer when element is hovered over with pointer.
	/// </summary>
	Left = 9,

	/// <summary>
	/// The preferred location of the ToolTip is with the top-left corner of the tooltip positioned
	/// at the mouse pointer location when hovered over with mouse, above the target element
	/// when focused with keyboard.
	/// </summary>
	Mouse = 7,

	/// <summary>
	/// The preferred location of the ToolTip is to the right of the target element when element
	/// receives keyboard focus, to the right of the mouse pointer when element is hovered over with pointer.
	/// </summary>
	Right = 4,

	/// <summary>
	/// The preferred location of the ToolTip is above the target element when element receives keyboard focus,
	/// at the top of the mouse pointer when element is hovered over with pointer.
	/// </summary>
	Top = 10
}
