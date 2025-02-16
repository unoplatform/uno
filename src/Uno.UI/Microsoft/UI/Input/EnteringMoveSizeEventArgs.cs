using Windows.Graphics;

#pragma warning disable CS0067

namespace Microsoft.UI.Input;

/// <summary>
/// Contains event data for the EnteringMoveSize event.
/// </summary>
public partial class EnteringMoveSizeEventArgs
{
	/// <summary>
	/// Gets or sets the ID of the window for which to enter the move-size loop.
	/// </summary>
	public WindowId MoveSizeWindowId { get; set; }

	/// <summary>
	/// Gets the type of operation being performed in the move-size loop.
	/// </summary>
	public MoveSizeOperation MoveSizeOperation { get; }

	/// <summary>
	/// Gets the position of the pointer upon entering the move-size loop.
	/// </summary>
	public PointInt32 PointerScreenPoint { get; }
}
