using Windows.Graphics;

#pragma warning disable CS0067

namespace Microsoft.UI.Input;

/// <summary>
/// Contains event data for the WindowRectChanging event.
/// </summary>
public partial class WindowRectChangingEventArgs
{
	/// <summary>
	/// Gets or sets whether the window should be shown.
	/// </summary>
	public bool ShowWindow { get; set; }

	/// <summary>
	/// Gets or sets the new rect that the window will change to (if nothing prevents the change).
	/// </summary>
	public RectInt32 NewWindowRect { get; set; }

	/// <summary>
	/// Gets or sets whether the window rect is allowed to change.
	/// </summary>
	public bool AllowRectChange { get; set; }

	/// <summary>
	/// Gets the type of operation being performed in the move-size loop.
	/// </summary>
	public MoveSizeOperation MoveSizeOperation { get; }

	/// <summary>
	/// Gets the old rect from which the window will be changing (if nothing prevents the change).
	/// </summary>
	public RectInt32 OldWindowRect { get; }

	/// <summary>
	/// Gets the position of the pointer when the window position is about to change.
	/// </summary>
	public PointInt32 PointerScreenPoint { get; }
}
