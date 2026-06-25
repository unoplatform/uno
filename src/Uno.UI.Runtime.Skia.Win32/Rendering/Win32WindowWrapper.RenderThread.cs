using System;
using System.Threading;
using SkiaSharp;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	/// <summary>
	/// Dedicated render thread that owns Draw + CopyPixels (SwapBuffers/BitBlt), mirroring
	/// WPF's milcore render thread. The UI thread records SKPictures and signals presents.
	/// </summary>
	private sealed class RenderThread : IDisposable
	{
		private readonly Thread _thread;
		private readonly AutoResetEvent _frameSignal = new(false);
		private readonly ManualResetEventSlim _presentedEvent = new(false);
		private readonly IRenderer _renderer;
		private readonly Func<(SKPath clipPath, int width, int height)?> _drawFrame;
		private readonly Action<SKPath> _onClipPathUpdated;
		private volatile bool _disposed;

		internal RenderThread(
			IRenderer renderer,
			Func<(SKPath, int, int)?> drawFrame,
			Action<SKPath> onClipPathUpdated)
		{
			_renderer = renderer;
			_drawFrame = drawFrame;
			_onClipPathUpdated = onClipPathUpdated;
			_thread = new Thread(RenderLoop) { Name = "Uno Render Thread", IsBackground = true };
			_thread.Start();
		}

		/// <summary>
		/// Signals that a new frame is available; calls coalesce into a single wake-up. Resets
		/// the present-completion event so <see cref="WaitForNextPresent"/> always observes the
		/// next present, never a stale one.
		/// </summary>
		internal void SignalNewFrame()
		{
			_presentedEvent.Reset();
			_frameSignal.Set();
		}

		/// <summary>
		/// Blocks until the render thread presents a frame; false if the timeout elapsed first.
		/// </summary>
		internal bool WaitForNextPresent(TimeSpan timeout) => _presentedEvent.Wait(timeout);

		private void RenderLoop()
		{
			while (!_disposed)
			{
				_frameSignal.WaitOne();
				if (_disposed)
				{
					break;
				}

				var startPaintSucceeded = false;
				try
				{
					_renderer.StartPaint();
					startPaintSucceeded = true;

					var result = _drawFrame();
					if (result is { } frame)
					{
						var (clipPath, width, height) = frame;
						_onClipPathUpdated(clipPath);
						_renderer.CopyPixels(width, height); // SwapBuffers/BitBlt — may block for VSync

						_presentedEvent.Set();
					}
				}
				catch (Exception ex)
				{
					typeof(RenderThread).LogError()?.Error($"Render thread error: {ex}");
				}
				finally
				{
					if (startPaintSucceeded)
					{
						_renderer.EndPaint();
					}
				}
			}
		}

		/// <summary>
		/// Stops the render thread, waits for it to exit, then releases its synchronization
		/// primitives. The join is intentionally unbounded: the loop only delays observing
		/// <see cref="_disposed"/> while a present is in flight, and a present completes in bounded
		/// time (a vsync wait, or a GPU TDR reset for a hung present), so the thread always exits.
		/// The caller must dispose the renderer and surface only after this returns — once the
		/// thread, the sole user of those resources, is guaranteed stopped.
		/// </summary>
		public void Dispose()
		{
			_disposed = true;
			_frameSignal.Set(); // Wake the loop if it's parked in WaitOne
			_thread.Join();

			_frameSignal.Dispose();
			_presentedEvent.Dispose();
		}
	}
}
