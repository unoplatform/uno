namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IGridItemProvider.
/// </summary>
public partial class GridItemPatternIdentifiers
{
	internal GridItemPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the Column automation property.
	/// </summary>
	public static AutomationProperty ColumnProperty => new();

	/// <summary>
	/// Identifies the ColumnSpan automation property.
	/// </summary>
	public static AutomationProperty ColumnSpanProperty => new();

	/// <summary>
	/// Identifies the ContainingGrid automation property.
	/// </summary>
	public static AutomationProperty ContainingGridProperty => new();

	/// <summary>
	/// Identifies the Row automation property.
	/// </summary>
	public static AutomationProperty RowProperty => new();

	/// <summary>
	/// Identifies the RowSpan property.
	/// </summary>
	public static AutomationProperty RowSpanProperty => new();
}
