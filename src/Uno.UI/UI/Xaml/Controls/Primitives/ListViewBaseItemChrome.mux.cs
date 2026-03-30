// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/1.4.2

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemChrome
{
	/// <summary>
	/// Main entry point for visual state transitions.
	/// Called when the VisualStateManager requests a state change.
	/// MUX Reference: CListViewBaseItemChrome::GoToChromedState
	/// </summary>
	internal bool GoToChromedState(string stateName, bool useTransitions)
	{
		bool wentToState = false;
		bool dirty = false;
		bool needsMeasure = false;
		bool needsArrange = false;
		bool disabledChanged = false;

		var templateChild = GetTemplateChildIfExists();
		var isRoundedChromeEnabled = IsRoundedListViewBaseItemChromeEnabled;
		var oldCommonState2 = _visualStates.commonState2;
		var selectionIndicatorContentOffset = ListViewBaseItemChromeConstants.SelectionIndicatorMargin.Left +
			ListViewBaseItemChromeConstants.SelectionIndicatorSize.Width +
			ListViewBaseItemChromeConstants.SelectionIndicatorMargin.Right;

		// Try CommonStates2 first (new style)
		if (UpdateVisualStateGroup(stateName, ref _visualStates.commonState2, out wentToState))
		{
			ProcessCommonStates2Change(oldCommonState2, useTransitions, templateChild, isRoundedChromeEnabled, selectionIndicatorContentOffset, ref dirty, ref needsArrange);
		}

		// Try DisabledStates
		if (!wentToState && UpdateVisualStateGroup(stateName, ref _visualStates.disabledState, out wentToState))
		{
			ProcessDisabledStateChange(isRoundedChromeEnabled, ref dirty);
			disabledChanged = true;
		}

		// Try FocusStates
		if (!wentToState && UpdateVisualStateGroup(stateName, ref _visualStates.focusState, out wentToState))
		{
			dirty = true;
		}

		// Try MultiSelectStates
		if (!wentToState && UpdateVisualStateGroup(stateName, ref _visualStates.multiSelectState, out wentToState))
		{
			ProcessMultiSelectStateChange(useTransitions, templateChild, isRoundedChromeEnabled, selectionIndicatorContentOffset, ref dirty, ref needsMeasure, ref needsArrange);
		}

		// Try SelectionIndicatorStates
		if (!wentToState && UpdateVisualStateGroup(stateName, ref _visualStates.selectionIndicatorState, out wentToState))
		{
			ProcessSelectionIndicatorStateChange(useTransitions, templateChild, selectionIndicatorContentOffset, ref dirty, ref needsMeasure, ref needsArrange);
		}

		// Try ReorderHintStates
		var oldReorderHintState = _visualStates.reorderHintState;
		if (!wentToState && UpdateVisualStateGroup(stateName, ref _visualStates.reorderHintState, out wentToState))
		{
			ProcessReorderHintStateChange(oldReorderHintState, useTransitions, ref dirty);
		}

		// Try DragStates
		var oldDragState = _visualStates.dragState;
		if (!wentToState && UpdateVisualStateGroup(stateName, ref _visualStates.dragState, out wentToState))
		{
			ProcessDragStateChange(oldDragState, useTransitions, ref dirty);
		}

		// Try DataVirtualizationStates
		if (!wentToState && UpdateVisualStateGroup(stateName, ref _visualStates.dataVirtualizationState, out wentToState))
		{
			dirty = true;
		}

		// Handle disabled opacity change
		if (disabledChanged)
		{
			ApplyDisabledOpacity(templateChild, isRoundedChromeEnabled);
		}

		// Invalidate as needed
		if (needsMeasure)
		{
			_owner.InvalidateMeasure();
		}
		if (needsArrange)
		{
			_owner.InvalidateArrange();
		}
		if (dirty)
		{
			InvalidateRender();
		}

		return wentToState;
	}

	/// <summary>
	/// Processes pending animation commands from the chrome.
	/// Called by the presenter after GoToChromedState.
	/// MUX Reference: ListViewBaseItemPresenter::ProcessAnimationCommands (DXAML layer)
	/// </summary>
	internal void ProcessAnimationCommands(IListViewBaseItemAnimationCommandVisitor visitor)
	{
		// Process all pending animation commands
		while (true)
		{
			var command = GetNextPendingAnimation();
			if (command == null)
			{
				break;
			}

			// Check if this animation should proceed
			if (LockLayersForAnimation(command))
			{
				// Let the visitor process the command
				command.Accept(visitor);
			}

			// Unlock and dispose
			UnlockLayersForAnimationAndDisposeCommand(command);
		}
	}

	private void ProcessCommonStates2Change(
		CommonStates2 oldState,
		bool useTransitions,
		UIElement? templateChild,
		bool isRoundedChromeEnabled,
		double selectionIndicatorContentOffset,
		ref bool dirty,
		ref bool needsArrange)
	{
		var selected = _visualStates.HasState(CommonStates2.Selected) ||
					   _visualStates.HasState(CommonStates2.PressedSelected) ||
					   _visualStates.HasState(CommonStates2.PointerOverSelected);

		var roundedGridViewItem = isRoundedChromeEnabled && IsChromeForGridViewItem;

		// Handle selected state changes
		if (selected)
		{
			if (_multiSelectCheckGlyph != null)
			{
				_multiSelectCheckGlyph.Opacity = 1.0;
			}

			if (roundedGridViewItem)
			{
				EnsureOuterBorder();
				EnsureInnerSelectionBorder();
			}
		}
		else
		{
			if (_multiSelectCheckGlyph != null)
			{
				_multiSelectCheckGlyph.Opacity = 0.0;
			}

			if (roundedGridViewItem)
			{
				RemoveInnerSelectionBorder();
				var pointerOver = _visualStates.HasState(CommonStates2.PointerOver);
				var disabled = _visualStates.HasState(DisabledStates.Disabled);

				if (!disabled && pointerOver)
				{
					EnsureOuterBorder();
				}
				else
				{
					RemoveOuterBorder();
				}
			}
		}

		// Handle checkbox background updates
		if (_multiSelectCheckBoxRectangle != null)
		{
			SetMultiSelectCheckBoxBackground();
			if (isRoundedChromeEnabled)
			{
				SetMultiSelectCheckBoxBorder();
			}
		}

		// Handle selection indicator
		var isInSelectionIndicatorMode = IsInSelectionIndicatorMode();
		if (isInSelectionIndicatorMode || _selectionIndicatorRectangle != null)
		{
			ProcessSelectionIndicatorVisibilityChange(oldState, selected, useTransitions, ref needsArrange);
		}

		// Handle pressed animations
		if (!isRoundedChromeEnabled && templateChild != null)
		{
			ProcessPressedAnimations(oldState, useTransitions, templateChild);
		}

		// Update backplate background
		if (_backplateRectangle != null && isRoundedChromeEnabled)
		{
			SetBackplateBackground();
		}

		SetForegroundBrush();
		dirty = true;
	}

	private void ProcessDisabledStateChange(bool isRoundedChromeEnabled, ref bool dirty)
	{
		var roundedGridViewItem = isRoundedChromeEnabled && IsChromeForGridViewItem;
		var disabled = _visualStates.HasState(DisabledStates.Disabled);
		var pointerOver = _visualStates.HasState(CommonStates2.PointerOver);
		var selected = _visualStates.HasState(CommonStates2.Selected) ||
					   _visualStates.HasState(CommonStates2.PressedSelected) ||
					   _visualStates.HasState(CommonStates2.PointerOverSelected);

		if (roundedGridViewItem)
		{
			if (selected || (!disabled && pointerOver))
			{
				EnsureOuterBorder();
			}
			else
			{
				RemoveOuterBorder();
			}

			if (selected)
			{
				EnsureInnerSelectionBorder();
			}
			else
			{
				RemoveInnerSelectionBorder();
			}
		}

		if (isRoundedChromeEnabled)
		{
			SetBackplateBackground();
			if (_multiSelectCheckGlyph != null)
			{
				SetMultiSelectCheckBoxForeground();
			}
			if (_multiSelectCheckBoxRectangle != null)
			{
				SetMultiSelectCheckBoxBackground();
				SetMultiSelectCheckBoxBorder();
			}
			if (_selectionIndicatorRectangle != null)
			{
				SetSelectionIndicatorBackground();
			}
		}

		dirty = true;
	}

	private void ProcessMultiSelectStateChange(
		bool useTransitions,
		UIElement? templateChild,
		bool isRoundedChromeEnabled,
		double selectionIndicatorContentOffset,
		ref bool dirty,
		ref bool needsMeasure,
		ref bool needsArrange)
	{
		var entering = _visualStates.HasState(MultiSelectStates.MultiSelectEnabled);
		double contentTranslationX = isRoundedChromeEnabled ?
			ListViewBaseItemChromeConstants.MultiSelectRoundedContentOffset :
			ListViewBaseItemChromeConstants.ListViewItemMultiSelectContentOffset;

		if (entering)
		{
			if (_isInIndicatorSelect)
			{
				contentTranslationX -= selectionIndicatorContentOffset;
			}
			_isInIndicatorSelect = false;
			_isInMultiSelect = true;
			EnsureMultiSelectCheckBox();
		}
		else if (IsInSelectionIndicatorMode())
		{
			contentTranslationX -= selectionIndicatorContentOffset;
		}

		// Enqueue multi-select animation
		var command = new ListViewBaseItemAnimationCommand_MultiSelect(
			entering,
			_checkMode == ListViewItemPresenterCheckMode.Inline,
			_multiSelectCheckBoxRectangle != null ? new WeakReference<UIElement>(_multiSelectCheckBoxRectangle) : null,
			templateChild != null ? new WeakReference<UIElement>(templateChild) : null,
			isStarting: true,
			steadyStateOnly: !useTransitions);
		EnqueueAnimationCommand(command);

		needsMeasure = true;
		needsArrange = true;
		dirty = true;
	}

	private void ProcessSelectionIndicatorStateChange(
		bool useTransitions,
		UIElement? templateChild,
		double selectionIndicatorContentOffset,
		ref bool dirty,
		ref bool needsMeasure,
		ref bool needsArrange)
	{
		var entering = _visualStates.HasState(SelectionIndicatorStates.SelectionIndicatorEnabled);
		var enqueueAnimationCommand = !_isInMultiSelect;

		if (entering)
		{
			_isInIndicatorSelect = true;
			_isInMultiSelect = false;
		}
		else if (_visualStates.HasState(MultiSelectStates.MultiSelectDisabled))
		{
			_isInIndicatorSelect = false;
			_isInMultiSelect = false;
		}

		if (enqueueAnimationCommand)
		{
			var command = new ListViewBaseItemAnimationCommand_IndicatorSelect(
				entering,
				_selectionIndicatorRectangle != null ? new WeakReference<UIElement>(_selectionIndicatorRectangle) : null,
				templateChild != null ? new WeakReference<UIElement>(templateChild) : null,
				isStarting: true,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}

		needsMeasure = true;
		needsArrange = true;
		dirty = true;
	}

	private void ProcessReorderHintStateChange(ReorderHintStates oldState, bool useTransitions, ref bool dirty)
	{
		if (_visualStates.HasState(ReorderHintStates.NoReorderHint))
		{
			var offset = ComputeReorderHintOffset(oldState);
			var command = new ListViewBaseItemAnimationCommand_ReorderHint(
				(float)offset.X, (float)offset.Y,
				new WeakReference<UIElement>(_owner),
				isStarting: false,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}
		else
		{
			if (oldState != ReorderHintStates.NoReorderHint)
			{
				// Clear old hint first
				var offset = ComputeReorderHintOffset(oldState);
				var clearCommand = new ListViewBaseItemAnimationCommand_ReorderHint(
					(float)offset.X, (float)offset.Y,
					new WeakReference<UIElement>(_owner),
					isStarting: false,
					steadyStateOnly: true);
				EnqueueAnimationCommand(clearCommand);
			}

			var newOffset = ComputeReorderHintOffset(_visualStates.reorderHintState);
			var command = new ListViewBaseItemAnimationCommand_ReorderHint(
				(float)newOffset.X, (float)newOffset.Y,
				new WeakReference<UIElement>(_owner),
				isStarting: true,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}
		dirty = true;
	}

	private void ProcessDragStateChange(DragStates oldState, bool useTransitions, ref bool dirty)
	{
		bool dragCountTextBlockVisible = false;

		if (_visualStates.HasState(DragStates.NotDragging))
		{
			var command = new ListViewBaseItemAnimationCommand_DragDrop(
				DragStates.NotDragging,
				new WeakReference<UIElement>(_owner),
				null, null, null,
				isStarting: false,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}
		else
		{
			// Ensure secondary chrome for drag operations
			AddSecondaryChrome();

			if (_visualStates.HasState(DragStates.MultipleDraggingPrimary) ||
				_visualStates.HasState(DragStates.MultipleReorderingPrimary))
			{
				dragCountTextBlockVisible = true;
			}

			// Handle placeholder states
			if (_visualStates.HasState(DragStates.DraggedPlaceholder) ||
				_visualStates.HasState(DragStates.ReorderedPlaceholder))
			{
				if (_visualStates.HasState(MultiSelectStates.MultiSelectEnabled))
				{
					EnsureMultiSelectCheckBox();
				}
				else
				{
					RemoveMultiSelectCheckBox();
				}
			}

			// Clear old state if needed
			if (oldState != DragStates.NotDragging)
			{
				var clearCommand = new ListViewBaseItemAnimationCommand_DragDrop(
					_visualStates.dragState,
					new WeakReference<UIElement>(_owner),
					_secondaryChrome != null ? new WeakReference<UIElement>(_secondaryChrome) : null,
					null, null,
					isStarting: false,
					steadyStateOnly: true);
				EnqueueAnimationCommand(clearCommand);
			}

			var command = new ListViewBaseItemAnimationCommand_DragDrop(
				_visualStates.dragState,
				new WeakReference<UIElement>(_owner),
				_secondaryChrome != null ? new WeakReference<UIElement>(_secondaryChrome) : null,
				null, null,
				isStarting: true,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}

		SetDragOverlayTextBlockVisible(dragCountTextBlockVisible);
		dirty = true;
	}

	private void ProcessSelectionIndicatorVisibilityChange(
		CommonStates2 oldState,
		bool selected,
		bool useTransitions,
		ref bool needsArrange)
	{
		var isInSelectionIndicatorMode = IsInSelectionIndicatorMode();
		var oldPressed = oldState == CommonStates2.Pressed || oldState == CommonStates2.PressedSelected;
		var pressed = _visualStates.HasState(CommonStates2.Pressed) || _visualStates.HasState(CommonStates2.PressedSelected);

		bool updateSelectionIndicatorVisibility = true;
		bool showSelectionIndicator = true;
		float fromScale = 0.0f;

		if (isInSelectionIndicatorMode)
		{
			var oldSelected = oldState == CommonStates2.Selected ||
							  oldState == CommonStates2.PressedSelected ||
							  oldState == CommonStates2.PointerOverSelected;
			updateSelectionIndicatorVisibility = oldSelected != selected;
			showSelectionIndicator = selected;
		}
		else
		{
			showSelectionIndicator = false;
		}

		if (updateSelectionIndicatorVisibility)
		{
			if (showSelectionIndicator)
			{
				EnsureSelectionIndicator();
			}

			var command = new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(
				showSelectionIndicator,
				fromScale,
				_selectionIndicatorRectangle != null ? new WeakReference<UIElement>(_selectionIndicatorRectangle) : null,
				isStarting: true,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}
		else if (_selectionIndicatorRectangle != null && showSelectionIndicator && oldPressed != pressed)
		{
			// Handle pressed shrink/expand
			var currentHeight = _selectionIndicatorRectangle.ActualHeight;
			if (pressed && currentHeight <= ListViewBaseItemChromeConstants.SelectionIndicatorHeightShrinkage)
			{
				fromScale = 1.0f;
			}
			else
			{
				fromScale = pressed
					? (float)(currentHeight / (currentHeight - ListViewBaseItemChromeConstants.SelectionIndicatorHeightShrinkage))
					: (float)(currentHeight / (currentHeight + ListViewBaseItemChromeConstants.SelectionIndicatorHeightShrinkage));
			}

			var command = new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(
				true,
				fromScale,
				new WeakReference<UIElement>(_selectionIndicatorRectangle),
				isStarting: true,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}

		if (showSelectionIndicator)
		{
			SetSelectionIndicatorBackground();
		}

		if (oldPressed != pressed)
		{
			needsArrange = true;
		}
	}

	private void ProcessPressedAnimations(CommonStates2 oldState, bool useTransitions, UIElement templateChild)
	{
		if (_visualStates.HasState(CommonStates2.Pressed) || _visualStates.HasState(CommonStates2.PressedSelected))
		{
			var command = new ListViewBaseItemAnimationCommand_Pressed(
				true,
				new WeakReference<UIElement>(templateChild),
				isStarting: true,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}
		else if (_visualStates.HasState(CommonStates2.Normal) ||
				 _visualStates.HasState(CommonStates2.PointerOver) ||
				 _visualStates.HasState(CommonStates2.Selected) ||
				 _visualStates.HasState(CommonStates2.PointerOverSelected))
		{
			if (oldState == CommonStates2.Pressed || oldState == CommonStates2.PressedSelected)
			{
				var command = new ListViewBaseItemAnimationCommand_Pressed(
					false,
					new WeakReference<UIElement>(templateChild),
					isStarting: true,
					steadyStateOnly: !useTransitions);
				EnqueueAnimationCommand(command);
			}
		}
		else
		{
			var command = new ListViewBaseItemAnimationCommand_Pressed(
				false,
				new WeakReference<UIElement>(templateChild),
				isStarting: false,
				steadyStateOnly: !useTransitions);
			EnqueueAnimationCommand(command);
		}
	}

	private void ApplyDisabledOpacity(UIElement? templateChild, bool isRoundedChromeEnabled)
	{
		var opacity = _visualStates.HasState(DisabledStates.Disabled) ? GetDisabledOpacity() : 1.0;

		if (isRoundedChromeEnabled)
		{
			if (templateChild != null)
			{
				templateChild.Opacity = opacity;
			}
		}
		else
		{
			var parent = GetParentListViewBaseItemNoRef();
			if (parent != null)
			{
				parent.Opacity = opacity;
			}
		}
	}

	private Point ComputeReorderHintOffset(ReorderHintStates state)
	{
		var reorderHintOffset = GetReorderHintOffset();
		return state switch
		{
			ReorderHintStates.BottomReorderHint => new Point(0, reorderHintOffset),
			ReorderHintStates.TopReorderHint => new Point(0, -reorderHintOffset),
			ReorderHintStates.LeftReorderHint => new Point(-reorderHintOffset, 0),
			ReorderHintStates.RightReorderHint => new Point(reorderHintOffset, 0),
			_ => new Point(0, 0)
		};
	}

	/// <summary>
	/// Enqueues an animation command for processing by the presenter.
	/// MUX Reference: CListViewBaseItemChrome::EnqueueAnimationCommand
	/// </summary>
	internal void EnqueueAnimationCommand(ListViewBaseItemAnimationCommand command)
	{
		EnsureTransitionTarget();
		_animationCommands.Add(command);
	}

	/// <summary>
	/// Gets the next pending animation command, or null if none exist.
	/// MUX Reference: CListViewBaseItemChrome::DequeueAnimationCommand / GetNextPendingAnimation
	/// </summary>
	internal ListViewBaseItemAnimationCommand? GetNextPendingAnimation()
	{
		if (_animationCommands.Count == 0)
		{
			return null;
		}

		var command = _animationCommands[0];
		_animationCommands.RemoveAt(0);
		return command;
	}

	/// <summary>
	/// Locks layers for animation and returns whether the animation should proceed.
	/// MUX Reference: CListViewBaseItemChrome::LockLayersForAnimation
	/// </summary>
	internal bool LockLayersForAnimation(ListViewBaseItemAnimationCommand command)
	{
		var commandPriority = command.GetPriority();

		if (_currentHighestCommandPriority >= commandPriority)
		{
			// Generate stop command for current animation if supplanting it
			if (_currentHighestPriorityCommand != null && _currentHighestCommandPriority != commandPriority)
			{
				var stopCommand = _currentHighestPriorityCommand.Clone();
				stopCommand.IsStarting = false;
				_animationCommands.Insert(0, stopCommand);
			}

			_currentHighestPriorityCommand = command;
			_currentHighestCommandPriority = commandPriority;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Unlocks layers after animation and disposes the command.
	/// MUX Reference: CListViewBaseItemChrome::UnlockLayersForAnimationAndDisposeCommand
	/// </summary>
	internal void UnlockLayersForAnimationAndDisposeCommand(ListViewBaseItemAnimationCommand? command)
	{
		if (_currentHighestPriorityCommand == command)
		{
			_currentHighestPriorityCommand = null;
			_currentHighestCommandPriority = int.MaxValue;
		}
	}

	private void EnsureTransitionTarget()
	{
		// In Uno, we don't need to create explicit TransitionTargets like WinUI does.
		// The animation system handles this automatically.
	}
}
