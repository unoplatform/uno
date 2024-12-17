namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Provides access to items (cells) in a spreadsheet.
/// </summary>
public partial interface ISpreadsheetProvider
{
	/// <summary>
	/// Returns a Microsoft UI Automation element that represents the spreadsheet cell that has the specified name.
	/// </summary>
	/// <param name="name">The name of the target cell.</param>
	/// <returns>A Microsoft UI Automation element that represents the target cell.</returns>
	IRawElementProviderSimple GetItemByName(string name);
}
