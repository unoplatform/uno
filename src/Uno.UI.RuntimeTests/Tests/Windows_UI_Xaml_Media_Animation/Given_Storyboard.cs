using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

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

	// Repro tests for https://github.com/unoplatform/uno/issues/3434
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3434")]
	public async Task When_Storyboard_Duration_Set_Animation_Runs()
	{
		// Issue: When Duration is set on the Storyboard (not on the DoubleAnimation child),
		// the animation breaks on Android/WASM/iOS. Duration on Storyboard should behave
		// the same as Duration on the individual animation.

		var target = new Microsoft.UI.Xaml.Controls.TextBlock { Text = "test", Opacity = 1.0 };
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForLoaded(target);
		await TestServices.WindowHelper.WaitForIdle();

		// Animation with Duration on the DoubleAnimation (reference — should always work)
		var animOnChild = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
		{
			From = 1.0,
			To = 0.5,
			Duration = TimeSpan.FromMilliseconds(300),
			FillBehavior = Microsoft.UI.Xaml.Media.Animation.FillBehavior.HoldEnd,
		};
		Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animOnChild, target);
		Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animOnChild, "Opacity");

		var sbReference = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
		sbReference.Children.Add(animOnChild);

		bool referenceCompleted = false;
		sbReference.Completed += (s, e) => referenceCompleted = true;
		sbReference.Begin();
		await TestServices.WindowHelper.WaitFor(() => referenceCompleted, timeoutMS: 2000);
		var referenceOpacity = target.Opacity;

		sbReference.Stop();
		target.Opacity = 1.0;
		await TestServices.WindowHelper.WaitForIdle();

		// Animation with Duration on the Storyboard (the buggy scenario)
		var animOnStoryboard = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
		{
			From = 1.0,
			To = 0.5,
			// No Duration here — set on Storyboard instead
			FillBehavior = Microsoft.UI.Xaml.Media.Animation.FillBehavior.HoldEnd,
		};
		Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animOnStoryboard, target);
		Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animOnStoryboard, "Opacity");

		var sbWithDuration = new Microsoft.UI.Xaml.Media.Animation.Storyboard
		{
			Duration = TimeSpan.FromMilliseconds(300), // Duration on Storyboard, not on child
		};
		sbWithDuration.Children.Add(animOnStoryboard);

		bool durationCompleted = false;
		sbWithDuration.Completed += (s, e) => durationCompleted = true;
		sbWithDuration.Begin();
		await TestServices.WindowHelper.WaitFor(() => durationCompleted, timeoutMS: 2000);
		var durationOpacity = target.Opacity;

		sbWithDuration.Stop();

		// Both approaches should produce the same result (opacity near 0.5)
		Assert.AreEqual(referenceOpacity, durationOpacity, 0.05,
			$"Expected Storyboard.Duration to produce same result as DoubleAnimation.Duration. " +
			$"Reference opacity: {referenceOpacity}, Storyboard-duration opacity: {durationOpacity}. " +
			$"When Duration is set on the Storyboard, the animation may not run correctly.");
	}
}
