// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemPresenter_Partial.cpp, tag winui3/release/2.5.1

#nullable enable

using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemPresenter
{
	// LVBIP_DBG / LVBIP_DEBUG trace switches are not ported.

	private const long REORDER_ANIMATION_DURATION_MSEC = 240;

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 23-39
	private protected void ProcessAnimationCommands()
	{
		var pCommand = GetNextPendingAnimation();
		while (pCommand is not null)
		{
			pCommand.Accept(this);
			pCommand = GetNextPendingAnimation();
		}
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 41-135
	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_Pressed command)
	{
		ListViewBaseItemAnimationCommand? pCommand = command;

		try
		{
			if (command.m_isStarting)
			{
				Storyboard? spStoryboard = null;

				{
					var shouldProceed = LockLayersForAnimation(command);

					if (shouldProceed)
					{
						if (command.m_pAnimationTarget.TryGetTarget(out _))
						{
							// TODO Uno: PointerDownThemeAnimation/PointerUpThemeAnimation are not ported (follow-up A3); no storyboard is created, so the command is disposed below.
						}
					}
				}

				// Clear the current animation to start the new one. It is important
				// to call Stop on the current animation only after we have called
				// Begin on the new one, otherwise the animated value won't be handed
				// off correctly. For this specific case, consider the
				// PointerDownThemeAnimation handing off the animated value to the
				// PointerUpThemeAnimation.
				if (m_pointerPressedAnimation.tpStoryboard is not null)
				{
					ClearAnimation(m_pointerPressedAnimation);
				}

				if (spStoryboard is not null)
				{
					m_pointerPressedAnimation.tpStoryboard = spStoryboard;
					m_pointerPressedAnimation.pCommand = pCommand;
					pCommand = null;
				}
			}
			else
			{
				if (pCommand == m_pointerPressedAnimation.pCommand)
				{
					pCommand = null;
				}

				ClearAnimation(m_pointerPressedAnimation);
			}
		}
		finally
		{
			UnlockLayersForAnimationAndDisposeCommand(pCommand);
		}
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 137-267
	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_ReorderHint command)
	{
		ListViewBaseItemAnimationCommand? pCommand = command;

		try
		{
			if (command.m_isStarting)
			{
				if (m_reorderHintAnimation.tpStoryboard is null)
				{
					var shouldProceed = LockLayersForAnimation(command);

					if (shouldProceed)
					{
						if (command.m_pAnimationTarget.TryGetTarget(out var spTarget))
						{
							var onlySteadyState = command.m_steadyStateOnly;
							Point destinationOffset = new(command.m_offsetX, command.m_offsetY);
							Point startOffset = new(0, 0);
							string? strTargetName = null; // No target name, passing target via ref.

							Storyboard spStoryboard = new();
							var spTimelineCollection = spStoryboard.Children;

							// TAS_DRAGENTER is guarded by __if_exists and is not defined in vsanimation.h, so only DRAGBETWEENENTER runs.
							ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_DRAGBETWEENENTER, ThemeGenerator.TA_DRAGBETWEENENTER_AFFECTED, strTargetName, spTarget, onlySteadyState, startOffset, destinationOffset, spTimelineCollection);

							spStoryboard.Begin();

							m_reorderHintAnimation.tpStoryboard = spStoryboard;
							m_reorderHintAnimation.pCommand = pCommand;
							pCommand = null;
						}
					}
				}
			}
			else
			{
				if (m_reorderHintAnimation.tpStoryboard is not null)
				{
					var shouldProceed = LockLayersForAnimation(command);

					if (shouldProceed)
					{
						if (command.m_pAnimationTarget.TryGetTarget(out var spTarget))
						{
							var onlySteadyState = command.m_steadyStateOnly;

							if (onlySteadyState)
							{
								// Usually we'd create a blank animation even for steady state only, but in this case we want to clear out the
								// animation immediately as we're about to create a new one in the next command.

								if (pCommand == m_reorderHintAnimation.pCommand)
								{
									pCommand = null;
								}

								ClearAnimation(m_reorderHintAnimation);
							}
							else
							{
								Point startOffset = new(command.m_offsetX, command.m_offsetY);
								Point destinationOffset = new(0, 0);
								string? strTargetName = null; // No target name, passing target via ref.

								Storyboard spStoryboard = new();
								var spTimelineCollection = spStoryboard.Children;

								ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_DRAGBETWEENLEAVE, ThemeGenerator.TA_DRAGBETWEENLEAVE_AFFECTED, strTargetName, spTarget, onlySteadyState, startOffset, destinationOffset, spTimelineCollection);

								// When this animation completes, we don't need to lock the layers anymore. So, we add a handler
								// to unlock the layers at the correct time. Not doing so would unnecessarily block other visual
								// states from working.
								spStoryboard.Completed += OnReorderHintReturnCompleted;

								spStoryboard.Begin();

								ClearAnimation(m_reorderHintAnimation);
								m_reorderHintAnimation.tpStoryboard = spStoryboard;
								m_reorderHintAnimation.pCommand = pCommand;
								pCommand = null;
							}
						}
					}
				}
				else
				{
					if (pCommand == m_reorderHintAnimation.pCommand)
					{
						pCommand = null;
					}

					ClearAnimation(m_reorderHintAnimation);
				}
			}
		}
		finally
		{
			UnlockLayersForAnimationAndDisposeCommand(pCommand);
		}
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 269-284
	private void OnReorderHintReturnCompleted(object? pUnused1, object? pUnused2)
	{
		if (m_reorderHintAnimation.tpStoryboard is not null)
		{
			m_reorderHintAnimation.tpStoryboard.Completed -= OnReorderHintReturnCompleted;
		}

		ClearAnimation(m_reorderHintAnimation);
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 286-459
	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_DragDrop command)
	{
		ListViewBaseItemAnimationCommand? pCommand = command;

		try
		{
			if (command.m_isStarting)
			{
				if (m_dragDropAnimation.tpStoryboard is null)
				{
					var shouldProceed = LockLayersForAnimation(command);

					if (shouldProceed)
					{
						command.m_pBaseAnimationTarget.TryGetTarget(out var spBaseTarget);
						command.m_pFadeOutAnimationTarget.TryGetTarget(out var spFadeTarget);

						if (spFadeTarget is not null && spBaseTarget is not null)
						{

							var onlySteadyState = command.m_steadyStateOnly;
							Point destinationOffset = new(0, 0);
							Point startOffset = new(0, 0);
							string? strTargetName = null; // No target name, passing target via ref.

							Storyboard spStoryboard = new();
							var spTimelineCollection = spStoryboard.Children;

							switch (command.m_state)
							{
								case ListViewBaseItemAnimationCommand_DragDrop.DragDropState.SinglePrimary:
									{
										ThemeGenerator.AddTimelinesForThemeAnimation(
											ThemeGenerator.TAS_DRAGSOURCESTART,
											ThemeGenerator.TA_DRAGSOURCESTART_DRAGSOURCE,
											strTargetName,
											spBaseTarget,
											onlySteadyState,
											startOffset,
											destinationOffset,
											spTimelineCollection);
										ThemeGenerator.AddTimelinesForThemeAnimation(
											ThemeGenerator.TAS_FADEOUT,
											ThemeGenerator.TA_FADEOUT_HIDDEN,
											strTargetName,
											spFadeTarget,
											onlySteadyState,
											startOffset,
											destinationOffset,
											spTimelineCollection);
										break;
									}
								case ListViewBaseItemAnimationCommand_DragDrop.DragDropState.MultiPrimary:
									{
										ThemeGenerator.AddTimelinesForThemeAnimation(
											ThemeGenerator.TAS_DRAGSOURCESTART,
											ThemeGenerator.TA_DRAGSOURCESTART_DRAGSOURCE,
											strTargetName,
											spBaseTarget,
											onlySteadyState,
											startOffset,
											destinationOffset,
											spTimelineCollection);
										ThemeGenerator.AddTimelinesForThemeAnimation(
											ThemeGenerator.TAS_FADEOUT,
											ThemeGenerator.TA_FADEOUT_HIDDEN,
											strTargetName,
											spFadeTarget,
											onlySteadyState,
											startOffset,
											destinationOffset,
											spTimelineCollection);
										break;
									}
								case ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderingSinglePrimary:
								case ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderingMultiPrimary:
									{
										// Query Reorder item's opacity from resource dictionary
										double reorderOpacity = 1;
										GetValueFromThemeResources("ListViewItemReorderThemeOpacity", ref reorderOpacity);

										ThemeGeneratorHelper theme = new(startOffset, destinationOffset, strTargetName, spBaseTarget, onlySteadyState, spTimelineCollection);
										theme.Initialize();
										TimingFunctionDescription easing = new();
										theme.RegisterKeyFrame(theme.GetOpacityPropertyName(), reorderOpacity, 0, REORDER_ANIMATION_DURATION_MSEC, easing);
										ThemeGenerator.AddTimelinesForThemeAnimation(
											ThemeGenerator.TAS_FADEOUT,
											ThemeGenerator.TA_FADEOUT_HIDDEN,
											strTargetName,
											spFadeTarget,
											onlySteadyState,
											startOffset,
											destinationOffset,
											spTimelineCollection);
										break;
									}
								case ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderingTarget:
									{
										// Query ReorderTarget's opacity and scale from resource dictionary
										double reorderTargetOpacity = 1;
										GetValueFromThemeResources("ListViewItemReorderTargetThemeOpacity", ref reorderTargetOpacity);
										double reorderTargetScale = 1;
										GetValueFromThemeResources("ListViewItemReorderTargetThemeScale", ref reorderTargetScale);

										ThemeGeneratorHelper theme = new(startOffset, destinationOffset, strTargetName, spBaseTarget, onlySteadyState, spTimelineCollection);
										theme.Initialize();
										TimingFunctionDescription easing = new();
										theme.RegisterKeyFrame(theme.GetOpacityPropertyName(), reorderTargetOpacity, 0, REORDER_ANIMATION_DURATION_MSEC, easing);
										theme.Set2DTransformOriginValues(new Point(0.5f, 0.5f));
										theme.RegisterKeyFrame(theme.GetScaleXPropertyName(), reorderTargetScale, 0, REORDER_ANIMATION_DURATION_MSEC, easing);
										theme.RegisterKeyFrame(theme.GetScaleYPropertyName(), reorderTargetScale, 0, REORDER_ANIMATION_DURATION_MSEC, easing);
										break;
									}
								case ListViewBaseItemAnimationCommand_DragDrop.DragDropState.ReorderedPlaceholder:
									{
										ThemeGenerator.AddTimelinesForThemeAnimation(
											ThemeGenerator.TAS_FADEOUT,
											ThemeGenerator.TA_FADEOUT_HIDDEN,
											strTargetName,
											spFadeTarget,
											onlySteadyState,
											startOffset,
											destinationOffset,
											spTimelineCollection);
										break;
									}
								case ListViewBaseItemAnimationCommand_DragDrop.DragDropState.DragOver:
									{
										ThemeGenerator.AddTimelinesForThemeAnimation(
											ThemeGenerator.TAS_DRAGSOURCESTART,
											ThemeGenerator.TA_DRAGSOURCESTART_AFFECTED,
											strTargetName,
											spBaseTarget,
											onlySteadyState,
											startOffset,
											destinationOffset,
											spTimelineCollection);
										break;
									}
							}

							spStoryboard.Begin();

							m_dragDropAnimation.tpStoryboard = spStoryboard;
							m_dragDropAnimation.pCommand = pCommand;
							pCommand = null;
						}
					}
				}
			}
			else
			{
				if (pCommand == m_dragDropAnimation.pCommand)
				{
					pCommand = null;
				}

				ClearAnimation(m_dragDropAnimation);
			}
		}
		finally
		{
			UnlockLayersForAnimationAndDisposeCommand(pCommand);
		}
	}

	// TODO Uno: MultiSelect, IndicatorSelect and SelectionIndicatorVisibility visitors and their Completed handlers (lines 461-1065) are ported by P2; until then they dispose the command.
	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_MultiSelect command)
		=> UnlockLayersForAnimationAndDisposeCommand(command);

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_IndicatorSelect command)
		=> UnlockLayersForAnimationAndDisposeCommand(command);

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility command)
		=> UnlockLayersForAnimationAndDisposeCommand(command);

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 1067-1085
	private void ClearAnimation(AnimationState pAnimation)
	{
		if (pAnimation.tpStoryboard is not null)
		{
			UnlockLayersForAnimationAndDisposeCommand(pAnimation.pCommand);
			pAnimation.pCommand = null;

			pAnimation.tpStoryboard.Stop();
			pAnimation.tpStoryboard = null;
		}
	}

	// Gets the value of a resource by querying the ThemeResource dictionary
	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 1087-1113
	private void GetValueFromThemeResources(string resourceKey, ref double pValue)
	{
		var resources = Resources;

		// Only the presenter's own dictionary (with its merged/theme dictionaries), as the WinUI IMap lookup.
		if (resources.TryGetValue(resourceKey, out var boxedResource, shouldCheckSystem: false))
		{
			// WinUI fails on a null or non-double resource (IFCPTR / IReference<double> cast).
			pValue = (double)boxedResource!;
		}
	}
}
