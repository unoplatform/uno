namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IValueProvider.
/// </summary>
public partial class ValuePatternIdentifiers
{
	/// <summary>
	/// Identifies the IsReadOnly property.
	/// </summary>
	public static AutomationProperty IsReadOnlyProperty { get; } = new();

	/// <summary>
	/// Identifies the Value automation property.
	/// </summary>
	public static AutomationProperty ValueProperty { get; } = new();
}
