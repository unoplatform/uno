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
/// Keeps the Skia two-phase render cycle ticking for a single headless window on a dedicated thread, so
/// the app lifecycle, composition animations and <c>RenderTargetBitmap</c> behave like a real target.
/// The window itself produces no pixel output; the paint walk is skipped globally
/// (<c>FeatureConfiguration.Rendering.SkipVisualTreePainting</c>) and frames are drawn to a null surface.
/// </summary>
internal sealed class HeadlessRenderer : IDisposable
{
	private readonly IXamlRootHost _host;
	private readonly AutoResetEvent _renderInvalidationEvent = new(false);
	private readonly Thread _renderThread;
	private volatile bool _disposed;

	private SKSurface? _nullSurface;

	public HeadlessRenderer(IXamlRootHost host)
	{
		_host = host;

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

	/// <summary>Ticks the render cycle (draws nothing, keeping scheduling/animations/RenderTargetBitmap alive).</summary>
	public void Invalidate() => _renderInvalidationEvent.Set();

	private void Render()
	{
		// The visual tree may not be available yet on the first invalidation(s).
		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget ct)
		{
			return;
		}

		ct.OnNativePlatformFrameRequested(_nullSurface?.Canvas, size =>
		{
			_nullSurface?.Dispose();
			_nullSurface = SKSurface.CreateNull((int)size.Width, (int)size.Height);
			return _nullSurface.Canvas;
		});
		_nullSurface?.Flush();
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

		_nullSurface?.Dispose();
		_renderInvalidationEvent.Dispose();
	}
}
