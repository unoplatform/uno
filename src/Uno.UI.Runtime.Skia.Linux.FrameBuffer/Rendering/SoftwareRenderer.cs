using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderer : FrameBufferRenderer
	{
		private readonly FrameBufferDevice _fbDev;
		private readonly AutoResetEvent _renderInvalidationEvent = new(false);
		private readonly Thread _renderThread;
		private volatile bool _disposed;

		// A backend-neutral BGRA8888 staging buffer the Skia-free backend composes into; converted+blitted to the
		// framebuffer after VSync (the framebuffer's own layout may be BGRA/RGBA/RGB565, honored on copy).
		private IntPtr _buffer;
		private int _bufferWidth;
		private int _bufferHeight;
		private FrameBufferSoftwareRenderTarget? _target;

		public SoftwareRenderer(IXamlRootHost host, MouseIndicatorOptions mouseIndicatorOptions) : base(host, mouseIndicatorOptions)
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();
			FrameBufferWindowWrapper.Instance.SetSize(new Size(_fbDev.ScreenSize.Width, _fbDev.ScreenSize.Height));

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Software renderer initialized: {_fbDev.ScreenSize.Width}x{_fbDev.ScreenSize.Height}, {_fbDev.PixelFormat}");
			}

			_renderThread = new Thread(_ =>
			{
				while (!_disposed)
				{
					try
					{
						_renderInvalidationEvent.WaitOne();
						if (_disposed)
						{
							break;
						}
						Render();
						_fbDev.VSync();
						BlitToFramebuffer();
					}
					catch (Exception ex)
					{
						this.LogError()?.Error("Error during software rendering", ex);
					}
				}
			})
			{
				IsBackground = true,
				Name = "FrameBuffer software rendering thread"
			};
			_renderThread.Start();
		}

		protected override IRenderTarget? CurrentTarget => _target;

		protected override unsafe IRenderTarget CreateTarget(int width, int height)
		{
			width = Math.Max(1, width);
			height = Math.Max(1, height);
			if (_buffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_buffer);
			}
			_bufferWidth = width;
			_bufferHeight = height;
			_buffer = Marshal.AllocHGlobal(width * height * 4);
			new Span<byte>((void*)_buffer, width * height * 4).Clear();
			_target = new FrameBufferSoftwareRenderTarget(_buffer, width * 4, width, height);
			return _target;
		}

		public override void InvalidateRender() => _renderInvalidationEvent.Set();

		protected override IDisposable MakeCurrent() => Disposable.Empty;

		// Convert the composed BGRA8888 staging buffer into the framebuffer's native layout.
		private unsafe void BlitToFramebuffer()
		{
			if (_buffer == IntPtr.Zero)
			{
				return;
			}

			var src = (byte*)_buffer;
			var dst = (byte*)_fbDev.BufferAddress;
			var dstStride = _fbDev.RowBytes;
			var srcStride = _bufferWidth * 4;
			var format = _fbDev.PixelFormat;

			for (var y = 0; y < _bufferHeight; y++)
			{
				var srcRow = src + y * srcStride;
				var dstRow = dst + y * dstStride;
				switch (format)
				{
					case FramebufferColorFormat.Bgra8888:
						Buffer.MemoryCopy(srcRow, dstRow, dstStride, Math.Min(srcStride, dstStride));
						break;
					case FramebufferColorFormat.Rgba8888:
						for (var x = 0; x < _bufferWidth; x++)
						{
							var s = srcRow + x * 4;
							var d = dstRow + x * 4;
							d[0] = s[2]; // R
							d[1] = s[1]; // G
							d[2] = s[0]; // B
							d[3] = s[3]; // A
						}
						break;
					case FramebufferColorFormat.Rgb565:
						var dst16 = (ushort*)dstRow;
						for (var x = 0; x < _bufferWidth; x++)
						{
							var s = srcRow + x * 4;
							dst16[x] = (ushort)(((s[2] & 0xF8) << 8) | ((s[1] & 0xFC) << 3) | (s[0] >> 3));
						}
						break;
				}
			}
		}

		public override void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;

			// Wake the render thread so it can observe _disposed and exit before we tear down the framebuffer.
			_renderInvalidationEvent.Set();
			try
			{
				_renderThread.Join(TimeSpan.FromSeconds(1));
			}
			catch (Exception e)
			{
				this.LogDebug()?.Debug($"Failed to join the software rendering thread on exit: {e.Message}");
			}

			// Clearing the mapped framebuffer makes the shell prompt visible again once the shell writes to it.
			try
			{
				_fbDev.Clear();
			}
			catch (Exception e)
			{
				this.LogDebug()?.Debug($"Failed to clear the framebuffer on exit: {e.Message}");
			}

			try
			{
				_fbDev.Dispose();
			}
			catch (Exception e)
			{
				this.LogDebug()?.Debug($"Failed to dispose the framebuffer device on exit: {e.Message}");
			}

			if (_buffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_buffer);
				_buffer = IntPtr.Zero;
			}
		}

		private sealed class FrameBufferSoftwareRenderTarget(IntPtr pixels, int rowBytes, int width, int height) : ISoftwareRenderTarget
		{
			public nint Pixels => pixels;
			public int RowBytes => rowBytes;
			public int Width => width;
			public int Height => height;
			public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
			public void Dispose() { }
		}
	}
}
