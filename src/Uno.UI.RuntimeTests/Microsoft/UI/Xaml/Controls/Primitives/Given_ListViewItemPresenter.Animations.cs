#if HAS_UNO
#nullable enable

using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using DragDropState = Microsoft.UI.Xaml.Controls.Primitives.ListViewBaseItemAnimationCommand_DragDrop.DragDropState;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_ListViewItemPresenter
{
	private const double AnimationTolerance = 1e-5;

	[TestMethod]
	public async Task When_ReorderHint_Starts_Presenter_Translates_To_Offset()
	{
		var (presenter, _) = await CreateLayoutItem();

		Process(presenter, ReorderHint(presenter, 10, 4, isStarting: true));

		Assert.IsNotNull(GetAnimationStoryboard(presenter, "m_reorderHintAnimation"));

		var transform = presenter.TransitionTarget!.CompositeTransform!;
		await WindowHelper.WaitFor(() => Math.Abs(transform.TranslateX - 10) < AnimationTolerance, message: "TranslateX did not reach the hint offset");

		Assert.AreEqual(4, transform.TranslateY, AnimationTolerance);
		Assert.AreEqual(1, GetCurrentPriority(presenter), "ReorderHint keeps the layer lock while hinting");
	}

	[TestMethod]
	public async Task When_ReorderHint_Steady_State_Applies_Offset_Synchronously()
	{
		var (presenter, _) = await CreateLayoutItem();

		Process(presenter, ReorderHint(presenter, 10, 0, isStarting: true, steadyStateOnly: true));

		Assert.AreEqual(10, presenter.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance);
	}

	[TestMethod]
	public async Task When_ReorderHint_Returns_Hands_Off_And_Unlocks_On_Completion()
	{
		var (presenter, _) = await CreateLayoutItem();
		var transform = await StartSettledReorderHint(presenter);

		Process(presenter, ReorderHint(presenter, 10, 0, isStarting: false));

		Assert.AreEqual(10, transform.TranslateX, 0.5, "Return animation starts from the hinted offset");

		await WindowHelper.WaitFor(() => GetAnimationStoryboard(presenter, "m_reorderHintAnimation") is null, message: "Completed handler did not clear the animation");

		Assert.AreEqual(0, transform.TranslateX, AnimationTolerance);
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter), "Layers unlocked once the return completes");
	}

	[TestMethod]
	public async Task When_ReorderHint_Steady_State_Stop_Clears_Immediately()
	{
		var (presenter, _) = await CreateLayoutItem();
		var transform = await StartSettledReorderHint(presenter);

		Process(presenter, ReorderHint(presenter, 10, 0, isStarting: false, steadyStateOnly: true));

		Assert.IsNull(GetAnimationStoryboard(presenter, "m_reorderHintAnimation"));
		Assert.AreEqual(0, transform.TranslateX, AnimationTolerance);
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_DragDrop_Source_Scales_Fades_And_Hides_Overlay(bool multi)
	{
		var state = multi ? DragDropState.MultiPrimary : DragDropState.SinglePrimary;
		var (presenter, content) = await CreateLayoutItem();

		Process(presenter, DragDrop(presenter, content, state, isStarting: true));

		var tt = presenter.TransitionTarget!;
		await WindowHelper.WaitFor(() => Math.Abs(tt.Opacity - 0.65f) < AnimationTolerance, message: "Drag source opacity did not settle");

		Assert.AreEqual(1.05f, tt.CompositeTransform!.ScaleX, AnimationTolerance);
		Assert.AreEqual(1.05f, tt.CompositeTransform.ScaleY, AnimationTolerance);
		Assert.AreEqual(new Windows.Foundation.Point(0.5, 0.5), tt.TransformOrigin);
		Assert.AreEqual(0, content.TransitionTarget!.Opacity, AnimationTolerance, "Fade-out target is hidden");
		Assert.AreEqual(0, GetCurrentPriority(presenter));
	}

	[TestMethod]
	public async Task When_DragDrop_Stopped_Values_Revert_And_Unlock()
	{
		var (presenter, content) = await CreateLayoutItem();

		Process(presenter, DragDrop(presenter, content, DragDropState.SinglePrimary, isStarting: true, steadyStateOnly: true));

		var tt = presenter.TransitionTarget!;
		Assert.AreEqual(0.65f, tt.Opacity, AnimationTolerance, "Steady state applies synchronously");

		Process(presenter, DragDrop(presenter, content, DragDropState.SinglePrimary, isStarting: false));

		Assert.IsNull(GetAnimationStoryboard(presenter, "m_dragDropAnimation"));
		Assert.AreEqual(1, tt.Opacity, AnimationTolerance);
		Assert.AreEqual(1, tt.CompositeTransform!.ScaleX, AnimationTolerance);
		Assert.AreEqual(1, content.TransitionTarget!.Opacity, AnimationTolerance);
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	public async Task When_DragOver_Affected_Scales_Down()
	{
		var (presenter, content) = await CreateLayoutItem();

		Process(presenter, DragDrop(presenter, content, DragDropState.DragOver, isStarting: true));

		var transform = presenter.TransitionTarget!.CompositeTransform!;
		await WindowHelper.WaitFor(() => Math.Abs(transform.ScaleX - 0.95f) < AnimationTolerance, message: "Affected scale did not settle");

		Assert.AreEqual(0.95f, transform.ScaleY, AnimationTolerance);
		Assert.AreEqual(1, presenter.TransitionTarget.Opacity, AnimationTolerance);
		Assert.IsFalse(content.HasTransitionTarget() && content.TransitionTarget!.Opacity != 1, "DragOver does not fade");
	}

	[TestMethod]
	public async Task When_ReorderedPlaceholder_Only_Fades_Out()
	{
		var (presenter, content) = await CreateLayoutItem();

		Process(presenter, DragDrop(presenter, content, DragDropState.ReorderedPlaceholder, isStarting: true, steadyStateOnly: true));

		Assert.AreEqual(0, content.TransitionTarget!.Opacity, AnimationTolerance);
		Assert.AreEqual(1, presenter.TransitionTarget!.Opacity, AnimationTolerance);
		Assert.AreEqual(1, presenter.TransitionTarget.CompositeTransform!.ScaleX, AnimationTolerance);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Reordering_Primary_Reads_Opacity_From_Presenter_Resources(bool multi)
	{
		var state = multi ? DragDropState.ReorderingMultiPrimary : DragDropState.ReorderingSinglePrimary;
		var (presenter, content) = await CreateLayoutItem();
		presenter.Resources["ListViewItemReorderThemeOpacity"] = 0.4;

		Process(presenter, DragDrop(presenter, content, state, isStarting: true));

		var tt = presenter.TransitionTarget!;
		await WindowHelper.WaitFor(() => Math.Abs(tt.Opacity - 0.4) < AnimationTolerance, message: "Reorder opacity did not settle");

		Assert.AreEqual(0, content.TransitionTarget!.Opacity, AnimationTolerance);
	}

	[TestMethod]
	public async Task When_Reordering_Primary_Resource_Missing_Defaults_To_One()
	{
		// The app/system dictionaries define ListViewItemReorderThemeOpacity = 0.8; only the presenter's own Resources count.
		var (presenter, content) = await CreateLayoutItem();

		Process(presenter, DragDrop(presenter, content, DragDropState.ReorderingSinglePrimary, isStarting: true, steadyStateOnly: true));

		Assert.AreEqual(1, presenter.TransitionTarget!.Opacity, AnimationTolerance);
		Assert.AreEqual(0, content.TransitionTarget!.Opacity, AnimationTolerance);
	}

	[TestMethod]
	public async Task When_Reordering_Target_Reads_Opacity_And_Scale_From_Presenter_Resources()
	{
		var (presenter, content) = await CreateLayoutItem();
		presenter.Resources["ListViewItemReorderTargetThemeOpacity"] = 0.5;
		presenter.Resources["ListViewItemReorderTargetThemeScale"] = 0.9;

		Process(presenter, DragDrop(presenter, content, DragDropState.ReorderingTarget, isStarting: true));

		var tt = presenter.TransitionTarget!;
		await WindowHelper.WaitFor(() => Math.Abs(tt.CompositeTransform!.ScaleX - 0.9) < AnimationTolerance, message: "Reorder target scale did not settle");

		Assert.AreEqual(0.9, tt.CompositeTransform!.ScaleY, AnimationTolerance);
		Assert.AreEqual(0.5, tt.Opacity, AnimationTolerance);
		Assert.AreEqual(new Windows.Foundation.Point(0.5, 0.5), tt.TransformOrigin);
		Assert.IsFalse(content.HasTransitionTarget() && content.TransitionTarget!.Opacity != 1, "ReorderingTarget does not fade");
	}

	[TestMethod]
	public async Task When_DragDrop_Holds_Lock_ReorderHint_Is_Refused()
	{
		var (presenter, content) = await CreateLayoutItem();

		Process(presenter, DragDrop(presenter, content, DragDropState.SinglePrimary, isStarting: true, steadyStateOnly: true));
		Process(presenter, ReorderHint(presenter, 10, 0, isStarting: true, steadyStateOnly: true));

		Assert.IsNull(GetAnimationStoryboard(presenter, "m_reorderHintAnimation"));
		Assert.AreEqual(0, presenter.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance);
		Assert.AreEqual(0, GetCurrentPriority(presenter), "The refused command does not release the drag lock");
	}

	[TestMethod]
	public async Task When_Pressed_Command_Is_Disposed()
	{
		var (presenter, _) = await CreateLayoutItem();

		Process(presenter, new ListViewBaseItemAnimationCommand_Pressed(true, new WeakReference<UIElement>(presenter), isStarting: true, steadyStateOnly: false));

		Assert.IsNull(GetAnimationStoryboard(presenter, "m_pointerPressedAnimation"));
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter), "No storyboard until PointerDown/UpThemeAnimation are ported, so the lock is released");
		Assert.AreEqual(0, GetPendingCommandCount(presenter));
	}

	[TestMethod]
	public async Task When_MultiSelect_Inline_Enters_Then_CheckBox_And_Content_Slide_In()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true);
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: true));

		var checkBoxTT = checkBox.TransitionTarget!;
		Assert.AreEqual(-20, checkBoxTT.CompositeTransform!.TranslateX, AnimationTolerance, "Check box starts one square to the left");
		Assert.AreEqual(20, checkBoxTT.ClipTransform!.TranslateX, AnimationTolerance, "Clip starts compensated");
		Assert.IsTrue(checkBoxTT.HasClipAnimation);
		Assert.AreEqual(-28, content.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance);
		Assert.AreEqual(3, GetCurrentPriority(presenter));

		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");

		Assert.AreEqual(0, checkBoxTT.CompositeTransform.TranslateX, AnimationTolerance);
		Assert.AreEqual(0, checkBoxTT.ClipTransform.TranslateX, AnimationTolerance);
		Assert.AreEqual(0, content.TransitionTarget.CompositeTransform.TranslateX, AnimationTolerance);
		Assert.AreSame(checkBox, GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"), "Entering keeps the check box");
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	[DataRow(true, true, 28.0, 250.0)]
	[DataRow(true, false, 28.0, 167.0)]
	[DataRow(true, true, 19.0, 169.0)] // 250 * 19 / 28 = 169.6, truncated
	[DataRow(true, false, 19.0, 113.0)]
	[DataRow(false, true, 32.0, 333.0)]
	[DataRow(false, false, 32.0, 333.0)]
	public async Task When_MultiSelect_Inline_Duration(bool rounded, bool entering, double contentTranslationX, double expectedMs)
	{
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();

		Process(presenter, MultiSelect(presenter, content, entering, rounded: rounded, contentTranslationX: contentTranslationX));

		var storyboard = GetAnimationStoryboard(presenter, "m_multiSelectAnimation")!;
		Assert.AreEqual(3, storyboard.Children.Count, "Check box translate + clip translate, content translate");
		Assert.AreEqual(TimeSpan.FromMilliseconds(expectedMs), GetLastKeyTime(storyboard));
	}

	[TestMethod]
	public async Task When_MultiSelect_Inline_Leaves_Then_CheckBox_Removed_On_Completion()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true);
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: false));

		var checkBoxTT = checkBox.TransitionTarget!;
		Assert.AreEqual(0, checkBoxTT.CompositeTransform!.TranslateX, AnimationTolerance);
		Assert.AreEqual(28, content.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance, "Content starts at its multi-select offset");

		await WindowHelper.WaitFor(() => checkBoxTT.CompositeTransform.TranslateX < -10, message: "Check box did not slide out");
		Assert.IsTrue(checkBoxTT.ClipTransform!.TranslateX > 10, "Clip slides the other way so the check box stays revealed in place");
		Assert.AreSame(checkBox, GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"), "Still present while animating");

		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
		Assert.IsNull(checkBox.GetParent());
		Assert.AreEqual(0, content.TransitionTarget.CompositeTransform.TranslateX, AnimationTolerance);
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	public async Task When_MultiSelect_Leave_Interrupts_Enter_Then_CheckBox_Removed()
	{
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: true));
		var entering = GetAnimationStoryboard(presenter, "m_multiSelectAnimation")!;

		Process(presenter, MultiSelect(presenter, content, entering: false));

		Assert.AreNotSame(entering, GetAnimationStoryboard(presenter, "m_multiSelectAnimation"), "The running enter animation is cleared");
		Assert.AreEqual(0, GetCompletedHandlerCount(entering), "The cleared animation's Completed handler is unhooked");

		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
		Assert.IsNull(checkBox.GetParent());
	}

	[TestMethod]
	public async Task When_MultiSelect_Completed_Clears_Its_Own_Storyboard_Without_Leak()
	{
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();

		for (var i = 0; i < 2; i++)
		{
			Process(presenter, MultiSelect(presenter, content, entering: true));
			var storyboard = GetAnimationStoryboard(presenter, "m_multiSelectAnimation")!;

			// OnMultiSelectCompleted stops the storyboard that is raising Completed (ClearAnimation).
			await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");

			Assert.AreEqual(0, GetCompletedHandlerCount(storyboard));
			Assert.IsNull(GetAnimationCommand(presenter, "m_multiSelectAnimation"));
			Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
			Assert.AreEqual(0, GetPendingCommandCount(presenter));
		}
	}

	[TestMethod]
	public async Task When_MultiSelect_Inline_Steady_State_Jumps()
	{
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: true, steadyStateOnly: true));

		Assert.AreEqual(0, GetAnimationStoryboard(presenter, "m_multiSelectAnimation")!.Children.Count, "Inline steady state has no timelines");
		Assert.AreEqual(0, checkBox.TransitionTarget!.CompositeTransform!.TranslateX);
		Assert.IsFalse(content.HasTransitionTarget() && content.TransitionTarget!.CompositeTransform!.TranslateX != 0);

		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");
		Assert.AreSame(checkBox, GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));

		Process(presenter, MultiSelect(presenter, content, entering: false, steadyStateOnly: true));
		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
	}

	[TestMethod]
	public async Task When_MultiSelect_Overlay_Fades_Out_Then_CheckBox_Removed()
	{
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: false, checkMode: ListViewItemPresenterCheckMode.Overlay));

		var storyboard = GetAnimationStoryboard(presenter, "m_multiSelectAnimation")!;
		Assert.AreEqual(TimeSpan.FromMilliseconds(167), GetLastKeyTime(storyboard));

		var checkBoxTT = checkBox.TransitionTarget!;
		await WindowHelper.WaitFor(() => checkBoxTT.Opacity < 0.9, message: "Check box did not fade");
		Assert.AreEqual(0, checkBoxTT.CompositeTransform!.TranslateX, "Overlay does not slide");
		Assert.IsFalse(content.HasTransitionTarget() && content.TransitionTarget!.CompositeTransform!.TranslateX != 0, "Overlay does not move the content");

		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
	}

	[TestMethod]
	public async Task When_MultiSelect_Overlay_Steady_State_Jumps_And_Keeps_CheckBox()
	{
		var (presenter, content) = await CreateLayoutItem();
		presenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: false, checkMode: ListViewItemPresenterCheckMode.Overlay, steadyStateOnly: true));

		Assert.AreEqual(0, checkBox.TransitionTarget!.Opacity, AnimationTolerance, "Steady fade-out applies synchronously");
		Assert.AreSame(checkBox, GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"), "Completion is never raised from inside Begin");

		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");
		Assert.IsNull(GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));

		presenter.EnsureMultiSelectCheckBox();
		checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: true, checkMode: ListViewItemPresenterCheckMode.Overlay, steadyStateOnly: true));
		Assert.AreEqual(1, checkBox.TransitionTarget!.Opacity, AnimationTolerance);

		await WaitForAnimationCleared(presenter, "m_multiSelectAnimation");
		Assert.AreSame(checkBox, GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"), "Entering keeps the check box");
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	public async Task When_IndicatorSelect_Enters_Then_Indicator_And_Content_Slide_In()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, content, indicator) = await CreateIndicatorItem();

		Process(presenter, IndicatorSelect(indicator, content, entering: true));

		var indicatorTT = indicator.TransitionTarget!;
		Assert.AreEqual(-7, indicatorTT.CompositeTransform!.TranslateX, AnimationTolerance);
		Assert.AreEqual(7, indicatorTT.ClipTransform!.TranslateX, AnimationTolerance);
		Assert.AreEqual(-7, content.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance);
		Assert.AreEqual(TimeSpan.FromMilliseconds(83), GetLastKeyTime(GetAnimationStoryboard(presenter, "m_indicatorSelectAnimation")!));

		await WaitForAnimationCleared(presenter, "m_indicatorSelectAnimation");

		Assert.AreEqual(0, indicatorTT.CompositeTransform.TranslateX, AnimationTolerance);
		Assert.AreEqual(0, indicatorTT.ClipTransform.TranslateX, AnimationTolerance);
		Assert.AreEqual(0, content.TransitionTarget.CompositeTransform.TranslateX, AnimationTolerance);
		Assert.AreSame(indicator, GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"), "IndicatorSelect never removes the indicator");
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	public async Task When_IndicatorSelect_Mid_Animation_Indicator_Clipped_To_Own_Bounds()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, content, indicator) = await CreateIndicatorItem();
		var root = (FrameworkElement)WindowHelper.WindowContent!;

		var indicatorBounds = indicator.TransformToVisual(root).TransformBounds(new Windows.Foundation.Rect(0, 0, indicator.ActualWidth, indicator.ActualHeight));
		var centerY = indicatorBounds.Y + indicatorBounds.Height / 2;
		var restingX = indicatorBounds.X + indicatorBounds.Width / 2;
		var slidX = restingX - 7;

		var before = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(before, (float)restingX, (float)centerY, Microsoft.UI.Colors.Red, tolerance: 5);
		ImageAssert.DoesNotHaveColorAt(before, (float)slidX, (float)centerY, Microsoft.UI.Colors.Red, tolerance: 5);

		Process(presenter, IndicatorSelect(indicator, content, entering: true));

		// Freeze at t=0: the indicator is translated 7px left and the TT clip, shifted back, still covers its resting bounds.
		var storyboard = GetAnimationStoryboard(presenter, "m_indicatorSelectAnimation")!;
		storyboard.Pause();
		try
		{
			Assert.AreEqual(-7, indicator.TransitionTarget!.CompositeTransform!.TranslateX, 0.5);

			var during = await UITestHelper.ScreenShot(root);
			ImageAssert.DoesNotHaveColorAt(during, (float)slidX, (float)centerY, Microsoft.UI.Colors.Red, tolerance: 5);
			ImageAssert.DoesNotHaveColorAt(during, (float)restingX, (float)centerY, Microsoft.UI.Colors.Red, tolerance: 5);
		}
		finally
		{
			storyboard.Resume();
		}

		await WaitForAnimationCleared(presenter, "m_indicatorSelectAnimation");

		var after = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(after, (float)restingX, (float)centerY, Microsoft.UI.Colors.Red, tolerance: 5);
	}

	[TestMethod]
	public async Task When_IndicatorSelect_Leaves_Then_Slides_Out()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, content, indicator) = await CreateIndicatorItem();

		Process(presenter, IndicatorSelect(indicator, content, entering: false));

		var storyboard = GetAnimationStoryboard(presenter, "m_indicatorSelectAnimation")!;
		Assert.AreEqual(7, content.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance);
		Assert.AreEqual(0, indicator.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance);

		await WaitForAnimationCleared(presenter, "m_indicatorSelectAnimation");

		Assert.AreEqual(0, GetCompletedHandlerCount(storyboard));
		Assert.AreEqual(0, content.TransitionTarget.CompositeTransform.TranslateX, AnimationTolerance);
	}

	[TestMethod]
	public async Task When_IndicatorSelect_Steady_State_Jumps()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, content, indicator) = await CreateIndicatorItem();

		Process(presenter, IndicatorSelect(indicator, content, entering: true, steadyStateOnly: true));

		Assert.AreEqual(0, GetAnimationStoryboard(presenter, "m_indicatorSelectAnimation")!.Children.Count);
		Assert.AreEqual(0, indicator.TransitionTarget!.CompositeTransform!.TranslateX);

		await WaitForAnimationCleared(presenter, "m_indicatorSelectAnimation");
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	public async Task When_SelectionIndicator_Shown_Then_Fades_And_Grows_In()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, _, indicator) = await CreateIndicatorItem();

		Process(presenter, SelectionIndicatorVisibility(indicator, selected: true));

		var tt = indicator.TransitionTarget!;
		Assert.AreEqual(0, tt.Opacity, AnimationTolerance, "Starts transparent");
		Assert.AreEqual(0, tt.CompositeTransform!.ScaleY, AnimationTolerance, "Starts from fromScale");
		Assert.AreEqual(new Windows.Foundation.Point(0.5, 0.5), tt.TransformOrigin);
		Assert.AreEqual(TimeSpan.FromMilliseconds(167), GetLastKeyTime(GetAnimationStoryboard(presenter, "m_selectionIndicatorAnimation")!));

		await WindowHelper.WaitFor(() => tt.Opacity > 0.5 && tt.CompositeTransform.ScaleY > 0.5, message: "Indicator did not fade/grow in");

		await WaitForAnimationCleared(presenter, "m_selectionIndicatorAnimation");

		Assert.AreEqual(1, tt.Opacity, AnimationTolerance);
		Assert.AreEqual(1, tt.CompositeTransform.ScaleY, AnimationTolerance);
		Assert.AreSame(indicator, GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"));
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
	}

	[TestMethod]
	public async Task When_SelectionIndicator_Hidden_Then_Removed_On_Completion()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, _, indicator) = await CreateIndicatorItem();

		Process(presenter, SelectionIndicatorVisibility(indicator, selected: false));

		var tt = indicator.TransitionTarget!;
		await WindowHelper.WaitFor(() => tt.Opacity < 0.9, message: "Indicator did not fade out");
		Assert.AreSame(indicator, GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"), "Still present while fading");

		await WaitForAnimationCleared(presenter, "m_selectionIndicatorAnimation");

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"));
		Assert.IsNull(indicator.GetParent());
	}

	[TestMethod]
	public async Task When_SelectionIndicator_Steady_State_Jumps()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, _, indicator) = await CreateIndicatorItem();

		Process(presenter, SelectionIndicatorVisibility(indicator, selected: true, fromScale: 0.5, steadyStateOnly: true));

		var tt = indicator.TransitionTarget!;
		Assert.AreEqual(1, tt.Opacity, AnimationTolerance);
		Assert.AreEqual(1, tt.CompositeTransform!.ScaleY, AnimationTolerance);

		await WaitForAnimationCleared(presenter, "m_selectionIndicatorAnimation");
		Assert.AreSame(indicator, GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"), "Showing keeps the indicator");

		Process(presenter, SelectionIndicatorVisibility(indicator, selected: false, steadyStateOnly: true));

		Assert.AreEqual(0, tt.Opacity, AnimationTolerance);
		Assert.AreSame(indicator, GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"), "Completion is never raised from inside Begin");

		await WaitForAnimationCleared(presenter, "m_selectionIndicatorAnimation");
		Assert.IsNull(GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"));
	}

	[TestMethod]
	public async Task When_SelectionIndicator_Retriggered_While_Animating_Continues_From_Current_Value()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, _, indicator) = await CreateIndicatorItem();

		Process(presenter, SelectionIndicatorVisibility(indicator, selected: false));
		var hiding = GetAnimationStoryboard(presenter, "m_selectionIndicatorAnimation")!;
		var tt = indicator.TransitionTarget!;
		await WindowHelper.WaitFor(() => tt.Opacity < 0.9, message: "Indicator did not fade out");

		Process(presenter, SelectionIndicatorVisibility(indicator, selected: true));

		Assert.AreEqual(0, GetCompletedHandlerCount(hiding), "The interrupted animation no longer removes the indicator");
		Assert.IsTrue(tt.Opacity > 0.05, "No 0 opacity keyframe when an animation is already running");

		await WaitForAnimationCleared(presenter, "m_selectionIndicatorAnimation");

		Assert.AreSame(indicator, GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"));
		Assert.AreEqual(1, tt.Opacity, AnimationTolerance);
	}

	[TestMethod]
	public async Task When_FlushChromeAnimations_Runs_Completion_Synchronously()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);
		var (presenter, content, indicator) = await CreateIndicatorItem();
		presenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");

		Process(presenter, MultiSelect(presenter, content, entering: false));
		var multiSelect = GetAnimationStoryboard(presenter, "m_multiSelectAnimation")!;

		// MultiSelect holds the priority-3 lock, so the indicator command must run on its own.
		presenter.FlushChromeAnimations();
		Process(presenter, SelectionIndicatorVisibility(indicator, selected: false));
		Assert.IsNotNull(GetAnimationStoryboard(presenter, "m_selectionIndicatorAnimation"));

		presenter.FlushChromeAnimations();

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
		Assert.IsNull(checkBox.GetParent());
		Assert.AreEqual(0, GetCompletedHandlerCount(multiSelect));
		Assert.IsNull(GetChromeField<Border?>(presenter, "m_selectionIndicatorRectangle"));
		Assert.IsNull(indicator.GetParent());
		Assert.IsNull(GetAnimationStoryboard(presenter, "m_selectionIndicatorAnimation"));
		Assert.AreEqual(int.MaxValue, GetCurrentPriority(presenter));
		Assert.AreEqual(0, content.TransitionTarget!.CompositeTransform!.TranslateX, AnimationTolerance);
	}

	private static async Task<(ListViewItemPresenter presenter, FrameworkElement content, Border indicator)> CreateIndicatorItem()
	{
		var (presenter, content) = await CreateLayoutItem(
			inListView: true,
			configure: p => p.SelectionIndicatorBrush = new SolidColorBrush(Microsoft.UI.Colors.Red),
			configureItem: item => item.Margin = new Thickness(20, 0, 0, 0));
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);
		presenter.EnsureSelectionIndicator();
		await Relayout(presenter);

		return (presenter, content, GetChromeField<Border>(presenter, "m_selectionIndicatorRectangle"));
	}

	private static ListViewBaseItemAnimationCommand_MultiSelect MultiSelect(
		ListViewBaseItemPresenter presenter,
		UIElement content,
		bool entering,
		bool rounded = true,
		double contentTranslationX = 28,
		ListViewItemPresenterCheckMode checkMode = ListViewItemPresenterCheckMode.Inline,
		bool steadyStateOnly = false)
		=> new(
			rounded,
			entering,
			20 /*checkBoxTranslationX*/,
			contentTranslationX,
			checkMode,
			new WeakReference<UIElement>(GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle")),
			new WeakReference<UIElement>(content),
			isStarting: true,
			steadyStateOnly);

	private static ListViewBaseItemAnimationCommand_IndicatorSelect IndicatorSelect(UIElement indicator, UIElement content, bool entering, bool steadyStateOnly = false)
		=> new(entering, 7, ListViewItemPresenterSelectionIndicatorMode.Inline, new WeakReference<UIElement>(indicator), new WeakReference<UIElement>(content), isStarting: true, steadyStateOnly);

	private static ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility SelectionIndicatorVisibility(UIElement indicator, bool selected, double fromScale = 0, bool steadyStateOnly = false)
		=> new(selected, fromScale, new WeakReference<UIElement>(indicator), isStarting: true, steadyStateOnly);

	private static async Task WaitForAnimationCleared(ListViewBaseItemPresenter presenter, string animationStateField)
	{
		await WindowHelper.WaitFor(() => GetAnimationStoryboard(presenter, animationStateField) is null, message: $"{animationStateField} was not cleared by its Completed handler");
		await WindowHelper.WaitForIdle();
	}

	private static TimeSpan GetLastKeyTime(Storyboard storyboard)
	{
		var last = TimeSpan.Zero;
		foreach (var child in storyboard.Children)
		{
			if (child is DoubleAnimationUsingKeyFrames keyFrames)
			{
				foreach (var keyFrame in keyFrames.KeyFrames)
				{
					last = keyFrame.KeyTime.TimeSpan > last ? keyFrame.KeyTime.TimeSpan : last;
				}
			}
		}

		return last;
	}

	private static object? GetAnimationCommand(ListViewBaseItemPresenter presenter, string animationStateField)
	{
		var state = GetChromeField<object>(presenter, animationStateField);
		return state.GetType().GetField("pCommand", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(state);
	}

	private static int GetCompletedHandlerCount(Storyboard storyboard)
		=> (typeof(Timeline).GetField("_completedHandlers", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(storyboard) as IList)?.Count ?? 0;

	private static async Task<CompositeTransform> StartSettledReorderHint(ListViewItemPresenter presenter)
	{
		Process(presenter, ReorderHint(presenter, 10, 0, isStarting: true));

		var transform = presenter.TransitionTarget!.CompositeTransform!;
		await WindowHelper.WaitFor(() => Math.Abs(transform.TranslateX - 10) < AnimationTolerance, message: "TranslateX did not reach the hint offset");

		return transform;
	}

	private static ListViewBaseItemAnimationCommand_ReorderHint ReorderHint(ListViewBaseItemPresenter presenter, float x, float y, bool isStarting, bool steadyStateOnly = false)
		=> new(x, y, new WeakReference<ListViewBaseItemPresenter>(presenter), isStarting, steadyStateOnly);

	private static ListViewBaseItemAnimationCommand_DragDrop DragDrop(ListViewBaseItemPresenter presenter, FrameworkElement fadeOutTarget, DragDropState state, bool isStarting, bool steadyStateOnly = false)
		=> new(state, new WeakReference<ListViewBaseItemPresenter>(presenter), new WeakReference<FrameworkElement>(fadeOutTarget), isStarting, steadyStateOnly);

	// EnqueueAnimationCommand and ProcessAnimationCommands are private (C++ private/protected); GoToChromedState is not ported yet.
	private static void Process(ListViewBaseItemPresenter presenter, ListViewBaseItemAnimationCommand command)
	{
		typeof(ListViewBaseItemPresenter).GetMethod("EnqueueAnimationCommand", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(presenter, [command]);
		typeof(ListViewBaseItemPresenter).GetMethod("ProcessAnimationCommands", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(presenter, null);
	}

	private static Storyboard? GetAnimationStoryboard(ListViewBaseItemPresenter presenter, string animationStateField)
	{
		var state = GetChromeField<object>(presenter, animationStateField);
		return (Storyboard?)state.GetType().GetField("tpStoryboard", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(state);
	}

	private static int GetCurrentPriority(ListViewBaseItemPresenter presenter) => GetChromeField<int>(presenter, "m_currentHighestCommandPriority");

	private static int GetPendingCommandCount(ListViewBaseItemPresenter presenter) => GetChromeField<IList>(presenter, "m_animationCommands").Count;
}
#endif
