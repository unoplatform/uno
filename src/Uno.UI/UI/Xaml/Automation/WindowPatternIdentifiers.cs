namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IWindowProvider.
/// </summary>
public partial class WindowPatternIdentifiers
{

	internal WindowPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the Maximizable automation property.
	/// </summary>
	public static AutomationProperty CanMaximizeProperty { get; } = new();

	/// <summary>
	/// Identifies the Minimizable automation property.
	/// </summary>
	public static AutomationProperty CanMinimizeProperty { get; } = new();

	/// <summary>
	/// Identifies the IsModal automation property.
	/// </summary>
	public static AutomationProperty IsModalProperty { get; } = new();

	/// <summary>
	/// Identifies the IsTopmost automation property.
	/// </summary>
	public static AutomationProperty IsTopmostProperty { get; } = new();

	/// <summary>
	/// Identifies the InteractionState automation property.
	/// </summary>
	public static AutomationProperty WindowInteractionStateProperty { get; } = new();

	/// <summary>
	/// Identifies the VisualState automation property.
	/// </summary>
	public static AutomationProperty WindowVisualStateProperty { get; } = new();
}
