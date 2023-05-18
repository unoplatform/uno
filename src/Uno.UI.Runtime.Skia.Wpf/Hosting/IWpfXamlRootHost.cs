#nullable enable

using Uno;
using Windows.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf.Hosting;

internal interface IWpfXamlRootHost
{
	WpfCanvas? NativeOverlayLayer { get; }

	XamlRoot? XamlRoot { get; }

	void InvalidateRender();

	void ReleasePointerCapture();

	void SetPointerCapture();
}
