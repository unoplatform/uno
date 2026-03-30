// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GridViewItemPresenter_Partial.h, GridViewItemPresenter_Partial.cpp, tag winui3/release/1.4.2

#nullable enable
#pragma warning disable CS0169 // Field is never used (partial implementation - will be used later)
#pragma warning disable CS0414 // Field is assigned but never used (partial implementation)
#pragma warning disable CS0649 // Field is never assigned (partial implementation)

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Chrome helper class for GridViewItemPresenter that handles visual state management and animations.
/// Uses composition pattern to avoid changing the class hierarchy of the generated presenter.
/// </summary>
internal sealed class GridViewItemPresenterChrome : IListViewBaseItemAnimationCommandVisitor
{
	private readonly GridViewItemPresenter _presenter;

	// Visual states tracking
	private ListViewBaseItemChromeVisualStates _visualStates;

	// Animation command queue
	private readonly List<ListViewBaseItemAnimationCommand> _animationCommands = new();
	private ListViewBaseItemAnimationCommand? _currentHighestPriorityCommand;
	private int _currentHighestCommandPriority = int.MaxValue;

	// Animation state tracking
	private AnimationState _pointerPressedAnimation;
	private AnimationState _reorderHintAnimation;
	private AnimationState _multiSelectAnimation;
	private AnimationState _selectionIndicatorAnimation;

	// Chrome elements
	private Border? _backplateRectangle;
	private Border? _multiSelectCheckBoxRectangle;
	private TextBlock? _multiSelectCheckGlyph;
	private Border? _selectionIndicatorRectangle;
	private Border? _outerBorder;
	private Border? _innerSelectionBorder;

	// State flags
	private bool _isInMultiSelect;
	private bool _isInIndicatorSelect;

	// Static cached theme resource value
	private static bool? _isRoundedListViewBaseItemChromeEnabled;

	private struct AnimationState
	{
		public Storyboard? Storyboard;
		public ListViewBaseItemAnimationCommand? Command;

		public void Clear()
		{
			Storyboard?.Stop();
			Storyboard = null;
			Command = null;
		}
	}

	public GridViewItemPresenterChrome(GridViewItemPresenter presenter)
	{
		_presenter = presenter;
		_visualStates = new ListViewBaseItemChromeVisualStates();
	}

	/// <summary>
	/// Gets whether rounded chrome style is enabled.
	/// </summary>
	private bool IsRoundedListViewBaseItemChromeEnabled
	{
		get
		{
			_isRoundedListViewBaseItemChromeEnabled ??= true;
			return _isRoundedListViewBaseItemChromeEnabled.Value;
		}
	}

	/// <summary>
	/// Main entry point for visual state transitions.
	/// </summary>
	public bool GoToChromedState(string stateName, bool useTransitions)
	{
		bool wentToState = false;
		bool dirty = false;
		bool needsMeasure = false;
		bool needsArrange = false;

		var templateChild = _presenter.Content as UIElement;
		var isRoundedChromeEnabled = IsRoundedListViewBaseItemChromeEnabled;
		var oldCommonState2 = _visualStates.commonState2;

		// Try CommonStates2 first (new style)
		if (TryUpdateCommonStates2(stateName, out wentToState))
		{
			ProcessCommonStates2Change(oldCommonState2, useTransitions, templateChild, isRoundedChromeEnabled, ref dirty, ref needsArrange);
		}

		// Try DisabledStates
		if (!wentToState && TryUpdateDisabledStates(stateName, out wentToState))
		{
			ProcessDisabledStateChange(templateChild, isRoundedChromeEnabled);
			dirty = true;
		}

		// Try FocusStates
		if (!wentToState && TryUpdateFocusStates(stateName, out wentToState))
		{
			dirty = true;
		}

		// Try MultiSelectStates
		if (!wentToState && TryUpdateMultiSelectStates(stateName, out wentToState))
		{
			ProcessMultiSelectStateChange(useTransitions, templateChild, isRoundedChromeEnabled, ref dirty, ref needsMeasure, ref needsArrange);
		}

		// Try SelectionIndicatorStates
		if (!wentToState && TryUpdateSelectionIndicatorStates(stateName, out wentToState))
		{
			ProcessSelectionIndicatorStateChange(useTransitions, templateChild, ref dirty, ref needsMeasure, ref needsArrange);
		}

		// Try ReorderHintStates
		var oldReorderHintState = _visualStates.reorderHintState;
		if (!wentToState && TryUpdateReorderHintStates(stateName, out wentToState))
		{
			ProcessReorderHintStateChange(oldReorderHintState, useTransitions);
			dirty = true;
		}

		// Try DragStates
		if (!wentToState && TryUpdateDragStates(stateName, out wentToState))
		{
			dirty = true;
		}

		// Try DataVirtualizationStates
		if (!wentToState && TryUpdateDataVirtualizationStates(stateName, out wentToState))
		{
			dirty = true;
		}

		// Invalidate as needed
		if (needsMeasure)
		{
			_presenter.InvalidateMeasure();
		}
		if (needsArrange)
		{
			_presenter.InvalidateArrange();
		}
		if (dirty)
		{
			_presenter.InvalidateArrange();
		}

		return wentToState;
	}

