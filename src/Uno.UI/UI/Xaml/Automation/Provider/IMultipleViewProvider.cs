namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support Microsoft UI Automation client 
/// access to controls that provide, and are able to switch between, multiple 
/// representations of the same set of information or child controls. Implement 
/// this interface in order to support the capabilities that an automation client 
/// requests with a GetPattern call and PatternInterface.MultipleView.
/// </summary>
public partial interface IMultipleViewProvider
{
	/// <summary>
	/// Gets the current control-specific view.
	/// </summary>
	int CurrentView { get; }

	/// <summary>
	/// Retrieves a collection of control-specific view identifiers.
	/// </summary>
	/// <returns>A collection of values that identifies the views available for a UI Automation element.</returns>
	int[] GetSupportedViews();

	/// <summary>
	/// Retrieves the name of a control-specific view.
	/// </summary>
	/// <param name="viewId">The view identifier.</param>
	/// <returns>A localized name for the view.</returns>
	string GetViewName(int viewId);

	/// <summary>
	/// Sets the current control-specific view.
	/// </summary>
	/// <param name="viewId">A view identifier.</param>
	void SetCurrentView(int viewId);
}
