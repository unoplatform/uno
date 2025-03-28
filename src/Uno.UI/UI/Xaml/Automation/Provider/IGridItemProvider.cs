namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation 
/// client to individual child controls of containers that implement IGridProvider. 
/// Implement this interface in order to support the capabilities that an automation 
/// client requests with a GetPattern call and PatternInterface.GridItem.
/// </summary>
public partial interface IGridItemProvider
{
	/// <summary>
	/// Gets the ordinal number of the column that contains the cell or item.
	/// </summary>
	int Column { get; }

	/// <summary>
	/// Gets the number of columns that are spanned by a cell or item.
	/// </summary>
	int ColumnSpan { get; }

	/// <summary>
	/// Gets a UI Automation provider that implements IGridProvider and that represents the container of the cell or item.
	/// </summary>
	IRawElementProviderSimple ContainingGrid { get; }

	/// <summary>
	/// Gets the ordinal number of the row that contains the cell or item.
	/// </summary>
	int Row { get; }

	/// <summary>
	/// Gets the number of rows spanned by a cell or item.
	/// </summary>
	int RowSpan { get; }
}
