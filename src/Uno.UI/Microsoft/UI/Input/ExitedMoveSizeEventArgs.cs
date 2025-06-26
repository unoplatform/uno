using Windows.Graphics;

#pragma warning disable CS0067

namespace Microsoft.UI.Input;

/// <summary>
/// Contains event data for the ExitedMoveSize event.
/// </summary>
public partial class ExitedMoveSizeEventArgs
{
	/// <summary>
	/// Gets the type of operation being performed in the move-size loop.
	/// </summary>
	public MoveSizeOperation MoveSizeOperation { get; }

	/// <summary>
	/// Gets the position of the pointer prior to entering the move-size loop.
	/// </summary>
	public PointInt32 PointerScreenPoint { get; }
}
