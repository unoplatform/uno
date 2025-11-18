using System;
using Windows.Foundation;
using SkiaSharp;
using Windows.Graphics.Display;
using System.Timers;
using Uno.Disposables;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderer : FrameBufferRenderer
	{
		private readonly Timer _renderTimer;
		private FrameBufferDevice _fbDev;

		public SoftwareRenderer(IXamlRootHost host) : base(host)
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();
			FrameBufferWindowWrapper.Instance.SetSize(new Size(_fbDev.ScreenSize.Width, _fbDev.ScreenSize.Height),
				(float)DisplayInformation.GetForCurrentViewSafe().RawPixelsPerViewPixel);
			_renderTimer = CreateRenderTimer();
		}

		private Timer CreateRenderTimer()
		{
			var timer = new Timer
			{
				AutoReset = false,
				Interval = TimeSpan.FromSeconds(1.0 / FeatureConfiguration.CompositionTarget.FrameRate)
					.TotalMilliseconds
			};
			timer.Elapsed += (_, _) =>
			{
				_fbDev.VSync();
				Render();
				_surface?.ReadPixels(
					new SKImageInfo((int)_fbDev.ScreenSize.Width, (int)_fbDev.ScreenSize.Width, _fbDev.PixelFormat, SKAlphaType.Premul),
					_fbDev.BufferAddress,
					_fbDev.RowBytes,
					0,
					0);
			};
			return timer;
		}

		public override void InvalidateRender() => _renderTimer.Enabled = true;

		protected override IDisposable MakeCurrent() => Disposable.Empty;

		protected override SKSurface UpdateSize(int width, int height)
			=> SKSurface.Create(new SKImageInfo(width, height, _fbDev.PixelFormat, SKAlphaType.Premul));
	}
}
