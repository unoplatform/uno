using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using SamplesApp.UITests;

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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10629")]
	public async Task When_AutoReverse_True_Animation_Reverses()
	{
		// Issue #10629: Storyboard AutoReverse=True does nothing on Uno.
		// The animation should go From->To then reverse To->From.
		var rect = new Rectangle
		{
			Width = 100,
			Height = 100,
			Opacity = 1.0,
		};

		TestServices.WindowHelper.WindowContent = rect;
		await TestServices.WindowHelper.WaitForIdle();

		var animation = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
		};
		Storyboard.SetTarget(animation, rect);
		Storyboard.SetTargetProperty(animation, "Opacity");

		var storyboard = new Storyboard
		{
			AutoReverse = true,
		};
		storyboard.Children.Add(animation);

		var completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		// Wait for the storyboard to complete (forward + reverse = 600ms)
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 5000,
			message: "Storyboard with AutoReverse did not complete in time");

		// After AutoReverse, the opacity should be back to 1.0 (the From value)
		// because the animation reversed from 0.0 back to 1.0.
		// Allow some tolerance for animation timing.
		Assert.IsTrue(rect.Opacity > 0.8,
			$"After AutoReverse, Opacity should be close to 1.0 (reversed back from 0.0), " +
			$"but was {rect.Opacity}. AutoReverse does not appear to be working.");
	}
}
