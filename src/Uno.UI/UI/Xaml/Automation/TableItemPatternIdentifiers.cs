namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by ITableProvider.
/// </summary>
public partial class TableItemPatternIdentifiers
{

	internal TableItemPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the automation property that retrieves all the column headers associated with a table item or cell.
	/// </summary>
	public static AutomationProperty ColumnHeaderItemsProperty { get; } = new();

	/// <summary>
	/// Identifies the automation property that retrieves all the row headers associated with a table item or cell.
	/// </summary>
	public static AutomationProperty RowHeaderItemsProperty { get; } = new();
}
