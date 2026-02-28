using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

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
	public async Task When_DependentAnimation_Skipped_Storyboard_Completes()
	{
		// When a child animation targets a dependent property (e.g. Height)
		// and EnableDependentAnimation is false (default), the animation
		// bails out in Play(). The storyboard must still fire Completed.
		var target = new Border { Width = 100, Height = 50 };
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForLoaded(target);

		var animation = new DoubleAnimation
		{
			From = 50,
			To = 200,
			Duration = new Duration(System.TimeSpan.FromMilliseconds(250)),
			// EnableDependentAnimation defaults to false, so animating
			// Height (a dependent property) will cause the animation to
			// bail out without running.
		};
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, "Height");

		var storyboard = new Storyboard { Children = { animation } };

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(
			() => completed,
			message: "Storyboard.Completed should fire even when child animations are skipped as dependent");
	}

	[TestMethod]
	public async Task When_MixedDependentAndIndependent_Storyboard_Completes()
	{
		// A storyboard with both a dependent (skipped) animation and an
		// independent animation should still complete normally.
		var target = new Border { Width = 100, Height = 50, Opacity = 1.0 };
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForLoaded(target);

		// This animation targets Height (dependent) and will be skipped
		var dependentAnimation = new DoubleAnimation
		{
			From = 50,
			To = 200,
			Duration = new Duration(System.TimeSpan.FromMilliseconds(50)),
		};
		Storyboard.SetTarget(dependentAnimation, target);
		Storyboard.SetTargetProperty(dependentAnimation, "Height");

		// This animation targets Opacity (independent) and will run
		var independentAnimation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.5,
			Duration = new Duration(System.TimeSpan.FromMilliseconds(50)),
		};
		Storyboard.SetTarget(independentAnimation, target);
		Storyboard.SetTargetProperty(independentAnimation, "Opacity");

		var storyboard = new Storyboard
		{
			Children = { dependentAnimation, independentAnimation }
		};

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(
			() => completed,
			message: "Storyboard.Completed should fire when mix of dependent and independent animations");
	}
}
