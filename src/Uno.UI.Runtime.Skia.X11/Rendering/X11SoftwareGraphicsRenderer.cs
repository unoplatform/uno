#nullable enable

using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Drives the shared render loop over a negotiated <see cref="ISwapChain"/>, naming no GPU-library type.
/// The host (X11XamlRootHost) owns the negotiation; this renderer defers sizing and the record/present cycle to
/// <see cref="CompositionTarget.OnNativePlatformFrameRequested"/>, then presents.
/// </summary>
internal sealed class X11SoftwareGraphicsRenderer : IX11Renderer
{
	private readonly IXamlRootHost _host;
	private readonly ISwapChain _context;
	private Color _background;

	public X11SoftwareGraphicsRenderer(IXamlRootHost host, ISwapChain context)
	{
		_host = host;
		_context = context;
	}

	public void SetBackgroundColor(Color color) => _background = color;

	public void Render()
	{
		if (_host is X11XamlRootHost { Closed.IsCompleted: true })
		{
			return;
		}

		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget compositionTarget)
		{
			return;
		}

		_ = compositionTarget.OnNativePlatformFrameRequested(null, size => _context.AcquireRenderTarget((int)size.Width, (int)size.Height));
		_context.Present();
	}

	public void Dispose() => _context.Dispose();
}
