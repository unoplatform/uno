// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemPresenter_Partial.cpp, tag winui3/release/2.5.1

#nullable enable

using System.Collections.Generic;
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

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 461-679
	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_MultiSelect command)
	{
		ListViewBaseItemAnimationCommand? pCommand = command;
		ThemeGeneratorHelper pSupplier;

		try
		{
			if (command.m_isStarting)
			{
				// if an animation was already playing, we clear it
				// not clearing will cause the following cases

				// Case 1: Single -> Multiple -> Single
				// Switching to multiple creates the CheckBox and starts the animation from left to right to push the content.
				// Switching to single will not do anything since we will still be animating to multiple.
				// The single animation is responsible for deleting the CheckBox at the end of its animation.
				// Not playing the single animation will leave in Single mode with a CheckBox on top of the content.

				// Case 2: Multiple -> Single -> Single
				// Switching to single animates to the left and delete the CheckBox at the end.
				// Switching for multiple will not do anything since the Single animation is still playing.
				// At the end of the single animation, the CheckBox will be deleted.
				// Current state will be MultiSelect mode without a CheckBox which might cause a crash in certain cases where
				// we make the assumption that a CheckBox has been created.
				if (m_multiSelectAnimation.tpStoryboard is not null)
				{
					m_multiSelectAnimation.tpStoryboard.Completed -= OnMultiSelectCompleted;

					ClearAnimation(m_multiSelectAnimation);
				}

				// Create the animation and its storyboard
				{
					var shouldProceed = LockLayersForAnimation(command);

					if (shouldProceed)
					{
						Point nullPoint = default;
						long animationDuration; // in milliseconds
						IList<Timeline>? spTimelineCollection = null;
						Storyboard? spStoryboard = null;
						TimingFunctionDescription easing = new();

						if (command.m_isRoundedListViewBaseItemChromeEnabled)
						{
							// pCommand->m_contentTranslationX is 28px for None/Multiple transitions, and 19px for Single/Multiple & Extended/Multiple transitions.
							if (command.m_entering)
							{
								// Using 250ms for 28px and 169ms for 19px.
								animationDuration = (long)(250 * command.m_contentTranslationX / 28);
								easing.cp2.X = 0.0f;
								easing.cp3.X = 0.0f;
							}
							else
							{
								// Using 167ms for 28px and 113ms for 19px.
								animationDuration = (long)(167 * command.m_contentTranslationX / 28);
								easing.cp2.X = 1.0f;
								easing.cp3.X = 1.0f;
							}
							easing.cp2.Y = 0.0f;
						}
						else
						{
							animationDuration = 333;
							easing.cp2.X = 0.1f;
							easing.cp2.Y = 0.9f;
							easing.cp3.X = 0.2f;
						}
						easing.cp3.Y = 1.0f;

						// checkbox animation
						{
							if (command.m_multiSelectCheckBox.TryGetTarget(out var spTarget))
							{
								spStoryboard = new();

								spTimelineCollection = spStoryboard.Children;

								if (command.m_checkMode == ListViewItemPresenterCheckMode.Inline)
								{
									if (!command.m_steadyStateOnly)
									{
										var checkBoxTranslationValue = command.m_entering ? -command.m_checkBoxTranslationX : command.m_checkBoxTranslationX;

										pSupplier = new(nullPoint, nullPoint, null, spTarget, false, spTimelineCollection);
										pSupplier.Initialize();

										if (command.m_entering)
										{
											pSupplier.RegisterKeyFrame(pSupplier.GetTranslateXPropertyName(), checkBoxTranslationValue, 0, 0, easing);
											pSupplier.RegisterKeyFrame(pSupplier.GetTranslateXPropertyName(), 0, 0, animationDuration, easing);

											pSupplier.RegisterKeyFrame(pSupplier.GetClipTranslateXPropertyName(), -checkBoxTranslationValue, 0, 0, easing);
											pSupplier.RegisterKeyFrame(pSupplier.GetClipTranslateXPropertyName(), 0, 0, animationDuration, easing);
										}
										else
										{
											pSupplier.RegisterKeyFrame(pSupplier.GetTranslateXPropertyName(), 0, 0, 0, easing);
											pSupplier.RegisterKeyFrame(pSupplier.GetTranslateXPropertyName(), -checkBoxTranslationValue, 0, animationDuration, easing);

											pSupplier.RegisterKeyFrame(pSupplier.GetClipTranslateXPropertyName(), 0, 0, 0, easing);
											pSupplier.RegisterKeyFrame(pSupplier.GetClipTranslateXPropertyName(), checkBoxTranslationValue, 0, animationDuration, easing);
										}
									}
								}
								else if (command.m_checkMode == ListViewItemPresenterCheckMode.Overlay)
								{
									if (command.m_entering)
									{
										// FadeIn
										ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_FADEIN, ThemeGenerator.TA_FADEIN_SHOWN, null, spTarget, command.m_steadyStateOnly, nullPoint, nullPoint, spTimelineCollection);
									}
									else
									{
										// FadeOut
										ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_FADEOUT, ThemeGenerator.TA_FADEOUT_HIDDEN, null, spTarget, command.m_steadyStateOnly, nullPoint, nullPoint, spTimelineCollection);
									}
								}
							}
						}

						// content presenter animation
						{
							if (command.m_contentPresenter.TryGetTarget(out var spTarget))
							{
								if (spStoryboard is null)
								{
									spStoryboard = new();
									spTimelineCollection = spStoryboard.Children;
								}

								if (!command.m_steadyStateOnly)
								{
									var contentTranslationValue = command.m_entering ? -command.m_contentTranslationX : command.m_contentTranslationX;

									pSupplier = new(nullPoint, nullPoint, null, spTarget, false, spTimelineCollection!);
									pSupplier.Initialize();

									if (command.m_checkMode == ListViewItemPresenterCheckMode.Inline)
									{
										pSupplier.RegisterKeyFrame(pSupplier.GetTranslateXPropertyName(), contentTranslationValue, 0, 0, easing);
										pSupplier.RegisterKeyFrame(pSupplier.GetTranslateXPropertyName(), 0, 0, animationDuration, easing);
									}
								}
							}
						}

						if (spStoryboard is not null)
						{
							// When this animation completes, we clear the animation so we can use it again
							spStoryboard.Completed += OnMultiSelectCompleted;

							spStoryboard.Begin();

							m_multiSelectAnimation.tpStoryboard = spStoryboard;
							m_multiSelectAnimation.pCommand = pCommand;
							pCommand = null;
						}
					}
				}
			}
			else
			{
				if (pCommand == m_multiSelectAnimation.pCommand)
				{
					pCommand = null;
				}

				ClearAnimation(m_multiSelectAnimation);
			}
		}
		finally
		{
			UnlockLayersForAnimationAndDisposeCommand(pCommand);
		}
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 681-707
	private void OnMultiSelectCompleted(object? pUnused1, object? pUnused2)
	{
		if (m_multiSelectAnimation.pCommand is null ||
			!((ListViewBaseItemAnimationCommand_MultiSelect)m_multiSelectAnimation.pCommand).m_entering)
		{
			RemoveMultiSelectCheckBox();
		}

		if (m_multiSelectAnimation.tpStoryboard is not null)
		{
			m_multiSelectAnimation.tpStoryboard.Completed -= OnMultiSelectCompleted;
		}

		ClearAnimation(m_multiSelectAnimation);
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 709-865
	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_IndicatorSelect command)
	{
		ListViewBaseItemAnimationCommand? pCommand = command;
		ThemeGeneratorHelper supplier;

		try
		{
			if (command.m_isStarting)
			{
				// Create the animation and its storyboard
				{
					var shouldProceed = LockLayersForAnimation(command);

					if (shouldProceed)
					{
						var translationValue = command.m_entering ? -command.m_translationX : command.m_translationX;
						Point nullPoint = default;
						const long animationDuration = 83; // in milliseconds
						IList<Timeline>? timelineCollection = null;
						Storyboard? storyboard = null;
						TimingFunctionDescription easing = new();

						if (command.m_entering)
						{
							easing.cp2.X = 0.0f;
							easing.cp3.X = 0.0f;
						}
						else
						{
							easing.cp2.X = 1.0f;
							easing.cp3.X = 1.0f;
						}
						easing.cp2.Y = 0.0f;
						easing.cp3.Y = 1.0f;

						// Selection indicator animation
						{
							if (command.m_selectionIndicator.TryGetTarget(out var target))
							{
								storyboard = new();

								timelineCollection = storyboard.Children;

								if (command.m_selectionIndicatorMode == ListViewItemPresenterSelectionIndicatorMode.Inline)
								{
									if (!command.m_steadyStateOnly)
									{
										supplier = new(nullPoint, nullPoint, null, target, false, timelineCollection);
										supplier.Initialize();

										if (command.m_entering)
										{
											supplier.RegisterKeyFrame(supplier.GetTranslateXPropertyName(), translationValue, 0, 0, easing);
											supplier.RegisterKeyFrame(supplier.GetTranslateXPropertyName(), 0, 0, animationDuration, easing);

											supplier.RegisterKeyFrame(supplier.GetClipTranslateXPropertyName(), -translationValue, 0, 0, easing);
											supplier.RegisterKeyFrame(supplier.GetClipTranslateXPropertyName(), 0, 0, animationDuration, easing);
										}
										else
										{
											supplier.RegisterKeyFrame(supplier.GetTranslateXPropertyName(), 0, 0, 0, easing);
											supplier.RegisterKeyFrame(supplier.GetTranslateXPropertyName(), -translationValue, 0, animationDuration, easing);

											supplier.RegisterKeyFrame(supplier.GetClipTranslateXPropertyName(), 0, 0, 0, easing);
											supplier.RegisterKeyFrame(supplier.GetClipTranslateXPropertyName(), translationValue, 0, animationDuration, easing);
										}
									}
								}
							}
						}

						// content presenter animation
						{
							if (command.m_contentPresenter.TryGetTarget(out var target))
							{
								if (storyboard is null)
								{
									storyboard = new();
									timelineCollection = storyboard.Children;
								}

								if (!command.m_steadyStateOnly)
								{
									supplier = new(nullPoint, nullPoint, null, target, false, timelineCollection!);
									supplier.Initialize();

									if (command.m_selectionIndicatorMode == ListViewItemPresenterSelectionIndicatorMode.Inline)
									{
										supplier.RegisterKeyFrame(supplier.GetTranslateXPropertyName(), translationValue, 0, 0, easing);
										supplier.RegisterKeyFrame(supplier.GetTranslateXPropertyName(), 0, 0, animationDuration, easing);
									}
								}
							}
						}

						if (storyboard is not null)
						{
							// When this animation completes, we clear the animation so we can use it again
							storyboard.Completed += OnIndicatorSelectCompleted;

							storyboard.Begin();

							m_indicatorSelectAnimation.tpStoryboard = storyboard;
							m_indicatorSelectAnimation.pCommand = pCommand;
							pCommand = null;
						}
					}
				}
			}
			else
			{
				if (pCommand == m_indicatorSelectAnimation.pCommand)
				{
					pCommand = null;
				}

				ClearAnimation(m_indicatorSelectAnimation);
			}
		}
		finally
		{
			UnlockLayersForAnimationAndDisposeCommand(pCommand);
		}
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 867-886
	private void OnIndicatorSelectCompleted(object? unused1, object? unused2)
	{
		if (m_indicatorSelectAnimation.tpStoryboard is not null)
		{
			m_indicatorSelectAnimation.tpStoryboard.Completed -= OnIndicatorSelectCompleted;
		}

		ClearAnimation(m_indicatorSelectAnimation);
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 888-1033
	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility command)
	{
		ListViewBaseItemAnimationCommand? pCommand = command;

		try
		{
			if (command.m_isStarting)
			{
				var isAnimating = m_selectionIndicatorAnimation.pCommand is not null;

				// Create the animation and its storyboard
				var shouldProceed = LockLayersForAnimation(command);

				if (shouldProceed)
				{
					Storyboard? storyboard = null;

					// Selection indicator animation
					{
						if (command.m_selectionIndicator.TryGetTarget(out var target))
						{
							storyboard = new();

							var timelineCollection = storyboard.Children;

							Point nullPoint = default;
							ThemeGeneratorHelper supplier;

							supplier = new(nullPoint /*startOffset*/, nullPoint /*destinationOffset*/, null /*targetName*/, target, command.m_steadyStateOnly, timelineCollection);
							supplier.Initialize();

							const long opacityAnimationDuration = 83; // in milliseconds
							TimingFunctionDescription easingOpacity = new();

							// Opacity animation to fade in or out
							if (command.m_selected && !isAnimating)
							{
								// Start from 0.0 opacity unless there is already an animation
								supplier.RegisterKeyFrame(supplier.GetOpacityPropertyName(), 0.0, 0, 0, easingOpacity);
							}

							supplier.RegisterKeyFrame(supplier.GetOpacityPropertyName(), command.m_selected ? 1.0 : 0.0, 0, opacityAnimationDuration, easingOpacity);

							if (command.m_selected)
							{
								const long scaleAnimationDuration = 167; // in milliseconds
								TimingFunctionDescription easingScale = new();

								if (command.m_fromScale != 0.0)
								{
									easingScale.cp2.X = 0.0f;
									easingScale.cp2.Y = 0.0f;
								}
								else
								{
									easingScale.cp2.X = 0.167f;
									easingScale.cp2.Y = 0.167f;
								}
								easingScale.cp3.X = 0.0f;
								easingScale.cp3.Y = 1.0f;

								supplier.Set2DTransformOriginValues(new Point(0.5f, 0.5f));

								if (!isAnimating)
								{
									supplier.RegisterKeyFrame(supplier.GetScaleYPropertyName(), command.m_fromScale, 0, 0, easingScale);
								}

								supplier.RegisterKeyFrame(supplier.GetScaleYPropertyName(), 1.0, 0, scaleAnimationDuration, easingScale);
							}
						}
					}

					if (storyboard is not null)
					{
						if (m_selectionIndicatorAnimation.tpStoryboard is not null)
						{
							global::System.Diagnostics.Debug.Assert(isAnimating);
							// Unhook completion event for ongoing animation
							m_selectionIndicatorAnimation.tpStoryboard.Completed -= OnSelectionIndicatorCompleted;

							ClearAnimation(m_selectionIndicatorAnimation);
						}

						// When this animation completes, we clear the animation so we can use it again
						storyboard.Completed += OnSelectionIndicatorCompleted;

						storyboard.Begin();

						m_selectionIndicatorAnimation.tpStoryboard = storyboard;
						m_selectionIndicatorAnimation.pCommand = pCommand;
						pCommand = null;
					}
				}
			}
			else
			{
				if (pCommand == m_selectionIndicatorAnimation.pCommand)
				{
					pCommand = null;
				}

				ClearAnimation(m_selectionIndicatorAnimation);
			}
		}
		finally
		{
			UnlockLayersForAnimationAndDisposeCommand(pCommand);
		}
	}

	// MUX Reference ListViewBaseItemPresenter_Partial.cpp, lines 1035-1065
	private void OnSelectionIndicatorCompleted(object? unused1, object? unused2)
	{
		if (m_selectionIndicatorAnimation.pCommand is null ||
			!((ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility)m_selectionIndicatorAnimation.pCommand).m_selected)
		{
			RemoveSelectionIndicator();
		}

		if (m_selectionIndicatorAnimation.tpStoryboard is not null)
		{
			m_selectionIndicatorAnimation.tpStoryboard.Completed -= OnSelectionIndicatorCompleted;
		}

		ClearAnimation(m_selectionIndicatorAnimation);
	}

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
