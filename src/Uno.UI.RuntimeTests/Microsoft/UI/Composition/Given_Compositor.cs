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

	/// <summary>
	/// While nothing animates, frames only come when something changes, so the gaps between them are all there is
	/// to sample. They must not become the interval: a motion starting from rest back-dates its first frame by it,
	/// and a 350ms back-date plays most of a wheel notch in a single frame.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Frames_Are_Sparse_Then_Frame_Interval_Is_Not_Skewed()
	{
		var clock = new Uno.UI.Composition.FrameClock();

		var raw = TimeSpan.TicksPerSecond;
		for (var i = 0; i < 20; i++)
		{
			raw += 350 * TimeSpan.TicksPerMillisecond;
			clock.NextTimestamp(raw);
		}

		Assert.AreEqual(TimeSpan.TicksPerSecond / 60, clock.IntervalInTicks, $"sparse frames skewed the interval to {Ms(clock.IntervalInTicks)}ms");

		for (var i = 0; i < 10; i++)
		{
			raw += Period;
			clock.NextTimestamp(raw);
		}

		Assert.AreEqual(Period, clock.IntervalInTicks, "the interval should come from the first continuous frames");
	}

	/// <summary>
	/// A record evaluates its animations against the frame's timestamp, not the instant the record happened to
	/// run at, so every animation in the frame moves on the same even grid as the frame drivers.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Recording_Then_KeyFrame_Animation_Evaluates_At_Frame_Time()
	{
		var compositor = Compositor.GetSharedCompositor();
		var (animation, start) = StartSecondsAnimation(compositor);
		try
		{
			var firstFrame = start + TimeSpan.TicksPerSecond;
			compositor.FrameTimestampInTicks = firstFrame;
			animation.Evaluate();

			compositor.FrameTimestampInTicks = firstFrame + 250 * TimeSpan.TicksPerSecond;
			Assert.AreEqual(250f, (float)animation.Evaluate(), 0.01f, "the animation should be evaluated at the frame's timestamp");
		}
		finally
		{
			compositor.FrameTimestampInTicks = null;
			animation.Stop();
		}
	}

	/// <summary>
	/// Like a composition commit, an animation starts at the first frame that shows it: one started between frames
	/// must not show the time since it was requested in that frame, which for an ease-out is a large first step.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Started_Between_Frames_Then_First_Frame_Shows_Its_Start()
	{
		var compositor = Compositor.GetSharedCompositor();
		var (animation, start) = StartSecondsAnimation(compositor);
		try
		{
			compositor.FrameTimestampInTicks = start + 30 * TimeSpan.TicksPerMillisecond;
			Assert.AreEqual(0f, (float)animation.Evaluate(), 0.0001f, "the first frame should show the animation's start");

			compositor.FrameTimestampInTicks = start + 40 * TimeSpan.TicksPerMillisecond;
			Assert.AreEqual(0.01f, (float)animation.Evaluate(), 0.0001f, "the next frame should have moved by one frame's worth");
		}
		finally
		{
			compositor.FrameTimestampInTicks = null;
			animation.Stop();
		}
	}

	/// <summary>
	/// The real clock and the frame clock take turns, and the frame clock may run ahead of the real one: the
	/// playhead must never move backwards when the next reading is behind the previous one.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public void When_Frame_Time_Is_Ahead_Of_Real_Clock_Then_KeyFrame_Animation_Never_Steps_Back()
	{
		var compositor = Compositor.GetSharedCompositor();
		var (animation, start) = StartSecondsAnimation(compositor);
		try
		{
			compositor.FrameTimestampInTicks = start;
			animation.Evaluate();

			compositor.FrameTimestampInTicks = start + 250 * TimeSpan.TicksPerSecond;
			animation.Evaluate();

			compositor.FrameTimestampInTicks = start + 100 * TimeSpan.TicksPerSecond;
			Assert.AreEqual(250f, (float)animation.Evaluate(), 0.01f, "an earlier frame timestamp must not rewind the animation");

			compositor.FrameTimestampInTicks = null;
			Assert.AreEqual(250f, (float)animation.Evaluate(), 0.01f, "the real clock, behind the frame one, must not rewind the animation");

			compositor.FrameTimestampInTicks = start + 260 * TimeSpan.TicksPerSecond;
			Assert.AreEqual(260f, (float)animation.Evaluate(), 0.01f, "the animation should resume from where it was");
		}
		finally
		{
			compositor.FrameTimestampInTicks = null;
			animation.Stop();
		}
	}

	// Linear over 1000s, so the value reads as the number of seconds elapsed.
	private static (ScalarKeyFrameAnimation Animation, long Start) StartSecondsAnimation(Compositor compositor)
	{
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(0f, 0f, compositor.CreateLinearEasingFunction());
		animation.InsertKeyFrame(1f, 1000f, compositor.CreateLinearEasingFunction());
		animation.Duration = TimeSpan.FromSeconds(1000);

		var properties = compositor.CreatePropertySet();
		properties.InsertScalar("Value", 0f);

		var start = compositor.TimestampInTicks;
		properties.StartAnimation("Value", animation);

		return (animation, start);
	}

	private static double Ms(long ticks) => ticks / (double)TimeSpan.TicksPerMillisecond;
}
#endif
