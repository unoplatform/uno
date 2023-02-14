#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace Uno.UI.XamlHost.Extensions;

public static class DesktopWindowXamlSourceExtension
{
	public static UIElement? GetVisualTreeRoot(this DesktopWindowXamlSource source) =>
		source.Content?.XamlRoot?.VisualTree.RootElement;
}
