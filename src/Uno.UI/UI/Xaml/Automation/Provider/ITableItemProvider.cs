namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support Microsoft UI Automation client access 
/// to child controls of containers that implement ITableProvider. Implement this 
/// interface in order to support the capabilities that an automation client requests 
/// with a GetPattern call and PatternInterface.TableItem.
/// </summary>
public partial interface ITableItemProvider
{
	/// <summary>
	/// Retrieves an array of UI Automation providers representing all the column 
	/// headers associated with a table item or cell.
	/// </summary>
	/// <returns>An array of UI Automation providers.</returns>
	IRawElementProviderSimple[] GetColumnHeaderItems();

	/// <summary>
	/// Retrieves an array of UI Automation providers representing all the 
	/// row headers associated with a table item or cell.
	/// </summary>
	/// <returns>An array of UI Automation providers.</returns>
	IRawElementProviderSimple[] GetRowHeaderItems();
}
