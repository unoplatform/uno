using System.Diagnostics;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Composition;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Hosting;

namespace Uno.UI.Runtime.X11;

/// <remarks>
/// Frames start on the window's vsync (<see cref="X11PresentVsync"/>) and carry its time. A frame requested after
/// the render thread was idle, or after a frame that ran long, starts at once on the latest vsync rather than
/// waiting for the next one, so input isn't held back by up to a refresh period.
/// <para>
/// A FrameRate that isn't the refresh rate (SetFrameRateAsScreenRefreshRate = false) stays on vsync when the refresh
/// rate is a multiple of it. Otherwise, and while the window is unmapped or hidden, or the server has no Present
/// extension, frames are paced by a timer and carry no vsync time, so the frame clock falls back to its grid.
/// When the server stops reporting vsyncs (a CRTC turned off, a window it fakes vblanks for), frames also fall
/// back to the timer, and vsync is retried a second later.
/// </para>
/// </remarks>
internal partial class X11XamlRootHost
{
	private const int VsyncFailureThreshold = 3;

	private readonly AutoResetEvent _renderRequested = new(false);
	private volatile bool _renderLoopRunning = true;
	private readonly Thread _renderThread;
	private readonly FramePacer _framePacer;

	// Its connection is used only by the render thread, and closed once that has stopped.
	private X11PresentVsync? _vsync;
	private readonly long _frameRateInterval = (long)(Stopwatch.Frequency / FeatureConfiguration.CompositionTarget.FrameRate);
	// The vsync the latest frame started on, 0 when it wasn't on one.
	private long _frameVsync;
	private int _consecutiveVsyncFailures;
	private long _vsyncRetryTimestamp;
	private bool _vsyncSuspendedLogged;

	private volatile bool _isUnmapped;
	private volatile bool _isFullyObscured;

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

	private bool IsPacedByVsync
		=> _vsync is not null
			&& !_isUnmapped
			&& !_isFullyObscured
			&& Stopwatch.GetTimestamp() >= Volatile.Read(ref _vsyncRetryTimestamp)
			&& GetVsyncDivisor() != 0;

	private void RenderLoop()
	{
		while (_renderLoopRunning)
		{
			_renderRequested.WaitOne();
			if (!_renderLoopRunning)
			{
				break;
			}

			var vsync = IsPacedByVsync ? WaitForFrameVsync() : null;
			_framePacer.OnFrameStart();
			_renderer?.Render(vsync);
		}
	}

	/// <returns>The Stopwatch time of the vsync this frame starts on, or null when it couldn't get one.</returns>
	private long? WaitForFrameVsync()
	{
		var vsync = _vsync!;
		var divisor = GetVsyncDivisor();
		var period = vsync.Period;
		var latest = vsync.GetLatestVsync(Stopwatch.GetTimestamp());

		// The vsync this frame is due on already passed: the render thread was idle, or the last frame ran long.
		if (latest != 0 && (_frameVsync == 0 || latest >= _frameVsync + divisor * period - period / 2))
		{
			return _frameVsync = latest;
		}

		if (vsync.WaitForVsync(divisor, out var failure) is { } next)
		{
			OnVsyncSucceeded();
			return _frameVsync = next;
		}

		OnVsyncFailed(failure);
		_frameVsync = 0;
		return null;
	}

	// Vsyncs per frame, or 0 when FrameRate isn't the refresh rate divided by a whole number.
	private int GetVsyncDivisor()
		=> FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate || _vsync is not { Period: > 0 and var period }
			? 1
			: FrameClock.GetVsyncDivisor(_frameRateInterval, period);

	private void OnVsyncSucceeded()
	{
		_consecutiveVsyncFailures = 0;
		if (_vsyncSuspendedLogged)
		{
			_vsyncSuspendedLogged = false;
			this.LogInfo()?.Info("The X server reports vsyncs again; frames are on vsync again.");
		}
	}

	private void OnVsyncFailed(string? reason)
	{
		this.LogDebug()?.Debug($"Waiting for vsync failed: the X server {reason}.");

		if (++_consecutiveVsyncFailures < VsyncFailureThreshold)
		{
			return;
		}

		_consecutiveVsyncFailures = 0;
		Volatile.Write(ref _vsyncRetryTimestamp, Stopwatch.GetTimestamp() + Stopwatch.Frequency);

		if (!_vsyncSuspendedLogged)
		{
			_vsyncSuspendedLogged = true;
			this.LogWarn()?.Warn(
				$"Waiting for vsync failed {VsyncFailureThreshold} times in a row (the X server {reason}); " +
				$"pacing frames with a {_framePacer.TargetIntervalMs:F1} ms timer until it reports vsyncs again.");
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
		if (_closed.Task.IsCompleted)
		{
			return;
		}

		// On vsync, the render loop does the waiting.
		if (IsPacedByVsync)
		{
			_renderRequested.Set();
		}
		else
		{
			_framePacer.RequestFrame();
		}
	}
}
