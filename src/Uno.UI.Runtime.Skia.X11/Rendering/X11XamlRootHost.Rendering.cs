using Microsoft.UI.Xaml.Media;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost
{
	private X11RenderThread? _renderThread;

	bool IXamlRootHost.SupportsRenderThrottle => true;

	private void InitializeRenderThread()
	{
		if (_renderer is null)
		{
			return;
		}

		_renderThread = new X11RenderThread(
			_renderer,
			onFramePresented: () =>
			{
				NativeDispatcher.Main.Enqueue(
					OnFramePresented,
					NativeDispatcherPriority.High);
			});
	}

	/// <summary>
	/// No-op. Previously updated the timer interval based on screen refresh rate.
	/// With the render thread, VSync pacing comes from glXSwapBuffers/eglSwapBuffers
	/// blocking, so explicit frame rate configuration is no longer needed.
	/// </summary>
	internal void UpdateRenderTimerFps(double fps)
	{
		// Render thread is paced by VSync (SwapBuffers blocking).
	}

	private void OnFramePresented()
	{
		var ct = ((IXamlRootHost)this).RootElement?.Visual.CompositionTarget as CompositionTarget;
		ct?.OnFramePresented();
	}

	void IXamlRootHost.InvalidateRender()
	{
		if (!_closed.Task.IsCompleted)
		{
			_renderThread?.SignalNewFrame();
		}
	}
}
