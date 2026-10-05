#if __SKIA__
using System.Diagnostics;
using Uno.UI.Composition;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_VsyncFrameScheduler
{
	// A 59.94Hz display: no timer at a round rate stays on its grid.
	private static readonly long Period = (long)(Stopwatch.Frequency / 59.94);
	private static readonly long Ms = Stopwatch.Frequency / 1000;
	private static readonly long Origin = 1000 * Stopwatch.Frequency;

	private static long VsyncAt(long now) => Origin + (now - Origin) / Period * Period;

	[TestMethod]
	public void When_Idle_Then_Frame_Starts_Now_On_The_Latest_Vsync()
	{
		var scheduler = new VsyncFrameScheduler(60);
		var now = Origin + 100 * Period + 3 * Ms;

		var (start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);

		Assert.AreEqual(now, start, "input after an idle period must not wait for a vsync");
		Assert.AreEqual(VsyncAt(now), vsync);
	}

	[TestMethod]
	public void When_Interval_Has_A_Frame_Then_Next_Frame_Waits_For_The_Next_Vsync()
	{
		var scheduler = new VsyncFrameScheduler(60);
		var now = Origin + 100 * Period + 3 * Ms;
		var (start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);
		scheduler.OnFrame(start, vsync, unchanged: false);

		now += 2 * Ms;
		(start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);

		Assert.AreEqual(VsyncAt(now) + Period, vsync);
		Assert.AreEqual(vsync, start);
	}

	[TestMethod]
	public void When_Too_Late_In_The_Interval_Then_Waits_For_The_Next_Vsync()
	{
		var scheduler = new VsyncFrameScheduler(60);
		var now = Origin + 100 * Period + Period - Ms;

		var (start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);

		Assert.AreEqual(VsyncAt(now) + Period, vsync, "a frame started this late would only queue ahead of the next one");
		Assert.AreEqual(vsync, start);
	}

	[TestMethod]
	public void When_Animating_Then_Every_Frame_Starts_On_The_Next_Vsync()
	{
		var scheduler = new VsyncFrameScheduler(60);
		var now = Origin + 100 * Period + 3 * Ms;
		long? previous = null;

		for (var i = 0; i < 600; i++)
		{
			var (start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);
			if (previous is { } p)
			{
				Assert.AreEqual(p + Period, vsync, $"frame {i} skipped or repeated a vsync");
				Assert.AreEqual(vsync, start, $"frame {i} did not start on its vsync");
			}

			// The thread wakes a little late, draws, and the next request comes in a few ms into the interval.
			now = start + Ms / 2;
			scheduler.OnFrame(now, vsync, unchanged: false);
			scheduler.OnFrameDrawn(2 * Ms);
			now += 3 * Ms;
			previous = vsync;
		}
	}

	[TestMethod]
	public void When_Idle_Frame_Shows_Nothing_New_Then_The_Next_One_Still_Starts_Now()
	{
		var scheduler = new VsyncFrameScheduler(60);
		var now = Origin + 100 * Period + 3 * Ms;
		var (start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);
		scheduler.OnFrame(start, vsync, unchanged: true);

		now += Ms;
		(start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);

		Assert.AreEqual(now, start, "the frame with the recorded change must not wait for the next vsync");
		Assert.AreEqual(VsyncAt(now), vsync);
	}

	[TestMethod]
	public void When_Frames_Keep_Showing_Nothing_New_Then_They_Are_Still_Paced()
	{
		var scheduler = new VsyncFrameScheduler(60);
		var now = Origin + 100 * Period + 3 * Ms;

		for (var i = 0; i < 2; i++)
		{
			var (frameStart, frameVsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);
			scheduler.OnFrame(frameStart, frameVsync, unchanged: true);
			now += Ms;
		}

		var (start, vsync) = scheduler.GetNextFrame(now, VsyncAt(now), Period);

		Assert.AreEqual(VsyncAt(now) + Period, vsync, "frames that draw nothing must not spin");
		Assert.AreEqual(vsync, start);
	}

	[TestMethod]
	public void When_No_Vsync_Then_Paced_By_The_Frame_Rate()
	{
		var scheduler = new VsyncFrameScheduler(50);
		var interval = Stopwatch.Frequency / 50;
		var now = Origin;

		var (start, vsync) = scheduler.GetNextFrame(now, null, 0);
		Assert.AreEqual(now, start, "the first frame after idle starts right away");
		Assert.IsNull(vsync);
		scheduler.OnFrame(start, vsync, unchanged: false);

		now += 5 * Ms;
		(start, _) = scheduler.GetNextFrame(now, null, 0);
		Assert.AreEqual(Origin + interval, start);
		scheduler.OnFrame(start + Ms, null, unchanged: false);

		// Waking late doesn't push the next frame back.
		now = start + 3 * Ms;
		(start, _) = scheduler.GetNextFrame(now, null, 0);
		Assert.AreEqual(Origin + 2 * interval, start);
	}

	[TestMethod]
	public void When_Display_Rate_Changes_Then_Frames_Follow_The_New_Period()
	{
		var scheduler = new VsyncFrameScheduler(60);
		var now = Origin + 3 * Ms;
		var (start, vsync) = scheduler.GetNextFrame(now, Origin, Period);
		scheduler.OnFrame(start, vsync, unchanged: false);

		// The display switches to 120Hz on the next vsync.
		var fast = Period / 2;
		var grid = Origin + Period;
		now = grid + fast + Ms;
		(start, vsync) = scheduler.GetNextFrame(now, grid + fast, fast);

		Assert.AreEqual(grid + fast, vsync);
		Assert.AreEqual(now, start);
	}
}
#endif
