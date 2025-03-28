using Color = Windows.UI.Color;

namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Provides access to the visual styles associated with the content of a document.
/// </summary>
public partial interface IStylesProvider
{
	/// <summary>
	/// Gets a string value that contains additional property info. The info is for 
	/// properties are that are not included in this control pattern, but that provide 
	/// information about the document content that might be useful to the user.
	/// </summary>
	string ExtendedProperties { get; }

	/// <summary>
	/// Gets the fill color of an element in a document.
	/// </summary>
	Color FillColor { get; }

	/// <summary>
	/// Gets the color of the pattern used to fill an element in a document.
	/// </summary>
	Color FillPatternColor { get; }

	/// <summary>
	/// Gets a string that represents the fill pattern style of an element in a document.
	/// </summary>
	string FillPatternStyle { get; }

	/// <summary>
	/// Gets a string that represents the shape of an element in a document.
	/// </summary>
	string Shape { get; }

	/// <summary>
	/// Gets the identifier for a visual style of an element in a document.
	/// </summary>
	int StyleId { get; }

	/// <summary>
	/// Gets the name of the visual style of an element in a document.
	/// </summary>
	string StyleName { get; }
}
