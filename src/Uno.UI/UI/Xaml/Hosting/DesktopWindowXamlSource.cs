using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Hosting;

public partial class DesktopWindowXamlSource : global::System.IDisposable
{
	private XamlIslandRoot _root;

	public global::Windows.UI.Xaml.UIElement Content
	{
		get => _root?.ContentRoot.VisualTree.PublicRootVisual;
		set
		{
			if (_root is null)
			{
				_root = new XamlIslandRoot(CoreServices.Instance);
			}

			_root.SetPublicRootVisual(value);

			UIElement.LoadingRootElement(_root);
			UIElement.RootElementLoaded(_root);
		}
	}	

	public bool HasFocus => false;

	public DesktopWindowXamlSource()
	{
		
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
