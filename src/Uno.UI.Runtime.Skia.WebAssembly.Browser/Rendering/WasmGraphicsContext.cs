#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Neutral WebGL <see cref="ISwapChain"/> for the browser — wraps the emscripten WebGL renderer, making its
/// context current on acquire and handing the backend a neutral <see cref="IGLRenderTarget"/> (the canvas default
/// framebuffer). The Skia backend builds its GRContext-GL against the current context. Names no Skia type.
/// </summary>
internal sealed class WasmGLGraphicsContext : ISwapChain
{
	private readonly WebGlBrowserRenderer _renderer;

	public WasmGLGraphicsContext(WebGlBrowserRenderer renderer) => _renderer = renderer;

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		_renderer.MakeCurrent();
		return _renderer.Resize(Math.Max(1, width), Math.Max(1, height));
	}

	public void Present() => _renderer.Flush();

	public void Dispose() { }
}

/// <summary>
/// Neutral software <see cref="ISwapChain"/> for the browser — wraps the 2D-canvas software renderer,
/// handing the backend a neutral <see cref="ISoftwareRenderTarget"/> over the JS pixel buffer. The Skia backend
/// wraps it as its surface; <see cref="Present"/> blits it to the canvas. Names no Skia type.
/// </summary>
internal sealed class WasmSoftwareGraphicsContext : ISwapChain
{
	private readonly SoftwareBrowserRenderer _renderer;
	private IRenderTarget? _target;
	private int _width;
	private int _height;

	public WasmSoftwareGraphicsContext(SoftwareBrowserRenderer renderer) => _renderer = renderer;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		// Reallocate the JS pixel buffer only on a size change or when it was invalidated (canvas re-created).
		if (_target is null || width != _width || height != _height || _renderer.NeedsForceResize())
		{
			_target?.Dispose();
			_target = _renderer.Resize(width, height);
			_width = width;
			_height = height;
		}

		return _target;
	}

	public void Present() => _renderer.Flush();

	public void Dispose() => _target?.Dispose();
}

