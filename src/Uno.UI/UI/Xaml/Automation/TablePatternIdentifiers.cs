namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by ITableProvider.
/// </summary>
public partial class TablePatternIdentifiers
{
	internal TablePatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the automation property that is accessed by the GetColumnHeaders method.
	/// </summary>
	public static AutomationProperty ColumnHeadersProperty => new();

	/// <summary>
	/// Identifies the automation property that is accessed by the GetRowHeaders method.
	/// </summary>
	public static AutomationProperty RowHeadersProperty => new();

	/// <summary>
	/// Identifies the RowOrColumnMajor automation property.
	/// </summary>
	public static AutomationProperty RowOrColumnMajorProperty => new();
}
