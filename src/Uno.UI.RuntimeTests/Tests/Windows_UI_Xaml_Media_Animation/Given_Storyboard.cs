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
}
