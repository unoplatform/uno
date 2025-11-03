using System;
using Windows.Foundation;
using SkiaSharp;
using Uno.Foundation.Logging;
using Windows.Graphics.Display;
using System.Timers;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderer : IFBRenderer
	{
		private readonly Timer _renderTimer;
		private readonly IXamlRootHost _host;
		private FrameBufferDevice _fbDev;
		private SKSurface? _surface;
		private int renderCount;

		public SoftwareRenderer(IXamlRootHost host)
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();
			FrameBufferWindowWrapper.Instance.SetSize(new Size(_fbDev.ScreenSize.Width, _fbDev.ScreenSize.Height), (float)DisplayInformation.GetForCurrentViewSafe().RawPixelsPerViewPixel);
			_renderTimer = CreateRenderTimer();

			_host = host;
		}

		private Timer CreateRenderTimer()
		{
			var timer = new Timer { AutoReset = false, Interval = TimeSpan.FromSeconds(1.0 / FeatureConfiguration.CompositionTarget.FrameRate).TotalMilliseconds };
			timer.Elapsed += (_, _) => Render();
			return timer;
		}

		public void InvalidateRender() => _renderTimer.Enabled = true;

		void Render()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			_surface?.Canvas.Clear(SKColors.Transparent);
			((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
			{
				_surface?.Dispose();
				_surface = SKSurface.Create(new SKImageInfo((int)size.Width, (int)size.Height, _fbDev.PixelFormat, SKAlphaType.Premul));
				_surface.Canvas.Clear(SKColors.Transparent);
				return _surface.Canvas;
			});
			if (_surface is not null)
			{
				_surface.Flush();
			}
			_fbDev.VSync();
			if (_surface is not null)
			{
				_surface.ReadPixels(new SKImageInfo((int)_fbDev.ScreenSize.Width, (int)_fbDev.ScreenSize.Width, _fbDev.PixelFormat, SKAlphaType.Premul), _fbDev.BufferAddress, _fbDev.RowBytes, 0, 0);
			}
		}
	}
}
