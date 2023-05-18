#nullable enable

using Uno;
using Windows.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf.Hosting;

internal interface IWpfXamlRootHost
{
	bool IsIsland { get; }

	UIElement? RootElement { get; }

	WpfCanvas? NativeOverlayLayer { get; }

	XamlRoot? XamlRoot { get; }

	bool IgnorePixelScaling { get; }

	void InvalidateRender();

	void ReleasePointerCapture();

	void SetPointerCapture();
}
