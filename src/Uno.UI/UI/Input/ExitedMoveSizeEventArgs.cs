using Windows.Graphics;

namespace Microsoft.UI.Input;

/// <summary>
/// Provides data for the ExitedMoveSize event, which occurs when a window has finished a move or size operation.
/// </summary>
public partial class ExitedMoveSizeEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ExitedMoveSizeEventArgs"/> class with the specified
	/// move/size operation and the pointer position in screen coordinates.
	/// </summary>
	/// <param name="moveSizeOperation">The type of move or size operation that was exited.</param>
	/// <param name="pointerScreenPoint">The screen coordinates of the pointer when the operation ended.</param>
	internal ExitedMoveSizeEventArgs(MoveSizeOperation moveSizeOperation, PointInt32 pointerScreenPoint)
	{
		MoveSizeOperation = moveSizeOperation;
		PointerScreenPoint = pointerScreenPoint;
	}

	/// <summary>
	/// Gets the type of move or size operation that was exited (for example, move or resize from a specific edge or corner).
	/// </summary>
	public MoveSizeOperation MoveSizeOperation { get; }

	/// <summary>
	/// Gets the screen coordinates of the pointer when the move or size operation ended.
	/// </summary>
	public PointInt32 PointerScreenPoint { get; }
}
