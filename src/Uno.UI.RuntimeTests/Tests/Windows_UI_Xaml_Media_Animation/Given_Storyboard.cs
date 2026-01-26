using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_Storyboard
{
	[TestMethod]
	public async Task When_Empty_Storyboard_Completes()
	{
		var storyboard = new Storyboard();
		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed);
	}

	[TestMethod]
	public async Task When_Empty_Storyboard_Completed_Attached_After()
	{
		var storyboard = new Storyboard();
		bool completed = false;
		storyboard.Begin();
		storyboard.Completed += (s, e) => completed = true;
		await TestServices.WindowHelper.WaitFor(() => completed);
	}

	[TestMethod]
	public async Task When_Empty_Storyboard_Stopped()
	{
		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => Assert.Fail("Storyboard should not trigger Completed");
		storyboard.Begin();
		storyboard.Stop();
		Assert.AreEqual(ClockState.Stopped, storyboard.GetCurrentState());
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task When_Empty_Storyboard_Paused()
	{
		var storyboard = new Storyboard();
		var completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();
		storyboard.Pause();
		Assert.AreEqual(ClockState.Active, storyboard.GetCurrentState());
		await TestServices.WindowHelper.WaitFor(() => completed);
	}

	[TestMethod]
	public async Task When_Storyboard_Duration_Longer_Than_Child()
	{
		// Storyboard Duration=2s, child Duration=Automatic (1s default).
		// Completed should fire at ~2s because the Storyboard waits for its Duration to elapse.
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			// Duration is Automatic (default) → resolves to 1s independently, NOT inherited
		};
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, "Opacity");

		var storyboard = new Storyboard
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(2000))
		};
		storyboard.Children.Add(animation);

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;

		var stopwatch = Stopwatch.StartNew();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed, timeout: 5000);
		stopwatch.Stop();

		// Completed should fire at ~2s (Storyboard's Duration), not ~1s (child's default).
		Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 1500,
			$"Storyboard completed too early at {stopwatch.ElapsedMilliseconds}ms, expected ~2000ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000,
			$"Storyboard completed too late at {stopwatch.ElapsedMilliseconds}ms, expected ~2000ms");
	}

	[TestMethod]
	public async Task When_Storyboard_Duration_Shorter_Than_Child()
	{
		// Storyboard Duration=500ms, child Duration=Automatic (1s default).
		// Completed should fire at ~500ms; child is clipped (opacity frozen mid-animation).
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			// Duration is Automatic (default) → 1s, but Storyboard will clip at 500ms
		};
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, "Opacity");

		var storyboard = new Storyboard
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(500))
		};
		storyboard.Children.Add(animation);

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;

		var stopwatch = Stopwatch.StartNew();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed, timeout: 2000);
		stopwatch.Stop();

		// Completed should fire at ~500ms (Storyboard's Duration), not ~1s (child default).
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 800,
			$"Storyboard completed too late at {stopwatch.ElapsedMilliseconds}ms, expected ~500ms");

		// Opacity should be approximately mid-way (~0.5), NOT 0.0 (the child's To value).
		// The child was clipped/frozen at its current progress, not jumped to end.
		Assert.IsTrue(ellipse.Opacity > 0.2,
			$"Opacity was {ellipse.Opacity}, expected ~0.5 (clipped mid-animation, not jumped to end)");
	}

	[TestMethod]
	public async Task When_Storyboard_Duration_With_RepeatBehavior()
	{
		// Storyboard Duration=500ms, RepeatBehavior=2x.
		// Should take ~1000ms total (two 500ms cycles).
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			// Duration is Automatic → 1s default, but Storyboard Duration clips at 500ms per cycle
		};
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, "Opacity");

		var storyboard = new Storyboard
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
			RepeatBehavior = new RepeatBehavior(2)
		};
		storyboard.Children.Add(animation);

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;

		var stopwatch = Stopwatch.StartNew();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed, timeout: 5000);
		stopwatch.Stop();

		// Two 500ms cycles → ~1000ms total.
		Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 700,
			$"Storyboard completed too early at {stopwatch.ElapsedMilliseconds}ms, expected ~1000ms (2x500ms)");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000,
			$"Storyboard completed too late at {stopwatch.ElapsedMilliseconds}ms, expected ~1000ms (2x500ms)");
	}

	[TestMethod]
	public async Task When_Neither_Has_Duration_Should_Default_1_Second()
	{
		// When neither Storyboard nor child has explicit Duration, child defaults to 1 second.
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			// Duration is Automatic (default) → 1s
		};
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, "Opacity");

		var storyboard = new Storyboard();
		// Duration is Automatic (default) → no expiration boundary
		storyboard.Children.Add(animation);

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;

		var stopwatch = Stopwatch.StartNew();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed, timeout: 2000);
		stopwatch.Stop();

		// The animation should complete in approximately 1000ms (the default).
		Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 800 && stopwatch.ElapsedMilliseconds < 1500,
			$"Animation took {stopwatch.ElapsedMilliseconds}ms, expected ~1000ms (default)");
	}

	[TestMethod]
	public async Task When_Child_Has_Explicit_Duration_No_Storyboard_Duration()
	{
		// Storyboard Duration=Automatic, child Duration=500ms.
		// Completed should fire at ~500ms (child's explicit duration). Regression check.
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			Duration = new Duration(TimeSpan.FromMilliseconds(500))
		};
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, "Opacity");

		var storyboard = new Storyboard();
		// Duration is Automatic (default)
		storyboard.Children.Add(animation);

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;

		var stopwatch = Stopwatch.StartNew();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed, timeout: 2000);
		stopwatch.Stop();

		// Completed at ~500ms (child's duration), not 1s (default).
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 800,
			$"Animation took {stopwatch.ElapsedMilliseconds}ms, expected ~500ms (child's explicit Duration)");
	}
}
