using Windows.Graphics;

#pragma warning disable CS0067

namespace Microsoft.UI.Input;

/// <summary>
/// Contains event data for the EnteredMoveSize event.
/// </summary>
public partial class EnteredMoveSizeEventArgs
{
	/// <summary>
	/// Gets the type of operation being performed in the move-size loop.
	/// </summary>
	public MoveSizeOperation MoveSizeOperation { get; }

	/// <summary>
	/// Gets the position of the pointer upon entering the move-size loop.
	/// </summary>
	public PointInt32 PointerScreenPoint { get; }
}
