using Microsoft.UI.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.Windows_UI_Input;

[TestClass]
public class Given_ScrollVelocityTracker
{
	private const double Tolerance = 1e-6;

	[TestMethod]
	public void When_LessThanMinSamples_Then_NoVelocity()
	{
		ScrollVelocityTracker tracker = new();
		tracker.AddPosition(0, new Point(0, 0));
		tracker.AddPosition(8, new Point(0, 16));

		Assert.IsNull(tracker.GetVelocity());
	}

	[TestMethod]
	public void When_LinearMotion_Then_VelocityIsTheSlope()
	{
		ScrollVelocityTracker tracker = new();
		for (var t = 0; t <= 80; t += 8)
		{
			tracker.AddPosition(t, new Point(-1.5 * t, 2 * t));
		}

		var velocity = GetVelocity(tracker);

		Assert.AreEqual(-1.5, velocity.X, Tolerance);
		Assert.AreEqual(2, velocity.Y, Tolerance);
	}

	[TestMethod]
	public void When_AcceleratingMotion_Then_VelocityIsTheInstantaneousOneAtTheNewestSample()
	{
		ScrollVelocityTracker tracker = new();
		for (var t = 0; t <= 80; t += 8)
		{
			// y = t + 0.01t², so dy/dt = 1 + 0.02t
			tracker.AddPosition(t, new Point(0, t + 0.01 * t * t));
		}

		var velocity = GetVelocity(tracker);

		Assert.AreEqual(0, velocity.X, Tolerance);
		Assert.AreEqual(1 + 0.02 * 80, velocity.Y, Tolerance);
	}

	[TestMethod]
	public void When_GapLongerThanAssumeStopped_Then_OlderSamplesAreIgnored()
	{
		ScrollVelocityTracker tracker = new();

		// A fast motion, then the finger rests for 50ms, then a slow one.
		for (var t = 0; t <= 40; t += 8)
		{
			tracker.AddPosition(t, new Point(0, 10 * t));
		}

		for (var i = 0; i <= 4; i++)
		{
			tracker.AddPosition(90 + i * 8d, new Point(0, 400 + i * 8d));
		}

		var velocity = GetVelocity(tracker);

		Assert.AreEqual(1, velocity.Y, Tolerance);
	}

	[TestMethod]
	public void When_PointerRestsBeforeTheNewestSample_Then_VelocityIsZero()
	{
		ScrollVelocityTracker tracker = new();

		// A fast motion, then a move 100ms later where the finger had stopped: no move is sent while still.
		for (var t = 0; t <= 40; t += 8)
		{
			tracker.AddPosition(t, new Point(0, 10 * t));
		}

		tracker.AddPosition(140, new Point(0, 400));

		var velocity = GetVelocity(tracker);

		Assert.AreEqual(0, velocity.X, Tolerance);
		Assert.AreEqual(0, velocity.Y, Tolerance);
	}

	[TestMethod]
	[DataRow(2d)]
	[DataRow(6d)]
	[DataRow(12d)]
	public void When_ReleaseShortlyAfterTheLastMove_Then_VelocityIsTheMotionSlope(double releaseDelayMs)
	{
		ScrollVelocityTracker tracker = new();
		for (var t = 0; t <= 32; t += 8)
		{
			tracker.AddPosition(t, new Point(0, 100 - 2 * t));
		}

		var velocity = tracker.GetVelocity(releaseTimeMs: 32 + releaseDelayMs) ?? throw new AssertFailedException("Expected a velocity.");

		Assert.AreEqual(-2, velocity.Y, Tolerance);
	}

	[TestMethod]
	public void When_ShortFlickReleasedShortlyAfterTheLastMove_Then_VelocityKeepsItsDirection()
	{
		ScrollVelocityTracker tracker = new();
		tracker.AddPosition(0, new Point(0, 100));
		tracker.AddPosition(8, new Point(0, 84));
		tracker.AddPosition(16, new Point(0, 64));

		var velocity = tracker.GetVelocity(releaseTimeMs: 26) ?? throw new AssertFailedException("Expected a velocity.");

		Assert.IsLessThan(0, velocity.Y);
	}

	[TestMethod]
	public void When_ReleaseLongAfterTheLastMove_Then_VelocityIsZero()
	{
		ScrollVelocityTracker tracker = new();
		for (var t = 0; t <= 40; t += 8)
		{
			tracker.AddPosition(t, new Point(0, 10 * t));
		}

		var velocity = tracker.GetVelocity(releaseTimeMs: 100) ?? throw new AssertFailedException("Expected a velocity.");

		Assert.AreEqual(0, velocity.X, Tolerance);
		Assert.AreEqual(0, velocity.Y, Tolerance);
	}

	[TestMethod]
	public void When_SamplesOlderThanTheHorizon_Then_TheyAreIgnored()
	{
		ScrollVelocityTracker tracker = new();

		for (var t = 0; t <= 160; t += 16)
		{
			var y = t < 48 ? 10 * t : 480 + (t - 48);
			tracker.AddPosition(t, new Point(0, y));
		}

		var velocity = GetVelocity(tracker);

		Assert.AreEqual(1, velocity.Y, Tolerance);
	}

	[TestMethod]
	public void When_DuplicateTimestamps_Then_NoVelocity()
	{
		ScrollVelocityTracker tracker = new();
		for (var i = 0; i < 5; i++)
		{
			tracker.AddPosition(10, new Point(0, i * 5));
		}

		Assert.IsNull(tracker.GetVelocity());
	}

	[TestMethod]
	public void When_Reset_Then_NoVelocity()
	{
		ScrollVelocityTracker tracker = new();
		for (var t = 0; t <= 80; t += 8)
		{
			tracker.AddPosition(t, new Point(0, 2 * t));
		}

		tracker.Reset();

		Assert.IsNull(tracker.GetVelocity());
	}

	private static Point GetVelocity(ScrollVelocityTracker tracker)
		=> tracker.GetVelocity() ?? throw new AssertFailedException("Expected a velocity.");
}
