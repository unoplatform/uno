namespace Microsoft.UI.Windowing;

/// <summary>
/// Provides data for the AppWindow.Changed event.
/// </summary>
public partial class AppWindowChangedEventArgs
{
	internal AppWindowChangedEventArgs(
		bool didPositionChange,
		bool didPresenterChange,
		bool didSizeChange,
		bool didVisibilityChange,
		bool didZOrderChange,
		bool isZOrderAtBottom,
		bool isZOrderAtTop,
		WindowId zOrderBelowWindowId)
	{
		DidPositionChange = didPositionChange;
		DidPresenterChange = didPresenterChange;
		DidSizeChange = didSizeChange;
		DidVisibilityChange = didVisibilityChange;
		DidZOrderChange = didZOrderChange;
		IsZOrderAtBottom = isZOrderAtBottom;
		IsZOrderAtTop = isZOrderAtTop;
		ZOrderBelowWindowId = zOrderBelowWindowId;
	}

	/// <summary>
	/// Gets a value that indicates whether the AppWindow.Position property changed.
	/// </summary>
	public bool DidPositionChange { get; }

	/// <summary>
	/// Gets a value that indicates whether the AppWindow.Presenter property changed.
	/// </summary>
	public bool DidPresenterChange { get; }

	/// <summary>
	/// Gets a value that indicates whether the AppWindow.Size property changed.
	/// </summary>
	public bool DidSizeChange { get; }

	/// <summary>
	/// Gets a value that indicates whether the AppWindow.IsVisible property changed.
	/// </summary>
	public bool DidVisibilityChange { get; }

	/// <summary>
	/// Gets a value that indicates whether the window's position in the Z-order changed.
	/// </summary>
	public bool DidZOrderChange { get; }

	/// <summary>
	/// Gets a value that indicates whether the window is now at the bottom of the Z-order.
	/// </summary>
	public bool IsZOrderAtBottom { get; }

	/// <summary>
	/// Gets a value that indicates whether the window is now at the top of the Z-order.
	/// </summary>
	public bool IsZOrderAtTop { get; }

	/// <summary>
	/// Gets the ID of the window directly above this window in Z-order, if Z-order changed and this window is not the top window.
	/// </summary>
	public WindowId ZOrderBelowWindowId { get; }
}
