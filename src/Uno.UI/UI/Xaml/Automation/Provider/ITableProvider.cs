namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation 
/// client to controls that act as containers for a collection of child elements. 
/// The children of this element must implement ITableItemProvider and be organized 
/// in a two-dimensional logical coordinate system that can be traversed 
/// (a Microsoft UI Automation client can move to adjacent controls, which 
/// are headers or cells of the table) by using the keyboard.
/// </summary>
public partial interface ITableProvider
{
	/// <summary>
	/// Gets the primary direction of traversal for the table.
	/// </summary>
	RowOrColumnMajor RowOrColumnMajor { get; }

	/// <summary>
	/// Returns a collection of UI Automation providers that represents all the column headers in a table.
	/// </summary>
	/// <returns>An array of UI Automation providers.</returns>
	IRawElementProviderSimple[] GetColumnHeaders();

	/// <summary>
	/// Returns a collection of UI Automation providers that represents all row headers in the table.
	/// </summary>
	/// <returns>An array of UI Automation providers.</returns>
	IRawElementProviderSimple[] GetRowHeaders();
}
