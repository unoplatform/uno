namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation 
/// client to controls that expose their dock properties in a docking container. 
/// Implement this interface in order to support the capabilities that an automation 
/// client requests with a GetPattern call and PatternInterface.Dock.
/// </summary>
public partial interface IDockProvider
{
	/// <summary>
	/// Gets the current DockPosition of the control in a docking container.
	/// </summary>
	DockPosition DockPosition { get; }

	/// <summary>
	/// Docks the control in a docking container.
	/// </summary>
	/// <param name="dockPosition">
	/// The dock position, relative to the boundaries of the docking container and to other elements in the container.
	/// </param>
	void SetDockPosition(DockPosition dockPosition);
}
