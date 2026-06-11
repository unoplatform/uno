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

		public void Dispose() => DisposeAndTryJoin();

		/// <summary>
		/// Stops the render thread. Returns <c>false</c> when the thread had to be abandoned
		/// (stuck in a native present); the caller must then not dispose renderer/surface
		/// resources the abandoned thread might still be touching.
		/// </summary>
		internal bool DisposeAndTryJoin()
		{
			_disposed = true;
			_frameSignal.Set(); // Unblock if waiting

			// 250 ms covers a present (~16 ms at 60 Hz) plus slack; after one more bounded wait
			// (2 s), abandon the background thread — leaking the renderer and synchronization
			// primitives until process exit beats hanging the UI thread on window close.
			var joined = _thread.Join(timeout: TimeSpan.FromMilliseconds(250));
			if (!joined)
			{
				typeof(RenderThread).LogWarn()?.Warn(
					"Render thread did not exit within 250 ms during dispose; waiting up to 2 s more " +
					"before abandoning. This usually indicates a stuck GPU present " +
					"(SwapBuffers/BitBlt/DwmFlush blocked).");

				joined = _thread.Join(timeout: TimeSpan.FromSeconds(2));
				if (!joined)
				{
					typeof(RenderThread).LogError()?.Error(
						"Render thread still alive after 2 s; abandoning to keep window close responsive. " +
						"Leaking _frameSignal, _presentedEvent and renderer until process exit.");
					return false;
				}
			}

			_frameSignal.Dispose();
			_presentedEvent.Dispose();
			return true;
		}
	}
}
