namespace Microsoft.UI.Input;

/// <summary>
/// Specifies the type of move or size operation being performed on a window.
/// </summary>
public enum MoveSizeOperation
{
	/// <summary>
	/// The window is being moved.
	/// </summary>
	Move = 0,

	/// <summary>
	/// The window is being resized from the bottom edge.
	/// </summary>
	SizeBottom = 1,

	/// <summary>
	/// The window is being resized from the bottom-left corner.
	/// </summary>
	SizeBottomLeft = 2,

	/// <summary>
	/// The window is being resized from the bottom-right corner.
	/// </summary>
	SizeBottomRight = 3,

	/// <summary>
	/// The window is being resized from the left edge.
	/// </summary>
	SizeLeft = 4,

	/// <summary>
	/// The window is being resized from the right edge.
	/// </summary>
	SizeRight = 5,

	/// <summary>
	/// The window is being resized from the top edge.
	/// </summary>
	SizeTop = 6,

	/// <summary>
	/// The window is being resized from the top-left corner.
	/// </summary>
	SizeTopLeft = 7,

	/// <summary>
	/// The window is being resized from the top-right corner.
	/// </summary>
	SizeTopRight = 8,
}
