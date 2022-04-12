namespace Windows.UI.Xaml.Hosting;

public partial class DesktopWindowXamlSource : global::System.IDisposable
{
	public global::Windows.UI.Xaml.UIElement Content { get; set; }

	public bool HasFocus => false;

	public DesktopWindowXamlSource()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Hosting.DesktopWindowXamlSource", "DesktopWindowXamlSource.DesktopWindowXamlSource()");
	}
	public global::Windows.UI.Xaml.Hosting.XamlSourceFocusNavigationResult NavigateFocus(global::Windows.UI.Xaml.Hosting.XamlSourceFocusNavigationRequest request)
	{
		return null;
	}

	public void Dispose()
	{
	}

	public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Hosting.DesktopWindowXamlSource, global::Windows.UI.Xaml.Hosting.DesktopWindowXamlSourceGotFocusEventArgs> GotFocus;

	public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Hosting.DesktopWindowXamlSource, global::Windows.UI.Xaml.Hosting.DesktopWindowXamlSourceTakeFocusRequestedEventArgs> TakeFocusRequested;
}
