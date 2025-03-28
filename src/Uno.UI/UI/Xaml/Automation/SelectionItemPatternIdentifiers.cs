namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by ISelectionItemProvider.
/// </summary>
public partial class SelectionItemPatternIdentifiers
{
	/// <summary>
	/// Identifies the IsSelected automation property.
	/// </summary>
	public static AutomationProperty IsSelectedProperty { get; } = new();

	/// <summary>
	/// Identifies the SelectionContainer automation property.
	/// </summary>
	public static AutomationProperty SelectionContainerProperty { get; } = new();
}
