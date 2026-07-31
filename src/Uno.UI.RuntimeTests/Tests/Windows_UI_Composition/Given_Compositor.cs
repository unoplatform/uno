#if __SKIA__
using System;
using System.Linq;
using Microsoft.UI.Composition;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_Compositor
{
	private const long Period = TimeSpan.TicksPerSecond / 120;

	[TestMethod]
	[RunsOnUIThread]
	public void When_Skia_Backend_Then_IsSoftwareRenderer_Populated()
	{
		// Every Skia render backend must report whether it rasterizes on the CPU as soon as
		// its renderer is selected; effect brushes rely on this while recording the scene.
		Assert.IsNotNull(Compositor.GetSharedCompositor().IsSoftwareRenderer);
	}

	/// <summary>
	/// Frames present one per vsync but are not recorded on one, so a driver evaluating against the raw
	/// record instant turns that wobble into v·Δt of position error. The frame clock must hand drivers
	/// the cadence the frames are shown on instead.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Records_Jitter_Then_Frame_Clock_Steps_Evenly()
	{
		var compositor = new Compositor();
		var random = new Random(42);

		var raw = new long[400];
		var stamps = new long[400];
		for (var i = 0; i < raw.Length; i++)
		{
			// ±1.5ms of record-phase wobble around an exact 120Hz cadence.
			raw[i] = TimeSpan.TicksPerSecond + i * Period + (long)((random.NextDouble() - 0.5) * 3 * TimeSpan.TicksPerMillisecond);
			stamps[i] = compositor.GetFrameTimestamp(raw[i]);
		}

		var rawWorst = Deltas(raw).Skip(100).Max(d => Math.Abs(d - Period));
		var clockWorst = Deltas(stamps).Skip(100).Max(d => Math.Abs(d - Period));

		Assert.IsTrue(rawWorst > Period / 4, $"the raw clock should be visibly uneven, was {Ms(rawWorst)}ms off");
		Assert.IsTrue(clockWorst < Period / 16, $"the frame clock should be even, was {Ms(clockWorst)}ms off");
	}

	/// <summary>The grid may not drift: a driver reaching the end of its curve must do so on time.</summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Records_Jitter_Then_Frame_Clock_Does_Not_Drift()
	{
		var compositor = new Compositor();
		var random = new Random(7);

		long first = 0, last = 0, firstRaw = 0, lastRaw = 0;
		for (var i = 0; i < 400; i++)
		{
			var raw = TimeSpan.TicksPerSecond + i * Period + (long)((random.NextDouble() - 0.5) * 3 * TimeSpan.TicksPerMillisecond);
			var stamp = compositor.GetFrameTimestamp(raw);

			if (i == 100)
			{
				(first, firstRaw) = (stamp, raw);
			}

			(last, lastRaw) = (stamp, raw);
		}

		Assert.IsTrue(Math.Abs((last - first) - (lastRaw - firstRaw)) < Period, "the frame clock drifted from the real one");
	}

	/// <summary>
	/// A record that overruns its vsync holds the previous picture on screen for two intervals, so the
	/// motion has to cover both — smoothing that away would show as a dropped frame plus a slow one.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Frame_Dropped_Then_Frame_Clock_Steps_Twice()
	{
		var compositor = new Compositor();

		var stamps = new long[60];
		for (var i = 0; i < stamps.Length; i++)
		{
			// One record overruns, so the frames after it sit a whole interval later.
			var raw = TimeSpan.TicksPerSecond + i * Period + (i >= 40 ? Period : 0);
			stamps[i] = compositor.GetFrameTimestamp(raw);
		}

		Assert.AreEqual(2 * Period, stamps[40] - stamps[39], "the overrun frame should advance by two intervals");
		Assert.AreEqual(Period, stamps[45] - stamps[44], "the cadence should resume immediately after");
	}

	/// <summary>After an idle gap the grid's phase means nothing, so it must re-anchor rather than crawl.</summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Loop_Goes_Idle_Then_Frame_Clock_Reanchors()
	{
		var compositor = new Compositor();

		long stamp = 0, raw = 0;
		for (var i = 0; i < 60; i++)
		{
			raw = TimeSpan.TicksPerSecond + i * Period;
			stamp = compositor.GetFrameTimestamp(raw);
		}

		raw += 5 * TimeSpan.TicksPerSecond;
		stamp = compositor.GetFrameTimestamp(raw);

		Assert.IsTrue(Math.Abs(stamp - raw) < Period, $"expected to re-anchor near the real clock, was {Ms(stamp - raw)}ms away");
	}

	private static long[] Deltas(long[] values)
		=> Enumerable.Range(1, values.Length - 1).Select(i => values[i] - values[i - 1]).ToArray();

	private static double Ms(long ticks) => ticks / (double)TimeSpan.TicksPerMillisecond;
}
#endif
