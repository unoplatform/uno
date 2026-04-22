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
	/// This mirrors the pattern used by WPF (milcore render thread), Avalonia
	/// (ServerCompositor), and Uno Android (GLSurfaceView GL thread).
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

						// Only signal "presented" when an actual frame was drawn and copied.
						// WM_PAINT delivered before the first SynchronousRender produces a null
						// result; firing OnFramePresented in that case would surface a phantom
						// CompositionTarget.FrameRendered with no actual present behind it.
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

			// 250 ms is enough for the thread to finish a present (~16 ms at 60 Hz) plus
			// some slack. If the join times out the GPU is likely hung — warn and then
			// keep waiting so we never dispose synchronization primitives (or let the
			// caller dispose _renderer next) while the render thread can still touch them.
			var joined = _thread.Join(timeout: TimeSpan.FromMilliseconds(250));
			if (!joined)
			{
				typeof(RenderThread).LogWarn()?.Warn(
					"Render thread did not exit within 250 ms during dispose; waiting for it to exit " +
					"before releasing shared resources. This usually indicates a stuck GPU present " +
					"(SwapBuffers/BitBlt blocked on VSync).");
				_thread.Join();
			}

			_frameSignal.Dispose();
			_presentedEvent.Dispose();
		}
	}
}
