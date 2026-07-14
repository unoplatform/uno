#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.Headless;

/// <summary>
/// Drives the Skia two-phase render cycle for a single headless window on a dedicated thread. The
/// keep-alive cycle always runs (drawing into a null surface) so the app lifecycle, composition
/// animations and <c>RenderTargetBitmap</c> behave like a real target. On top of it, callers may pull
/// a frame via <see cref="RenderIntoAsync"/> (typically in response to the window's new-frame signal);
/// the host schedules that render, drawing into the caller's buffer and completing the task when done.
/// </summary>
internal sealed class HeadlessRenderer : IDisposable
{
	private readonly record struct RenderRequest(IntPtr Buffer, int RowBytes, SKColorType ColorType, TaskCompletionSource Completion, CancellationToken CancellationToken);

	private readonly IXamlRootHost _host;
	private readonly int _rawWidth;
	private readonly int _rawHeight;

	private readonly ConcurrentQueue<RenderRequest> _requests = new();
	private readonly AutoResetEvent _renderInvalidationEvent = new(false);
	private readonly Thread _renderThread;
	private volatile bool _disposed;

	private SKSurface? _nullSurface;

	public HeadlessRenderer(IXamlRootHost host, int rawWidth, int rawHeight)
	{
		_host = host;
		_rawWidth = rawWidth;
		_rawHeight = rawHeight;

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

					// Serve pull requests on top of the cycle; when none are pending, still tick the
					// keep-alive pass so the lifecycle/animations/RenderTargetBitmap keep advancing.
					var served = false;
					while (_requests.TryDequeue(out var request))
					{
						ProcessRequest(request);
						served = true;
					}

					if (!served)
					{
						RenderKeepAlive();
					}
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

	/// <summary>Ticks the render cycle: serves any pending pull requests, else runs a keep-alive pass.</summary>
	public void Invalidate() => _renderInvalidationEvent.Set();

	/// <summary>Queues a render of the current frame into the caller's buffer; see <see cref="HeadlessWindow.RenderIntoAsync"/>.</summary>
	internal Task RenderIntoAsync(IntPtr buffer, int rowBytes, HeadlessPixelFormat pixelFormat, CancellationToken cancellationToken)
	{
		if (buffer == IntPtr.Zero)
		{
			throw new ArgumentException("The render buffer pointer must not be null.", nameof(buffer));
		}

		var minStride = _rawWidth * pixelFormat.BytesPerPixel();
		if (rowBytes < minStride)
		{
			throw new ArgumentOutOfRangeException(nameof(rowBytes), $"The render buffer stride must be at least {minStride} bytes for a {_rawWidth}px-wide {pixelFormat} frame.");
		}

		if (_disposed)
		{
			return Task.FromException(new ObjectDisposedException(nameof(HeadlessRenderer)));
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}

		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_requests.Enqueue(new RenderRequest(buffer, rowBytes, pixelFormat.ToSkColorType(), completion, cancellationToken));
		_renderInvalidationEvent.Set();
		return completion.Task;
	}

	private void ProcessRequest(RenderRequest request)
	{
		if (request.CancellationToken.IsCancellationRequested)
		{
			request.Completion.TrySetCanceled(request.CancellationToken);
			return;
		}

		try
		{
			// No visual tree yet (e.g. before content is loaded): complete without touching the buffer.
			if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget ct)
			{
				request.Completion.TrySetResult();
				return;
			}

			SKSurface? surface = null;
			try
			{
				ct.OnNativePlatformFrameRequested(null, size =>
				{
					surface?.Dispose();
					surface = SKSurface.Create(new SKImageInfo((int)size.Width, (int)size.Height, request.ColorType, SKAlphaType.Premul), request.Buffer, request.RowBytes);
					return surface.Canvas;
				});
				surface?.Flush();
			}
			finally
			{
				surface?.Dispose();
			}

			request.Completion.TrySetResult();
		}
		catch (Exception e)
		{
			request.Completion.TrySetException(e);
		}
	}

	private void RenderKeepAlive()
	{
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

		// Fail any awaiters that never got serviced so they don't hang.
		while (_requests.TryDequeue(out var request))
		{
			request.Completion.TrySetException(new ObjectDisposedException(nameof(HeadlessRenderer)));
		}

		_nullSurface?.Dispose();
		_renderInvalidationEvent.Dispose();
	}
}
