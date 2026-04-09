using System;
using System.Threading;
using Windows.Foundation;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// A renderer that renders to an in-memory Skia surface with no device or
/// display output. This is suitable for CI environments and headless testing
/// where no framebuffer device, DRM device, or display server is available.
/// </summary>
internal class HeadlessRenderer : FrameBufferRenderer
{
	private readonly int _width;
	private readonly int _height;
	private readonly AutoResetEvent _renderInvalidationEvent = new(false);

	public HeadlessRenderer(IXamlRootHost host, int width, int height, MouseIndicatorOptions mouseIndicatorOptions)
		: base(host, mouseIndicatorOptions)
	{
		_width = width;
		_height = height;

		FrameBufferWindowWrapper.Instance.SetSize(new Size(width, height));

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Headless renderer initialized: {width}x{height}");
		}

		new Thread(_ =>
		{
			while (true)
			{
				try
				{
					_renderInvalidationEvent.WaitOne();
					Render();
				}
				catch (Exception ex)
				{
					this.LogError()?.Error("Error during headless rendering", ex);
				}
			}
		})
		{
			IsBackground = true,
			Name = "Headless rendering thread"
		}.Start();
	}

	public override void InvalidateRender() => _renderInvalidationEvent.Set();

	protected override IDisposable MakeCurrent() => Disposable.Empty;

	protected override SKSurface UpdateSize(int width, int height)
		=> SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
}
