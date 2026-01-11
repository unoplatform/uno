using System.Threading.Tasks;
using Private.Infrastructure;
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
	public async Task When_DependentAnimation_Without_EnableDependentAnimation_Completes()
	{
		var border = new Microsoft.UI.Xaml.Controls.Border
		{
			Width = 100,
			Height = 100
		};

		var animation = new DoubleAnimation
		{
			From = 100,
			To = 200,
			Duration = new Duration(System.TimeSpan.FromMilliseconds(100)),
			EnableDependentAnimation = false // Explicitly set to false
		};

		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Height");

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();

		// The Completed event should fire even though the animation doesn't actually run
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 2000);
		Assert.IsTrue(completed, "Storyboard.Completed should fire even for dependent animations without EnableDependentAnimation");
	}

	[TestMethod]
	public async Task When_DependentAnimation_Stopped_Before_Completion()
	{
		var border = new Microsoft.UI.Xaml.Controls.Border
		{
			Width = 100,
			Height = 100
		};

		var animation = new DoubleAnimation
		{
			From = 100,
			To = 200,
			Duration = new Duration(System.TimeSpan.FromMilliseconds(100)),
			EnableDependentAnimation = false
		};

		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Height");

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();
		storyboard.Stop(); // Stop immediately

		// Completed should not fire when stopped before completion
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsFalse(completed, "Storyboard.Completed should not fire when stopped before completion");
	}
}
