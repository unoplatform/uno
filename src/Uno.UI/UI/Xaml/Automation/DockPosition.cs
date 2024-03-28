namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values that specify the dock position of an object within a docking container. Used by IDockProvider.DockPosition.
/// </summary>
public enum DockPosition
{
	/// <summary>
	/// Indicates that the UI Automation element is docked along the top edge of the docking container.
	/// </summary>
	Top = 0,

	/// <summary>
	/// Indicates that the UI Automation element is docked along the left edge of the docking container.
	/// </summary>
	Left = 1,

	/// <summary>
	/// Indicates that the UI Automation element is docked along the bottom edge of the docking container.
	/// </summary>
	Bottom = 2,

	/// <summary>
	/// Indicates that the UI Automation element is docked along the right edge of the docking container.
	/// </summary>
	Right = 3,

	/// <summary>
	/// Indicates that the UI Automation element is docked along all edges of the docking container 
	/// and fills all available space within the container.
	/// </summary>
	Fill = 4,

	/// <summary>
	/// Indicates that the UI Automation element is not docked to any edge of the docking container.
	/// </summary>
	None = 5,
}
