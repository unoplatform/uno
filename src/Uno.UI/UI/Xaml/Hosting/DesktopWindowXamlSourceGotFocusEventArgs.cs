namespace Windows.UI.Xaml.Hosting;

/// <summary>
/// Provides event data for the GotFocus event.
/// </summary>
public partial class DesktopWindowXamlSourceGotFocusEventArgs
{
	internal DesktopWindowXamlSourceGotFocusEventArgs(XamlSourceFocusNavigationRequest request)
	{
		Request = request;
	}

	/// <summary>
	/// Gets a XamlSourceFocusNavigationRequest object that specifies the reason and other info for the focus navigation.
	/// </summary>
	public XamlSourceFocusNavigationRequest Request { get; }
}
