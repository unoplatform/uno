namespace Microsoft.UI.Input;

/// <summary>
/// Specifies the types of move and resize operations being performed on an object.
/// </summary>
public enum MoveSizeOperation
{
	/// <summary>
	/// The object is being moved.
	/// </summary>
	Move = 0,

	/// <summary>
	/// The object is being resized with the bottom border.
	/// </summary>
	SizeBottom = 1,

	/// <summary>
	/// The object is being resized with the bottom-left corner.
	/// </summary>
	SizeBottomLeft = 2,

	/// <summary>
	/// The object is being resized with the bottom-right corner.
	/// </summary>
	SizeBottomRight = 3,

	/// <summary>
	/// The object is being resized with the left border.
	/// </summary>
	SizeLeft = 4,

	/// <summary>
	/// The object is being resized with the right border.
	/// </summary>
	SizeRight = 5,

	/// <summary>
	/// The object is being resized with the top border.
	/// </summary>
	SizeTop = 6,

	/// <summary>
	/// The object is being resized with the top-left corner.
	/// </summary>
	SizeTopLeft = 7,

	/// <summary>
	/// The object is being resized with the top-right corner.
	/// </summary>
	SizeTopRight = 8,
}
