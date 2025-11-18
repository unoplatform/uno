using System;
using Windows.Graphics.Interop.Direct2D;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia;

internal abstract class FrameBufferRenderer
{
	protected readonly IXamlRootHost _host;
	protected SKSurface? _surface;
	private int _renderCount;
	private readonly SKPaint _cursorPaint = new() { Color = SKColors.Red };

	protected FrameBufferRenderer(IXamlRootHost host)
	{
		_host = host;
	}

	protected void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		using var _ = MakeCurrent();
		_surface?.Canvas.Clear(SKColors.Transparent);
		((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_surface?.Dispose();
			_surface = UpdateSize((int)size.Width, (int)size.Height);
			_surface.Canvas.Clear(SKColors.Transparent);
			return _surface.Canvas;
		});
		_surface?.Canvas.DrawCircle(FrameBufferPointerInputSource.Instance.MousePosition.ToSkia(), 5, _cursorPaint);
		_surface?.Flush();
	}

	public abstract void InvalidateRender();

	protected abstract IDisposable MakeCurrent();

	protected abstract SKSurface UpdateSize(int width, int height);
}
