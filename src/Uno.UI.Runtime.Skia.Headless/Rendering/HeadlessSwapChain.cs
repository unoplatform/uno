#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.Headless;

/// <summary>
/// A no-output <see cref="ISwapChain"/> for the headless host: the backend composes into a throwaway CPU buffer
/// (sized to the frame, reallocated only on resize) that is never presented, keeping the render cycle ticking
/// without producing pixels.
/// </summary>
internal sealed class HeadlessSwapChain : ISwapChain
{
	private nint _buffer;
	private int _width;
	private int _height;
	private HeadlessRenderTarget? _target;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	// Nothing is presented, so there is no previous frame to preserve — a full repaint each tick is fine.
	public bool PreservesContents => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_target is null || width != _width || height != _height)
		{
			if (_buffer != 0)
			{
				Marshal.FreeHGlobal(_buffer);
			}
			_width = width;
			_height = height;
			_buffer = Marshal.AllocHGlobal(width * height * 4);
			_target = new HeadlessRenderTarget(_buffer, width * 4, width, height);
		}

		return _target;
	}

	public void Present() { }

	public void Dispose()
	{
		if (_buffer != 0)
		{
			Marshal.FreeHGlobal(_buffer);
			_buffer = 0;
		}
	}

	private sealed class HeadlessRenderTarget(nint pixels, int rowBytes, int width, int height) : ISoftwareRenderTarget
	{
		public nint Pixels => pixels;
		public int RowBytes => rowBytes;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		public void Dispose() { }
	}
}
