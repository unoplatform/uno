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

	// Diagnostic (UNO_RENDER_CONTINUOUS): present every ~1ms regardless of frame requests, so per-frame render cost
	// can be profiled headless (Xvfb has no compositor keeping an animation's frame requests flowing). Each present
	// re-enqueues a record, so the tree stays current; the 1ms yield keeps the UI thread free to record. Off by default.
	private static readonly bool _continuousRender = System.Environment.GetEnvironmentVariable("UNO_RENDER_CONTINUOUS") == "1";

	private void RenderLoop()
	{
		while (_renderLoopRunning)
		{
			if (_continuousRender) { Thread.Sleep(1); }
			else { _renderRequested.WaitOne(); }

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
