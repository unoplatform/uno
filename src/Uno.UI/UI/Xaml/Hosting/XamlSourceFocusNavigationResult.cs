namespace Windows.UI.Xaml.Hosting;

/// <summary>
/// Provides data for a request to navigate focus to
/// a DesktopWindowXamlSource object by using the NavigateFocus method.
/// </summary>
public partial class XamlSourceFocusNavigationResult
{
	/// <summary>
	/// Initializes a new instance of the XamlSourceFocusNavigationResult class.
	/// </summary>
	/// <param name="focusMoved">True if the focus successfully moved
	/// to the DesktopWindowXamlSource object; otherwise, false.</param>
	public XamlSourceFocusNavigationResult(bool focusMoved)
	{
		WasFocusMoved = focusMoved;
	}

	/// <summary>
	/// Gets a value that indicates whether the focus successfully
	/// moved to the DesktopWindowXamlSource object.
	/// </summary>
	public bool WasFocusMoved { get; }
}
