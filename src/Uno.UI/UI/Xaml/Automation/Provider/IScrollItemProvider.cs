namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by Microsoft UI Automation 
/// client to individual child controls of containers that implement IScrollProvider. 
/// Implement this interface in order to support the capabilities that an automation 
/// client requests with a GetPattern call and PatternInterface.ScrollItem.
/// </summary>
public partial interface IScrollItemProvider
{
	/// <summary>
	/// Scrolls the content area of a container object in order to display the control 
	/// within the visible region (viewport) of the container.
	/// </summary>
	void ScrollIntoView();
}
