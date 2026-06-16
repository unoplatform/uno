using System;
using System.Threading;
using Uno.Foundation.Logging;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Dedicated render thread for X11. The UI thread records SKPictures and signals
/// this thread to present them. Mirrors Win32's RenderThread and iOS's CADisplayLink
/// render thread patterns.
///
/// Key improvements over the previous timer-based approach:
/// - GL context stays on a single dedicated thread (required by GLX/EGL)
/// - glXSwapBuffers/eglSwapBuffers blocking for VSync correctly blocks this thread
/// - AutoResetEvent coalesces multiple invalidation signals
/// - OnFramePresented posted to UI thread enables render throttling
/// </summary>
internal sealed class X11RenderThread : IDisposable
{
	private readonly Thread _thread;
	private readonly AutoResetEvent _frameSignal = new(false);
	private readonly ManualResetEventSlim _presentedEvent = new(false);
	private readonly X11Renderer _renderer;
	private readonly Action _onFramePresented;
	private volatile bool _disposed;

	internal X11RenderThread(X11Renderer renderer, Action onFramePresented)
	{
		_renderer = renderer;
		_onFramePresented = onFramePresented;
		_thread = new Thread(RenderLoop) { Name = "Uno X11 Render Thread", IsBackground = true };
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
	/// Used to ensure the first frame is painted before the window is shown.
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
			try
			{
				_renderer.Render();
			}
			catch (Exception ex)
			{
				typeof(X11RenderThread).LogError()?.Error($"Render thread error: {ex}");
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
