namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes a method to support the virtualized item control pattern. 
/// Implement this interface in order to support the capabilities that 
/// an automation client requests with a GetPattern call and PatternInterface.VirtualizedItem.
/// </summary>
public partial interface IVirtualizedItemProvider
{
	/// <summary>
	/// Makes the virtual item fully accessible as a UI Automation element.
	/// </summary>
	void Realize();
}
