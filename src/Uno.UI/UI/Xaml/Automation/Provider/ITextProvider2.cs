namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Extends the ITextProvider interface to enable Microsoft UI Automation providers 
/// to expose textual content that is the target of an annotation or selection. 
/// Implement this interface in order to support the capabilities that an automation 
/// client requests with a GetPattern call and PatternInterface.Text2.
/// </summary>
public partial interface ITextProvider2 : ITextProvider
{
	/// <summary>
	/// Exposes a text range that contains the text that is the target of the annotation 
	/// associated with the specified annotation element.
	/// </summary>
	/// <param name="annotationElement">The provider for an element that implements the 
	/// IAnnotationProvider interface. The annotation element is a sibling of the element 
	/// that implements the ITextProvider2 interface for the document.</param>
	/// <returns>A text range that contains the annotation target text.</returns>
	ITextRangeProvider RangeFromAnnotation(IRawElementProviderSimple annotationElement);

	/// <summary>
	/// Retrieves a zero-length text range at the location of the caret that belongs to
	/// the text-based control.
	/// </summary>
	/// <param name="isActive">true if the text-based control that contains the caret 
	/// has keyboard focus; otherwise, false.</param>
	/// <returns>A text range that represents the current location of the caret that 
	/// belongs to the text-based control.</returns>
	ITextRangeProvider GetCaretRange(out bool isActive);
}
