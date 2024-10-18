namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by ISelectionProvider.
/// </summary>
public partial class SelectionPatternIdentifiers
{
	internal SelectionPatternIdentifiers()
	{

	}

	/// <summary>
	/// Identifies the CanSelectMultiple automation property.
	/// </summary>
	public static AutomationProperty CanSelectMultipleProperty { get; } = new();

	/// <summary>
	/// Identifies the IsSelectionRequired automation property.
	/// </summary>
	public static AutomationProperty IsSelectionRequiredProperty { get; } = new();

	/// <summary>
	/// Identifies the property that gets the selected items in a container.
	/// </summary>
	public static AutomationProperty SelectionProperty { get; } = new();
}
