namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IDropTargetProvider.
/// </summary>
public partial class DropTargetPatternIdentifiers
{
	internal DropTargetPatternIdentifiers()
	{
	}

	/// <summary>
	/// Gets the identifier for the DropEffect automation property.
	/// </summary>
	public static AutomationProperty DropTargetEffectProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the DropEffects automation property.
	/// </summary>
	public static AutomationProperty DropTargetEffectsProperty { get; } = new();
}
