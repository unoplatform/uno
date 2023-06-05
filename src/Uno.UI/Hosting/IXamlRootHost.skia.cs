#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Hosting;

internal interface IXamlRootHost
{
	bool IsIsland { get; }

	UIElement? RootElement { get; }

	XamlRoot? XamlRoot { get; }

	void InvalidateRender();
}
