#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Hosting;

internal interface IXamlRootHost
{
	UIElement? RootElement { get; }

	void InvalidateRender();
}
