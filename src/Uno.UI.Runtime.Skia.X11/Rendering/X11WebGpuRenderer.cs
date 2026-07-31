using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media;
using Silk.NET.WebGPU;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

// WebGPU X11 renderer on the NEUTRAL drawing seam: drives CompositionTarget's frame pipeline
// (OnNativePlatformFrameRequested) with an offscreen WebGPU render target, then reads back the
// texture and blits it to the X11 window via XPutImage. No wgpu swapchain (see X11WebGpuSwapchainRenderer).
// The Skia factory stays for resource production (geometry/fonts/images); WebGPU does the rendering.
internal sealed unsafe class X11WebGpuRenderer : X11Renderer
{
	private readonly WebGpuDevice _device;
	private readonly WebGpuRenderer _backend;
	private WebGpuRenderSurface? _target;
	private readonly IntPtr _gc;
	private readonly uint _depth;
	private int _w, _h;
	private IntPtr _buffer;          // unmanaged BGRA scanlines backing the XImage
	private IntPtr _xImage;
	private Silk.NET.WebGPU.Buffer* _readback;
	private nuint _readbackSize;

	public X11WebGpuRenderer(IXamlRootHost host, X11Window x11Window) : base(host, x11Window)
	{
		_device = new WebGpuDevice();
		_backend = new WebGpuRenderer(_device);

		// Route the shared render loop through WebGPU.
		CompositionTarget.Renderer = _backend;
		// Wrap the existing factory with the device-bound WebGPU one so images become WebGpuImageTexture
		// (consumable by the WebGPU renderer); everything else delegates to the inner factory.
		DrawingFactory.Register(new WebGpuDrawingFactory(_device, DrawingFactory.Current));

		using (X11Helper.XLock(_x11Window.Display))
		{
			_gc = X11Helper.XCreateGC(x11Window.Display, x11Window.Window, 0, 0);
			XWindowAttributes attr = default;
			_ = XLib.XGetWindowAttributes(_x11Window.Display, _x11Window.Window, ref attr);
			_depth = (uint)attr.depth;
		}

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"X11WebGpuRenderer: offscreen WebGPU render + XPutImage (depth {_depth})");
		}
	}

	private void EnsureSize(int width, int height)
	{
		if (width == _w && height == _h && _target is not null)
		{
			return;
		}

		_w = width;
		_h = height;
		_target?.Dispose();
		_target = new WebGpuRenderSurface(_device, width, height);

		if (_xImage != IntPtr.Zero) { ((XImage*)_xImage)->data = IntPtr.Zero; XLib.XDestroyImage(_xImage); _xImage = IntPtr.Zero; }
		if (_buffer != IntPtr.Zero) { Marshal.FreeHGlobal(_buffer); }
		_buffer = Marshal.AllocHGlobal(width * height * 4);
		_xImage = X11Helper.XCreateImage(_x11Window.Display, 0, _depth, 2 /*ZPixmap*/, 0, _buffer, (uint)width, (uint)height, 32, 0);

		int bpr = (width * 4 + 255) / 256 * 256;
		var size = (nuint)(bpr * height);
		if (_readback is not null && _readbackSize == size)
		{
			return;
		}
		if (_readback is not null) { _device.W.BufferDestroy(_readback); }
		var bd = new BufferDescriptor { Size = size, Usage = BufferUsage.CopyDst | BufferUsage.MapRead };
		_readback = _device.W.DeviceCreateBuffer(_device.Dev, ref bd);
		_readbackSize = size;
	}

	public override void Render()
	{
		if (_host is X11XamlRootHost { Closed.IsCompleted: true })
		{
			return;
		}
		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget compositionTarget)
		{
			return;
		}

		_ = compositionTarget.OnNativePlatformFrameRequested(_target, size =>
		{
			EnsureSize((int)size.Width, (int)size.Height);
			return _target!;
		});

		if (_target is null)
		{
			return; // nothing recorded yet
		}

		ReadbackToBuffer();

		using (X11Helper.XLock(_x11Window.Display))
		{
			if (_xImage != IntPtr.Zero)
			{
				_ = X11Helper.XPutImage(_x11Window.Display, _x11Window.Window, _gc, _xImage, 0, 0, 0, 0, (uint)_w, (uint)_h);
			}
			_ = XLib.XFlush(_x11Window.Display);
		}
	}

	// Copy the offscreen RGBA texture to CPU and swizzle into the BGRA XImage buffer (X TrueColor, little-endian).
	private void ReadbackToBuffer()
	{
		var W = _device.W;
		int w = _w, h = _h;
		int bpr = (w * 4 + 255) / 256 * 256;

		var enc = W.DeviceCreateCommandEncoder(_device.Dev, null);
		var src = new ImageCopyTexture { Texture = _target!.Tex, MipLevel = 0, Origin = default, Aspect = TextureAspect.All };
		var dst = new ImageCopyBuffer { Buffer = _readback, Layout = new TextureDataLayout { Offset = 0, BytesPerRow = (uint)bpr, RowsPerImage = (uint)h } };
		var ext = new Extent3D((uint)w, (uint)h, 1);
		W.CommandEncoderCopyTextureToBuffer(enc, in src, in dst, in ext);
		var cb = W.CommandEncoderFinish(enc, null);
		W.QueueSubmit(_device.Q, 1, &cb);

		bool done = false;
		W.BufferMapAsync(_readback, MapMode.Read, 0, _readbackSize, new PfnBufferMapCallback((s, _) => done = true), null);
		while (!done) { _device.Native.DevicePoll(_device.Dev, true, null); }
		var ptr = (byte*)W.BufferGetConstMappedRange(_readback, 0, _readbackSize);
		var dstBuf = (byte*)_buffer;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				int si = y * bpr + x * 4;
				int di = (y * w + x) * 4;
				dstBuf[di] = ptr[si + 2];     // B
				dstBuf[di + 1] = ptr[si + 1]; // G
				dstBuf[di + 2] = ptr[si];     // R
				dstBuf[di + 3] = ptr[si + 3]; // A
			}
		}
		W.BufferUnmap(_readback);
	}

	protected override SkiaSharp.SKSurface UpdateSize(int width, int height) => throw new NotSupportedException("X11WebGpuRenderer does not use SKSurface.");

	protected override void Flush() { }

	public override void Dispose()
	{
		if (_xImage != IntPtr.Zero) { ((XImage*)_xImage)->data = IntPtr.Zero; XLib.XDestroyImage(_xImage); _xImage = IntPtr.Zero; }
		if (_buffer != IntPtr.Zero) { Marshal.FreeHGlobal(_buffer); _buffer = IntPtr.Zero; }
		if (_readback is not null) { _device.W.BufferDestroy(_readback); _readback = null; }
		_target?.Dispose();
		_device.Dispose();
	}
}
