using Uno;

namespace Windows.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// Marks a DependencyObject element as a type that contains a collection of segments.
/// </summary>
/// <remarks>
/// The type is needed in order to redraw an element after changes to its properties,
/// or properties of its children.
/// </remarks>
[UnoOnly]
internal partial interface ISegmentsElement
{
	/// <summary>
	/// Sets the state of segments as "Invalid".
	/// </summary>
	/// <remarks>
	/// The status "Invalid" indicates, that the element needs to be redrawn.
	/// This method is required for optimization. 
	/// If only the text has changed, the redraw process will be faster using this method.
	/// </remarks>
	void InvalidateSegments();

	/// <summary>
	/// Sets the state of element as "Invalid".
	/// </summary>
	/// <remarks>
	/// The status "Invalid" indicates, that the element needs to be redrawn.
	/// This method is called if any properties have been changed.
	/// </remarks>
	void InvalidateElement();
}
