using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml
{
	public sealed partial class XamlRoot
	{
		//TODO Uno: This implementation does not match WinUI, but we currently support only
		//a single XamlRoot and a single window. This will need to be adjusted later though.
		internal VisualTree VisualTree => DXamlCore.Current.GetHandle().ContentRootCoordinator.CoreWindowContentRoot.VisualTree;
	}
}
