// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/1.4.2

#nullable enable

using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemChrome
{
	#region Brush Setters (called by presenter property change handlers)

	internal void SetCheckBrush(Brush? value) => _checkBrush = value;
	internal void SetCheckHintBrush(Brush? value) => _checkHintBrush = value;
	internal void SetCheckSelectingBrush(Brush? value) => _checkSelectingBrush = value;
	internal void SetCheckPressedBrush(Brush? value) => _checkPressedBrush = value;
	internal void SetCheckDisabledBrush(Brush? value) => _checkDisabledBrush = value;
	internal void SetFocusBorderBrush(Brush? value) => _focusBorderBrush = value;
	internal void SetFocusSecondaryBorderBrush(Brush? value) => _focusSecondaryBorderBrush = value;
	internal void SetPlaceholderBackground(Brush? value) => _placeholderBackground = value;
	internal void SetPointerOverBackground(Brush? value) => _pointerOverBackground = value;
	internal void SetPointerOverBorderBrush(Brush? value) => _pointerOverBorderBrush = value;
	internal void SetPointerOverForeground(Brush? value) => _pointerOverForeground = value;
	internal void SetSelectedBackground(Brush? value) => _selectedBackground = value;
	internal void SetSelectedForeground(Brush? value) => _selectedForeground = value;
	internal void SetSelectedBorderBrush(Brush? value) => _selectedBorderBrush = value;
	internal void SetSelectedInnerBorderBrush(Brush? value) => _selectedInnerBorderBrush = value;
	internal void SetSelectedPointerOverBackground(Brush? value) => _selectedPointerOverBackground = value;
	internal void SetSelectedPointerOverBorderBrush(Brush? value) => _selectedPointerOverBorderBrush = value;
	internal void SetSelectedPressedBackground(Brush? value) => _selectedPressedBackground = value;
	internal void SetSelectedPressedBorderBrush(Brush? value) => _selectedPressedBorderBrush = value;
	internal void SetSelectedDisabledBackground(Brush? value) => _selectedDisabledBackground = value;
	internal void SetSelectedDisabledBorderBrush(Brush? value) => _selectedDisabledBorderBrush = value;
	internal void SetPressedBackground(Brush? value) => _pressedBackground = value;
	internal void SetDragBackground(Brush? value) => _dragBackground = value;
	internal void SetDragForeground(Brush? value) => _dragForeground = value;

	// Checkbox brushes
	internal void SetCheckBoxBrush(Brush? value) => _checkBoxBrush = value;
	internal void SetCheckBoxPointerOverBrush(Brush? value) => _checkBoxPointerOverBrush = value;
	internal void SetCheckBoxPressedBrush(Brush? value) => _checkBoxPressedBrush = value;
	internal void SetCheckBoxDisabledBrush(Brush? value) => _checkBoxDisabledBrush = value;
	internal void SetCheckBoxSelectedBrush(Brush? value) => _checkBoxSelectedBrush = value;
	internal void SetCheckBoxSelectedPointerOverBrush(Brush? value) => _checkBoxSelectedPointerOverBrush = value;
	internal void SetCheckBoxSelectedPressedBrush(Brush? value) => _checkBoxSelectedPressedBrush = value;
	internal void SetCheckBoxSelectedDisabledBrush(Brush? value) => _checkBoxSelectedDisabledBrush = value;
	internal void SetCheckBoxBorderBrush(Brush? value) => _checkBoxBorderBrush = value;
	internal void SetCheckBoxPointerOverBorderBrush(Brush? value) => _checkBoxPointerOverBorderBrush = value;
	internal void SetCheckBoxPressedBorderBrush(Brush? value) => _checkBoxPressedBorderBrush = value;
	internal void SetCheckBoxDisabledBorderBrush(Brush? value) => _checkBoxDisabledBorderBrush = value;

	// Selection indicator brushes
	internal void SetSelectionIndicatorBrush(Brush? value) => _selectionIndicatorBrush = value;
	internal void SetSelectionIndicatorPointerOverBrush(Brush? value) => _selectionIndicatorPointerOverBrush = value;
	internal void SetSelectionIndicatorPressedBrush(Brush? value) => _selectionIndicatorPressedBrush = value;
	internal void SetSelectionIndicatorDisabledBrush(Brush? value) => _selectionIndicatorDisabledBrush = value;

	#endregion

	#region Property Accessors (read from owner presenter)

	/// <summary>
	/// Gets whether SelectionCheckMarkVisualEnabled is set on the presenter.
	/// MUX Reference: CListViewBaseItemChrome::GetSelectionCheckMarkVisualEnabled
	/// </summary>
	internal bool GetSelectionCheckMarkVisualEnabled()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.SelectionCheckMarkVisualEnabled;
		}
		if (_owner is GridViewItemPresenter gvip)
		{
			return gvip.SelectionCheckMarkVisualEnabled;
		}
		return true;
	}

	/// <summary>
	/// Gets whether SelectionIndicatorVisualEnabled is set (ListViewItemPresenter only).
	/// MUX Reference: CListViewBaseItemChrome::IsSelectionIndicatorVisualEnabled
	/// </summary>
	internal bool IsSelectionIndicatorVisualEnabled()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return IsRoundedListViewBaseItemChromeEnabled && lvip.SelectionIndicatorVisualEnabled;
		}
		return false;
	}

	/// <summary>
	/// Gets whether we're in selection indicator mode (indicator visible + Single/Extended selection).
	/// MUX Reference: CListViewBaseItemChrome::IsInSelectionIndicatorMode
	/// </summary>
	internal bool IsInSelectionIndicatorMode()
	{
		if (!IsSelectionIndicatorVisualEnabled())
		{
			return false;
		}

		// TODO Uno: Need to check the parent ListView's selection mode
		// For now, return true if indicator is enabled
		return true;
	}

	/// <summary>
	/// Gets the selection indicator mode (Inline vs Overlay).
	/// MUX Reference: CListViewBaseItemChrome::GetSelectionIndicatorMode
	/// </summary>
	internal ListViewItemPresenterSelectionIndicatorMode GetSelectionIndicatorMode()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.SelectionIndicatorMode;
		}
		return ListViewItemPresenterSelectionIndicatorMode.Inline;
	}

	/// <summary>
	/// Gets the disabled opacity value from the presenter.
	/// MUX Reference: CListViewBaseItemChrome::GetDisabledOpacity
	/// </summary>
	internal double GetDisabledOpacity()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.DisabledOpacity;
		}
		if (_owner is GridViewItemPresenter gvip)
		{
			return gvip.DisabledOpacity;
		}
		return GetDefaultDisabledOpacity(IsRoundedListViewBaseItemChromeEnabled);
	}

	/// <summary>
	/// Gets the reorder hint offset value from the presenter.
	/// MUX Reference: CListViewBaseItemChrome::GetReorderHintOffset
	/// </summary>
	internal double GetReorderHintOffset()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.ReorderHintOffset;
		}
		if (_owner is GridViewItemPresenter gvip)
		{
			return gvip.ReorderHintOffset;
		}
		return IsChromeForListViewItem ? GetDefaultListViewItemReorderHintOffset() : GetDefaultGridViewItemReorderHintOffset();
	}

	/// <summary>
	/// Gets the SelectedBorderThickness value from the presenter.
	/// MUX Reference: CListViewBaseItemChrome::GetSelectedBorderThickness
	/// </summary>
	internal Thickness GetSelectedBorderThickness()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.SelectedBorderThickness;
		}
		if (_owner is GridViewItemPresenter gvip)
		{
			return gvip.SelectedBorderThickness;
		}
		return GetSelectedBorderXThickness(IsRoundedListViewBaseItemChromeEnabled);
	}

	/// <summary>
	/// Gets the general corner radius for borders.
	/// MUX Reference: CListViewBaseItemChrome::GetGeneralCornerRadius
	/// </summary>
	internal CornerRadius GetGeneralCornerRadius()
	{
		// Check if presenter has a CornerRadius property set
		// TODO Uno: Add CornerRadius support to presenters
		if (IsRoundedListViewBaseItemChromeForced())
		{
			return new CornerRadius(ListViewBaseItemChromeConstants.GeneralCornerRadius);
		}
		return new CornerRadius(ListViewBaseItemChromeConstants.GeneralCornerRadius);
	}

	/// <summary>
	/// Gets the checkbox corner radius from the presenter.
	/// MUX Reference: CListViewBaseItemChrome::GetCheckBoxCornerRadius
	/// </summary>
	internal CornerRadius GetCheckBoxCornerRadius()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.CheckBoxCornerRadius;
		}
		return ListViewBaseItemChromeConstants.DefaultCheckBoxCornerRadius;
	}

	/// <summary>
	/// Gets the selection indicator corner radius from the presenter.
	/// MUX Reference: CListViewBaseItemChrome::GetSelectionIndicatorCornerRadius
	/// </summary>
	internal CornerRadius GetSelectionIndicatorCornerRadius()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.SelectionIndicatorCornerRadius;
		}
		return ListViewBaseItemChromeConstants.DefaultSelectionIndicatorCornerRadius;
	}

	/// <summary>
	/// Gets the horizontal content alignment from the presenter.
	/// MUX Reference: Part of the property accessors
	/// </summary>
	internal HorizontalAlignment GetHorizontalContentAlignment()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.ListViewItemPresenterHorizontalContentAlignment;
		}
		if (_owner is GridViewItemPresenter gvip)
		{
			return gvip.GridViewItemPresenterHorizontalContentAlignment;
		}
		return HorizontalAlignment.Stretch;
	}

	/// <summary>
	/// Gets the vertical content alignment from the presenter.
	/// MUX Reference: Part of the property accessors
	/// </summary>
	internal VerticalAlignment GetVerticalContentAlignment()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return lvip.ListViewItemPresenterVerticalContentAlignment;
		}
		if (_owner is GridViewItemPresenter gvip)
		{
			return gvip.GridViewItemPresenterVerticalContentAlignment;
		}
		return VerticalAlignment.Stretch;
	}

	/// <summary>
	/// Gets the drag opacity value.
	/// MUX Reference: CListViewBaseItemChrome::GetDragOpacity
	/// </summary>
	internal float GetDragOpacity()
	{
		if (_owner is ListViewItemPresenter lvip)
		{
			return (float)lvip.DragOpacity;
		}
		if (_owner is GridViewItemPresenter gvip)
		{
			return (float)gvip.DragOpacity;
		}
		return GetDefaultDragOpacity();
	}

	#endregion
}
