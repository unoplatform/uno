#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media_Animation;

[UITestClass]
public class Given_CPUBoundAnimator
{
	[TestMethod]
	public async Task When_No_Frame_Comes_Then_Ends_On_Time()
	{
		using var sut = new FramelessAnimator();
		sut.SetDuration(50);
		var ended = new TaskCompletionSource();
		sut.AnimationEnd += (_, _) => ended.TrySetResult();

		sut.Start();
		await ended.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.AreEqual(10d, (double)sut.AnimatedValue);
		Assert.IsFalse(sut.IsRunning);
	}

	[TestMethod]
	public async Task When_Paused_Then_Does_Not_End()
	{
		using var sut = new FramelessAnimator();
		sut.SetDuration(50);
		var ended = false;
		sut.AnimationEnd += (_, _) => ended = true;

		sut.Start();
		sut.Pause();
		await Task.Delay(200);

		Assert.IsFalse(ended);
	}

	[TestMethod]
	public async Task When_Not_Ending_Without_Frames_Then_Waits_For_A_Frame()
	{
		using var sut = new FramelessAnimator { EndsWithoutFrames = false };
		sut.SetDuration(50);
		var ended = false;
		sut.AnimationEnd += (_, _) => ended = true;

		sut.Start();
		await Task.Delay(200);

		Assert.IsFalse(ended);
	}

	private sealed class FramelessAnimator() : CPUBoundAnimator<double>(0, 10)
	{
		protected override void EnableFrameReporting() { }

		protected override void DisableFrameReporting() { }

		protected override double GetUpdatedValue(long frame, double from, double to) => from;
	}
}
