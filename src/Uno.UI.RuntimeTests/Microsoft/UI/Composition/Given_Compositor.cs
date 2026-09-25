#if __SKIA__
using System;
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
	/// The pause between two motions is not a frame interval. Short bursts separated by pauses would
	/// otherwise fill the window with those pauses, and the same interval back-dates a fling's launch —
	/// so a skewed one starts the curve part-way down its travel.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Motion_Comes_In_Short_Bursts_Then_Frame_Interval_Is_Not_Skewed()
	{
		var clock = new Uno.UI.Composition.FrameClock();

		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < 40; i++)
		{
			raw += Period;
			clock.NextTimestamp(raw);
		}

		for (var burst = 0; burst < 40; burst++)
		{
			for (var i = 0; i < 2; i++)
			{
				raw += Period;
				clock.NextTimestamp(raw);
			}

			raw += TimeSpan.TicksPerMillisecond * 500;
			clock.NextTimestamp(raw);
		}

		Assert.AreEqual(Period, clock.IntervalInTicks, $"pauses skewed the interval to {Ms(clock.IntervalInTicks)}ms");
	}

	private static double Ms(long ticks) => ticks / (double)TimeSpan.TicksPerMillisecond;
}
#endif
