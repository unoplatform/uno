// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/1.4.2

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

#region Visual State Enums

internal enum CommonStates
{
	Normal = 0,
	PointerOver = 1,
	Pressed = 2,
	PointerOverPressed = 3,
	Disabled = 4,
	Invalid = 5,
}

internal enum FocusStates
{
	Focused = 0,
	Unfocused = 1,
	PointerFocused = 2,
	Invalid = 3,
}

internal enum SelectionHintStates
{
	HorizontalSelectionHint = 0,
	VerticalSelectionHint = 1,
	NoSelectionHint = 2,
	Invalid = 3,
}

internal enum SelectionStates
{
	Unselecting = 0,
	Unselected = 1,
	UnselectedPointerOver = 2,
	UnselectedSwiping = 3,
	Selecting = 4,
	Selected = 5,
	SelectedSwiping = 6,
	SelectedUnfocused = 7,
	Invalid = 8,
}

internal enum DragStates
{
	NotDragging = 0,
	Dragging = 1,
	DraggingTarget = 2,
	MultipleDraggingPrimary = 3,
	MultipleDraggingSecondary = 4,
	DraggedPlaceholder = 5,
	Reordering = 6,
	ReorderingTarget = 7,
	MultipleReorderingPrimary = 8,
	ReorderedPlaceholder = 9,
	DragOver = 10,
	Invalid = 11,
}

internal enum ReorderHintStates
{
	NoReorderHint = 0,
	BottomReorderHint = 1,
	TopReorderHint = 2,
	RightReorderHint = 3,
	LeftReorderHint = 4,
	Invalid = 5,
}

internal enum DataVirtualizationStates
{
	DataAvailable = 0,
	DataPlaceholder = 1,
	Invalid = 2,
}

// "New style" visual state groups (used by WinUI 3 / modern templates)
internal enum CommonStates2
{
	Normal = 0,
	PointerOver = 1,
	Pressed = 2,
	Selected = 3,
	PointerOverSelected = 4,
	PressedSelected = 5,
	Invalid = 6,
}

internal enum DisabledStates
{
	Enabled = 0,
	Disabled = 1,
	Invalid = 2,
}

internal enum MultiSelectStates
{
	MultiSelectDisabled = 0,
	MultiSelectEnabled = 1,
	Invalid = 2,
}

internal enum SelectionIndicatorStates
{
	SelectionIndicatorDisabled = 0,
	SelectionIndicatorEnabled = 1,
	Invalid = 2,
}

#endregion

/// <summary>
/// Tracks the current visual states across all state groups.
/// </summary>
internal struct ListViewBaseItemChromeVisualStates
{
	public CommonStates commonState;
	public FocusStates focusState;
	public SelectionHintStates selectionHintState;
	public SelectionStates selectionState;
	public DragStates dragState;
	public ReorderHintStates reorderHintState;
	public DataVirtualizationStates dataVirtualizationState;

	// "New style" state groups
	public CommonStates2 commonState2;
	public DisabledStates disabledState;
	public MultiSelectStates multiSelectState;
	public SelectionIndicatorStates selectionIndicatorState;

	public ListViewBaseItemChromeVisualStates()
	{
		commonState = CommonStates.Normal;
		focusState = FocusStates.Unfocused;
		selectionHintState = SelectionHintStates.NoSelectionHint;
		selectionState = SelectionStates.Unselected;
		dragState = DragStates.NotDragging;
		reorderHintState = ReorderHintStates.NoReorderHint;
		dataVirtualizationState = DataVirtualizationStates.DataAvailable;

		commonState2 = CommonStates2.Normal;
		disabledState = DisabledStates.Enabled;
		multiSelectState = MultiSelectStates.MultiSelectDisabled;
		selectionIndicatorState = SelectionIndicatorStates.SelectionIndicatorDisabled;
	}

	// Helper methods to check for states
	public readonly bool HasState(CommonStates state) => commonState == state;
	public readonly bool HasState(FocusStates state) => focusState == state;
	public readonly bool HasState(SelectionHintStates state) => selectionHintState == state;
	public readonly bool HasState(SelectionStates state) => selectionState == state;
	public readonly bool HasState(DragStates state) => dragState == state;
	public readonly bool HasState(ReorderHintStates state) => reorderHintState == state;
	public readonly bool HasState(DataVirtualizationStates state) => dataVirtualizationState == state;
	public readonly bool HasState(CommonStates2 state) => commonState2 == state;
	public readonly bool HasState(DisabledStates state) => disabledState == state;
	public readonly bool HasState(MultiSelectStates state) => multiSelectState == state;
	public readonly bool HasState(SelectionIndicatorStates state) => selectionIndicatorState == state;
}
