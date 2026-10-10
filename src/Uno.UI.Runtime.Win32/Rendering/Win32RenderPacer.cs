using System;
using System.Diagnostics;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Runtime.Hosting;

namespace Uno.UI.Runtime.Win32;

/// <summary>
/// Starts a window's frames on the DWM compositor's vsync (<see cref="Win32Vsync"/>) and tells each frame the time
/// of the vsync it started on. Contexts whose present returns without blocking (software BitBlt, Vulkan MAILBOX,
/// GL under a fixed FrameRate) wait here after presenting, so the render thread neither spins nor starts two frames
/// on one vsync; contexts whose present blocks on the display only take the vsync time from it.
/// <para>
/// A frame that starts after the render thread was idle starts at once rather than on the next vsync, so input
/// isn't held back by up to a refresh period. It still gets the vsync it started after.
/// </para>
/// <para>
/// A FrameRate that isn't the refresh rate (SetFrameRateAsScreenRefreshRate = false) stays on vsync when the refresh
/// rate is a multiple of it, waiting that many vsyncs per frame. Otherwise frames are paced by a timer and get no
/// vsync time, so the frame clock falls back to its grid.
/// </para>
/// <para>
/// When the vsync wait fails or returns without a vsync (display off, DWM restarting, a GPU reset), frames fall back
/// to the timer, and the vsync is retried a second later.
/// </para>
/// </summary>
internal sealed class Win32RenderPacer : IDisposable
{
	private const int VsyncFailureThreshold = 3;

	private readonly bool _followRefreshRate;
	private readonly double _frameRate;
	private readonly FramePacer _framePacer;
	private readonly AutoResetEvent _frameDeadlineReached = new(false);

	// The vsync the current frame started on, 0 when it isn't on vsync, and DWM's refresh period, in QPC ticks.
	private long _frameVsync;
	private long _period;
	// Vsyncs per frame.
	private int _divisor;

	private int _consecutiveVsyncFailures;
	private long _vsyncRetryTimestamp;
	private bool _vsyncSuspendedLogged;

	/// <param name="frameRate">The configured FrameRate, or the screen refresh rate when following it; also the timer's fallback rate.</param>
	/// <param name="followRefreshRate">True: one frame per vsync. False: <paramref name="frameRate"/>, on vsync when the refresh rate is a multiple of it.</param>
	public Win32RenderPacer(double frameRate, bool followRefreshRate)
	{
		_followRefreshRate = followRefreshRate;
		_frameRate = frameRate;
		_framePacer = new FramePacer(frameRate, () => _frameDeadlineReached.Set());
	}

	/// <summary>
	/// Call on the render thread as a frame starts, before it is drawn.
	/// </summary>
	/// <returns>The <see cref="Stopwatch"/> time of the vsync the frame starts on, or null when frames aren't on vsync.</returns>
	public long? BeginFrame()
	{
		_framePacer.OnFrameStart();

		var now = Stopwatch.GetTimestamp();
		if (now < _vsyncRetryTimestamp || !Win32Vsync.TryGetLatestVsync(now, out var vsync, out var period))
		{
			_frameVsync = 0;
			return null;
		}

		_period = period;
		_divisor = _followRefreshRate ? 1 : FrameClock.GetVsyncDivisor((long)(Stopwatch.Frequency / _frameRate), period);
		_frameVsync = _divisor > 0 ? vsync : 0;

		return _frameVsync != 0 ? _frameVsync : null;
	}

	/// <summary>
	/// Blocks until the next frame is due: the vsync after the one this frame started on (or the matching later one
	/// under a fixed FrameRate), or the timer deadline. Call after presenting.
	/// </summary>
	public void WaitForNextFrame()
	{
		if (_frameVsync == 0)
		{
			WaitForTimer();
			return;
		}

		// A frame that took longer than its interval needs no wait: its successor is already late.
		var elapsed = (Stopwatch.GetTimestamp() - _frameVsync) / _period;
		for (var vsync = elapsed + 1; vsync <= _divisor; vsync++)
		{
			var due = _frameVsync + vsync * _period;
			var succeeded = Win32Vsync.WaitForVsync(out var status);

			// A wait that comes back well before the vsync was due didn't wait for one: the compositor clock does
			// that when the display is off, and spinning on it would burn a core.
			if (!succeeded || Stopwatch.GetTimestamp() < due - _period / 4)
			{
				OnVsyncFailed(succeeded ? "returned early" : $"failed with 0x{status:X8}");
				WaitForTimer();
				return;
			}
		}

		OnVsyncSucceeded();
	}

	/// <summary>Retargets the timer (the fallback, or the pacer for a FrameRate the refresh rate isn't a multiple of).</summary>
	public void UpdateTargetFps(double fps) => _framePacer.UpdateTargetFps(fps);

	private void WaitForTimer()
	{
		_framePacer.RequestFrame();
		_frameDeadlineReached.WaitOne();
	}

	private void OnVsyncSucceeded()
	{
		if (_vsyncSuspendedLogged)
		{
			_vsyncSuspendedLogged = false;
			this.LogInfo()?.Info("The compositor's vsync is back; frames are on vsync again.");
		}

		_consecutiveVsyncFailures = 0;
	}

	private void OnVsyncFailed(string reason)
	{
		this.LogDebug()?.Debug($"Waiting for the compositor's vsync {reason}.");

		if (++_consecutiveVsyncFailures < VsyncFailureThreshold)
		{
			return;
		}

		_consecutiveVsyncFailures = 0;
		_vsyncRetryTimestamp = Stopwatch.GetTimestamp() + Stopwatch.Frequency;

		if (!_vsyncSuspendedLogged)
		{
			_vsyncSuspendedLogged = true;
			this.LogWarn()?.Warn(
				$"Waiting for the compositor's vsync {reason} {VsyncFailureThreshold} times in a row; " +
				$"pacing frames with a {_framePacer.TargetIntervalMs:F1} ms timer until it recovers.");
		}
	}

	public void Dispose()
	{
		_framePacer.Dispose();
		_frameDeadlineReached.Dispose();
	}
}
