namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IAnnotationProvider.
/// </summary>
public partial class AnnotationPatternIdentifiers
{
	internal AnnotationPatternIdentifiers()
	{
	}

	/// <summary>
	/// Gets the identifier for the AnnotationTypeId automation property.
	/// </summary>
	public static AutomationProperty AnnotationTypeIdProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the AnnotationTypeName automation property.
	/// </summary>
	public static AutomationProperty AnnotationTypeNameProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the Author automation property.
	/// </summary>
	public static AutomationProperty AuthorProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the DateTime automation property.
	/// </summary>
	public static AutomationProperty DateTimeProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the Target automation property.
	/// </summary>
	public static AutomationProperty TargetProperty { get; } = new();
}
