using System;
using System.Diagnostics;
using System.Threading;
using Uno.UI;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost
{
	private readonly AutoResetEvent _renderRequested = new(false);
	private volatile bool _renderLoopRunning = true;
	private double _targetInterval = 1000.0 / FeatureConfiguration.CompositionTarget.FrameRate;

	private void InitRenderThread()
	{
		new Thread(RenderLoop)
		{
			IsBackground = true,
			Name = "X11RenderThread",
			Priority = ThreadPriority.AboveNormal
		}.Start();
	}

	private void RenderLoop()
	{
		var stopwatch = Stopwatch.StartNew();

		while (_renderLoopRunning)
		{
			_renderRequested.WaitOne();

			var frameStart = stopwatch.Elapsed.TotalMilliseconds;
			_renderer?.Render();
			var elapsed = stopwatch.Elapsed.TotalMilliseconds - frameStart;

			var remaining = Volatile.Read(ref _targetInterval) - elapsed;
			if (remaining > 0)
			{
				Thread.Sleep((int)remaining);
			}
		}
	}

	internal void UpdateRenderTimerFps(double fps)
	{
		if (FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate)
		{
			Volatile.Write(ref _targetInterval, TimeSpan.FromSeconds(1.0 / fps).TotalMilliseconds);
		}
	}

	void IXamlRootHost.InvalidateRender()
	{
		if (!_closed.Task.IsCompleted)
		{
			_renderRequested.Set();
		}
	}
}
