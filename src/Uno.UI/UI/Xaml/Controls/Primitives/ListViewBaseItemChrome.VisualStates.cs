// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/1.4.2

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemChrome
{
	/// <summary>
	/// State name to enum mappings for each visual state group.
	/// </summary>
	private static readonly Dictionary<string, CommonStates> CommonStatesMap = new()
	{
		["Normal"] = CommonStates.Normal,
		["PointerOver"] = CommonStates.PointerOver,
		["Pressed"] = CommonStates.Pressed,
		["PointerOverPressed"] = CommonStates.PointerOverPressed,
		["Disabled"] = CommonStates.Disabled,
	};

	private static readonly Dictionary<string, FocusStates> FocusStatesMap = new()
	{
		["Focused"] = FocusStates.Focused,
		["Unfocused"] = FocusStates.Unfocused,
		["PointerFocused"] = FocusStates.PointerFocused,
	};

	private static readonly Dictionary<string, SelectionHintStates> SelectionHintStatesMap = new()
	{
		["HorizontalSelectionHint"] = SelectionHintStates.HorizontalSelectionHint,
		["VerticalSelectionHint"] = SelectionHintStates.VerticalSelectionHint,
		["NoSelectionHint"] = SelectionHintStates.NoSelectionHint,
	};

	private static readonly Dictionary<string, SelectionStates> SelectionStatesMap = new()
	{
		["Unselecting"] = SelectionStates.Unselecting,
		["Unselected"] = SelectionStates.Unselected,
		["UnselectedPointerOver"] = SelectionStates.UnselectedPointerOver,
		["UnselectedSwiping"] = SelectionStates.UnselectedSwiping,
		["Selecting"] = SelectionStates.Selecting,
		["Selected"] = SelectionStates.Selected,
		["SelectedSwiping"] = SelectionStates.SelectedSwiping,
		["SelectedUnfocused"] = SelectionStates.SelectedUnfocused,
	};

	private static readonly Dictionary<string, DragStates> DragStatesMap = new()
	{
		["NotDragging"] = DragStates.NotDragging,
		["Dragging"] = DragStates.Dragging,
		["DraggingTarget"] = DragStates.DraggingTarget,
		["MultipleDraggingPrimary"] = DragStates.MultipleDraggingPrimary,
		["MultipleDraggingSecondary"] = DragStates.MultipleDraggingSecondary,
		["DraggedPlaceholder"] = DragStates.DraggedPlaceholder,
		["Reordering"] = DragStates.Reordering,
		["ReorderingTarget"] = DragStates.ReorderingTarget,
		["MultipleReorderingPrimary"] = DragStates.MultipleReorderingPrimary,
		["ReorderedPlaceholder"] = DragStates.ReorderedPlaceholder,
		["DragOver"] = DragStates.DragOver,
	};

	private static readonly Dictionary<string, ReorderHintStates> ReorderHintStatesMap = new()
	{
		["NoReorderHint"] = ReorderHintStates.NoReorderHint,
		["BottomReorderHint"] = ReorderHintStates.BottomReorderHint,
		["TopReorderHint"] = ReorderHintStates.TopReorderHint,
		["RightReorderHint"] = ReorderHintStates.RightReorderHint,
		["LeftReorderHint"] = ReorderHintStates.LeftReorderHint,
	};

	private static readonly Dictionary<string, DataVirtualizationStates> DataVirtualizationStatesMap = new()
	{
		["DataAvailable"] = DataVirtualizationStates.DataAvailable,
		["DataPlaceholder"] = DataVirtualizationStates.DataPlaceholder,
	};

	private static readonly Dictionary<string, CommonStates2> CommonStates2Map = new()
	{
		["Normal"] = CommonStates2.Normal,
		["PointerOver"] = CommonStates2.PointerOver,
		["Pressed"] = CommonStates2.Pressed,
		["Selected"] = CommonStates2.Selected,
		["PointerOverSelected"] = CommonStates2.PointerOverSelected,
		["PressedSelected"] = CommonStates2.PressedSelected,
	};

	private static readonly Dictionary<string, DisabledStates> DisabledStatesMap = new()
	{
		["Enabled"] = DisabledStates.Enabled,
		["Disabled"] = DisabledStates.Disabled,
	};

	private static readonly Dictionary<string, MultiSelectStates> MultiSelectStatesMap = new()
	{
		["MultiSelectDisabled"] = MultiSelectStates.MultiSelectDisabled,
		["MultiSelectEnabled"] = MultiSelectStates.MultiSelectEnabled,
	};

	private static readonly Dictionary<string, SelectionIndicatorStates> SelectionIndicatorStatesMap = new()
	{
		["SelectionIndicatorDisabled"] = SelectionIndicatorStates.SelectionIndicatorDisabled,
		["SelectionIndicatorEnabled"] = SelectionIndicatorStates.SelectionIndicatorEnabled,
	};

	/// <summary>
	/// Updates a visual state group if the state name matches.
	/// </summary>
	/// <returns>True if the state was updated, false otherwise.</returns>
	private static bool UpdateVisualStateGroup<T>(string stateName, ref T currentState, out bool wentToState)
		where T : struct, Enum
	{
		wentToState = false;

		// Try to find the state name in the appropriate dictionary
		if (typeof(T) == typeof(CommonStates) && CommonStatesMap.TryGetValue(stateName, out var commonState))
		{
			currentState = (T)(object)commonState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(FocusStates) && FocusStatesMap.TryGetValue(stateName, out var focusState))
		{
			currentState = (T)(object)focusState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(SelectionHintStates) && SelectionHintStatesMap.TryGetValue(stateName, out var selectionHintState))
		{
			currentState = (T)(object)selectionHintState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(SelectionStates) && SelectionStatesMap.TryGetValue(stateName, out var selectionState))
		{
			currentState = (T)(object)selectionState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(DragStates) && DragStatesMap.TryGetValue(stateName, out var dragState))
		{
			currentState = (T)(object)dragState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(ReorderHintStates) && ReorderHintStatesMap.TryGetValue(stateName, out var reorderHintState))
		{
			currentState = (T)(object)reorderHintState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(DataVirtualizationStates) && DataVirtualizationStatesMap.TryGetValue(stateName, out var dataVirtualizationState))
		{
			currentState = (T)(object)dataVirtualizationState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(CommonStates2) && CommonStates2Map.TryGetValue(stateName, out var commonState2))
		{
			currentState = (T)(object)commonState2;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(DisabledStates) && DisabledStatesMap.TryGetValue(stateName, out var disabledState))
		{
			currentState = (T)(object)disabledState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(MultiSelectStates) && MultiSelectStatesMap.TryGetValue(stateName, out var multiSelectState))
		{
			currentState = (T)(object)multiSelectState;
			wentToState = true;
			return true;
		}
		if (typeof(T) == typeof(SelectionIndicatorStates) && SelectionIndicatorStatesMap.TryGetValue(stateName, out var selectionIndicatorState))
		{
			currentState = (T)(object)selectionIndicatorState;
			wentToState = true;
			return true;
		}

		return false;
	}
}
