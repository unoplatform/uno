using Windows.Graphics;

namespace Microsoft.UI.Input;

/// <summary>
/// Provides data for the EnteredMoveSize event, which occurs when a window enters a move or size operation.
/// </summary>
public partial class EnteredMoveSizeEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EnteredMoveSizeEventArgs"/> class with the specified
	/// move/size operation and the pointer position in screen coordinates.
	/// </summary>
	/// <param name="moveSizeOperation">The type of move or size operation that was entered.</param>
	/// <param name="pointerScreenPoint">The screen coordinates of the pointer when the operation was entered.</param>
	internal EnteredMoveSizeEventArgs(MoveSizeOperation moveSizeOperation, PointInt32 pointerScreenPoint)
	{
		MoveSizeOperation = moveSizeOperation;
		PointerScreenPoint = pointerScreenPoint;
	}

	/// <summary>
	/// Gets the type of move or size operation that was entered (for example, move or resize from a specific edge or corner).
	/// </summary>
	public MoveSizeOperation MoveSizeOperation { get; }

	/// <summary>
	/// Gets the screen coordinates of the pointer when the move or size operation was entered.
	/// </summary>
	public PointInt32 PointerScreenPoint { get; }
}
