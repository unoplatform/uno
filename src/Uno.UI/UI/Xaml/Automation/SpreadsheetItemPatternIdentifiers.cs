namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as automation property identifiers for properties of the ISpreadsheetItemProvider pattern.
/// </summary>
public partial class SpreadsheetItemPatternIdentifiers
{
	internal SpreadsheetItemPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the Formula automation property.
	/// </summary>
	public static AutomationProperty FormulaProperty { get; } = new();
}
