// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/1.4.2

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemChrome
{
	#region Backplate

	private void EnsureBackplate()
	{
		if (_backplateRectangle != null)
		{
			return;
		}

		_backplateRectangle = new Border
		{
			IsHitTestVisible = false,
			CornerRadius = new CornerRadius(ListViewBaseItemChromeConstants.GeneralCornerRadius),
		};

		// Set accessibility view to Raw
		AutomationProperties.SetAccessibilityView(_backplateRectangle, AccessibilityView.Raw);

		SetBackplateBackground();

		// Add to visual tree at the bottom (z-index 0 = backplate layer)
		AddChromeChild(_backplateRectangle, 0);
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
			// Disabled state - no background
		}
		else if (_visualStates.HasState(CommonStates2.Pressed))
		{
			brush = _pressedBackground;
		}
		else if (_visualStates.HasState(CommonStates2.PointerOver))
		{
			brush = _pointerOverBackground;
		}
		else if (_visualStates.HasState(CommonStates2.Selected))
		{
			brush = _selectedBackground;
		}
		else if (_visualStates.HasState(CommonStates2.PointerOverSelected))
		{
			brush = _selectedPointerOverBackground;
		}
		else if (_visualStates.HasState(CommonStates2.PressedSelected))
		{
			brush = _selectedPressedBackground;
		}

		_backplateRectangle.Background = brush;
	}

	#endregion

	#region MultiSelect CheckBox

	private void EnsureMultiSelectCheckBox()
	{
		if (_multiSelectCheckBoxRectangle != null)
		{
			return;
		}

		// Create the checkbox border
		_multiSelectCheckBoxRectangle = new Border
		{
			IsHitTestVisible = false,
			Width = ListViewBaseItemChromeConstants.MultiSelectSquareSize.Width,
			Height = ListViewBaseItemChromeConstants.MultiSelectSquareSize.Height,
			VerticalAlignment = VerticalAlignment.Center,
			CornerRadius = GetCheckBoxCornerRadius(),
		};

		// Set margin based on mode
		_multiSelectCheckBoxRectangle.Margin = _checkMode == ListViewItemPresenterCheckMode.Inline
			? (IsRoundedListViewBaseItemChromeEnabled
				? ListViewBaseItemChromeConstants.MultiSelectRoundedSquareInlineMargin
				: ListViewBaseItemChromeConstants.MultiSelectSquareInlineMargin)
			: ListViewBaseItemChromeConstants.MultiSelectSquareOverlayMargin;

		// Set horizontal alignment based on mode
		_multiSelectCheckBoxRectangle.HorizontalAlignment = _checkMode == ListViewItemPresenterCheckMode.Inline
			? HorizontalAlignment.Left
			: HorizontalAlignment.Right;

		AutomationProperties.SetAccessibilityView(_multiSelectCheckBoxRectangle, AccessibilityView.Raw);

		// Create the check glyph
		_multiSelectCheckGlyph = new TextBlock
		{
			Text = ListViewBaseItemChromeConstants.CheckMarkGlyph,
			FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
			FontSize = ListViewBaseItemChromeConstants.CheckMarkGlyphFontSize,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0,
		};

		_multiSelectCheckBoxRectangle.Child = _multiSelectCheckGlyph;

		SetMultiSelectCheckBoxBackground();
		SetMultiSelectCheckBoxBorder();
		SetMultiSelectCheckBoxForeground();

		// Add checkbox on top of content (null index = append)
		AddChromeChild(_multiSelectCheckBoxRectangle);
	}

	private void RemoveMultiSelectCheckBox()
	{
		if (_multiSelectCheckBoxRectangle != null)
		{
			RemoveChromeChild(_multiSelectCheckBoxRectangle);
			_multiSelectCheckBoxRectangle = null;
			_multiSelectCheckGlyph = null;
		}
	}

	private void SetMultiSelectCheckBoxBackground()
	{
		if (_multiSelectCheckBoxRectangle == null)
		{
			return;
		}

		var selected = _visualStates.HasState(CommonStates2.Selected) ||
					   _visualStates.HasState(CommonStates2.PressedSelected) ||
					   _visualStates.HasState(CommonStates2.PointerOverSelected);
		var disabled = _visualStates.HasState(DisabledStates.Disabled);
		var pointerOver = _visualStates.HasState(CommonStates2.PointerOver) ||
						  _visualStates.HasState(CommonStates2.PointerOverSelected);
		var pressed = _visualStates.HasState(CommonStates2.Pressed) ||
					  _visualStates.HasState(CommonStates2.PressedSelected);

		Brush? brush = null;

		if (selected)
		{
			if (disabled)
				brush = _checkBoxSelectedDisabledBrush;
			else if (pressed)
				brush = _checkBoxSelectedPressedBrush;
			else if (pointerOver)
				brush = _checkBoxSelectedPointerOverBrush;
			else
				brush = _checkBoxSelectedBrush;
		}
		else
		{
			if (disabled)
				brush = _checkBoxDisabledBrush;
			else if (pressed)
				brush = _checkBoxPressedBrush;
			else if (pointerOver)
				brush = _checkBoxPointerOverBrush;
			else
				brush = _checkBoxBrush;
		}

		_multiSelectCheckBoxRectangle.Background = brush;
	}

	private void SetMultiSelectCheckBoxBorder()
	{
		if (_multiSelectCheckBoxRectangle == null)
		{
			return;
		}

		var disabled = _visualStates.HasState(DisabledStates.Disabled);
		var pointerOver = _visualStates.HasState(CommonStates2.PointerOver) ||
						  _visualStates.HasState(CommonStates2.PointerOverSelected);
		var pressed = _visualStates.HasState(CommonStates2.Pressed) ||
					  _visualStates.HasState(CommonStates2.PressedSelected);

		Brush? brush = null;

		if (disabled)
			brush = _checkBoxDisabledBorderBrush;
		else if (pressed)
			brush = _checkBoxPressedBorderBrush;
		else if (pointerOver)
			brush = _checkBoxPointerOverBorderBrush;
		else
			brush = _checkBoxBorderBrush;

		_multiSelectCheckBoxRectangle.BorderBrush = brush;
		_multiSelectCheckBoxRectangle.BorderThickness = IsRoundedListViewBaseItemChromeEnabled
			? ListViewBaseItemChromeConstants.MultiSelectRoundedSquareThickness
			: ListViewBaseItemChromeConstants.MultiSelectSquareThickness;
	}

	private void SetMultiSelectCheckBoxForeground()
	{
		if (_multiSelectCheckGlyph == null)
		{
			return;
		}

		// Use check brush for foreground
		_multiSelectCheckGlyph.Foreground = _checkBrush;
	}

	#endregion

	#region Selection Indicator

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
			CornerRadius = GetSelectionIndicatorCornerRadius(),
		};

		AutomationProperties.SetAccessibilityView(_selectionIndicatorRectangle, AccessibilityView.Raw);

		SetSelectionIndicatorBackground();

		// Selection indicator appears alongside content
		AddChromeChild(_selectionIndicatorRectangle);
	}

	private void RemoveSelectionIndicator()
	{
		if (_selectionIndicatorRectangle != null)
		{
			RemoveChromeChild(_selectionIndicatorRectangle);
			_selectionIndicatorRectangle = null;
		}
	}

	private void SetSelectionIndicatorBackground()
	{
		if (_selectionIndicatorRectangle == null)
		{
			return;
		}

		var disabled = _visualStates.HasState(DisabledStates.Disabled);
		var pointerOver = _visualStates.HasState(CommonStates2.PointerOverSelected);
		var pressed = _visualStates.HasState(CommonStates2.PressedSelected);

		Brush? brush;

		if (disabled)
			brush = _selectionIndicatorDisabledBrush;
		else if (pressed)
			brush = _selectionIndicatorPressedBrush;
		else if (pointerOver)
			brush = _selectionIndicatorPointerOverBrush;
		else
			brush = _selectionIndicatorBrush;

		_selectionIndicatorRectangle.Background = brush;
	}

	#endregion

	#region Inner/Outer Borders (GridViewItem)

	private void EnsureOuterBorder()
	{
		if (_outerBorder != null)
		{
			return;
		}

		_outerBorder = new Border
		{
			IsHitTestVisible = false,
			CornerRadius = GetGeneralCornerRadius(),
		};

		AutomationProperties.SetAccessibilityView(_outerBorder, AccessibilityView.Raw);

		SetOuterBorderBrush();
		SetOuterBorderThickness();

		// Outer border is above backplate but below content
		AddChromeChild(_outerBorder, 1);
	}

	private void RemoveOuterBorder()
	{
		if (_outerBorder != null)
		{
			RemoveChromeChild(_outerBorder);
			_outerBorder = null;
		}
	}

	private void SetOuterBorderBrush()
	{
		if (_outerBorder == null)
		{
			return;
		}

		var selected = _visualStates.HasState(CommonStates2.Selected) ||
					   _visualStates.HasState(CommonStates2.PressedSelected) ||
					   _visualStates.HasState(CommonStates2.PointerOverSelected);
		var disabled = _visualStates.HasState(DisabledStates.Disabled);
		var pointerOver = _visualStates.HasState(CommonStates2.PointerOver);
		var pressed = _visualStates.HasState(CommonStates2.Pressed);

		Brush? brush = null;

		if (selected)
		{
			if (disabled)
				brush = _selectedDisabledBorderBrush;
			else if (pressed || _visualStates.HasState(CommonStates2.PressedSelected))
				brush = _selectedPressedBorderBrush;
			else if (pointerOver || _visualStates.HasState(CommonStates2.PointerOverSelected))
				brush = _selectedPointerOverBorderBrush;
			else
				brush = _selectedBorderBrush;
		}
		else
		{
			brush = _pointerOverBorderBrush;
		}

		_outerBorder.BorderBrush = brush;
	}

	private void SetOuterBorderThickness()
	{
		if (_outerBorder == null)
		{
			return;
		}

		var selected = _visualStates.HasState(CommonStates2.Selected) ||
					   _visualStates.HasState(CommonStates2.PressedSelected) ||
					   _visualStates.HasState(CommonStates2.PointerOverSelected);

		_outerBorder.BorderThickness = selected
			? ListViewBaseItemChromeConstants.SelectedBorderThicknessRounded
			: ListViewBaseItemChromeConstants.BorderThickness;
	}

	private void EnsureInnerSelectionBorder()
	{
		if (_innerSelectionBorder != null)
		{
			return;
		}

		_innerSelectionBorder = new Border
		{
			IsHitTestVisible = false,
			CornerRadius = new CornerRadius(ListViewBaseItemChromeConstants.InnerBorderCornerRadius),
		};

		AutomationProperties.SetAccessibilityView(_innerSelectionBorder, AccessibilityView.Raw);

		SetInnerSelectionBorderBrush();
		SetInnerSelectionBorderProperties();

		// Add to outer border if it exists, otherwise to this
		if (_outerBorder != null)
		{
			_outerBorder.Child = _innerSelectionBorder;
		}
		else
		{
			// Inner selection border is above backplate/outer border but below content
			AddChromeChild(_innerSelectionBorder, 2);
		}
	}

	private void RemoveInnerSelectionBorder()
	{
		if (_innerSelectionBorder != null)
		{
			if (_outerBorder?.Child == _innerSelectionBorder)
			{
				_outerBorder.Child = null;
			}
			else
			{
				RemoveChromeChild(_innerSelectionBorder);
			}
			_innerSelectionBorder = null;
		}
	}

	private void SetInnerSelectionBorderBrush()
	{
		if (_innerSelectionBorder == null)
		{
			return;
		}

		_innerSelectionBorder.BorderBrush = _selectedInnerBorderBrush;
	}

	private void SetInnerSelectionBorderProperties()
	{
		if (_innerSelectionBorder == null)
		{
			return;
		}

		_innerSelectionBorder.BorderThickness = ListViewBaseItemChromeConstants.InnerSelectionBorderThickness;
	}

	#endregion

	#region Secondary Chrome

	private void AddSecondaryChrome()
	{
		if (_secondaryChrome != null)
		{
			return;
		}

		_secondaryChrome = new ListViewBaseItemSecondaryChrome
		{
			PrimaryChromeNoRef = this,
		};

		// Secondary chrome (earmark) is on top of everything
		AddChromeChild(_secondaryChrome);
	}

	#endregion

	#region Drag Overlay Text

	private void EnsureDragOverlayTextBlock()
	{
		if (_dragItemsCountTextBlock != null)
		{
			return;
		}

		_dragItemsCountTextBlock = new TextBlock
		{
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed,
		};

		AutomationProperties.SetAccessibilityView(_dragItemsCountTextBlock, AccessibilityView.Raw);
	}

	private void SetDragOverlayTextBlockVisible(bool isVisible)
	{
		if (isVisible)
		{
			EnsureDragOverlayTextBlock();
			EnsureMultiSelectCheckBox();

			// Configure for drag count display
			if (_multiSelectCheckBoxRectangle != null && _dragItemsCountTextBlock != null)
			{
				_multiSelectCheckBoxRectangle.Child = _dragItemsCountTextBlock;
				_multiSelectCheckBoxRectangle.Visibility = Visibility.Visible;

				if (_checkMode == ListViewItemPresenterCheckMode.Overlay)
				{
					_multiSelectCheckBoxRectangle.BorderThickness = new Thickness(2);
					_multiSelectCheckBoxRectangle.VerticalAlignment = VerticalAlignment.Center;
					_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Center;
				}
			}

			_owner.InvalidateMeasure();
		}

		if (_dragItemsCountTextBlock != null)
		{
			_dragItemsCountTextBlock.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	internal void SetDragItemsCount(uint count)
	{
		EnsureDragOverlayTextBlock();
		if (_dragItemsCountTextBlock != null)
		{
			_dragItemsCountTextBlock.Text = count.ToString();
		}
	}

	#endregion

	#region Child Management

	// MUX Reference: CListViewBaseItemChrome uses internal child collections
	// In WinUI, children are managed via m_pSecondaryChrome and internal rendering layers
	// In Uno, we add chrome elements as children of the presenter using AddChild/RemoveChild
	//
	// Z-order (bottom to top):
	// 0. Backplate (background)
	// 1. OuterBorder
	// 2. InnerSelectionBorder
	// 3. Content (template child - managed by ContentPresenter)
	// 4. SelectionIndicator (left side pill)
	// 5. MultiSelectCheckBox
	// 6. SecondaryChrome (earmark overlay)

	/// <summary>
	/// Adds a chrome element to the visual tree.
	/// MUX Reference: Chrome elements are rendered via IContentRenderer in WinUI.
	/// In Uno, we add them as children of the presenter.
	/// </summary>
	private void AddChromeChild(UIElement child, int? index = null)
	{
		if (child == null)
		{
			return;
		}

		// Add the chrome element as a child of the presenter
		// If index is null, element is appended (appears on top)
		// If index is 0, element is inserted at the beginning (appears at bottom)
		_owner.AddChild(child, index);
	}

	/// <summary>
	/// Removes a chrome element from the visual tree.
	/// </summary>
	private void RemoveChromeChild(UIElement child)
	{
		if (child == null)
		{
			return;
		}

		// RemoveChild is void on some platforms, bool on others
		// We just call it and ignore return value
		try
		{
			_owner.RemoveChild(child);
		}
		catch
		{
			// Ignore errors if child wasn't in the tree
		}
	}

	#endregion

	#region Foreground

	private void SetForegroundBrush()
	{
		var parent = GetParentListViewBaseItemNoRef();
		if (parent == null)
		{
			return;
		}

		var selected = _visualStates.HasState(CommonStates2.Selected) ||
					   _visualStates.HasState(CommonStates2.PressedSelected) ||
					   _visualStates.HasState(CommonStates2.PointerOverSelected);
		var pointerOver = _visualStates.HasState(CommonStates2.PointerOver);
		var disabled = _visualStates.HasState(DisabledStates.Disabled);

		Brush? foreground = null;

		if (selected)
		{
			foreground = _selectedForeground;
		}
		else if (pointerOver)
		{
			foreground = _pointerOverForeground;
		}
		else if (disabled && _dragForeground != null)
		{
			foreground = _dragForeground;
		}

		if (foreground != null)
		{
			parent.Foreground = foreground;
		}
	}

	#endregion
}
