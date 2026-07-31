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

	/// <summary>
	/// The record loop can wake more than once inside a refresh interval. The grid must still only ever
	/// move forward: a curve reads a negative elapsed time as "not started" and snaps back to its origin.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Records_Bunch_Up_Then_Frame_Clock_Never_Steps_Back()
	{
		var compositor = new Compositor();

		var stamps = new long[80];
		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < stamps.Length; i++)
		{
			// A steady cadence, then a burst of records packed into a single interval.
			raw += i < 40 ? Period : Period / 10;
			stamps[i] = compositor.GetFrameTimestamp(raw);
		}

		var worst = Deltas(stamps).Min();
		Assert.IsTrue(worst >= 0, $"the frame clock stepped backwards by {Ms(-worst)}ms");
	}

	/// <summary>
	/// The pause between two motions is not a frame interval. Short bursts separated by pauses would
	/// otherwise fill the window with those pauses, and the same interval back-dates a fling's launch —
	/// so a skewed one starts the curve part-way down its travel.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Motion_Comes_In_Short_Bursts_Then_Frame_Interval_Is_Not_Skewed()
	{
		var compositor = new Compositor();

		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < 40; i++)
		{
			raw += Period;
			compositor.GetFrameTimestamp(raw);
		}

		for (var burst = 0; burst < 40; burst++)
		{
			for (var i = 0; i < 2; i++)
			{
				raw += Period;
				compositor.GetFrameTimestamp(raw);
			}

			raw += TimeSpan.TicksPerMillisecond * 500;
			compositor.GetFrameTimestamp(raw);
		}

		Assert.AreEqual(Period, compositor.FrameIntervalInTicks, $"pauses skewed the interval to {Ms(compositor.FrameIntervalInTicks)}ms");
	}

	private static long[] Deltas(long[] values)
		=> Enumerable.Range(1, values.Length - 1).Select(i => values[i] - values[i - 1]).ToArray();

	private static double Ms(long ticks) => ticks / (double)TimeSpan.TicksPerMillisecond;
}
#endif
