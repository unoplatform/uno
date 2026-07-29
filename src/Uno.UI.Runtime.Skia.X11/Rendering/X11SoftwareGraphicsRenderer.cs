#nullable enable

using System;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Drives the shared render loop through the pluggable graphics pipeline, naming no GPU-library type. It
/// installs the X11 context factory (Uno owns context creation), negotiates the registered backend via
/// <see cref="GraphicsBackend.Activate"/>, and per frame acquires the context's target, records/presents the
/// frame through the neutral loop, and asks the context to present. The backend wraps the acquired target.
/// </summary>
internal sealed class X11SoftwareGraphicsRenderer : IX11Renderer
{
	private readonly IXamlRootHost _host;
	private readonly X11Window _x11Window;
	private readonly IPresentableGraphicsContext _context;
	private Color _background;

	public X11SoftwareGraphicsRenderer(IXamlRootHost host, X11Window x11Window)
	{
		_host = host;
		_x11Window = x11Window;

		// Uno owns context creation for the closed set of kinds; the X11 factory builds the software
		// (CPU-framebuffer + XPutImage) context. No backend/GPU-library type is named here.
		GraphicsBackend.ContextFactory = X11GraphicsContextFactory.Create;

		var (width, height) = GetWindowSize();
		var activation = GraphicsBackend.Activate(new X11GraphicsNativeWindow(x11Window, width, height));
		_context = (IPresentableGraphicsContext)activation.Context;

		// Route the shared render loop through the negotiated backend (whichever was registered).
		CompositionTarget.RenderBackend = activation.RenderBackend;
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
