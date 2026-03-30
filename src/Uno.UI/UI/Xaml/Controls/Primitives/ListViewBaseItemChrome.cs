// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/1.4.2

#nullable enable
#pragma warning disable CS0169 // Field is never used (partial implementation - will be used later)
#pragma warning disable CS0414 // Field is assigned but never used (partial implementation)

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Chrome helper class for ListViewItem and GridViewItem presenters.
/// Knows how to render all layers of the chrome, including layers in Grid/ListViewItem and secondary chrome.
/// </summary>
/// <remarks>
/// In WinUI, CListViewBaseItemChrome inherits from CContentPresenter in the core layer.
/// In Uno, this is a helper class used via composition since the generated presenters
/// already inherit from ContentPresenter and we cannot change that.
/// </remarks>
internal partial class ListViewBaseItemChrome
{
	// The presenter that owns this chrome
	private readonly ContentPresenter _owner;

	// Secondary chrome for earmark rendering
	private ListViewBaseItemSecondaryChrome? _secondaryChrome;

	// Parent ListViewBaseItem (weak reference to avoid cycles)
	private WeakReference<ContentControl>? _parentListViewBaseItemRef;

	// Visual states tracking
	private ListViewBaseItemChromeVisualStates _visualStates;

	// Animation command queue
	// MUX Reference: xvector<ListViewBaseItemAnimationCommand*> m_animationCommands;
	private readonly List<ListViewBaseItemAnimationCommand> _animationCommands = new();
	private ListViewBaseItemAnimationCommand? _currentHighestPriorityCommand;
	private int _currentHighestCommandPriority = int.MaxValue;

	// Chrome elements
	// MUX Reference: xref_ptr<CBorder> m_backplateRectangle;
	private Border? _backplateRectangle;
	// MUX Reference: xref_ptr<CBorder> m_multiSelectCheckBoxRectangle;
	private Border? _multiSelectCheckBoxRectangle;
	// MUX Reference: xref_ptr<CRectangleGeometry> m_multiSelectCheckBoxClip;
	private RectangleGeometry? _multiSelectCheckBoxClip;
	// MUX Reference: xref_ptr<CFontIcon> m_multiSelectCheckGlyph;
	// TODO Uno: Using TextBlock instead of FontIcon for simpler glyph display
	private TextBlock? _multiSelectCheckGlyph;
	// MUX Reference: xref_ptr<CBorder> m_selectionIndicatorRectangle;
	private Border? _selectionIndicatorRectangle;
	// MUX Reference: xref_ptr<CBorder> m_innerSelectionBorder;
	private Border? _innerSelectionBorder;
	// MUX Reference: xref_ptr<CBorder> m_outerBorder;
	private Border? _outerBorder;
	// MUX Reference: CTextBlock* m_pDragItemsCountTextBlock;
	private TextBlock? _dragItemsCountTextBlock;

	// Brush fields - these mirror properties from the presenter
	// MUX Reference: CBrush* m_pCheckHintBrush; etc.
	private Brush? _checkBrush;
	private Brush? _checkHintBrush;
	private Brush? _checkSelectingBrush;
	private Brush? _checkPressedBrush;
	private Brush? _checkDisabledBrush;
	private Brush? _focusBorderBrush;
	private Brush? _focusSecondaryBorderBrush;
	private Brush? _placeholderBackground;
	private Brush? _pointerOverBackground;
	private Brush? _pointerOverBorderBrush;
	private Brush? _pointerOverForeground;
	private Brush? _selectedBackground;
	private Brush? _selectedForeground;
	private Brush? _selectedBorderBrush;
	private Brush? _selectedInnerBorderBrush;
	private Brush? _selectedPointerOverBackground;
	private Brush? _selectedPointerOverBorderBrush;
	private Brush? _selectedPressedBackground;
	private Brush? _selectedPressedBorderBrush;
	private Brush? _selectedDisabledBackground;
	private Brush? _selectedDisabledBorderBrush;
	private Brush? _pressedBackground;
	private Brush? _dragBackground;
	private Brush? _dragForeground;

	// CheckBox brushes (rounded style)
	private Brush? _checkBoxBrush;
	private Brush? _checkBoxPointerOverBrush;
	private Brush? _checkBoxPressedBrush;
	private Brush? _checkBoxDisabledBrush;
	private Brush? _checkBoxSelectedBrush;
	private Brush? _checkBoxSelectedPointerOverBrush;
	private Brush? _checkBoxSelectedPressedBrush;
	private Brush? _checkBoxSelectedDisabledBrush;
	private Brush? _checkBoxBorderBrush;
	private Brush? _checkBoxPointerOverBorderBrush;
	private Brush? _checkBoxPressedBorderBrush;
	private Brush? _checkBoxDisabledBorderBrush;

