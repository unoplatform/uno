namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation 
/// client to controls that act as containers for a collection of child elements. 
/// Implement this interface in order to support the capabilities that an automation 
/// client requests with a GetPattern call and PatternInterface.Grid.
/// </summary>
public partial interface IGridProvider
{
	/// <summary>
	/// Gets the total number of columns in a grid.
	/// </summary>
	int ColumnCount { get; }

	/// <summary>
	/// Gets the total number of rows in a grid.
	/// </summary>
	int RowCount { get; }

	/// <summary>
	/// Retrieves the UI Automation provider for the specified cell.
	/// </summary>
	/// <param name="row">The ordinal number of the row that contains the cell.</param>
	/// <param name="column">The ordinal number of the column that contains the cell.</param>
	/// <returns>The UI Automation provider for the specified cell.</returns>
	IRawElementProviderSimple GetItem(int row, int column);
}
