namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes the properties of an annotation in a document. Implement this interface in order to support 
/// the capabilities that an automation client requests with a GetPattern call and PatternInterface.Annotation.
/// </summary>
public partial interface IAnnotationProvider
{
	/// <summary>
	/// Gets the annotation type identifier of this annotation.
	/// </summary>
	int AnnotationTypeId { get; }

	/// <summary>
	/// Gets the name of this annotation type.
	/// </summary>
	string AnnotationTypeName { get; }

	/// <summary>
	/// Gets the name of the annotation author.
	/// </summary>
	string Author { get; }

	/// <summary>
	/// Gets the date and time when this annotation was created.
	/// </summary>
	string DateTime { get; }

	/// <summary>
	/// Gets the UI Automation element that is being annotated.
	/// </summary>
	IRawElementProviderSimple Target { get; }
}
