#nullable enable

using System;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Drives the shared render loop over a negotiated <see cref="ISwapChain"/>, naming no GPU-library type.
/// The host (X11XamlRootHost) owns the negotiation; this renderer defers sizing and the record/present cycle to
/// <see cref="CompositionTarget.OnNativePlatformFrameRequested"/>, then shapes the top window to the returned
/// native-element clip so native X11 sub-windows show through.
/// </summary>
internal sealed class X11SoftwareGraphicsRenderer : IX11Renderer
{
	private readonly IXamlRootHost _host;
	private readonly X11Window _x11Window;
	private readonly ISwapChain _context;
	private Color _background;
	private X11AirspaceRenderHelper? _airspaceHelper;
	private int _airspaceWidth;
	private int _airspaceHeight;

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

		var nativeElementClipPath = compositionTarget.OnNativePlatformFrameRequested(_context);
		ApplyAirspaceClip(nativeElementClipPath);
	}

	// Shapes the top render window to the Uno-visible region (the clip path), leaving the native-element holes
	// unshaped so the sibling native X11 sub-windows raised below it show through.
	private void ApplyAirspaceClip(IGeometry nativeElementClipPath)
	{
		var (width, height) = GetWindowSize();
		if (_airspaceHelper is null || width != _airspaceWidth || height != _airspaceHeight)
		{
			_airspaceHelper?.Dispose();
			_airspaceHelper = new X11AirspaceRenderHelper(_x11Window.Display, _x11Window.Window, width, height);
			_airspaceWidth = width;
			_airspaceHeight = height;
		}

		_airspaceHelper.XShapeClip(nativeElementClipPath);
	}

	private (int width, int height) GetWindowSize()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(_x11Window.Display, _x11Window.Window, ref attributes);
		return (Math.Max(1, attributes.width), Math.Max(1, attributes.height));
	}

	public void Dispose()
	{
		_airspaceHelper?.Dispose();
		_context.Dispose();
	}
}
