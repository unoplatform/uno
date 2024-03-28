namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by ITransformProvider.
/// </summary>
public partial class TransformPatternIdentifiers
{
	internal TransformPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the CanMove automation property.
	/// </summary>
	public static AutomationProperty CanMoveProperty { get; } = new();

	/// <summary>
	/// Identifies the CanResize automation property.
	/// </summary>
	public static AutomationProperty CanResizeProperty { get; } = new();

	/// <summary>
	/// Identifies the CanRotate automation property.
	/// </summary>
	public static AutomationProperty CanRotateProperty { get; } = new();
}
