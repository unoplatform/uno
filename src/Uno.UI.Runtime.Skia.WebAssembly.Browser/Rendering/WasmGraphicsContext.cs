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

	public GraphicsContextKind Kind => GraphicsContextKind.WebGL;

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
	// Whether the compositor acquired a target this tick; it skips drawing entirely when there is no recorded frame
	// yet or the bounds are empty, and blitting then reads a JS pixel buffer that has not been allocated.
	private bool _frameAcquired;
	private bool _contentsPreserved;

	public WasmSoftwareGraphicsContext(SoftwareBrowserRenderer renderer) => _renderer = renderer;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	// Reuses one persistent CPU pixel buffer across frames, so the compositor can repaint only the damaged region —
	// except on a frame whose buffer was just (re)allocated and holds none of the previous frame's pixels.
	public bool PreservesContents => _contentsPreserved;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		// Mono growing its heap detaches the clamped array, so the buffer is reallocated (uninitialised) without any
		// size change: a same-size frame the damage clip would otherwise repaint only in part.
		var invalidated = _target is null || width != _width || height != _height || _renderer.NeedsForceResize();
		if (invalidated)
		{
			_target?.Dispose();
			_target = _renderer.Resize(width, height);
			_width = width;
			_height = height;
		}

		_contentsPreserved = !invalidated;
		_frameAcquired = true;
		return _target!;
	}

	public void Present()
	{
		if (!_frameAcquired)
		{
			return;
		}
		_frameAcquired = false;

		_renderer.Flush();
	}

	public void Dispose() => _target?.Dispose();
}

