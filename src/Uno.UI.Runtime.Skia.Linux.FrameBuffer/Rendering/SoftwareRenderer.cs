using System;
using System.Threading;
using Windows.Foundation;
using SkiaSharp;
using Windows.Graphics.Display;
using Uno.Disposables;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderer : FrameBufferRenderer
	{
		private FrameBufferDevice _fbDev;
		private readonly AutoResetEvent _renderInvalidationEvent = new(false);

		public SoftwareRenderer(IXamlRootHost host, bool? showMouseCursor, float mouseCursorRadius, System.Drawing.Color mouseCursorColor) : base(host, showMouseCursor, mouseCursorRadius, mouseCursorColor)
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();
			FrameBufferWindowWrapper.Instance.SetSize(new Size(_fbDev.ScreenSize.Width, _fbDev.ScreenSize.Height));

			new Thread(_ =>
			{
				while (true)
				{
					_renderInvalidationEvent.WaitOne();
					Render();
					_fbDev.VSync();
					_surface?.ReadPixels(
						new SKImageInfo((int)_fbDev.ScreenSize.Width, (int)_fbDev.ScreenSize.Height, _fbDev.PixelFormat, SKAlphaType.Premul),
						_fbDev.BufferAddress,
						_fbDev.RowBytes,
						0,
						0);
				}
			})
			{
				IsBackground = true,
				Name = "FrameBuffer software rendering thread"
			}.Start();
		}

		public override void InvalidateRender() => _renderInvalidationEvent.Set();

		protected override IDisposable MakeCurrent() => Disposable.Empty;

		protected override SKSurface UpdateSize(int width, int height)
			=> SKSurface.Create(new SKImageInfo(width, height, _fbDev.PixelFormat, SKAlphaType.Premul));
	}
}
