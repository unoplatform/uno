namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation
/// client to controls that visually expand to display content and that collapse 
/// to hide content. Implement this interface in order to support the capabilities
/// that an automation client requests with a GetPattern call and 
/// PatternInterface.ExpandCollapse.
/// </summary>
public partial interface IExpandCollapseProvider
{
	/// <summary>
	/// Gets the state (expanded or collapsed) of the control.
	/// </summary>
	ExpandCollapseState ExpandCollapseState { get; }

	/// <summary>
	/// Hides all nodes, controls, or content that are descendants of the control.
	/// </summary>
	void Collapse();

	/// <summary>
	/// Displays all child nodes, controls, or content of the control.
	/// </summary>
	void Expand();
}
