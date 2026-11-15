using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24600")]
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

		try
		{
			storyboard.Begin();
			await TestServices.WindowHelper.WaitFor(() => ReferenceEquals(target.Foreground, red));

			// A local value set after the animation wins while it is filling.
			target.Foreground = blue;
			Assert.AreSame(blue, target.Foreground);

			// Restarting re-applies the very value the animated slot already holds, which must retake precedence.
			storyboard.Begin();
			await TestServices.WindowHelper.WaitFor(() => ReferenceEquals(target.Foreground, red), message: "The restarted animation should win over the older local value.");
		}
		finally
		{
			// A filling animation must not outlive the test.
			storyboard.Stop();
		}
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

#if HAS_UNO
	[TestMethod]
	public async Task When_Finite_Storyboard_Runs_It_Is_Counted_As_Active()
	{
		var target = new Border { Width = 10, Height = 10 };
		await UITestHelper.Load(target);
		var storyboard = CreateOpacityStoryboard(target, TimeSpan.FromMilliseconds(200));
		var baseline = Storyboard.ActiveFiniteCount;
		var completed = false;
		storyboard.Completed += (_, _) => completed = true;

		storyboard.Begin();
		Assert.AreEqual(baseline + 1, Storyboard.ActiveFiniteCount);

		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 2000);
		Assert.AreEqual(baseline, Storyboard.ActiveFiniteCount);
	}

	[TestMethod]
	public async Task When_Finite_Storyboard_Stopped_It_Is_No_Longer_Counted()
	{
		var target = new Border { Width = 10, Height = 10 };
		await UITestHelper.Load(target);
		var storyboard = CreateOpacityStoryboard(target, TimeSpan.FromSeconds(10));
		var baseline = Storyboard.ActiveFiniteCount;

		storyboard.Begin();
		Assert.AreEqual(baseline + 1, Storyboard.ActiveFiniteCount);

		storyboard.Stop();
		Assert.AreEqual(baseline, Storyboard.ActiveFiniteCount);
	}

	[TestMethod]
	public async Task When_Storyboard_Repeats_Forever_It_Is_Not_Counted()
	{
		var target = new Border { Width = 10, Height = 10 };
		await UITestHelper.Load(target);
		var storyboard = CreateOpacityStoryboard(target, TimeSpan.FromMilliseconds(200));
		storyboard.RepeatBehavior = RepeatBehavior.Forever;
		var baseline = Storyboard.ActiveFiniteCount;

		try
		{
			storyboard.Begin();
			Assert.AreEqual(baseline, Storyboard.ActiveFiniteCount);
		}
		finally
		{
			storyboard.Stop();
		}
	}

	private static Storyboard CreateOpacityStoryboard(UIElement target, TimeSpan duration)
	{
		var animation = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(duration) };
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, "Opacity");
		return new Storyboard { Children = { animation } };
	}
#endif
}
