namespace Windows.UI.Xaml.Automation;


/// <summary>
/// Contains values used as identifiers by IDragProvider.
/// </summary>
public partial class DragPatternIdentifiers
{
	internal DragPatternIdentifiers()
	{
	}

	/// <summary>
	/// Gets the identifier for the DropEffect automation property.
	/// </summary>
	public static AutomationProperty DropEffectProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the DropEffects automation property.
	/// </summary>
	public static AutomationProperty DropEffectsProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the GrabbedItems automation property.
	/// </summary>
	public static AutomationProperty GrabbedItemsProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the IsGrabbed automation property.
	/// </summary>
	public static AutomationProperty IsGrabbedProperty { get; } = new();
}
