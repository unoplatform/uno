namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes a Microsoft UI Automation method to enable applications to find an element 
/// in a container, such as a virtualized list. Implement this interface in order to 
/// support the capabilities that an automation client requests with a GetPattern call
/// and PatternInterface.ItemContainer.
/// </summary>
public partial interface IItemContainerProvider
{
	/// <summary>
	/// Retrieves an element by the specified property value.
	/// </summary>
	/// <param name="startAfter">The item in the container after which to begin the search.</param>
	/// <param name="automationProperty">The property that contains the value to retrieve.</param>
	/// <param name="value">The value to retrieve.</param>
	/// <returns>The first item that matches the search criterion; otherwise, null.</returns>
	IRawElementProviderSimple FindItemByProperty(IRawElementProviderSimple startAfter, AutomationProperty automationProperty, object value);
}
