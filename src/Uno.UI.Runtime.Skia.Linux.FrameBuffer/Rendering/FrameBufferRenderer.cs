using System;
using Windows.Graphics.Display;
using Windows.Graphics.Interop.Direct2D;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Runtime.Skia;

internal abstract class FrameBufferRenderer
{
	protected readonly IXamlRootHost _host;
	protected SKSurface? _surface;
	private int _renderCount;
	private SKPaint _cursorPaint = new() { Color = FeatureConfiguration.LinuxFramebuffer.MouseCursorColor.ToSKColor() };
	private float _cursorRadius = FeatureConfiguration.LinuxFramebuffer.MouseCursorRadius;
	private bool _cursorVisible;

	protected FrameBufferRenderer(IXamlRootHost host)
	{
		_host = host;
		MouseCursorParamsUpdated();
		FeatureConfiguration.LinuxFramebuffer.MouseCursorParamsUpdated += MouseCursorParamsUpdated;
		FrameBufferPointerInputSource.Instance.MouseEventReceived += OnMouseEventReceived;
	}

	private void OnMouseEventReceived()
	{
		FrameBufferPointerInputSource.Instance.MouseEventReceived -= OnMouseEventReceived;
		MouseCursorParamsUpdated();
	}

	private void MouseCursorParamsUpdated()
	{
		_cursorPaint = new() { Color = FeatureConfiguration.LinuxFramebuffer.MouseCursorColor.ToSKColor() };
		_cursorRadius = FeatureConfiguration.LinuxFramebuffer.MouseCursorRadius;
		if (_cursorRadius < 0)
		{
			throw new ArgumentOutOfRangeException($"{nameof(FeatureConfiguration.LinuxFramebuffer.MouseCursorRadius)} should not be negative.");
		}
		_cursorVisible = FeatureConfiguration.LinuxFramebuffer.ShowMouseCursor ?? FrameBufferPointerInputSource.Instance.ReceivedMouseEvent;
	}

	protected void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		using var _ = MakeCurrent();
		var bounds = FrameBufferWindowWrapper.Instance.Size;
		var orientation = FrameBufferWindowWrapper.Instance.Orientation;
		var (degrees, transX, transY) = orientation switch
		{
			DisplayOrientations.None => (0, 0, 0),
			DisplayOrientations.Landscape => (0, 0, 0),
			DisplayOrientations.Portrait => (90, bounds.Height, 0),
			DisplayOrientations.LandscapeFlipped => (180, bounds.Width, bounds.Height),
			DisplayOrientations.PortraitFlipped => (-90, 0, bounds.Width),
			_ => throw new ArgumentOutOfRangeException()
		};
		_surface?.Canvas.Save();
		_surface?.Canvas.Translate(transX, transY);
		_surface?.Canvas.RotateDegrees(degrees);
		_surface?.Canvas.Clear(SKColors.Transparent);
		((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_surface?.Dispose();
			if (orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped)
			{
				size = new Size(size.Height, size.Width);
			}
			_surface = UpdateSize((int)size.Width, (int)size.Height);
			_surface.Canvas.Save();
			_surface.Canvas.Translate((float)transX, (float)transY);
			_surface.Canvas.RotateDegrees(degrees);
			_surface.Canvas.Clear(SKColors.Transparent);
			return _surface.Canvas;
		});
		if (_cursorVisible)
		{
			_surface?.Canvas.DrawCircle(FrameBufferPointerInputSource.Instance.MousePosition.ToSkia(), _cursorRadius, _cursorPaint);
		}
		_surface?.Canvas.Restore();
		_surface?.Flush();
	}

	public abstract void InvalidateRender();

	protected abstract IDisposable MakeCurrent();

	protected abstract SKSurface UpdateSize(int width, int height);
}
