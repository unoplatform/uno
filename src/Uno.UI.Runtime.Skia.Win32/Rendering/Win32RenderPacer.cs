using System;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Paces a render thread, either to the DWM compositor's vsync via <see cref="PInvoke.DwmFlush"/>
/// or to a fixed frame rate via a timer-driven <see cref="FramePacer"/>. Used by renderers whose
/// present returns without blocking (software BitBlt, Vulkan MAILBOX) so the dedicated render thread
/// doesn't spin and render frames the compositor just discards.
/// <para>
/// When following the refresh rate it blocks on <see cref="PInvoke.DwmFlush"/>; after repeated
/// failures (DWM paused, RDP reconnect, fast user switch, GPU TDR) it degrades permanently to the
/// timer — losing vsync alignment beats a hung render thread. Otherwise it always paces with the
/// timer, honoring a custom FeatureConfiguration.CompositionTarget.FrameRate
/// (SetFrameRateAsScreenRefreshRate = false).
/// </para>
/// </summary>
internal sealed class Win32RenderPacer : IDisposable
{
	// After this many consecutive DwmFlush failures we stop calling DwmFlush and pace via the timer.
	private const int DwmFlushFailureThreshold = 3;

	private readonly bool _followRefreshRate;
	private readonly FramePacer _framePacer;
	private readonly AutoResetEvent _frameDeadlineReached = new(false);

	private int _consecutiveDwmFlushFailures;
	private bool _dwmFlushDegraded;

	/// <param name="fps">Timer target — the degraded fallback, or the active pacer when not following the refresh rate.</param>
	/// <param name="followRefreshRate">True: pace to the display refresh via DwmFlush. False: pace to <paramref name="fps"/> via the timer.</param>
	public Win32RenderPacer(double fps, bool followRefreshRate)
	{
		_followRefreshRate = followRefreshRate;
		_framePacer = new FramePacer(fps, () => _frameDeadlineReached.Set());
	}

	/// <summary>
	/// Anchors the absolute target tick at the start of every frame so a subsequent degraded
	/// <see cref="FramePacer"/> wait schedules at the next deadline (no drift). Call before presenting.
	/// </summary>
	public void OnFrameStart() => _framePacer.OnFrameStart();

	/// <summary>
	/// Blocks until it's time for the next frame — the compositor's next vsync when following the
	/// refresh rate, otherwise the timer deadline. Call after presenting the frame.
	/// </summary>
	public void WaitForNextFrame()
	{
		// When not following the display refresh, pace via the timer at the configured FrameRate;
		// DwmFlush would instead lock the loop to the refresh rate.
		var paceViaFramePacer = !_followRefreshRate || _dwmFlushDegraded;

		if (_followRefreshRate && !_dwmFlushDegraded)
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
