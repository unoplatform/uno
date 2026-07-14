#nullable enable

using System;
using System.Threading;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.Headless;

/// <summary>
/// Drives the Skia two-phase render cycle for a single headless window. The cycle always runs so the
/// app lifecycle behaves like a real target, but rasterization is optional: when the window supplies
/// a target buffer, frames are drawn straight into it (zero-copy) and the <c>onFrameRendered</c>
/// callback is invoked; otherwise frames are drawn to a Skia null surface (the pipeline runs but
/// nothing is rasterized). Windows with no buffer at all across the whole app additionally skip the
/// paint walk globally via <c>FeatureConfiguration.Rendering.SkipVisualTreePainting</c> (set by the host).
/// </summary>
internal sealed class HeadlessRenderer : IDisposable
{
	private readonly IXamlRootHost _host;

	private readonly int _rawWidth;
	private readonly int _rawHeight;
	private readonly bool _hasBuffer;
	private readonly IntPtr _buffer;
	private readonly int _rowBytes;
	private readonly SKColorType _colorType;
	private readonly Action? _onFrameRendered;

	private readonly AutoResetEvent _renderInvalidationEvent = new(false);
	private readonly Thread _renderThread;
	private volatile bool _disposed;

	private SKSurface? _surface;
	private bool _surfaceTargetsBuffer;

	public HeadlessRenderer(IXamlRootHost host, int rawWidth, int rawHeight, HeadlessWindowOptions options)
	{
		_host = host;
		_rawWidth = rawWidth;
		_rawHeight = rawHeight;
		_hasBuffer = options.HasBuffer;
		_buffer = options.Buffer;
		_rowBytes = options.RowBytes;
		_colorType = options.PixelFormat.ToSkColorType();
		_onFrameRendered = options.OnFrameRendered;

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
		};
		_renderThread.Start();
	}

	public void InvalidateRender() => _renderInvalidationEvent.Set();

	private void Render()
	{
		// The visual tree may not be available yet on the first invalidation(s); the cycle will
		// be driven again once content is loaded.
		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget ct)
		{
			return;
		}

		ct.OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_surface?.Dispose();
			_surface = CreateSurface((int)size.Width, (int)size.Height);
			return _surface.Canvas;
		});

		_surface?.Flush();

		if (_surfaceTargetsBuffer && _surface is not null)
		{
			_onFrameRendered!.Invoke();
		}
	}

	private SKSurface CreateSurface(int width, int height)
	{
		if (_hasBuffer)
		{
			if (width == _rawWidth && height == _rawHeight)
			{
				_surfaceTargetsBuffer = true;
				return SKSurface.Create(new SKImageInfo(width, height, _colorType, SKAlphaType.Premul), _buffer, _rowBytes);
			}

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					$"Requested render size {width}x{height} does not match the configured headless buffer size " +
					$"{_rawWidth}x{_rawHeight}; drawing nothing for this frame.");
			}
		}

		_surfaceTargetsBuffer = false;
		return SKSurface.CreateNull(width, height);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		// Wake the render thread so it can observe _disposed and exit before we dispose the surface.
		_renderInvalidationEvent.Set();
		try
		{
			_renderThread.Join(TimeSpan.FromSeconds(1));
		}
		catch (Exception e)
		{
			this.LogDebug()?.Debug($"Failed to join the headless rendering thread on exit: {e.Message}");
		}

		_surface?.Dispose();
		_renderInvalidationEvent.Dispose();
	}
}
