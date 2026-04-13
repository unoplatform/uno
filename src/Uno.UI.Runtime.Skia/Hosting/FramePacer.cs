using System.Diagnostics;
using Timer = System.Timers.Timer;

namespace Uno.UI.Runtime.Skia.Hosting;

/// <summary>
/// Self-correcting frame pacer that tracks absolute target timestamps
/// to eliminate accumulated timer/sleep jitter.
/// This class tracks where each frame should start in absolute time,
/// so overshoot from one frame is absorbed by shortening the next wait.
/// </summary>
/// <remarks>
/// Usage: construct with a callback, then call <see cref="RequestFrame"/>
/// to schedule the callback after the corrected interval.
/// Call <see cref="OnFrameStart"/> at the beginning of each frame.
/// </remarks>
internal class FramePacer : IDisposable
{
	private readonly Timer _timer;
	private long _nextTargetTick;
	private long _targetIntervalTicks;

	/// <summary>
	/// Creates a frame pacer with an internal one-shot timer.
	/// <paramref name="onTimerElapsed"/> is invoked when it is time
	/// to start the next frame.
	/// </summary>
	public FramePacer(double fps, Action onTimerElapsed)
	{
		_targetIntervalTicks = FpsToTicks(fps);
		_timer = new Timer { AutoReset = false, Interval = TargetIntervalMs };
		_timer.Elapsed += (_, _) => onTimerElapsed();
	}

	/// <summary>
	/// The target interval in milliseconds (read-only snapshot; thread-safe).
	/// </summary>
	public double TargetIntervalMs => Interlocked.Read(ref _targetIntervalTicks) / (double)Stopwatch.Frequency * 1000;

	/// <summary>
	/// Update the target frame rate (e.g. when the display refresh rate changes).
	/// </summary>
	public void UpdateTargetFps(double fps)
	{
		Interlocked.Exchange(ref _targetIntervalTicks, FpsToTicks(fps));
	}

	/// <summary>
	/// Schedules the timer callback after the corrected interval.
	/// </summary>
	public void RequestFrame()
	{
		var now = Stopwatch.GetTimestamp();
		var remainingMs = (Interlocked.Read(ref _nextTargetTick) - now) / (double)Stopwatch.Frequency * 1000;
		_timer.Interval = Math.Clamp(remainingMs, 1, TargetIntervalMs);
		_timer.Enabled = true;
	}

	/// <summary>
	/// Call at the start of each frame. Advances the absolute target time
	/// by one interval. If we've fallen behind (first frame, long stall,
	/// or no frames requested for a while), snaps forward so we don't
	/// try to "catch up" with a burst of rapid frames.
	/// </summary>
	public void OnFrameStart()
	{
		var now = Stopwatch.GetTimestamp();
		var interval = Interlocked.Read(ref _targetIntervalTicks);
		var nextTargetTick = Interlocked.Read(ref _nextTargetTick) + interval;
		if (nextTargetTick <= now)
		{
			nextTargetTick = now + interval;
		}
		Interlocked.Exchange(ref _nextTargetTick, nextTargetTick);
	}

	private static long FpsToTicks(double fps)
	{
		return (long)(Stopwatch.Frequency / fps);
	}

	public void Dispose() => _timer.Dispose();
}
