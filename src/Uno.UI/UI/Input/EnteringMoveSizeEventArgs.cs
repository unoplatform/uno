using Windows.Graphics;

namespace Microsoft.UI.Input;

/// <summary>
/// Provides data for the EnteringMoveSize event, which occurs when a window is about to enter a move or size operation.
/// </summary>
public partial class EnteringMoveSizeEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EnteringMoveSizeEventArgs"/> class with the specified
	/// window identifier, move/size operation, and pointer position in screen coordinates.
	/// </summary>
	/// <param name="moveSizeWindowId">The identifier of the window that is entering the move or size operation.</param>
	/// <param name="moveSizeOperation">The type of move or size operation that is being entered.</param>
	/// <param name="pointerScreenPoint">The screen coordinates of the pointer when the operation is starting.</param>
	internal EnteringMoveSizeEventArgs(WindowId moveSizeWindowId, MoveSizeOperation moveSizeOperation, PointInt32 pointerScreenPoint)
	{
		MoveSizeWindowId = moveSizeWindowId;
		MoveSizeOperation = moveSizeOperation;
		PointerScreenPoint = pointerScreenPoint;
	}

	/// <summary>
	/// Gets or sets the identifier of the window that is entering the move or size operation.
	/// </summary>
	public WindowId MoveSizeWindowId { get; set; }

	/// <summary>
	/// Gets the type of move or size operation that is being entered (for example, move or resize from a specific edge or corner).
	/// </summary>
	public MoveSizeOperation MoveSizeOperation { get; }

	/// <summary>
	/// Gets the screen coordinates of the pointer when the move or size operation is starting.
	/// </summary>
	public PointInt32 PointerScreenPoint { get; }
}