	// Selection indicator brushes
	private Brush? _selectionIndicatorBrush;
	private Brush? _selectionIndicatorPointerOverBrush;
	private Brush? _selectionIndicatorPressedBrush;
	private Brush? _selectionIndicatorDisabledBrush;

	// Current brush state for rendering
	// MUX Reference: xref_ptr<CBrush> m_currentCheckBrush;
	private Brush? _currentCheckBrush;
	// MUX Reference: CBrush* m_pCurrentEarmarkBrush;
	private Brush? _currentEarmarkBrush;
	// MUX Reference: xref_ptr<CBrush> m_previousBackgroundBrush;
	private Brush? _previousBackgroundBrush;

	// Check geometry
	// MUX Reference: CGeometry* m_pCheckGeometryData;
	private Geometry? _checkGeometryData;
	// MUX Reference: XRECTF_RB m_checkGeometryBounds;
	private Rect _checkGeometryBounds;

	// Content margin
	// MUX Reference: XTHICKNESS m_contentMargin;
	private Thickness _contentMargin;

	// Check mode (Inline vs Overlay)
	// MUX Reference: DirectUI::ListViewItemPresenterCheckMode m_checkMode;
	private ListViewItemPresenterCheckMode _checkMode = ListViewItemPresenterCheckMode.Inline;

	// Opacity for swipe hint animation
	// MUX Reference: XFLOAT m_swipeHintCheckOpacity;
	private float _swipeHintCheckOpacity = ListViewBaseItemChromeConstants.OpacityUnset;

	// State flags
	// MUX Reference: bool m_shouldRenderChrome : 1;
	private bool _shouldRenderChrome = true;
	// MUX Reference: bool m_fFillBrushDirty : 1;
	private bool _fillBrushDirty;
	// MUX Reference: bool m_isInMultiSelect : 1;
	private bool _isInMultiSelect;
	// MUX Reference: bool m_isInIndicatorSelect : 1;
	private bool _isInIndicatorSelect;
	// MUX Reference: bool m_isFocusVisualDrawnByFocusManager : 1;
	private bool _isFocusVisualDrawnByFocusManager;

	// Static cached theme resource value
	// MUX Reference: static std::optional<bool> s_isRoundedListViewBaseItemChromeEnabled;
	private static bool? s_isRoundedListViewBaseItemChromeEnabled;

	/// <summary>
	/// Creates a new ListViewBaseItemChrome for the specified presenter.
	/// </summary>
	/// <param name="owner">The ContentPresenter that owns this chrome.</param>
	public ListViewBaseItemChrome(ContentPresenter owner)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		_visualStates = new ListViewBaseItemChromeVisualStates();
	}

	/// <summary>
	/// Gets the presenter that owns this chrome.
	/// </summary>
	internal ContentPresenter Owner => _owner;

	/// <summary>
	/// Gets or sets the parent ListViewBaseItem.
	/// MUX Reference: CListViewBaseItemChrome::SetChromedListViewBaseItem
	/// </summary>
	internal void SetChromedListViewBaseItem(UIElement? parent)
	{
		if (parent is ContentControl contentControl)
		{
			_parentListViewBaseItemRef = new WeakReference<ContentControl>(contentControl);
		}
		else
		{
			_parentListViewBaseItemRef = null;
		}
	}

	/// <summary>
	/// Gets the parent ListViewBaseItem.
	/// MUX Reference: CListViewBaseItemChrome::GetParentListViewBaseItemNoRef
	/// </summary>
	internal ContentControl? GetParentListViewBaseItemNoRef()
	{
		if (_parentListViewBaseItemRef?.TryGetTarget(out var parent) == true)
		{
			return parent;
		}
		return null;
	}

	/// <summary>
	/// Gets whether this chrome is for a GridViewItem.
	/// </summary>
	internal bool IsChromeForGridViewItem => _owner is GridViewItemPresenter;

	/// <summary>
	/// Gets whether this chrome is for a ListViewItem.
	/// </summary>
	internal bool IsChromeForListViewItem => _owner is ListViewItemPresenter;

	/// <summary>
	/// Gets whether rounded chrome style is enabled.
	/// MUX Reference: CListViewBaseItemChrome::IsRoundedListViewBaseItemChromeEnabled
	/// </summary>
	internal bool IsRoundedListViewBaseItemChromeEnabled
	{
		get
		{
			// TODO Uno: Implement theme resource lookup for "ListViewItemPresenterRoundedChromeEnabled"
			// For now, default to true for modern WinUI 3 style
			s_isRoundedListViewBaseItemChromeEnabled ??= true;
			return s_isRoundedListViewBaseItemChromeEnabled.Value;
		}
	}

	/// <summary>
	/// Clears the cached rounded chrome enabled value.
	/// MUX Reference: CListViewBaseItemChrome::ClearIsRoundedListViewBaseItemChromeEnabledCache
	/// </summary>
	internal static void ClearIsRoundedListViewBaseItemChromeEnabledCache()
	{
		s_isRoundedListViewBaseItemChromeEnabled = null;
	}

	/// <summary>
	/// Returns true if rounded chrome is forced (for testing).
	/// MUX Reference: CListViewBaseItemChrome::IsRoundedListViewBaseItemChromeForced
	/// </summary>
	internal static bool IsRoundedListViewBaseItemChromeForced()
	{
		// In WinUI, this checks for a registry key or environment variable for testing
		// For now, return false
		return false;
	}

