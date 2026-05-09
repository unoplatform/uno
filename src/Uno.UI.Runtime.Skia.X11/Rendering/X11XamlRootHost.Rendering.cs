using System.Threading;
using Uno.UI;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost
{
	private readonly AutoResetEvent _renderRequested = new(false);
	private volatile bool _renderLoopRunning = true;
	private readonly Thread _renderThread;
	private readonly FramePacer _framePacer;

	private FramePacer CreateFramePacer()
	{
		return new FramePacer(
			FeatureConfiguration.CompositionTarget.FrameRate,
			() => _renderRequested.Set());
	}

	private Thread InitRenderThread()
	{
		var thread = new Thread(RenderLoop)
		{
			IsBackground = true,
			Name = "X11RenderThread",
			Priority = ThreadPriority.AboveNormal
		};
		thread.Start();
		return thread;
	}

	private void RenderLoop()
	{
		while (_renderLoopRunning)
		{
			_renderRequested.WaitOne();

			_framePacer.OnFrameStart();
			_renderer?.Render();
		}
	}

	internal void UpdateRenderTimerFps(double fps)
	{
		if (FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate)
		{
			_framePacer.UpdateTargetFps(fps);
		}
	}

	void IXamlRootHost.InvalidateRender()
	{
		if (!_closed.Task.IsCompleted)
		{
			_framePacer.RequestFrame();
		}
	}
}
