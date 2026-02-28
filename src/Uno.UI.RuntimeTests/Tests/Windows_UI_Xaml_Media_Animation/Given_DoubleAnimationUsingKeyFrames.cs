using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Extensions;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation.TestPages;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_DoubleAnimationUsingKeyFrames
{
	[TestMethod]
	public async Task When_Quickly_Transitions()
	{
		// We are mimicking Fluent-style ToggleSwitch's ToggleStates here.
		var sut = new TestPages.QuickMultiTransitionsPage();
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);
		await Task.Delay(1000);

		// Quickly play through multiple visual-states to simulate what happens when tapping(*1) on a ToggleSwitch.
		/* *1: The full tapping experience is: [InitialState: Off]->Dragging->Off->On or [InitialState: A]->B->A->C
		 *		where A=Off, B=Dragging, C=Off
		 * SwitchKnobOn.Opacity	ToggleStates\AOff->COn			1 @[ControlFasterAnimationDuration] f=Linear
		 * SwitchKnobOn.Opacity	ToggleStates\BDragging->AOff	0 @[ControlFasterAnimationDuration] f=Linear
		 * SwitchKnobOn.Opacity	ToggleStates\BDragging->COn		1 @[ControlFasterAnimationDuration] f=Linear
		 * SwitchKnobOn.Opacity	ToggleStates\COn->AOff			0 @[ControlFasterAnimationDuration] f=Linear
		 * SwitchKnobOn.Opacity	ToggleStates\COn->BDragging		1 @0 f=Linear
		 * SwitchKnobOn.Opacity	ToggleStates\COn				1 @[ControlFasterAnimationDuration] f=Linear
		 */
		VisualStateManager.GoToState(sut, TestPages.QuickMultiTransitionsPage.TestStateNames.PhaseB, useTransitions: true);
		VisualStateManager.GoToState(sut, TestPages.QuickMultiTransitionsPage.TestStateNames.PhaseA, useTransitions: true);
		VisualStateManager.GoToState(sut, TestPages.QuickMultiTransitionsPage.TestStateNames.PhaseC, useTransitions: true);
		await WindowHelper.WaitForIdle();
		await Task.Delay(1000);

		// Given that it involves a race condition, we use a matrix of 4x4 to boost the failure rate.
		// If everything went smoothly, the end result should be an opacity of 1 for all the borders.
		var total = sut.RootGrid.Children.OfType<Border>().Count();
		var passed = sut.RootGrid.Children.OfType<Border>()
			.Count(x => x.Opacity == 1.0);
		Assert.AreEqual(total, passed, $"Only {passed} of {total} Border.Opacity is at expected value of 1.0");
	}

	[TestMethod]
	public async Task When_Begin_Stop_Begin()
	{
		var SUT = new BeginStopBegin();
		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.MyAnimatedTranslateTransform.X == 500);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_AutoReverse_True()
	{
		var target = new TextBlock() { Text = "Test KeyFrame AutoReverse" };
		WindowHelper.WindowContent = target;
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForLoaded(target);

		var transform = new TranslateTransform();
		target.RenderTransform = transform;

		var animation = new DoubleAnimationUsingKeyFrames()
		{
			Duration = TimeSpan.FromMilliseconds(400),
			AutoReverse = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new LinearDoubleKeyFrame()
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100)),
			Value = 25
		});
		animation.KeyFrames.Add(new LinearDoubleKeyFrame()
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)),
			Value = 100
		});
		Storyboard.SetTarget(animation, transform);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();

		// Wait for completion (400ms forward + 400ms reverse = 800ms)
		await WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

		// Final value should be back at start (~0)
		var finalValue = transform.X;
		Assert.IsTrue(Math.Abs(finalValue) < 10,
			$"Final value should be close to 0 after keyframe AutoReverse, got {finalValue}");
	}

}
