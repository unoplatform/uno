namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation 
/// client to controls that act as containers for a collection of individual, 
/// selectable child items. The children of this element must implement ISelectionItemProvider. 
/// Implement ISelectionProvider in order to support the capabilities that an automation 
/// client requests with a GetPattern call and PatternInterface.SelectionItem.
/// </summary>
public partial interface ISelectionProvider
{
	/// <summary>
	/// Gets a value that indicates whether the Microsoft UI Automation provider
	/// allows more than one child element to be selected concurrently.
	/// </summary>
	bool CanSelectMultiple { get; }

	/// <summary>
	/// Gets a value that indicates whether the UI Automation provider 
	/// requires at least one child element to be selected.
	/// </summary>
	bool IsSelectionRequired { get; }

	/// <summary>
	/// Retrieves a UI Automation provider for each child element that is selected.
	/// </summary>
	/// <returns>An array of UI Automation providers.</returns>
	IRawElementProviderSimple[] GetSelection();
}
