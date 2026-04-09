using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation.TestPages;
using Private.Infrastructure;
using MUXControlsTestApp.Utilities;

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
	public async Task When_KeyFrameValue_From_StaticResource_In_ControlTemplate()
	{
		// This tests that keyframe values sourced from StaticResource bindings
		// inside a ControlTemplate are correctly resolved when Begin() calls Play() synchronously.
		var page = new TestPages.KeyFrameAnimationTemplatePage();
		WindowHelper.WindowContent = page;
		await WindowHelper.WaitForLoaded(page);
		await WindowHelper.WaitForIdle();

		var control = page.TestControl;
		var border = control.FindVisualChildByName("AnimatedBorder") as Border;
		Assert.IsNotNull(border, "AnimatedBorder should exist in the template");

		// Verify initial state
		Assert.AreEqual(1.0, border.Opacity, "Initial opacity should be 1.0");

		// Trigger visual state with keyframe animations that use StaticResource values
		VisualStateManager.GoToState(control, "Animated", useTransitions: true);

		// Wait for the animations to apply
		await WindowHelper.WaitForIdle();
		await Task.Delay(500);

		// Verify the DoubleAnimationUsingKeyFrames applied the StaticResource value (0.5)
		Assert.AreEqual(0.5, border.Opacity, 0.01, "Opacity should be animated to 0.5 from StaticResource");

		// Verify the ColorAnimationUsingKeyFrames applied the StaticResource value (Green)
		var brush = border.Background as SolidColorBrush;
		Assert.IsNotNull(brush, "Background should be a SolidColorBrush");
		Assert.AreEqual(Colors.Green, brush.Color, "Background color should be animated to Green from StaticResource");
	}

	[TestMethod]
	public async Task When_DiscreteKeyFrame_At_Time0_Value_Applied_Immediately()
	{
		// In WinUI, a DiscreteDoubleKeyFrame at KeyTime=0 applies its value on the
		// very first tick (same frame as Begin). The value should be applied promptly
		// after Begin() is called.
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.3 });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();

		// WinUI applies time-0 values on the first tick (within the same rendering frame).
		// Use WaitFor to ensure the tick has been processed.
		await WindowHelper.WaitFor(() => Math.Abs(border.Opacity - 0.3) < 0.01, message: "Opacity should be 0.3 after time-0 discrete keyframe");
	}

	[TestMethod]
	public async Task When_DiscreteKeyFrame_At_Time0_In_Storyboard_With_Multiple_Animations()
	{
		// A storyboard with both OAKF and DAUKF targeting different properties.
		// Both should apply their time-0 values before the first render.
		var border = new Border
		{
			Width = 50,
			Height = 50,
			Opacity = 1.0,
			Background = new SolidColorBrush(Colors.Red),
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var doubleAnim = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(doubleAnim, border);
		Storyboard.SetTargetProperty(doubleAnim, "Opacity");
		doubleAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.5 });

		var objectAnim = new ObjectAnimationUsingKeyFrames();
		Storyboard.SetTarget(objectAnim, border);
		Storyboard.SetTargetProperty(objectAnim, "Tag");
		objectAnim.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = "Applied" });

		var storyboard = new Storyboard();
		storyboard.Children.Add(doubleAnim);
		storyboard.Children.Add(objectAnim);

		storyboard.Begin();

		await WindowHelper.WaitFor(() => Math.Abs(border.Opacity - 0.5) < 0.01, message: "DAUKF time-0 should apply Opacity=0.5");
		Assert.AreEqual("Applied", border.Tag, "OAKF time-0 should apply Tag='Applied'");
	}

	[TestMethod]
	public async Task When_Multiple_DiscreteKeyFrames_At_Time0()
	{
		// When multiple keyframes exist at time 0, the last keyframe's value should win
		// (since they all resolve to the same progress point).
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		// Two discrete keyframes at time 0: the second should prevail
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.2 });
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.7 });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();

		await WindowHelper.WaitFor(() => Math.Abs(border.Opacity - 0.7) < 0.01, message: "Last time-0 keyframe value should win");
	}

	[TestMethod]
	public async Task When_Mixed_KeyFrames_Time0_And_Animated()
	{
		// First keyframe at time-0 should apply promptly.
		// A subsequent keyframe animates over time to the final value.
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), Value = 0.8 });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();

		// The first keyframe (time-0, value=0.0) should be applied promptly.
		// The animation then animates toward 0.8, so once the first tick processes,
		// opacity should be at or near 0 (between 0 and something small).
		await WindowHelper.WaitFor(() => border.Opacity < 0.5, message: "Opacity should start near 0 from time-0 keyframe");

		// Wait for animation to complete
		await WindowHelper.WaitFor(() => Math.Abs(border.Opacity - 0.8) < 0.05, message: "Opacity should reach 0.8 after animation completes");
	}

	[TestMethod]
	public async Task When_VisualState_With_DAUKF_And_OAKF_Apply_Together()
	{
		// In a VisualState, both OAKF and DAUKF with time-0 keyframes should
		// apply their values in the same frame (no flash).
		var page = new TestPages.VisualStateDaukfOakfPage();
		WindowHelper.WindowContent = page;
		await WindowHelper.WaitForLoaded(page);
		await WindowHelper.WaitForIdle();

		var border = page.TestBorder;
		Assert.AreEqual(1.0, border.Opacity, "Initial opacity should be 1.0");
		Assert.AreEqual(Visibility.Visible, border.Visibility, "Initial visibility should be Visible");

		VisualStateManager.GoToState(page, "HiddenState", useTransitions: false);
		await WindowHelper.WaitForIdle();

		// Both DAUKF (Opacity=0) and OAKF (Visibility=Collapsed) should be applied
		Assert.AreEqual(0.0, border.Opacity, 0.01, "DAUKF should set Opacity=0");
		Assert.AreEqual(Visibility.Collapsed, border.Visibility, "OAKF should set Visibility=Collapsed");
	}

	[TestMethod]
	public async Task When_Storyboard_Begin_SkipToFill()
	{
		// The Begin() + SkipToFill() pattern is used by AppBar.UpdateTemplateSettings
		// to force animations to re-read binding values. This must work correctly.
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.4 });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();
		storyboard.SkipToFill();

		// After Begin + SkipToFill, the value must be applied immediately
		Assert.AreEqual(0.4, border.Opacity, 0.01, "Opacity should be 0.4 after Begin+SkipToFill");
	}

	[TestMethod]
	public async Task When_FillBehavior_HoldEnd_After_Time0()
	{
		// Default FillBehavior is HoldEnd. After a time-0 animation completes,
		// the animated value should be held.
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new DoubleAnimationUsingKeyFrames();
		animation.FillBehavior = FillBehavior.HoldEnd;
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.25 });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();
		await WindowHelper.WaitForIdle();
		await Task.Delay(100);

		// Value should be held at 0.25
		Assert.AreEqual(0.25, border.Opacity, 0.01, "HoldEnd should maintain animated value");
	}

	[TestMethod]
	public async Task When_Storyboard_All_Time0_Children_Fires_Completed()
	{
		// A storyboard whose children all have only time-0 keyframes should
		// fire its Completed event promptly.
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation1 = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation1, border);
		Storyboard.SetTargetProperty(animation1, "Opacity");
		animation1.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.5 });

		var animation2 = new ObjectAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation2, border);
		Storyboard.SetTargetProperty(animation2, "Tag");
		animation2.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = "Done" });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation1);
		storyboard.Children.Add(animation2);

		var completedTcs = new TaskCompletionSource<bool>();
		storyboard.Completed += (s, e) => completedTcs.TrySetResult(true);

		storyboard.Begin();

		// Completed should fire quickly since all children are time-0
		var completed = await Task.WhenAny(completedTcs.Task, Task.Delay(2000));
		Assert.AreEqual(completedTcs.Task, completed, "Storyboard.Completed should fire for all-time-0 storyboard");
	}

	[TestMethod]
	public async Task When_ColorKeyFrame_At_Time0_Value_Applied()
	{
		// ColorAnimationUsingKeyFrames should also apply time-0 values promptly.
		// We target SolidColorBrush.Color directly (not via sub-property path)
		// because programmatic CAUKF on sub-property paths like
		// "(Border.Background).(SolidColorBrush.Color)" is a separate known issue
		// on Uno (https://github.com/unoplatform/uno/issues/23002).
		var brush = new SolidColorBrush(Colors.Red);
		var border = new Border
		{
			Width = 50,
			Height = 50,
			Background = brush,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new ColorAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, brush);
		Storyboard.SetTargetProperty(animation, "Color");
		animation.KeyFrames.Add(new DiscreteColorKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = Colors.Blue });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();

		await WindowHelper.WaitFor(() => brush.Color == Colors.Blue, message: "Color should be Blue after time-0 color keyframe");
	}

	[TestMethod]
	public async Task When_VisualState_Opacity_Animation_No_Flash()
	{
		// Verifies that when a VisualState uses DAUKF to set Opacity=0 at time-0,
		// the element never renders with its initial Opacity=1.
		// This matches WinUI where the first TimeManager tick applies values before render.
		var page = new TestPages.VisualStateDaukfOakfPage();
		WindowHelper.WindowContent = page;
		await WindowHelper.WaitForLoaded(page);
		await WindowHelper.WaitForIdle();

		var border = page.TestBorder;
		Assert.AreEqual(1.0, border.Opacity, "Initial opacity should be 1.0");

		// Go to state that sets Opacity=0 at time-0
		VisualStateManager.GoToState(page, "TransparentState", useTransitions: false);
		await WindowHelper.WaitForIdle();

		// WinUI applies time-0 values on the first tick (same frame as Begin).
		Assert.AreEqual(0.0, border.Opacity, 0.01, "Opacity should be 0 after GoToState with time-0 keyframe");
	}

	[TestMethod]
	public async Task When_Rapid_Begin_Stop_Begin_Without_Dispatch()
	{
		// Ensures rapid Begin/Stop/Begin works correctly now that Begin is synchronous.
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.5 });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		// Rapid Begin/Stop/Begin sequence
		storyboard.Begin();
		storyboard.Stop();
		storyboard.Begin();

		// After the final Begin, the value should be applied on the first tick
		await WindowHelper.WaitFor(() => Math.Abs(border.Opacity - 0.5) < 0.01, message: "Opacity should be 0.5 after Begin/Stop/Begin");
	}

	[TestMethod]
	public async Task When_ObjectKeyFrame_At_Time0_Value_Applied()
	{
		// ObjectAnimationUsingKeyFrames should also apply time-0 values promptly
		// (same deferred-play mechanism as DAUKF/CAUKF on Skia).
		var border = new Border { Width = 50, Height = 50, Tag = "Initial" };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var animation = new ObjectAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Tag");
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = "Updated" });

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();

		await WindowHelper.WaitFor(() => (string)border.Tag == "Updated", message: "Tag should be 'Updated' after time-0 object keyframe");
	}

	[TestMethod]
	public async Task When_VisualState_OAKF_Applies_After_WaitForIdle()
	{
		// VisualState with OAKF should apply values and be settled after WaitForIdle.
		var page = new TestPages.VisualStateDaukfOakfPage();
		WindowHelper.WindowContent = page;
		await WindowHelper.WaitForLoaded(page);
		await WindowHelper.WaitForIdle();

		var border = page.TestBorder;
		Assert.AreEqual(Visibility.Visible, border.Visibility, "Initial visibility should be Visible");

		VisualStateManager.GoToState(page, "HiddenState", useTransitions: false);
		await WindowHelper.WaitForIdle();

		// OAKF (Visibility=Collapsed) should be applied after WaitForIdle
		Assert.AreEqual(Visibility.Collapsed, border.Visibility, "OAKF should set Visibility=Collapsed after WaitForIdle");
	}
}
