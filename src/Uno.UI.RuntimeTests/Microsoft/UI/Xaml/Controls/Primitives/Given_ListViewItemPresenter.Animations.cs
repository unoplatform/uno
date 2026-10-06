#if HAS_UNO
#nullable enable

using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
