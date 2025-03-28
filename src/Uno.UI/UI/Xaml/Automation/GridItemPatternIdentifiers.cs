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
	public static AutomationProperty ColumnProperty { get; } = new();

	/// <summary>
	/// Identifies the ColumnSpan automation property.
	/// </summary>
	public static AutomationProperty ColumnSpanProperty { get; } = new();

	/// <summary>
	/// Identifies the ContainingGrid automation property.
	/// </summary>
	public static AutomationProperty ContainingGridProperty { get; } = new();

	/// <summary>
	/// Identifies the Row automation property.
	/// </summary>
	public static AutomationProperty RowProperty { get; } = new();

	/// <summary>
	/// Identifies the RowSpan property.
	/// </summary>
	public static AutomationProperty RowSpanProperty { get; } = new();
}
