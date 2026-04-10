using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Extensions;

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
	public async Task When_Restarted_After_Natural_Completion_Fires_Completed_Again()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		var animation = new DoubleAnimation
		{
			To = 100,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		};
		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(translate.Y));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		int run1Completed = 0;
		int run2Completed = 0;
		storyboard.Completed += (s, e) => run1Completed = 1;

		// First run
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => run1Completed > 0, timeoutMS: 3000);

		Assert.AreEqual(1, run1Completed, "First run should fire Completed once");
		Assert.AreEqual(100.0, translate.Y, 1.0, "Fill value should be 100 after first run");

		// Second run without explicit Stop - should also complete
		storyboard.Completed += (s, e) => run2Completed = 1;
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => run2Completed > 0, timeoutMS: 3000);

		Assert.AreEqual(1, run2Completed, "Second run should fire Completed again");
		Assert.AreEqual(100.0, translate.Y, 1.0, "Fill value should be 100 after second run");
	}

	[TestMethod]
	public async Task When_Pause_Resume_Preserves_Progress()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		var animation = new DoubleAnimation
		{
			From = 0,
			To = 100,
			Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
		};
		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(translate.Y));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();
		await Task.Delay(300); // 300ms into 1000ms = ~30% done = ~30

		storyboard.Pause();
		var pausedValue = translate.Y;
		await Task.Delay(200); // Wait while paused - value should not change

		var afterPauseValue = translate.Y;
		Assert.AreEqual(pausedValue, afterPauseValue, 2.0, "Value should not change while paused");

		storyboard.Resume();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(2));

		// After animation completes, fill value should be 100
		Assert.AreEqual(100.0, translate.Y, 2.0, "Fill value should be 100 after resume+complete");
	}

	[TestMethod]
	public async Task When_Seek_Applies_Value_At_Offset()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		var animation = new DoubleAnimation
		{
			From = 0,
			To = 100,
			Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
		};
		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(translate.Y));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();
		await TestServices.WindowHelper.WaitForIdle();

		// Seek to 50% of the animation (500ms out of 1000ms)
		storyboard.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(500));

		// Value should be approximately 50 (50% of 0-100)
		Assert.AreEqual(50.0, translate.Y, 5.0, $"After seek to 50%, value should be ~50, was {translate.Y}");

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Storyboard_RepeatBehavior_Count_Loops_Children()
	{
		// Verify that RepeatBehavior.Count on the STORYBOARD (not on the animation)
		// causes children to repeat. Children should see time wrapped back to 0
		// at each iteration boundary.
		var translate = new TranslateTransform();
		var border = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		var animation = new DoubleAnimation
		{
			From = 0,
			To = 100,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
		};
		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(translate.Y));

		var storyboard = new Storyboard
		{
			RepeatBehavior = new RepeatBehavior(2),
		};
		storyboard.Children.Add(animation);

		// Total duration should be 2 * 200ms = 400ms
		var sw = System.Diagnostics.Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));
		sw.Stop();

		Assert.IsTrue(sw.ElapsedMilliseconds >= 350,
			$"2 repetitions of 200ms each should take ~400ms, took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(100.0, translate.Y, 2.0,
			$"After 2 repetitions, fill value should be 100, was {translate.Y}");
	}

	[TestMethod]
	public async Task When_Storyboard_AutoReverse_Doubles_Duration()
	{
		// Verify that AutoReverse on the STORYBOARD causes the total duration
		// to be doubled (forward + backward).
		var translate = new TranslateTransform();
		var border = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		var animation = new DoubleAnimation
		{
			From = 0,
			To = 100,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		};
		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(translate.Y));

		var storyboard = new Storyboard
		{
			AutoReverse = true,
		};
		storyboard.Children.Add(animation);

		// Total duration: 300ms forward + 300ms backward = 600ms
		var sw = System.Diagnostics.Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));
		sw.Stop();

		Assert.IsTrue(sw.ElapsedMilliseconds >= 500,
			$"AutoReverse storyboard should take ~600ms, took {sw.ElapsedMilliseconds}ms");
	}
}
