using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_Storyboard
{
	[TestMethod]
	public async Task When_Begin_Again_After_Local_Value_Animated_Value_Wins()
	{
		var red = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var blue = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		var target = new ContentControl { Content = "Target" };
		await UITestHelper.Load(target);

		var animation = new ObjectAnimationUsingKeyFrames
		{
			KeyFrames = { new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = red } },
		};
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, "Foreground");
		var storyboard = new Storyboard { Children = { animation } };

		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => ReferenceEquals(target.Foreground, red));

		// A local value set after the animation wins while it is filling.
		target.Foreground = blue;
		Assert.AreSame(blue, target.Foreground);

		// Restarting re-applies the very value the animated slot already holds, which must retake precedence.
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => ReferenceEquals(target.Foreground, red), message: "The restarted animation should win over the older local value.");
	}

	[TestMethod]
	public async Task When_Empty_Storyboard_Completes()
	{
		var storyboard = new Storyboard();
		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed);
	}

	// An empty Storyboard completes to ClockState.Stopped in Uno, whereas native WinUI's default
	// FillBehavior.HoldEnd leaves the clock in ClockState.Filling, so the final-state assertion is Uno-only.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23409")]
	public async Task When_UsingCompletedCallback()
	{
		var completedCount = 0;
		void OnCompleted(object sender, object e) => completedCount++;

		var sut = new Storyboard();
		sut.Completed += OnCompleted;

		Assert.AreEqual(0, completedCount);
		sut.Begin();

		await TestServices.WindowHelper.WaitFor(() => completedCount == 1);
		Assert.AreEqual(ClockState.Stopped, sut.GetCurrentState());
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
}
