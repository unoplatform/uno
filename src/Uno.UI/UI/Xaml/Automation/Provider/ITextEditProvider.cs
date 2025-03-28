namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Extends the ITextProvider interface to support access by a Microsoft UI Automation 
/// client to controls that support programmatic text-edit actions. Implement ITextEditProvider
/// in order to support the capabilities that an automation client requests with a 
/// GetPattern call and PatternInterface.TextEdit.
/// </summary>
public partial interface ITextEditProvider : ITextProvider
{
	/// <summary>
	/// Gets the active composition.
	/// </summary>
	/// <returns>The active composition.</returns>
	ITextRangeProvider GetActiveComposition();

	/// <summary>
	/// Gets the current conversion target.
	/// </summary>
	/// <returns>The current conversion target.</returns>
	ITextRangeProvider GetConversionTarget();
}