	#region Visual State Updates

	private bool TryUpdateCommonStates2(string stateName, out bool wentToState)
	{
		wentToState = false;
		CommonStates2 newState = stateName switch
		{
			"Normal" => CommonStates2.Normal,
			"PointerOver" => CommonStates2.PointerOver,
			"Pressed" => CommonStates2.Pressed,
			"Selected" => CommonStates2.Selected,
			"PointerOverSelected" => CommonStates2.PointerOverSelected,
			"PressedSelected" => CommonStates2.PressedSelected,
			_ => CommonStates2.Invalid
		};

		if (newState != CommonStates2.Invalid)
		{
			_visualStates.commonState2 = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	private bool TryUpdateDisabledStates(string stateName, out bool wentToState)
	{
		wentToState = false;
		DisabledStates newState = stateName switch
		{
			"Enabled" => DisabledStates.Enabled,
			"Disabled" => DisabledStates.Disabled,
			_ => DisabledStates.Invalid
		};

		if (newState != DisabledStates.Invalid)
		{
			_visualStates.disabledState = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	private bool TryUpdateFocusStates(string stateName, out bool wentToState)
	{
		wentToState = false;
		FocusStates newState = stateName switch
		{
			"Focused" => FocusStates.Focused,
			"Unfocused" => FocusStates.Unfocused,
			"PointerFocused" => FocusStates.PointerFocused,
			_ => FocusStates.Invalid
		};

		if (newState != FocusStates.Invalid)
		{
			_visualStates.focusState = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	private bool TryUpdateMultiSelectStates(string stateName, out bool wentToState)
	{
		wentToState = false;
		MultiSelectStates newState = stateName switch
		{
			"MultiSelectDisabled" => MultiSelectStates.MultiSelectDisabled,
			"MultiSelectEnabled" => MultiSelectStates.MultiSelectEnabled,
			_ => MultiSelectStates.Invalid
		};

		if (newState != MultiSelectStates.Invalid)
		{
			_visualStates.multiSelectState = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	private bool TryUpdateSelectionIndicatorStates(string stateName, out bool wentToState)
	{
		wentToState = false;
		SelectionIndicatorStates newState = stateName switch
		{
			"SelectionIndicatorDisabled" => SelectionIndicatorStates.SelectionIndicatorDisabled,
			"SelectionIndicatorEnabled" => SelectionIndicatorStates.SelectionIndicatorEnabled,
			_ => SelectionIndicatorStates.Invalid
		};

		if (newState != SelectionIndicatorStates.Invalid)
		{
			_visualStates.selectionIndicatorState = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	private bool TryUpdateReorderHintStates(string stateName, out bool wentToState)
	{
		wentToState = false;
		ReorderHintStates newState = stateName switch
		{
			"NoReorderHint" => ReorderHintStates.NoReorderHint,
			"BottomReorderHint" => ReorderHintStates.BottomReorderHint,
			"TopReorderHint" => ReorderHintStates.TopReorderHint,
			"RightReorderHint" => ReorderHintStates.RightReorderHint,
			"LeftReorderHint" => ReorderHintStates.LeftReorderHint,
			_ => ReorderHintStates.Invalid
		};

		if (newState != ReorderHintStates.Invalid)
		{
			_visualStates.reorderHintState = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	private bool TryUpdateDragStates(string stateName, out bool wentToState)
	{
		wentToState = false;
		DragStates newState = stateName switch
		{
			"NotDragging" => DragStates.NotDragging,
			"Dragging" => DragStates.Dragging,
			"DraggingTarget" => DragStates.DraggingTarget,
			"MultipleDraggingPrimary" => DragStates.MultipleDraggingPrimary,
			"MultipleDraggingSecondary" => DragStates.MultipleDraggingSecondary,
			"DraggedPlaceholder" => DragStates.DraggedPlaceholder,
			"Reordering" => DragStates.Reordering,
			"ReorderingTarget" => DragStates.ReorderingTarget,
			"MultipleReorderingPrimary" => DragStates.MultipleReorderingPrimary,
			"ReorderedPlaceholder" => DragStates.ReorderedPlaceholder,
			"DragOver" => DragStates.DragOver,
			_ => DragStates.Invalid
		};

		if (newState != DragStates.Invalid)
		{
			_visualStates.dragState = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	private bool TryUpdateDataVirtualizationStates(string stateName, out bool wentToState)
	{
		wentToState = false;
		DataVirtualizationStates newState = stateName switch
		{
			"DataAvailable" => DataVirtualizationStates.DataAvailable,
			"DataPlaceholder" => DataVirtualizationStates.DataPlaceholder,
			_ => DataVirtualizationStates.Invalid
		};

		if (newState != DataVirtualizationStates.Invalid)
		{
			_visualStates.dataVirtualizationState = newState;
			wentToState = true;
			return true;
		}
		return false;
	}

	#endregion

	#region State Change Processing

	private void ProcessCommonStates2Change(
		CommonStates2 oldState,
		bool useTransitions,
		UIElement? templateChild,
		bool isRoundedChromeEnabled,
		ref bool dirty,
		ref bool needsArrange)
	{
		var selected = _visualStates.HasState(CommonStates2.Selected) ||
					   _visualStates.HasState(CommonStates2.PressedSelected) ||
					   _visualStates.HasState(CommonStates2.PointerOverSelected);

		// Handle checkbox visibility for selection
		if (_multiSelectCheckGlyph != null)
		{
			_multiSelectCheckGlyph.Opacity = selected ? 1.0 : 0.0;
		}

		// Update backplate background
		if (_backplateRectangle != null && isRoundedChromeEnabled)
		{
			SetBackplateBackground();
		}

		dirty = true;
	}

	private void ProcessDisabledStateChange(UIElement? templateChild, bool isRoundedChromeEnabled)
	{
		var disabled = _visualStates.HasState(DisabledStates.Disabled);
		var opacity = disabled ? _presenter.DisabledOpacity : 1.0;

		if (isRoundedChromeEnabled && templateChild != null)
		{
			templateChild.Opacity = opacity;
		}
	}

	private void ProcessMultiSelectStateChange(
		bool useTransitions,
		UIElement? templateChild,
		bool isRoundedChromeEnabled,
		ref bool dirty,
		ref bool needsMeasure,
		ref bool needsArrange)
	{
		var entering = _visualStates.HasState(MultiSelectStates.MultiSelectEnabled);

		if (entering)
		{
			_isInIndicatorSelect = false;
			_isInMultiSelect = true;
			EnsureMultiSelectCheckBox();
		}

		// Enqueue animation - GridViewItemPresenter doesn't have CheckMode, use Overlay behavior
		var command = new ListViewBaseItemAnimationCommand_MultiSelect(
			entering,
			isInline: false, // GridView uses overlay mode
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
		ref bool dirty,
		ref bool needsMeasure,
		ref bool needsArrange)
	{
		var entering = _visualStates.HasState(SelectionIndicatorStates.SelectionIndicatorEnabled);

		if (entering)
		{
			_isInIndicatorSelect = true;
			_isInMultiSelect = false;
			EnsureSelectionIndicator();
		}

		needsMeasure = true;
		needsArrange = true;
		dirty = true;
	}

	private void ProcessReorderHintStateChange(ReorderHintStates oldState, bool useTransitions)
	{
		var offset = ComputeReorderHintOffset(_visualStates.reorderHintState);
		var command = new ListViewBaseItemAnimationCommand_ReorderHint(
			(float)offset.X, (float)offset.Y,
			new WeakReference<UIElement>(_presenter),
			isStarting: !_visualStates.HasState(ReorderHintStates.NoReorderHint),
			steadyStateOnly: !useTransitions);
		EnqueueAnimationCommand(command);
	}

	private Point ComputeReorderHintOffset(ReorderHintStates state)
	{
		var offset = _presenter.ReorderHintOffset;
		return state switch
		{
			ReorderHintStates.BottomReorderHint => new Point(0, offset),
			ReorderHintStates.TopReorderHint => new Point(0, -offset),
			ReorderHintStates.LeftReorderHint => new Point(-offset, 0),
			ReorderHintStates.RightReorderHint => new Point(offset, 0),
			_ => new Point(0, 0)
		};
	}

	#endregion

	#region Element Management

	private void EnsureMultiSelectCheckBox()
	{
		if (_multiSelectCheckBoxRectangle != null)
		{
			return;
		}

		_multiSelectCheckBoxRectangle = new Border
		{
			IsHitTestVisible = false,
			Width = ListViewBaseItemChromeConstants.MultiSelectSquareSize.Width,
			Height = ListViewBaseItemChromeConstants.MultiSelectSquareSize.Height,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right, // GridView uses overlay mode
			Background = _presenter.CheckBrush,
			BorderThickness = ListViewBaseItemChromeConstants.MultiSelectRoundedSquareThickness,
		};

		_multiSelectCheckGlyph = new TextBlock
		{
			Text = ListViewBaseItemChromeConstants.CheckMarkGlyph,
			FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
			FontSize = ListViewBaseItemChromeConstants.CheckMarkGlyphFontSize,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0,
			Foreground = _presenter.CheckBrush,
		};

		_multiSelectCheckBoxRectangle.Child = _multiSelectCheckGlyph;

		// Note: In a real implementation, this would be added to the visual tree
	}

	private void EnsureSelectionIndicator()
	{
		if (_selectionIndicatorRectangle != null)
		{
			return;
		}

		_selectionIndicatorRectangle = new Border
		{
			IsHitTestVisible = false,
			Width = ListViewBaseItemChromeConstants.SelectionIndicatorSize.Width,
			Height = ListViewBaseItemChromeConstants.SelectionIndicatorSize.Height,
			Margin = ListViewBaseItemChromeConstants.SelectionIndicatorMargin,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};
	}

	private void SetBackplateBackground()
	{
		if (_backplateRectangle == null)
		{
			return;
		}

		Brush? brush = null;

		if (_visualStates.HasState(DisabledStates.Disabled))
		{
			// No background
		}
		else if (_visualStates.HasState(CommonStates2.Pressed))
		{
			// GridViewItemPresenter doesn't have PressedBackground, use PointerOverBackground
			brush = _presenter.PointerOverBackground;
		}
		else if (_visualStates.HasState(CommonStates2.PointerOver))
		{
			brush = _presenter.PointerOverBackground;
		}
		else if (_visualStates.HasState(CommonStates2.Selected))
		{
			brush = _presenter.SelectedBackground;
		}
		else if (_visualStates.HasState(CommonStates2.PointerOverSelected))
		{
			brush = _presenter.SelectedPointerOverBackground;
		}
		else if (_visualStates.HasState(CommonStates2.PressedSelected))
		{
			// GridViewItemPresenter doesn't have SelectedPressedBackground
			brush = _presenter.SelectedPointerOverBackground;
		}

		_backplateRectangle.Background = brush;
	}

	#endregion

	#region Animation Command Processing

	private void EnqueueAnimationCommand(ListViewBaseItemAnimationCommand command)
	{
		_animationCommands.Add(command);
	}

	public void ProcessAnimationCommands()
	{
		while (_animationCommands.Count > 0)
		{
			var command = _animationCommands[0];
			_animationCommands.RemoveAt(0);
			command.Accept(this);
		}
	}

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_Pressed command)
	{
		if (command.SteadyStateOnly || !command.IsStarting)
		{
			_pointerPressedAnimation.Clear();
			return;
		}

		UIElement? target = null;
		command.Target?.TryGetTarget(out target);

		if (target == null)
		{
			return;
		}

		var storyboard = new Storyboard();

		if (command.IsPressed)
		{
			var animation = new PointerDownThemeAnimation();
			Storyboard.SetTarget(animation, target);
			storyboard.Children.Add(animation);
		}
		else
		{
			var animation = new PointerUpThemeAnimation();
			Storyboard.SetTarget(animation, target);
			storyboard.Children.Add(animation);
		}

		_pointerPressedAnimation.Clear();
		_pointerPressedAnimation = new AnimationState { Storyboard = storyboard, Command = command };
		storyboard.Begin();
	}

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_ReorderHint command)
	{
		if (command.SteadyStateOnly || !command.IsStarting)
		{
			_reorderHintAnimation.Clear();
			return;
		}

		// Reorder hint animation would translate the item
		// For now, just clear
		_reorderHintAnimation.Clear();
	}

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_DragDrop command)
	{
		// Drag/drop animations - simplified for now
	}

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_MultiSelect command)
	{
		if (command.SteadyStateOnly || !command.IsStarting)
		{
			_multiSelectAnimation.Clear();
			return;
		}

		// Multi-select animation slides checkbox in/out
		// For now, just clear
		_multiSelectAnimation.Clear();
	}

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_IndicatorSelect command)
	{
		// Indicator select animation
	}

	void IListViewBaseItemAnimationCommandVisitor.VisitAnimationCommand(ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility command)
	{
		if (command.SteadyStateOnly || !command.IsStarting)
		{
			_selectionIndicatorAnimation.Clear();
			return;
		}

		UIElement? target = null;
		command.IndicatorTarget?.TryGetTarget(out target);

		if (target == null)
		{
			return;
		}

		// Scale animation for showing/hiding selection indicator
		_selectionIndicatorAnimation.Clear();
	}

	#endregion
}
