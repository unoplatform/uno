using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Core;

internal static class FxCallbacks
{
	internal static void XamlRoot_RaiseChanged(XamlRoot xamlRoot) => xamlRoot.RaiseChangedEvent();
}
