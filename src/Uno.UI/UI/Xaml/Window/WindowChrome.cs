using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

internal sealed partial class WindowChrome : ContentControl
{
	public WindowChrome(Windows.UI.Xaml.Window parent)
	{
		HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
		HorizontalContentAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
		VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
	}
}
