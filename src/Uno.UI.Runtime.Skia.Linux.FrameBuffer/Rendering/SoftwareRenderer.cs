using System;
using System.Threading;
using Windows.Foundation;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderer : FrameBufferRenderer
	{
		private FrameBufferDevice _fbDev;
		private readonly AutoResetEvent _renderInvalidationEvent = new(false);
		private readonly Thread _renderThread;
		private volatile bool _disposed;

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
						_surface?.ReadPixels(
							new SKImageInfo((int)_fbDev.ScreenSize.Width, (int)_fbDev.ScreenSize.Height, _fbDev.PixelFormat, SKAlphaType.Premul),
							_fbDev.BufferAddress,
							_fbDev.RowBytes,
							0,
							0);
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

		public override void InvalidateRender() => _renderInvalidationEvent.Set();

		protected override IDisposable MakeCurrent() => Disposable.Empty;

		protected override SKSurface UpdateSize(int width, int height)
			=> SKSurface.Create(new SKImageInfo(width, height, _fbDev.PixelFormat, SKAlphaType.Premul));

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

			_surface?.Dispose();
		}
	}
}
