using Windows.UI.Xaml.Automation.Text;

namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support Microsoft UI Automation client access
/// to a span of continuous text in a text container that implements ITextProvider.
/// </summary>
public partial interface ITextRangeProvider
{
	/// <summary>
	/// Returns a new ITextRangeProvider that is identical to the original ITextRangeProvider 
	/// and that inherits all the properties of the original.
	/// </summary>
	/// <returns>The new text range. This method never returns null.</returns>
	ITextRangeProvider Clone();

	/// <summary>
	/// Returns a value that indicates whether the start and end points of a text 
	/// range are the same as another text range.
	/// </summary>
	/// <param name="textRangeProvider">A text range to compare to the implementing 
	/// peer's text range.</param>
	/// <returns>true if the span of both text ranges is identical; otherwise, false.</returns>
	bool Compare(ITextRangeProvider textRangeProvider);

	/// <summary>
	/// Returns a value that indicates whether two text ranges have identical endpoints.
	/// </summary>
	/// <param name="endpoint">The Start or End endpoint of the caller.</param>
	/// <param name="textRangeProvider">The target range for comparison.</param>
	/// <param name="targetEndpoint">The Start or End endpoint of the target.</param>
	/// <returns>Returns a negative value if the caller's endpoint occurs earlier in 
	/// the text than the target endpoint. Returns zero if the caller's endpoint is 
	/// at the same location as the target endpoint. Returns a positive value if the 
	/// caller's endpoint occurs later in the text than the target endpoint.</returns>
	int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider textRangeProvider, TextPatternRangeEndpoint targetEndpoint);

	/// <summary>
	/// Expands the text range to the specified text unit.
	/// </summary>
	/// <param name="unit">The text measure unit.</param>
	void ExpandToEnclosingUnit(TextUnit unit);

	/// <summary>
	/// Returns a text range subset that has the specified attribute ID and attribute value.
	/// </summary>
	/// <param name="attributeId">The attribute ID to search for.</param>
	/// <param name="value">The attribute value to search for. This value must 
	/// match the type specified for the attribute.</param>
	/// <param name="backward">true if the last occurring text range should be 
	/// returned instead of the first; otherwise, false.</param>
	/// <returns>A text range that has a matching attribute ID and attribute value; 
	/// otherwise null.</returns>
	ITextRangeProvider FindAttribute(int attributeId, object value, bool backward);

	/// <summary>
	/// Returns a text range subset that contains the specified text.
	/// </summary>
	/// <param name="text">The text string to search for.</param>
	/// <param name="backward">true to return the last occurring text range instead 
	/// of the first; otherwise, false.</param>
	/// <param name="ignoreCase">true to ignore case; otherwise, false.</param>
	/// <returns>A text range that matches the specified text; otherwise null.</returns>
	ITextRangeProvider FindText(string text, bool backward, bool ignoreCase);

	/// <summary>
	/// Retrieves the value of the specified attribute ID across the text range.
	/// </summary>
	/// <param name="attributeId">The text attribute ID.</param>
	/// <returns>Retrieves an object that represents the value of the specified attribute.</returns>
	object GetAttributeValue(int attributeId);

	/// <summary>
	/// Retrieves a collection of bounding rectangles for each fully or partially 
	/// visible line of text in a text range.
	/// </summary>
	/// <param name="returnValue">An array of bounding rectangles for each full or 
	/// partial line of text in a text range. An empty array for a degenerate range. 
	/// An empty array for a text range that has screen coordinates placing it completely 
	/// off-screen, scrolled out of view, or obscured by an overlapping window.</param>
	void GetBoundingRectangles(out double[] returnValue);

	/// <summary>
	/// Returns the innermost element that encloses the text range.
	/// </summary>
	/// <returns>The enclosing control, typically the text provider that provides 
	/// the text range. However, if the text provider supports child text elements
	/// such as tables or hyperlinks, the enclosing element can be a descendant of
	/// the text provider.</returns>
	IRawElementProviderSimple GetEnclosingElement();

	/// <summary>
	/// Retrieves the plain text of the range.
	/// </summary>
	/// <param name="maxLength">The maximum length of the string to return.
	/// Use � 1 to specify an unlimited length.</param>
	/// <returns>The plain text of the text range, which might represent a portion
	/// of the full string truncated at the specified maxLength.</returns>
	string GetText(int maxLength);

	/// <summary>
	/// Moves the text range the specified number of text units.
	/// </summary>
	/// <param name="unit">The text unit boundary.</param>
	/// <param name="count">The number of text units to move. A positive value 
	/// moves the text range forward; a negative value moves the text range backward; 
	/// and a value of 0 has no effect.</param>
	/// <returns>The number of units actually moved. This value can be less than the 
	/// count requested if either of the new text range endpoints is greater than or
	/// less than the DocumentRange endpoints. This value can be negative if navigation 
	/// is happening in the backward direction.</returns>
	int Move(TextUnit unit, int count);

	/// <summary>
	/// Moves one endpoint of the text range the specified number of text units within the document range.
	/// </summary>
	/// <param name="endpoint">The endpoint to move.</param>
	/// <param name="unit">The text measure unit for moving.</param>
	/// <param name="count">The number of units to move. A positive value moves the endpoint forward.
	/// A negative value moves it backward. A value of 0 has no effect.</param>
	/// <returns>The number of units actually moved, which can be less than the number requested if
	/// moving the endpoint runs into the beginning or end of the document.</returns>
	int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);

	/// <summary>
	/// Moves one endpoint of a text range to the specified endpoint of a second text range.
	/// </summary>
	/// <param name="endpoint">The endpoint to move.</param>
	/// <param name="textRangeProvider">Another range from the same text provider.</param>
	/// <param name="targetEndpoint">An endpoint on the other range.</param>
	void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider textRangeProvider, TextPatternRangeEndpoint targetEndpoint);

	/// <summary>
	/// Highlights text in the text control that corresponds to the start and end endpoints of the text range.
	/// </summary>
	void Select();

	/// <summary>
	/// Adds to the collection of highlighted text in a text container that supports multiple disjoint selections.
	/// </summary>
	void AddToSelection();

	/// <summary>
	/// From the collection of highlighted text in a text container that supports multiple disjoint selections,
	/// removes a highlighted section of text that corresponds to the caller's text range endpoints.
	/// </summary>
	void RemoveFromSelection();

	/// <summary>
	/// Causes the text control to scroll vertically until the text range is visible in the viewport.
	/// </summary>
	/// <param name="alignToTop"></param>
	void ScrollIntoView(bool alignToTop);

	/// <summary>
	/// Retrieves a collection of all the embedded objects that exist within the text range.
	/// </summary>
	/// <returns>A collection of child objects that exist within the range. Child objects that overlap 
	/// with the text range but are not completely enclosed by it are also included in the collection. 
	/// Returns an empty collection if no child objects exist.</returns>
	IRawElementProviderSimple[] GetChildren();

}
