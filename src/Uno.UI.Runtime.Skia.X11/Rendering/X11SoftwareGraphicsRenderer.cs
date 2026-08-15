#nullable enable

using System;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Drives the shared render loop over a negotiated <see cref="ISwapChain"/>, naming no GPU-library type.
/// The host (X11XamlRootHost) owns the negotiation (it creates the window+context per kind); this renderer just
/// acquires the context's target each frame, records/presents through the neutral loop, and presents.
/// </summary>
internal sealed class X11SoftwareGraphicsRenderer : IX11Renderer
{
	private readonly IXamlRootHost _host;
	private readonly X11Window _x11Window;
	private readonly ISwapChain _context;
	private Color _background;

	public X11SoftwareGraphicsRenderer(IXamlRootHost host, X11Window x11Window, ISwapChain context)
	{
		_host = host;
		_x11Window = x11Window;
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

		var (width, height) = GetWindowSize();
		var target = _context.AcquireRenderTarget(width, height);
		_ = compositionTarget.OnNativePlatformFrameRequested(target, size => _context.AcquireRenderTarget((int)size.Width, (int)size.Height));
		_context.Present();
	}

	private (int width, int height) GetWindowSize()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(_x11Window.Display, _x11Window.Window, ref attributes);
		return (Math.Max(1, attributes.width), Math.Max(1, attributes.height));
	}

	public void Dispose() => _context.Dispose();
}
