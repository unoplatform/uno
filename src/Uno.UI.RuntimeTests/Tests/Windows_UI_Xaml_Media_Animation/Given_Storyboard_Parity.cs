using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

/// <summary>
/// Parity tests ported from WinUI's StoryboardTests.cpp
/// (dxaml/test/native/external/foundation/graphics/animation/StoryboardTests.cpp).
/// Each test method documents the original WinUI test name it was ported from.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_Storyboard_Parity
{
	/// <summary>
	/// WinUI: StoryboardTests::ComplexStoryboardsWUC — "AutoReverse + SpeedRatio" section.
	/// SpeedRatio=2 on a 1s animation should complete in approximately 0.5s.
	/// </summary>
	[TestMethod]
	public async Task When_SpeedRatio_2x_Halves_Duration()
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
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();
		storyboard.SpeedRatio = 2;

		var sw = Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		// With SpeedRatio=2, a 1s animation should complete in ~500ms.
		// Allow generous tolerance: must be under 900ms (50% margin above 500ms is 750ms, we go wider).
		Assert.IsTrue(sw.ElapsedMilliseconds < 900,
			$"SpeedRatio=2 on 1s animation should complete well under 1s, took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(100.0, translate.X, 2.0,
			$"Fill value should be 100 after completion, was {translate.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::ComplexStoryboardsWUC — SpeedRatio variant.
	/// SpeedRatio=0.5 on a 500ms animation should complete in approximately 1s.
	/// </summary>
	[TestMethod]
	public async Task When_SpeedRatio_Half_Doubles_Duration()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();
		storyboard.SpeedRatio = 0.5;

		var sw = Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		// With SpeedRatio=0.5, a 500ms animation should take ~1000ms.
		// Must be at least 700ms (generous lower bound).
		Assert.IsTrue(sw.ElapsedMilliseconds >= 700,
			$"SpeedRatio=0.5 on 500ms animation should take ~1000ms, took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(100.0, translate.X, 2.0,
			$"Fill value should be 100 after completion, was {translate.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::ComplexStoryboardsWUC — "BeginTime" section.
	/// Storyboard.BeginTime delays all children by the specified amount.
	/// </summary>
	[TestMethod]
	public async Task When_Storyboard_BeginTime_Delays_All_Children()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();
		storyboard.BeginTime = TimeSpan.FromMilliseconds(500);

		var sw = Stopwatch.StartNew();
		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 5000);
		sw.Stop();

		// Total time: 500ms delay + 300ms animation = 800ms.
		// Allow generous tolerance: at least 550ms.
		Assert.IsTrue(sw.ElapsedMilliseconds >= 550,
			$"BeginTime=500ms + 300ms animation should take ~800ms, took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(100.0, translate.X, 2.0,
			$"Fill value should be 100 after delayed animation completes, was {translate.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::ComplexStoryboardsWUC — "DoubleAnimation BeginTime" section.
	/// An animation with BeginTime within a storyboard starts after its own delay.
	/// </summary>
	[TestMethod]
	public async Task When_Animation_BeginTime_Within_Storyboard()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			BeginTime = TimeSpan.FromMilliseconds(300),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		var sw = Stopwatch.StartNew();
		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 5000);
		sw.Stop();

		// Total: 300ms begin time + 300ms animation = 600ms.
		// Must take at least 400ms.
		Assert.IsTrue(sw.ElapsedMilliseconds >= 400,
			$"Animation BeginTime=300ms + 300ms duration should take ~600ms, took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(100.0, translate.X, 2.0,
			$"Fill value should be 100 after delayed animation, was {translate.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::ComplexStoryboardsWUC — "Multiple Animations" section.
	/// Two animations with different durations; storyboard completes when the longest finishes.
	/// </summary>
	[TestMethod]
	public async Task When_Multiple_Children_Different_Durations()
	{
		var translate1 = new TranslateTransform();
		var translate2 = new TranslateTransform();
		var border1 = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate1,
		};
		var border2 = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate2,
		};
		var panel = new StackPanel();
		panel.Children.Add(border1);
		panel.Children.Add(border2);
		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var animation1 = new DoubleAnimation
		{
			From = 0,
			To = 50,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		};
		Storyboard.SetTarget(animation1, translate1);
		Storyboard.SetTargetProperty(animation1, nameof(translate1.X));

		var animation2 = new DoubleAnimation
		{
			From = 0,
			To = 200,
			Duration = new Duration(TimeSpan.FromMilliseconds(600)),
		};
		Storyboard.SetTarget(animation2, translate2);
		Storyboard.SetTargetProperty(animation2, nameof(translate2.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation1);
		storyboard.Children.Add(animation2);

		var sw = Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		// The storyboard completes when the longest animation (600ms) finishes.
		// Must take at least 400ms (generous lower bound for 600ms).
		Assert.IsTrue(sw.ElapsedMilliseconds >= 400,
			$"Storyboard with 300ms and 600ms animations should take ~600ms, took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(50.0, translate1.X, 2.0,
			$"Short animation fill value should be 50, was {translate1.X}");
		Assert.AreEqual(200.0, translate2.X, 2.0,
			$"Long animation fill value should be 200, was {translate2.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::ComplexStoryboardsWUC — "RepeatForever" section.
	/// RepeatBehavior.Forever never fires the Completed event (verified after timeout).
	/// </summary>
	[TestMethod]
	public async Task When_RepeatForever_NeverFiresCompleted()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();
		storyboard.RepeatBehavior = RepeatBehavior.Forever;

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		// Wait 1s — Completed should never fire for RepeatBehavior.Forever.
		await Task.Delay(1000);

		Assert.IsFalse(completed,
			"RepeatBehavior.Forever should never fire Completed");

		storyboard.Stop();
	}

	/// <summary>
	/// WinUI: StoryboardTests::RestartStoryboardWUC.
	/// Stop then Begin again fires Completed a second time.
	/// </summary>
	[TestMethod]
	public async Task When_Restart_After_Completion()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		// First run
		int completedCount = 0;
		storyboard.Completed += (s, e) => completedCount++;
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completedCount >= 1, timeoutMS: 3000);

		Assert.AreEqual(1, completedCount, "First run should fire Completed once");

		// Stop and restart
		storyboard.Stop();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completedCount >= 2, timeoutMS: 3000);

		Assert.AreEqual(2, completedCount, "Second run should fire Completed again");
	}

	/// <summary>
	/// WinUI: StoryboardTests::SuspendResumeWUC.
	/// Pause mid-animation, wait, then resume. The total wall time should
	/// extend because the animation clock is frozen while paused.
	/// </summary>
	[TestMethod]
	public async Task When_Pause_Resume_Extends_WallTime()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		var sw = Stopwatch.StartNew();
		storyboard.Begin();

		// Let it run for ~150ms, then pause.
		await Task.Delay(150);
		storyboard.Pause();

		var pausedValue = translate.X;

		// Stay paused for 500ms — value should not change.
		await Task.Delay(500);
		var afterPauseValue = translate.X;
		Assert.AreEqual(pausedValue, afterPauseValue, 2.0,
			"Value should not change while paused");

		// Resume
		storyboard.Resume();
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 5000);
		sw.Stop();

		// Total wall time should be >= 500ms (animation) + 500ms (pause) = ~1000ms.
		// Use generous lower bound of 800ms.
		Assert.IsTrue(sw.ElapsedMilliseconds >= 800,
			$"Pause should extend wall time. Animation=500ms + pause=500ms, total took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(100.0, translate.X, 2.0,
			$"Fill value should be 100 after resume+complete, was {translate.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::CompletedEventWUC_ZeroDuration.
	/// A 0-duration animation completes immediately.
	/// </summary>
	[TestMethod]
	public async Task When_ZeroDuration_CompletesImmediately()
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
			Duration = new Duration(TimeSpan.Zero),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		bool sbCompleted = false;
		storyboard.Completed += (s, e) => sbCompleted = true;

		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => sbCompleted, timeoutMS: 3000);

		Assert.IsTrue(sbCompleted,
			"Zero-duration storyboard should fire Completed");
	}

	/// <summary>
	/// WinUI: StoryboardTests::CompletedEventWUC_BlankStoryboard.
	/// An empty storyboard (no children) completes immediately.
	/// </summary>
	[TestMethod]
	public async Task When_Empty_Storyboard_CompletesImmediately()
	{
		var storyboard = new Storyboard();
		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 3000);
		Assert.IsTrue(completed, "Empty storyboard should fire Completed");
	}

	/// <summary>
	/// WinUI: StoryboardTests::CompletedEventWUC_ClippedAnimation.
	/// A 600ms animation clipped by a 300ms storyboard duration completes at 300ms.
	/// </summary>
	[TestMethod]
	public async Task When_Animation_Clipped_By_Storyboard_Duration()
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

		// Animation is 600ms, but storyboard duration clips it to 300ms.
		var animation = new DoubleAnimation
		{
			From = 0,
			To = 100,
			Duration = new Duration(TimeSpan.FromMilliseconds(600)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();
		storyboard.Duration = new Duration(TimeSpan.FromMilliseconds(300));

		bool sbCompleted = false;
		storyboard.Completed += (s, e) => sbCompleted = true;

		var sw = Stopwatch.StartNew();
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => sbCompleted, timeoutMS: 5000);
		sw.Stop();

		Assert.IsTrue(sbCompleted,
			"Clipped storyboard should fire Completed");
		// The storyboard should complete around 300ms, not 600ms.
		Assert.IsTrue(sw.ElapsedMilliseconds < 550,
			$"Storyboard with Duration=300ms should not wait for 600ms animation, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// WinUI: StoryboardTests::CompletedEvent_TargetNotRenderedWUCFull.
	/// A storyboard targeting a collapsed/invisible element still fires Completed.
	/// </summary>
	[TestMethod]
	public async Task When_Collapsed_Target_Still_Fires_Completed()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Width = 50,
			Height = 50,
			RenderTransform = translate,
			Visibility = Visibility.Collapsed,
		};
		TestServices.WindowHelper.WindowContent = border;
		// WaitForLoaded may not fire for collapsed elements, so just wait for idle.
		await TestServices.WindowHelper.WaitForIdle();

		var animation = new DoubleAnimation
		{
			From = 0,
			To = 100,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		bool sbCompleted = false;
		storyboard.Completed += (s, e) => sbCompleted = true;

		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => sbCompleted, timeoutMS: 5000);

		Assert.IsTrue(sbCompleted,
			"Storyboard targeting collapsed element should still fire Completed");
	}

	/// <summary>
	/// Parity test: stopping a paused storyboard should clear the animated value.
	/// </summary>
	[TestMethod]
	public async Task When_Stop_While_Paused_Clears_Value()
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
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		storyboard.Begin();
		await Task.Delay(300);

		storyboard.Pause();
		var pausedValue = translate.X;
		Assert.IsTrue(pausedValue > 5,
			$"After 300ms of a 1s 0->100 animation, value should be > 5, was {pausedValue}");

		// Stop while paused — value should return to base (0 for TranslateTransform default).
		storyboard.Stop();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.0, translate.X, 2.0,
			$"After Stop, animated value should return to base (0), was {translate.X}");
	}

	/// <summary>
	/// Parity test: two storyboards targeting the same property — the second one wins.
	/// </summary>
	[TestMethod]
	public async Task When_Multiple_Storyboards_Same_Property_LastWins()
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

		// First storyboard: animate X to 50.
		var animation1 = new DoubleAnimation
		{
			From = 0,
			To = 50,
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
		}.BindTo(translate, nameof(translate.X));
		var storyboard1 = animation1.ToStoryboard();
		await storyboard1.RunAsync(timeout: TimeSpan.FromSeconds(3));

		// Verify first storyboard filled to 50.
		Assert.AreEqual(50.0, translate.X, 2.0,
			$"After first storyboard fills, X should be 50, was {translate.X}");

		// Second storyboard: animate X to 200. This should override.
		var animation2 = new DoubleAnimation
		{
			From = 0,
			To = 200,
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
		}.BindTo(translate, nameof(translate.X));
		var storyboard2 = animation2.ToStoryboard();
		await storyboard2.RunAsync(timeout: TimeSpan.FromSeconds(3));

		// Second storyboard wins.
		Assert.AreEqual(200.0, translate.X, 2.0,
			$"After second storyboard fills, X should be 200 (last wins), was {translate.X}");
	}

	/// <summary>
	/// Parity test: Seek while paused, then resume from the sought position.
	/// </summary>
	[TestMethod]
	public async Task When_Seek_During_Pause()
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
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();
		await TestServices.WindowHelper.WaitForIdle();

		// Pause immediately
		storyboard.Pause();
		await TestServices.WindowHelper.WaitForIdle();

		// Seek to 80% (800ms of 1000ms)
		storyboard.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(800));
		await TestServices.WindowHelper.WaitForIdle();

		var seekedValue = translate.X;
		Assert.IsTrue(seekedValue >= 70 && seekedValue <= 90,
			$"After seek to 80% of 0->100, value should be ~80, was {seekedValue}");

		// Resume — should complete quickly (only 200ms remaining).
		storyboard.Resume();
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 3000);

		Assert.IsTrue(completed,
			"Storyboard should complete after resuming from seek at 80%");
		Assert.AreEqual(100.0, translate.X, 2.0,
			$"Fill value should be 100 after resume from seek, was {translate.X}");
	}

	/// <summary>
	/// Parity test: SkipToFill immediately sets the end value and fires Completed.
	/// </summary>
	[TestMethod]
	public async Task When_SkipToFill_Applies_Final_Value()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(5000)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();
		await TestServices.WindowHelper.WaitForIdle();

		// SkipToFill — should jump to final value immediately.
		storyboard.SkipToFill();
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 3000);

		Assert.IsTrue(completed,
			"SkipToFill should fire Completed");
		Assert.AreEqual(100.0, translate.X, 2.0,
			$"SkipToFill should apply final value (100), was {translate.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::ComplexStoryboardsWUC — RepeatBehavior + AutoReverse combination.
	/// RepeatBehavior(2) + AutoReverse: each repetition is a full forward+reverse cycle.
	/// Total: 2 * (forward + reverse) = 4x the base duration.
	/// </summary>
	[TestMethod]
	public async Task When_RepeatBehavior_Count_With_AutoReverse()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();
		storyboard.RepeatBehavior = new RepeatBehavior(2);
		storyboard.AutoReverse = true;

		// Total: 2 reps * (200ms forward + 200ms reverse) = 800ms.
		var sw = Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.IsTrue(sw.ElapsedMilliseconds >= 500,
			$"2 reps with AutoReverse on 200ms should take ~800ms, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// Parity test: FillBehavior.Stop on the storyboard returns values to base after completion.
	/// </summary>
	[TestMethod]
	public async Task When_Storyboard_FillBehavior_Stop_Returns_To_Base()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.Stop,
		}.BindTo(translate, nameof(translate.X));

		var storyboard = animation.ToStoryboard();

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 5000);
		await TestServices.WindowHelper.WaitForIdle();

		// With FillBehavior.Stop, the value should return to the base value (0) after completion.
		Assert.AreEqual(0.0, translate.X, 2.0,
			$"FillBehavior.Stop should return value to base (0) after completion, was {translate.X}");
	}

	/// <summary>
	/// WinUI: StoryboardTests::CompletedEventWUC.
	/// Multiple animations with various configs (SpeedRatio, BeginTime, AutoReverse, RepeatBehavior)
	/// all fire Completed. This is a simplified port testing 5 representative configurations.
	/// </summary>
	[TestMethod]
	public async Task When_CompletedEvent_SpeedRatio_BeginTime_AutoReverse_RepeatCount()
	{
		// Config 1: Simple 300ms animation
		var t1 = new TranslateTransform();
		var b1 = new Border { Width = 10, Height = 10, RenderTransform = t1 };

		// Config 2: SpeedRatio=2 on animation (500ms at 2x = 250ms effective)
		var t2 = new TranslateTransform();
		var b2 = new Border { Width = 10, Height = 10, RenderTransform = t2 };

		// Config 3: BeginTime=200ms + 200ms animation
		var t3 = new TranslateTransform();
		var b3 = new Border { Width = 10, Height = 10, RenderTransform = t3 };

		// Config 4: AutoReverse (200ms forward + 200ms reverse = 400ms)
		var t4 = new TranslateTransform();
		var b4 = new Border { Width = 10, Height = 10, RenderTransform = t4 };

		// Config 5: RepeatBehavior(2) (200ms * 2 = 400ms)
		var t5 = new TranslateTransform();
		var b5 = new Border { Width = 10, Height = 10, RenderTransform = t5 };

		// Build the full tree upfront before setting as WindowContent
		var panel = new StackPanel();
		panel.Children.Add(b1);
		panel.Children.Add(b2);
		panel.Children.Add(b3);
		panel.Children.Add(b4);
		panel.Children.Add(b5);
		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var a1 = new DoubleAnimation
		{
			From = 0,
			To = 10,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		}.BindTo(t1, nameof(t1.X));
		var sb1 = a1.ToStoryboard();

		var a2 = new DoubleAnimation
		{
			From = 0,
			To = 10,
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
		}.BindTo(t2, nameof(t2.X));
		var sb2 = a2.ToStoryboard();
		sb2.SpeedRatio = 2;

		var a3 = new DoubleAnimation
		{
			From = 0,
			To = 10,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			BeginTime = TimeSpan.FromMilliseconds(200),
		}.BindTo(t3, nameof(t3.X));
		var sb3 = a3.ToStoryboard();

		var a4 = new DoubleAnimation
		{
			From = 0,
			To = 10,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
		}.BindTo(t4, nameof(t4.X));
		var sb4 = a4.ToStoryboard();
		sb4.AutoReverse = true;

		var a5 = new DoubleAnimation
		{
			From = 0,
			To = 10,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
		}.BindTo(t5, nameof(t5.X));
		var sb5 = a5.ToStoryboard();
		sb5.RepeatBehavior = new RepeatBehavior(2);

		// Track completion for all 5 storyboards.
		bool c1 = false, c2 = false, c3 = false, c4 = false, c5 = false;
		sb1.Completed += (s, e) => c1 = true;
		sb2.Completed += (s, e) => c2 = true;
		sb3.Completed += (s, e) => c3 = true;
		sb4.Completed += (s, e) => c4 = true;
		sb5.Completed += (s, e) => c5 = true;

		// Start all 5 at once.
		sb1.Begin();
		sb2.Begin();
		sb3.Begin();
		sb4.Begin();
		sb5.Begin();

		// Wait for all to complete. The slowest is Config 3/4/5 at ~400ms.
		await TestServices.WindowHelper.WaitFor(
			() => c1 && c2 && c3 && c4 && c5,
			timeoutMS: 10000);

		Assert.IsTrue(c1, "Config 1 (simple) should fire Completed");
		Assert.IsTrue(c2, "Config 2 (SpeedRatio=2) should fire Completed");
		Assert.IsTrue(c3, "Config 3 (BeginTime) should fire Completed");
		Assert.IsTrue(c4, "Config 4 (AutoReverse) should fire Completed");
		Assert.IsTrue(c5, "Config 5 (RepeatBehavior(2)) should fire Completed");
	}
}
