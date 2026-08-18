#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Neutral WebGL <see cref="ISwapChain"/> for the browser: makes the emscripten WebGL context current on acquire
/// and hands the backend a neutral <see cref="IGLRenderTarget"/> (the canvas default framebuffer).
/// </summary>
internal sealed class WasmGLGraphicsContext : ISwapChain, IGLDeviceContext
{
	private readonly WebGlBrowserRenderer _renderer;
	private IRenderTarget? _target;
	private int _width;
	private int _height;

	public WasmGLGraphicsContext(WebGlBrowserRenderer renderer) => _renderer = renderer;

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;

	public GLFlavor Flavor => GLFlavor.WebGL;
	public Func<string, nint> GetProcAddress => global::Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics.WasmGLFunctions.GetProcAddress;

	// The renderer draws into the canvas default framebuffer, which is undefined after present — no host retention
	// yet, so the compositor repaints the whole frame.
	public bool PreservesContents => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		_renderer.MakeCurrent();
		if (_target is null || width != _width || height != _height)
		{
			_target = _renderer.Resize(width, height);
			_width = width;
			_height = height;
		}

		return _target;
	}

	public void Present() => _renderer.Flush();

	public void Dispose() { }
}

/// <summary>
/// Neutral software <see cref="ISwapChain"/> for the browser: hands the backend a neutral
/// <see cref="ISoftwareRenderTarget"/> over the JS pixel buffer; <see cref="Present"/> blits it to the canvas.
/// </summary>
internal sealed class WasmSoftwareGraphicsContext : ISwapChain
{
	private readonly SoftwareBrowserRenderer _renderer;
	private IRenderTarget? _target;
	private int _width;
	private int _height;

	public WasmSoftwareGraphicsContext(SoftwareBrowserRenderer renderer) => _renderer = renderer;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	// Reuses one persistent CPU pixel buffer across frames (reallocated only on resize), so the compositor can
	// repaint only the damaged region.
	public bool PreservesContents => true;

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

