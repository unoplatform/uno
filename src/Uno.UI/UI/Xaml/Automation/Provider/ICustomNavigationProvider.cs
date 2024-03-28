namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes a method to support access by a Microsoft UI Automation client to controls that support 
/// a custom navigation order. Implement ICustomNavigationProvider to support the capabilities that 
/// an automation client requests with a GetPattern call and PatternInterface.CustomNavigation.
/// </summary>
public partial interface ICustomNavigationProvider
{
	/// <summary>
	/// Gets the next element in the specified direction within the logical UI tree.
	/// </summary>
	/// <param name="direction">The specified direction.</param>
	/// <returns>The next element.</returns>
	object NavigateCustom(Peers.AutomationNavigationDirection direction);
}
