using System;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Paces a render thread to the DWM compositor's vsync via <see cref="PInvoke.DwmFlush"/>.
/// Used by renderers whose present returns without blocking (software BitBlt, Vulkan MAILBOX),
/// so the dedicated render thread doesn't spin and render frames the compositor just discards.
/// After repeated DwmFlush failures (DWM paused, RDP reconnect, fast user switch, GPU TDR) it
/// degrades permanently to a timer-driven <see cref="FramePacer"/> — losing vsync alignment
/// beats risking a hung render thread on every frame.
/// </summary>
internal sealed class Win32VSyncPacer : IDisposable
{
	// After this many consecutive DwmFlush failures we stop calling DwmFlush and pace via the timer.
	private const int DwmFlushFailureThreshold = 3;

	private readonly FramePacer _framePacer;
	private readonly AutoResetEvent _frameDeadlineReached = new(false);

	private int _consecutiveDwmFlushFailures;
	private bool _dwmFlushDegraded;

	/// <param name="fps">Target frame rate for the degraded timer fallback.</param>
	public Win32VSyncPacer(double fps)
	{
		_framePacer = new FramePacer(fps, () => _frameDeadlineReached.Set());
	}

	/// <summary>
	/// Anchors the absolute target tick at the start of every frame so a subsequent degraded
	/// <see cref="FramePacer"/> wait schedules at the next deadline (no drift). Call before presenting.
	/// </summary>
	public void OnFrameStart() => _framePacer.OnFrameStart();

	/// <summary>
	/// Blocks until the compositor's next vsync (or the <see cref="FramePacer"/> deadline once
	/// degraded). Call after presenting the frame.
	/// </summary>
	public void WaitForVSync()
	{
		var paceViaFramePacer = _dwmFlushDegraded;

		if (!_dwmFlushDegraded)
		{
			var dwmFlushResult = PInvoke.DwmFlush();
			if (dwmFlushResult.Failed)
			{
				_consecutiveDwmFlushFailures++;
				this.LogError()?.Error($"{nameof(PInvoke.DwmFlush)} failed: {dwmFlushResult} (failure {_consecutiveDwmFlushFailures}/{DwmFlushFailureThreshold})");

				if (_consecutiveDwmFlushFailures >= DwmFlushFailureThreshold)
				{
					_dwmFlushDegraded = true;
					this.LogWarn()?.Warn(
						$"{nameof(PInvoke.DwmFlush)} failed {DwmFlushFailureThreshold} times consecutively; " +
						$"falling back to FramePacer-driven pacing at {_framePacer.TargetIntervalMs:F1} ms. " +
						"Frame timing will not be vsync-aligned for the rest of this window's lifetime.");
				}

				paceViaFramePacer = true;
			}
			else
			{
				_consecutiveDwmFlushFailures = 0;
			}
		}

		if (paceViaFramePacer)
		{
			_framePacer.RequestFrame();
			_frameDeadlineReached.WaitOne();
		}
	}

	/// <summary>
	/// Retargets the degraded <see cref="FramePacer"/> (e.g. when the screen refresh rate changes).
	/// </summary>
	public void UpdateTargetFps(double fps) => _framePacer.UpdateTargetFps(fps);

	public void Dispose()
	{
		_framePacer.Dispose();
		_frameDeadlineReached.Dispose();
	}
}
