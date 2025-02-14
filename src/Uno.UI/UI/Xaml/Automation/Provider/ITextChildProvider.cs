namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Provides access to a text-based control that is a child of another text-based
/// control. Implement this interface in order to support the capabilities that an
/// automation client requests with a GetPattern call and PatternInterface.TextChild.
/// </summary>
public partial interface ITextChildProvider
{
	/// <summary>
	/// Gets this element's nearest ancestor provider that supports the Text (ITextProvider) control pattern.
	/// </summary>
	IRawElementProviderSimple TextContainer { get; }

	/// <summary>
	/// Gets a text range that encloses this child element.
	/// </summary>
	ITextRangeProvider TextRange { get; }
}
