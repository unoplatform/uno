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
		/// (AutoResetEvent semantics).
		/// </summary>
		internal void SignalNewFrame() => _frameSignal.Set();

		/// <summary>
		/// Blocks the calling thread until the render thread finishes presenting a frame.
		/// Used by ShowCore to ensure the first frame is painted before the window is shown.
		/// </summary>
		internal void WaitForNextPresent(TimeSpan timeout) => _presentedEvent.Wait(timeout);

		private void RenderLoop()
		{
			while (!_disposed)
			{
				_frameSignal.WaitOne();
				if (_disposed)
				{
					break;
				}

				_presentedEvent.Reset();
				_renderer.StartPaint();
				try
				{
					var result = _drawFrame();
					if (result is var (clipPath, width, height))
					{
						_onClipPathUpdated(clipPath);
						_renderer.CopyPixels(width, height); // SwapBuffers/BitBlt — may block for VSync
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

				_presentedEvent.Set();
				_onFramePresented();
			}
		}

		public void Dispose()
		{
			_disposed = true;
			_frameSignal.Set(); // Unblock if waiting
			_thread.Join(timeout: TimeSpan.FromSeconds(2));
			_frameSignal.Dispose();
			_presentedEvent.Dispose();
		}
	}
}
