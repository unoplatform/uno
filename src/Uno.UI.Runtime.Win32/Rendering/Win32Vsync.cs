#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Uno.UI.Composition;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace Uno.UI.Runtime.Win32;

/// <summary>
/// The DWM compositor's vsync: a wait for its next frame, and the time of its latest one. Windowed content reaches
/// the screen on DWM's frames, so its clock is the one to start frames on, whatever the graphics context.
/// </summary>
/// <remarks>
/// Times are QPC values, which is what <see cref="Stopwatch.GetTimestamp"/> reads on Windows.
/// </remarks>
internal static unsafe class Win32Vsync
{
	private const uint WaitObject0 = 0;
	private const uint CompositorClockTimeoutMs = 100;

	// Windows 11 (22000+). It wakes as the compositor's frame starts, where DwmFlush returns once the frame is
	// composed, about a millisecond later. Not exported on Windows 10.
	private static readonly delegate* unmanaged[Stdcall]<uint, HANDLE*, uint, uint> s_waitForCompositorClock = GetCompositorClockWait();

	/// <summary>Blocks until the compositor's next frame. False when the wait failed (e.g. the display is off).</summary>
	public static bool WaitForVsync(out uint status)
	{
		if (s_waitForCompositorClock is not null)
		{
			status = s_waitForCompositorClock(0, null, CompositorClockTimeoutMs);
			return status == WaitObject0;
		}

		var result = PInvoke.DwmFlush();
		status = (uint)result.Value;
		return result.Succeeded;
	}

	/// <summary>The latest vsync at or before <paramref name="now"/> and the refresh period, in QPC ticks.</summary>
	public static bool TryGetLatestVsync(long now, out long vsync, out long period)
	{
		DWM_TIMING_INFO info = new() { cbSize = (uint)sizeof(DWM_TIMING_INFO) };

		// qpcVBlank is a vblank on DWM's cadence, but not reliably the last one: it is already the next one once
		// DwmFlush returns.
		if (PInvoke.DwmGetCompositionTimingInfo(HWND.Null, &info).Failed || info.qpcRefreshPeriod == 0 || info.qpcVBlank == 0)
		{
			vsync = period = 0;
			return false;
		}

		period = (long)info.qpcRefreshPeriod;
		vsync = FrameClock.LatestVsyncAtOrBefore((long)info.qpcVBlank, period, now);
		return true;
	}

	private static delegate* unmanaged[Stdcall]<uint, HANDLE*, uint, uint> GetCompositorClockWait()
		=> NativeLibrary.TryLoad("dcomp.dll", out var dcomp) && NativeLibrary.TryGetExport(dcomp, "DCompositionWaitForCompositorClock", out var export)
			? (delegate* unmanaged[Stdcall]<uint, HANDLE*, uint, uint>)export
			: null;
}
