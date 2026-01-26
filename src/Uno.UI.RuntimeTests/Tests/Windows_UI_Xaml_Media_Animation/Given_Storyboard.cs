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
	public async Task When_Storyboard_Has_Duration_Child_Should_Inherit()
	{
		// Issue #3434: When Duration is set on Storyboard, child animations with Duration.Automatic
		// should inherit the parent's Duration.
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			// Duration is Automatic (default) - should inherit from Storyboard
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

		// The animation should complete in approximately 500ms (Storyboard's Duration),
		// not 1000ms (the default). Allow some tolerance for timing variations.
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 800,
			$"Animation took {stopwatch.ElapsedMilliseconds}ms, expected ~500ms (inherited from Storyboard)");
	}

	[TestMethod]
	public async Task When_Child_Has_Own_Duration_Should_Not_Inherit()
	{
		// When child animation has its own explicit Duration, it should not inherit from Storyboard.
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)) // Explicit duration
		};
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, "Opacity");

		var storyboard = new Storyboard
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(1000)) // Longer parent duration
		};
		storyboard.Children.Add(animation);

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;

		var stopwatch = Stopwatch.StartNew();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed, timeout: 2000);
		stopwatch.Stop();

		// The animation should complete in approximately 300ms (child's explicit Duration),
		// not 1000ms (the Storyboard's Duration). Allow some tolerance.
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 600,
			$"Animation took {stopwatch.ElapsedMilliseconds}ms, expected ~300ms (child's explicit Duration)");
	}

	[TestMethod]
	public async Task When_Neither_Has_Duration_Should_Default_1_Second()
	{
		// When neither Storyboard nor child has explicit Duration, should default to 1 second.
		var ellipse = new Ellipse { Opacity = 1.0, Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = ellipse;
		await TestServices.WindowHelper.WaitForLoaded(ellipse);

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			// Duration is Automatic (default)
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

		// The animation should complete in approximately 1000ms (the default).
		// Allow some tolerance for timing variations.
		Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 800 && stopwatch.ElapsedMilliseconds < 1500,
			$"Animation took {stopwatch.ElapsedMilliseconds}ms, expected ~1000ms (default)");
	}
}
