namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Extends the ITextRange interface to enable Microsoft UI Automation providers
/// to programmatically open context menus that are contextual to text input operations.
/// </summary>
public partial interface ITextRangeProvider2 : ITextRangeProvider
{
	/// <summary>
	/// Shows the available context menu for the owner element.
	/// </summary>
	void ShowContextMenu();
}
