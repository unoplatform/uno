using System;
using System.Threading;
using SkiaSharp;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	/// <summary>
	/// Dedicated render thread that owns Draw + CopyPixels (SwapBuffers/BitBlt).
	/// The UI thread records SKPictures and signals this thread to present them.
	/// This mirrors the pattern used by WPF (milcore render thread) and the
	/// Uno Android GL thread.
	/// </summary>
	private sealed class RenderThread : IDisposable
	{
		private readonly Thread _thread;
		private readonly AutoResetEvent _frameSignal = new(false);
		private readonly ManualResetEventSlim _presentedEvent = new(false);
		private readonly IRenderer _renderer;
		private readonly Func<(SKPath clipPath, int width, int height)?> _drawFrame;
		private readonly Action<SKPath> _onClipPathUpdated;
		private readonly Action _onFramePresented;
		private volatile bool _disposed;

		internal RenderThread(
			IRenderer renderer,
			Func<(SKPath, int, int)?> drawFrame,
			Action<SKPath> onClipPathUpdated,
			Action onFramePresented)
		{
			_renderer = renderer;
			_drawFrame = drawFrame;
			_onClipPathUpdated = onClipPathUpdated;
			_onFramePresented = onFramePresented;
			_thread = new Thread(RenderLoop) { Name = "Uno Render Thread", IsBackground = true };
			_thread.Start();
		}

		/// <summary>
		/// Signals the render thread that a new frame is available for presentation.
		/// Multiple calls while the thread is busy coalesce into a single wake-up
		/// (AutoResetEvent semantics). Resets the present-completion event before
		/// signaling so a subsequent <see cref="WaitForNextPresent"/> always observes
		/// the next presentation, never a stale one from a prior frame.
		/// </summary>
		internal void SignalNewFrame()
		{
			_presentedEvent.Reset();
			_frameSignal.Set();
		}

		/// <summary>
		/// Blocks the calling thread until the render thread finishes presenting a frame.
		/// Used by ShowCore to ensure the first frame is painted before the window is shown.
		/// Returns true if a present completed within the timeout, false otherwise.
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

				_renderer.StartPaint();
				try
				{
					var result = _drawFrame();
					if (result is { } frame)
					{
						var (clipPath, width, height) = frame;
						_onClipPathUpdated(clipPath);
						_renderer.CopyPixels(width, height); // SwapBuffers/BitBlt — may block for VSync

						_presentedEvent.Set();
						_onFramePresented();
					}
				}
				catch (Exception ex)
				{
					typeof(RenderThread).LogError()?.Error($"Render thread error: {ex}");
				}
				finally
				{
					_renderer.EndPaint();
				}
			}
		}

		public void Dispose()
		{
			_disposed = true;
			_frameSignal.Set(); // Unblock if waiting

			// 250 ms covers a present (~16 ms at 60 Hz) plus slack. If that fails, the GPU
			// is likely hung. Wait one more bounded interval (2 s) and then abandon: leaving
			// the render thread (background, marked at construction) blocked is preferable
			// to hanging the UI thread on window close. The leaked synchronization
			// primitives and renderer outlive the thread, so the OS reclaims them at exit.
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
					return;
				}
			}

			_frameSignal.Dispose();
			_presentedEvent.Dispose();
		}
	}
}
