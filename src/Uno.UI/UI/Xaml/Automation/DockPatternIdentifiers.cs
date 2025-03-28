namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IDockProvider.
/// </summary>
public partial class DockPatternIdentifiers
{
	internal DockPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the DockPosition automation property.
	/// </summary>
	public static AutomationProperty DockPositionProperty { get; } = new();
}