#if DEBUG
	/// <summary>
	/// Returns true if selection indicator visual is forced (for testing).
	/// MUX Reference: CListViewBaseItemChrome::IsSelectionIndicatorVisualForced (DBG only)
	/// </summary>
	internal static bool IsSelectionIndicatorVisualForced()
	{
		// In WinUI, this checks for a registry key or environment variable for testing
		// For now, return false
		return false;
	}
#endif

	/// <summary>
	/// Gets the current visual states.
	/// </summary>
	internal ref ListViewBaseItemChromeVisualStates VisualStates => ref _visualStates;

	/// <summary>
	/// Gets the secondary chrome element.
	/// </summary>
	internal ListViewBaseItemSecondaryChrome? SecondaryChrome => _secondaryChrome;

	/// <summary>
	/// Gets the check mode (Inline vs Overlay).
	/// </summary>
	internal ListViewItemPresenterCheckMode CheckMode
	{
		get => _checkMode;
		set => _checkMode = value;
	}

	/// <summary>
	/// Gets or sets the content margin.
	/// </summary>
	internal Thickness ContentMarginInternal
	{
		get => _contentMargin;
		set => _contentMargin = value;
	}

	/// <summary>
	/// Gets or sets the swipe hint check opacity.
	/// MUX Reference: CListViewBaseItemChrome::SetSwipeHintCheckOpacity
	/// </summary>
	internal float SwipeHintCheckOpacity
	{
		get => _swipeHintCheckOpacity;
		set => _swipeHintCheckOpacity = value;
	}

	/// <summary>
	/// Sets or clears whether chrome should be rendered.
	/// MUX Reference: CListViewBaseItemChrome::SetShouldRenderChrome
	/// </summary>
	internal void SetShouldRenderChrome(bool shouldRenderChrome)
	{
		if (_shouldRenderChrome != shouldRenderChrome)
		{
			_shouldRenderChrome = shouldRenderChrome;
			InvalidateRender();
		}
	}

	/// <summary>
	/// Invalidates rendering.
	/// MUX Reference: CListViewBaseItemChrome::InvalidateRender
	/// </summary>
	private void InvalidateRender()
	{
		_owner.InvalidateArrange();
	}

	#region Static Default Values

	// MUX Reference: Static default value methods

	internal static float GetDefaultSelectionIndicatorCornerRadius() => 1.5f;

	internal static float GetDefaultCheckBoxCornerRadius() => 3.0f;

	internal static float GetDefaultDisabledOpacity(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? 0.3f : 0.55f;

	internal static float GetDefaultDragOpacity() => 0.8f;

	internal static float GetDefaultListViewItemReorderHintOffset() => 10.0f;

	internal static float GetDefaultGridViewItemReorderHintOffset() => 16.0f;

	internal static float GetSelectedBorderThickness(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? 2.0f : 0.0f;

	internal static bool GetDefaultSelectionCheckMarkVisualEnabled() => true;

	internal static Thickness GetSelectedBorderXThickness(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome
			? ListViewBaseItemChromeConstants.SelectedBorderThicknessRounded
			: ListViewBaseItemChromeConstants.SelectedBorderThickness;

	#endregion
}
