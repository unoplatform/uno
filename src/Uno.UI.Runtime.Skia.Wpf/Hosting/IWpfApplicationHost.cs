#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.Hosting;

internal interface IWpfApplicationHost : ISkiaApplicationHost
{
	bool IsIsland { get; }

	UIElement? RootElement { get; }

	XamlRoot? XamlRoot { get; }

	public bool IgnorePixelScaling { get; }

	void ReleasePointerCapture();

	void SetPointerCapture();

	void InvalidateRender();
}
