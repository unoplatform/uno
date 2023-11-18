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
	public static AutomationProperty AnnotationTypeIdProperty => new();

	/// <summary>
	/// Gets the identifier for the AnnotationTypeName automation property.
	/// </summary>
	public static AutomationProperty AnnotationTypeNameProperty => new();

	/// <summary>
	/// Gets the identifier for the Author automation property.
	/// </summary>
	public static AutomationProperty AuthorProperty => new();

	/// <summary>
	/// Gets the identifier for the DateTime automation property.
	/// </summary>
	public static AutomationProperty DateTimeProperty => new();

	/// <summary>
	/// Gets the identifier for the Target automation property.
	/// </summary>
	public static AutomationProperty TargetProperty => new();
}
