using Windows.Foundation;

namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support Microsoft UI Automation client 
/// access to controls that contain text. Implement this interface in order to 
/// support the capabilities that an automation client requests with a GetPattern 
/// call and PatternInterface.Text.
/// </summary>
public partial interface ITextProvider
{
	/// <summary>
	/// Gets a text range that encloses the main text of a document.
	/// </summary>
	ITextRangeProvider DocumentRange { get; }

	/// <summary>
	/// Gets a value that specifies whether a text provider supports selection, 
	/// and if it does, the type of selection that is supported.
	/// </summary>
	SupportedTextSelection SupportedTextSelection { get; }

	/// <summary>
	/// Retrieves a collection of disjoint text ranges that are associated 
	/// with the current text selection or selections.
	/// </summary>
	/// <returns>A collection of disjoint text ranges.</returns>
	ITextRangeProvider[] GetSelection();

	/// <summary>
	/// Retrieves an array of disjoint text ranges from a text container.
	/// Each text range begins with the first partially visible line and
	/// ends with the last partially visible line.
	/// </summary>
	/// <returns>
	/// The collection of visible text ranges within a container or
	/// an empty array. This method never returns null.
	/// </returns>
	ITextRangeProvider[] GetVisibleRanges();

	/// <summary>
	/// Retrieves a text range that encloses a child element, such as an image, hyperlink, or other embedded object.
	/// </summary>
	/// <param name="childElement">The enclosed object.</param>
	/// <returns>A range that spans the child element.</returns>
	ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement);

	/// <summary>
	/// Retrieves a text range from the vicinity of a screen coordinate.
	/// </summary>
	/// <param name="screenLocation">The coordinate screen location.</param>
	/// <returns>A range that contains text.</returns>
	ITextRangeProvider RangeFromPoint(Point screenLocation);
}
