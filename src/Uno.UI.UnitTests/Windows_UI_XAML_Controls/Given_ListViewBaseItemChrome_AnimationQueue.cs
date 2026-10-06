using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using DragDropState = Microsoft.UI.Xaml.Controls.Primitives.ListViewBaseItemAnimationCommand_DragDrop.DragDropState;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ListViewBaseItemChrome_AnimationQueue
{
	[TestMethod]
	public void When_Commands_Enqueued_Then_Dequeued_In_FIFO_Order()
	{
		var presenter = new ListViewItemPresenter();
		var first = CreateMultiSelect(presenter);
		var second = CreateReorderHint(presenter);
		var third = CreateDragDrop(presenter, DragDropState.SinglePrimary);

		Enqueue(presenter, first);
		Enqueue(presenter, second);
		Enqueue(presenter, third);

		Assert.AreSame(first, presenter.GetNextPendingAnimation());
		Assert.AreSame(second, presenter.GetNextPendingAnimation());
		Assert.AreSame(third, presenter.GetNextPendingAnimation());
		Assert.IsNull(presenter.GetNextPendingAnimation());
	}

	[TestMethod]
	public void When_Queue_Empty_Then_Next_Is_Null()
	{
		var presenter = new ListViewItemPresenter();

		Assert.IsNull(presenter.GetNextPendingAnimation());
	}

	[TestMethod]
	public void When_Lower_Priority_Locked_Then_Higher_Number_Does_Not_Proceed()
	{
		var presenter = new ListViewItemPresenter();
		var dragDrop = CreateDragDrop(presenter, DragDropState.SinglePrimary);

		Assert.IsTrue(presenter.LockLayersForAnimation(dragDrop));

		// Lower numbers win: 0 (drag) blocks 1 (reorder hint, drag target) and 3 (multi-select).
		Assert.IsFalse(presenter.LockLayersForAnimation(CreateDragDrop(presenter, DragDropState.Target)));
		Assert.IsFalse(presenter.LockLayersForAnimation(CreateReorderHint(presenter)));
		Assert.IsFalse(presenter.LockLayersForAnimation(CreateMultiSelect(presenter)));
		Assert.IsNull(presenter.GetNextPendingAnimation());
	}

	[TestMethod]
	public void When_Same_Priority_Locked_Then_Proceeds_Without_Stop_Command()
	{
		var presenter = new ListViewItemPresenter();

		Assert.IsTrue(presenter.LockLayersForAnimation(CreateMultiSelect(presenter)));
		Assert.IsTrue(presenter.LockLayersForAnimation(CreateIndicatorSelect(presenter)));

		Assert.IsNull(presenter.GetNextPendingAnimation());
	}

	[TestMethod]
	public void When_Supplanted_Then_Stop_Clone_Of_Current_Is_Enqueued()
	{
		var presenter = new ListViewItemPresenter();
		var multiSelect = CreateMultiSelect(presenter);

		Assert.IsTrue(presenter.LockLayersForAnimation(multiSelect));
		Assert.IsTrue(presenter.LockLayersForAnimation(CreateReorderHint(presenter)));

		var stop = presenter.GetNextPendingAnimation() as ListViewBaseItemAnimationCommand_MultiSelect;

		Assert.IsNotNull(stop);
		Assert.AreNotSame(multiSelect, stop);
		Assert.IsFalse(stop.m_isStarting);
		Assert.IsTrue(multiSelect.m_isStarting, "The running command must keep its own flag.");
		Assert.AreEqual(multiSelect.m_entering, stop.m_entering);
		Assert.AreEqual(multiSelect.m_checkBoxTranslationX, stop.m_checkBoxTranslationX);
		Assert.AreEqual(multiSelect.m_contentTranslationX, stop.m_contentTranslationX);
		Assert.AreSame(multiSelect.m_multiSelectCheckBox, stop.m_multiSelectCheckBox);
		Assert.AreSame(multiSelect.m_contentPresenter, stop.m_contentPresenter);
		Assert.IsNull(presenter.GetNextPendingAnimation());
	}

	[TestMethod]
	public void When_Current_Unlocked_Then_Priority_Resets()
	{
		var presenter = new ListViewItemPresenter();
		var dragDrop = CreateDragDrop(presenter, DragDropState.SinglePrimary);

		presenter.LockLayersForAnimation(dragDrop);
		presenter.UnlockLayersForAnimationAndDisposeCommand(dragDrop);

		Assert.IsTrue(presenter.LockLayersForAnimation(CreateMultiSelect(presenter)));

		// Nothing was current anymore, so nothing gets stopped.
		Assert.IsNull(presenter.GetNextPendingAnimation());
	}

	[TestMethod]
	public void When_Other_Command_Unlocked_Then_Lock_Is_Kept()
	{
		var presenter = new ListViewItemPresenter();

		presenter.LockLayersForAnimation(CreateDragDrop(presenter, DragDropState.SinglePrimary));
		presenter.UnlockLayersForAnimationAndDisposeCommand(CreateDragDrop(presenter, DragDropState.SinglePrimary));
		presenter.UnlockLayersForAnimationAndDisposeCommand(null);

		Assert.IsFalse(presenter.LockLayersForAnimation(CreateMultiSelect(presenter)));
	}

	[TestMethod]
	public void When_Cloned_Then_Every_Command_Keeps_Its_Fields()
	{
		var presenter = new ListViewItemPresenter();
		var target = new WeakReference<UIElement>(new Border());

		var pressed = (ListViewBaseItemAnimationCommand_Pressed)new ListViewBaseItemAnimationCommand_Pressed(true, target, isStarting: true, steadyStateOnly: true).Clone();
		Assert.IsTrue(pressed.m_pressed);
		Assert.AreSame(target, pressed.m_pAnimationTarget);
		Assert.IsTrue(pressed.m_isStarting);
		Assert.IsTrue(pressed.m_steadyStateOnly);

		var reorderHint = (ListViewBaseItemAnimationCommand_ReorderHint)new ListViewBaseItemAnimationCommand_ReorderHint(1.5f, -2.5f, new(presenter), isStarting: false, steadyStateOnly: true).Clone();
		Assert.AreEqual(1.5f, reorderHint.m_offsetX);
		Assert.AreEqual(-2.5f, reorderHint.m_offsetY);
		Assert.IsTrue(reorderHint.m_pAnimationTarget.TryGetTarget(out var reorderTarget));
		Assert.AreSame(presenter, reorderTarget);
		Assert.IsFalse(reorderHint.m_isStarting);
		Assert.IsTrue(reorderHint.m_steadyStateOnly);

		var original = (ListViewBaseItemAnimationCommand_DragDrop)CreateDragDrop(presenter, DragDropState.ReorderedPlaceholder);
		var dragDrop = (ListViewBaseItemAnimationCommand_DragDrop)original.Clone();
		Assert.AreEqual(DragDropState.ReorderedPlaceholder, dragDrop.m_state);
		Assert.AreSame(original.m_pBaseAnimationTarget, dragDrop.m_pBaseAnimationTarget);
		Assert.AreSame(original.m_pFadeOutAnimationTarget, dragDrop.m_pFadeOutAnimationTarget);

		var multiSelect = (ListViewBaseItemAnimationCommand_MultiSelect)CreateMultiSelect(presenter).Clone();
		Assert.IsTrue(multiSelect.m_isRoundedListViewBaseItemChromeEnabled);
		Assert.IsTrue(multiSelect.m_entering);
		Assert.AreEqual(-20.0, multiSelect.m_checkBoxTranslationX);
		Assert.AreEqual(-28.0, multiSelect.m_contentTranslationX);
		Assert.AreEqual(ListViewItemPresenterCheckMode.Overlay, multiSelect.m_checkMode);

		var indicatorSelect = (ListViewBaseItemAnimationCommand_IndicatorSelect)CreateIndicatorSelect(presenter).Clone();
		Assert.IsFalse(indicatorSelect.m_entering);
		Assert.AreEqual(7.0, indicatorSelect.m_translationX);
		Assert.AreEqual(ListViewItemPresenterSelectionIndicatorMode.Inline, indicatorSelect.m_selectionIndicatorMode);

		var visibility = (ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility)new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(true, 0.25, target, isStarting: true, steadyStateOnly: false).Clone();
		Assert.IsTrue(visibility.m_selected);
		Assert.AreEqual(0.25, visibility.m_fromScale);
		Assert.AreSame(target, visibility.m_selectionIndicator);
	}

	[TestMethod]
	public void When_Priorities_Queried_Then_Match_WinUI()
	{
		var presenter = new ListViewItemPresenter();
		var target = new WeakReference<UIElement>(new Border());

		Assert.AreEqual(3, new ListViewBaseItemAnimationCommand_Pressed(true, target, true, false).GetPriority());
		Assert.AreEqual(1, CreateReorderHint(presenter).GetPriority());
		Assert.AreEqual(0, CreateDragDrop(presenter, DragDropState.SinglePrimary).GetPriority());
		Assert.AreEqual(0, CreateDragDrop(presenter, DragDropState.ReorderingTarget).GetPriority());
		Assert.AreEqual(1, CreateDragDrop(presenter, DragDropState.Target).GetPriority());
		Assert.AreEqual(3, CreateMultiSelect(presenter).GetPriority());
		Assert.AreEqual(3, CreateIndicatorSelect(presenter).GetPriority());
		Assert.AreEqual(3, new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(true, 0, target, true, false).GetPriority());
	}

	[TestMethod]
	public void When_Accepted_Then_Matching_Visit_Overload_Called()
	{
		var presenter = new ListViewItemPresenter();
		var target = new WeakReference<UIElement>(new Border());
		var visitor = new RecordingVisitor();

		new ListViewBaseItemAnimationCommand_Pressed(true, target, true, false).Accept(visitor);
		CreateReorderHint(presenter).Accept(visitor);
		CreateDragDrop(presenter, DragDropState.DragOver).Accept(visitor);
		CreateMultiSelect(presenter).Accept(visitor);
		CreateIndicatorSelect(presenter).Accept(visitor);
		new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(true, 0, target, true, false).Accept(visitor);

		CollectionAssert.AreEqual(
			new[] { "Pressed", "ReorderHint", "DragDrop", "MultiSelect", "IndicatorSelect", "SelectionIndicatorVisibility" },
			visitor.Visits);
	}

	[TestMethod]
	public void When_Enqueued_Then_TransitionTargets_Created_On_All_Animation_Targets()
	{
		var presenter = new ListViewItemPresenter();
		var item = new ListViewItem();
		var templateChild = new Border();

		presenter.AddChild(templateChild);
		presenter.AddSecondaryChrome();
		presenter.SetChromedListViewBaseItem(item);
		var secondaryChrome = GetField<UIElement>(presenter, "m_pSecondaryChrome");

		Assert.AreSame(templateChild, presenter.GetTemplateChildIfExists());
		Assert.IsFalse(presenter.HasTransitionTarget());
		Assert.IsFalse(secondaryChrome.HasTransitionTarget());
		Assert.IsFalse(item.HasTransitionTarget());
		Assert.IsFalse(templateChild.HasTransitionTarget());

		Enqueue(presenter, CreateMultiSelect(presenter));

		Assert.IsTrue(presenter.HasTransitionTarget());
		Assert.IsTrue(secondaryChrome.HasTransitionTarget());
		Assert.IsTrue(item.HasTransitionTarget());
		Assert.IsTrue(templateChild.HasTransitionTarget());
		Assert.AreNotSame(presenter.TransitionTarget, item.TransitionTarget);
	}

	[TestMethod]
	public void When_TransitionTargets_Exist_Then_Kept()
	{
		var presenter = new ListViewItemPresenter();
		var templateChild = new Border();
		presenter.AddChild(templateChild);

		Enqueue(presenter, CreateMultiSelect(presenter));
		var presenterTarget = presenter.TransitionTarget;
		var childTarget = templateChild.TransitionTarget;

		Enqueue(presenter, CreateMultiSelect(presenter));

		Assert.AreSame(presenterTarget, presenter.TransitionTarget);
		Assert.AreSame(childTarget, templateChild.TransitionTarget);
	}

	[TestMethod]
	public void When_No_Optional_Targets_Then_Only_Chrome_Gets_TransitionTarget()
	{
		var presenter = new ListViewItemPresenter();

		Enqueue(presenter, CreateMultiSelect(presenter));

		Assert.IsTrue(presenter.HasTransitionTarget());
		Assert.IsNull(presenter.GetTemplateChildIfExists());
	}

	[TestMethod]
	public void When_Reorder_Hint_Offset_Computed()
	{
		var presenter = new ListViewItemPresenter();

		// LVIP ReorderHintOffset defaults to 10.
		Assert.AreEqual(new Point(0, 0), ComputeReorderHintOffset(presenter, ListViewBaseItemPresenter.ReorderHintStates.NoReorderHint));
		Assert.AreEqual(new Point(0, 10), ComputeReorderHintOffset(presenter, ListViewBaseItemPresenter.ReorderHintStates.BottomReorderHint));
		Assert.AreEqual(new Point(0, -10), ComputeReorderHintOffset(presenter, ListViewBaseItemPresenter.ReorderHintStates.TopReorderHint));
		Assert.AreEqual(new Point(-10, 0), ComputeReorderHintOffset(presenter, ListViewBaseItemPresenter.ReorderHintStates.LeftReorderHint));
		Assert.AreEqual(new Point(10, 0), ComputeReorderHintOffset(presenter, ListViewBaseItemPresenter.ReorderHintStates.RightReorderHint));
	}

	private static ListViewBaseItemAnimationCommand CreateReorderHint(ListViewBaseItemPresenter presenter)
		=> new ListViewBaseItemAnimationCommand_ReorderHint(0, 10, new(presenter), isStarting: true, steadyStateOnly: false);

	private static ListViewBaseItemAnimationCommand CreateDragDrop(ListViewBaseItemPresenter presenter, DragDropState state)
		=> new ListViewBaseItemAnimationCommand_DragDrop(state, new(presenter), new(new Border()), isStarting: true, steadyStateOnly: false);

	private static ListViewBaseItemAnimationCommand_MultiSelect CreateMultiSelect(ListViewBaseItemPresenter presenter)
		=> new(
			isRoundedListViewBaseItemChromeEnabled: true,
			entering: true,
			checkBoxTranslationX: -20,
			contentTranslationX: -28,
			ListViewItemPresenterCheckMode.Overlay,
			new(new Border()),
			new(presenter),
			isStarting: true,
			steadyStateOnly: false);

	private static ListViewBaseItemAnimationCommand CreateIndicatorSelect(ListViewBaseItemPresenter presenter)
		=> new ListViewBaseItemAnimationCommand_IndicatorSelect(
			entering: false,
			translationX: 7,
			ListViewItemPresenterSelectionIndicatorMode.Inline,
			new(new Border()),
			new(presenter),
			isStarting: true,
			steadyStateOnly: false);

	private static void Enqueue(ListViewBaseItemPresenter presenter, ListViewBaseItemAnimationCommand command)
		=> GetMethod("EnqueueAnimationCommand").Invoke(presenter, new object[] { command });

	private static Point ComputeReorderHintOffset(ListViewBaseItemPresenter presenter, ListViewBaseItemPresenter.ReorderHintStates state)
		=> (Point)GetMethod("ComputeReorderHintOffset").Invoke(presenter, new object[] { state })!;

	private static MethodInfo GetMethod(string name)
		=> typeof(ListViewBaseItemPresenter).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Method {name} not found.");

	private static T GetField<T>(ListViewBaseItemPresenter presenter, string name)
		=> (T)(typeof(ListViewBaseItemPresenter).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Field {name} not found.")).GetValue(presenter)!;

	private sealed class RecordingVisitor : IListViewBaseItemAnimationCommandVisitor
	{
		public List<string> Visits { get; } = new();

		public void VisitAnimationCommand(ListViewBaseItemAnimationCommand_Pressed command) => Visits.Add("Pressed");

		public void VisitAnimationCommand(ListViewBaseItemAnimationCommand_ReorderHint command) => Visits.Add("ReorderHint");

		public void VisitAnimationCommand(ListViewBaseItemAnimationCommand_DragDrop command) => Visits.Add("DragDrop");

		public void VisitAnimationCommand(ListViewBaseItemAnimationCommand_MultiSelect command) => Visits.Add("MultiSelect");

		public void VisitAnimationCommand(ListViewBaseItemAnimationCommand_IndicatorSelect command) => Visits.Add("IndicatorSelect");

		public void VisitAnimationCommand(ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility command) => Visits.Add("SelectionIndicatorVisibility");
	}
}
