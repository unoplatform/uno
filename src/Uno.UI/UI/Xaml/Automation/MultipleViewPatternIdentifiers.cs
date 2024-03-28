namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IMultipleViewProvider.
/// </summary>
public partial class MultipleViewPatternIdentifiers
{

	internal MultipleViewPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the CurrentView automation property.
	/// </summary>
	public static AutomationProperty CurrentViewProperty { get; } = new();

	/// <summary>
	/// Identifies the automation property that gets the control-specific collection of views.
	/// </summary>
	public static AutomationProperty SupportedViewsProperty { get; } = new();
}
