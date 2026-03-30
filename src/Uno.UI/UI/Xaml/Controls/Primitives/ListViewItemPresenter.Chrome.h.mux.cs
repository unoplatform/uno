// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/1.8.4

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

#pragma warning disable CS0649, CS0414, IDE0051, CS0169

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewItemPresenter
{
	// Enums and helpers specifying the visual state groups. Each group has an appropriately-typed
	// mapping that parses a string state name.

	private enum ChromeCommonStates : byte
	{
		Normal,
		PointerOver,
		Pressed,
		PointerOverPressed,
		Disabled,
	}

	private enum ChromeFocusStates : byte
	{
		Focused,
		Unfocused,
		PointerFocused,
	}

	private enum ChromeSelectionHintStates : byte
	{
		HorizontalSelectionHint,
		VerticalSelectionHint,
		NoSelectionHint,
	}

	private enum ChromeSelectionStates : byte
	{
		Unselecting,
		Unselected,
		UnselectedPointerOver,
		UnselectedSwiping,
		Selecting,
		Selected,
		SelectedSwiping,
		SelectedUnfocused,
	}

	private enum ChromeDragStates : byte
	{
		NotDragging,
		Dragging,
		DraggingTarget,
		MultipleDraggingPrimary,
		MultipleDraggingSecondary,
		DraggedPlaceholder,
		Reordering,
		ReorderingTarget,
		MultipleReorderingPrimary,
		ReorderedPlaceholder,
		DragOver,
	}

	private enum ChromeReorderHintStates : byte
	{
		NoReorderHint,
		BottomReorderHint,
		TopReorderHint,
		RightReorderHint,
		LeftReorderHint,
	}

	private enum ChromeDataVirtualizationStates : byte
	{
		DataAvailable,
		DataPlaceholder,
	}

	// Visual states for new ListViewBaseItem styles
	private enum ChromeCommonStates2 : byte
	{
		Normal,
		PointerOver,
		Pressed,
		Selected,
		PointerOverSelected,
		PressedSelected,
	}

	private enum ChromeDisabledStates : byte
	{
		Enabled,
		Disabled,
	}

	// enum FocusStates (using the same ChromeFocusStates as above)

	private enum ChromeMultiSelectStates : byte
	{
		MultiSelectDisabled,
		MultiSelectEnabled,
	}

	private enum ChromeSelectionIndicatorStates : byte
	{
		SelectionIndicatorDisabled,
		SelectionIndicatorEnabled,
	}

	// A struct holding and analyzing the current set of VisualStates active in the chrome.
	private struct ChromeVisualStates
	{
		public ChromeCommonStates CommonState;
		public ChromeFocusStates FocusState;
		public ChromeSelectionHintStates SelectionHintState;
		public ChromeSelectionStates SelectionState;
		public ChromeDragStates DragState;
		public ChromeReorderHintStates ReorderHintState;
		public ChromeDataVirtualizationStates DataVirtualizationState;

		public ChromeCommonStates2 CommonState2;
		public ChromeDisabledStates DisabledState;
		public ChromeMultiSelectStates MultiSelectState;
		public ChromeSelectionIndicatorStates SelectionIndicatorState;

		// These methods return true if the set of active VisualStates contains the given state.
		public readonly bool HasState(ChromeCommonStates state) => CommonState == state;
		public readonly bool HasState(ChromeFocusStates state) => FocusState == state;
		public readonly bool HasState(ChromeSelectionHintStates state) => SelectionHintState == state;
		public readonly bool HasState(ChromeSelectionStates state) => SelectionState == state;
		public readonly bool HasState(ChromeDragStates state) => DragState == state;
		public readonly bool HasState(ChromeReorderHintStates state) => ReorderHintState == state;
		public readonly bool HasState(ChromeDataVirtualizationStates state) => DataVirtualizationState == state;

		public readonly bool HasState(ChromeCommonStates2 state) => CommonState2 == state;
		public readonly bool HasState(ChromeDisabledStates state) => DisabledState == state;
		public readonly bool HasState(ChromeMultiSelectStates state) => MultiSelectState == state;
		public readonly bool HasState(ChromeSelectionIndicatorStates state) => SelectionIndicatorState == state;
	}

	// State name mapping dictionaries (correspond to Mapping<> template specializations in C++)

	private static readonly Dictionary<string, ChromeCommonStates> s_commonStatesMap = new()
	{
		["Normal"] = ChromeCommonStates.Normal,
		["PointerOver"] = ChromeCommonStates.PointerOver,
		["Pressed"] = ChromeCommonStates.Pressed,
		["PointerOverPressed"] = ChromeCommonStates.PointerOverPressed,
		["Disabled"] = ChromeCommonStates.Disabled,
	};

	private static readonly Dictionary<string, ChromeFocusStates> s_focusStatesMap = new()
	{
		["Focused"] = ChromeFocusStates.Focused,
		["Unfocused"] = ChromeFocusStates.Unfocused,
		["PointerFocused"] = ChromeFocusStates.PointerFocused,
	};

	private static readonly Dictionary<string, ChromeSelectionHintStates> s_selectionHintStatesMap = new()
	{
		["HorizontalSelectionHint"] = ChromeSelectionHintStates.HorizontalSelectionHint,
		["VerticalSelectionHint"] = ChromeSelectionHintStates.VerticalSelectionHint,
		["NoSelectionHint"] = ChromeSelectionHintStates.NoSelectionHint,
	};

	private static readonly Dictionary<string, ChromeSelectionStates> s_selectionStatesMap = new()
	{
		["Unselecting"] = ChromeSelectionStates.Unselecting,
		["Unselected"] = ChromeSelectionStates.Unselected,
		["UnselectedPointerOver"] = ChromeSelectionStates.UnselectedPointerOver,
		["UnselectedSwiping"] = ChromeSelectionStates.UnselectedSwiping,
		["Selecting"] = ChromeSelectionStates.Selecting,
		["Selected"] = ChromeSelectionStates.Selected,
		["SelectedSwiping"] = ChromeSelectionStates.SelectedSwiping,
		["SelectedUnfocused"] = ChromeSelectionStates.SelectedUnfocused,
	};

	private static readonly Dictionary<string, ChromeDragStates> s_dragStatesMap = new()
	{
		["NotDragging"] = ChromeDragStates.NotDragging,
		["Dragging"] = ChromeDragStates.Dragging,
		["DraggingTarget"] = ChromeDragStates.DraggingTarget,
		["MultipleDraggingPrimary"] = ChromeDragStates.MultipleDraggingPrimary,
		["MultipleDraggingSecondary"] = ChromeDragStates.MultipleDraggingSecondary,
		["DraggedPlaceholder"] = ChromeDragStates.DraggedPlaceholder,
		["Reordering"] = ChromeDragStates.Reordering,
		["ReorderingTarget"] = ChromeDragStates.ReorderingTarget,
		["MultipleReorderingPrimary"] = ChromeDragStates.MultipleReorderingPrimary,
		["ReorderedPlaceholder"] = ChromeDragStates.ReorderedPlaceholder,
		["DragOver"] = ChromeDragStates.DragOver,
	};

	private static readonly Dictionary<string, ChromeReorderHintStates> s_reorderHintStatesMap = new()
	{
		["NoReorderHint"] = ChromeReorderHintStates.NoReorderHint,
		["BottomReorderHint"] = ChromeReorderHintStates.BottomReorderHint,
		["TopReorderHint"] = ChromeReorderHintStates.TopReorderHint,
		["RightReorderHint"] = ChromeReorderHintStates.RightReorderHint,
		["LeftReorderHint"] = ChromeReorderHintStates.LeftReorderHint,
	};

	private static readonly Dictionary<string, ChromeDataVirtualizationStates> s_dataVirtualizationStatesMap = new()
	{
		["DataAvailable"] = ChromeDataVirtualizationStates.DataAvailable,
		["DataPlaceholder"] = ChromeDataVirtualizationStates.DataPlaceholder,
	};

	private static readonly Dictionary<string, ChromeCommonStates2> s_commonStates2Map = new()
	{
		["Normal"] = ChromeCommonStates2.Normal,
		["PointerOver"] = ChromeCommonStates2.PointerOver,
		["Pressed"] = ChromeCommonStates2.Pressed,
		["Selected"] = ChromeCommonStates2.Selected,
		["PointerOverSelected"] = ChromeCommonStates2.PointerOverSelected,
		["PressedSelected"] = ChromeCommonStates2.PressedSelected,
	};

	private static readonly Dictionary<string, ChromeDisabledStates> s_disabledStatesMap = new()
	{
		["Enabled"] = ChromeDisabledStates.Enabled,
		["Disabled"] = ChromeDisabledStates.Disabled,
	};

	private static readonly Dictionary<string, ChromeMultiSelectStates> s_multiSelectStatesMap = new()
	{
		["MultiSelectDisabled"] = ChromeMultiSelectStates.MultiSelectDisabled,
		["MultiSelectEnabled"] = ChromeMultiSelectStates.MultiSelectEnabled,
	};

	private static readonly Dictionary<string, ChromeSelectionIndicatorStates> s_selectionIndicatorStatesMap = new()
	{
		["SelectionIndicatorDisabled"] = ChromeSelectionIndicatorStates.SelectionIndicatorDisabled,
		["SelectionIndicatorEnabled"] = ChromeSelectionIndicatorStates.SelectionIndicatorEnabled,
	};

	// MUX Reference ListViewBaseItemChrome.cpp, lines 735-764
	// Given a string state name, tries to parse it as the appropriate VisualState type.
	// Returns true only if the state CHANGED. Sets stateFound to true if the state name
	// was found in the group (even if unchanged).
	private static bool UpdateVisualStateGroup<TEnum>(
		string stateName,
		Dictionary<string, TEnum> map,
		ref TEnum currentState,
		ref bool stateFound) where TEnum : struct
	{
		if (map.TryGetValue(stateName, out var newState))
		{
			stateFound = true;

			if (!EqualityComparer<TEnum>.Default.Equals(currentState, newState))
			{
				currentState = newState;
				return true;
			}

			return false;
		}

		return false;
	}

	// Static constants (MUX: header lines 226-258)

	private static class ChromeConstants
	{
		// static const XSIZEF s_selectionCheckMarkVisualSize
		public static readonly Size SelectionCheckMarkVisualSize = new(40.0, 40.0);

		// static const XTHICKNESS s_focusBorderThickness
		public static readonly Thickness FocusBorderThickness = new(2.0, 2.0, 2.0, 2.0);

		// static const XPOINTF s_checkmarkOffset
		public static readonly Point CheckmarkOffset = new(-20.0f, 6.0f);

		// static const XPOINTF s_swipeHintOffset
		public static readonly Point SwipeHintOffset = new(-23.0f, 15.0f);

		// static const XFLOAT s_swipingCheckSteadyStateOpacity
		public const float SwipingCheckSteadyStateOpacity = 0.5f;

		// static const XFLOAT s_selectingSwipingCheckSteadyStateOpacity
		public const float SelectingSwipingCheckSteadyStateOpacity = 1.0f;

		// static const XFLOAT s_generalCornerRadius
		public const float GeneralCornerRadius = 4.0f;

		// static const XFLOAT s_innerBorderCornerRadius
		public const float InnerBorderCornerRadius = 3.0f;

		// static const XSIZEF s_selectionIndicatorSize
		public static readonly Size SelectionIndicatorSize = new(3.0, 16.0);

		// static const XFLOAT s_selectionIndicatorHeightShrinkage
		public const float SelectionIndicatorHeightShrinkage = 6.0f;

		// static const XTHICKNESS s_selectionIndicatorMargin
		public static readonly Thickness SelectionIndicatorMargin = new(4.0, 20.0, 0.0, 20.0);

		// static const XSIZEF s_multiSelectSquareSize
		public static readonly Size MultiSelectSquareSize = new(20.0, 20.0);

		// static const XTHICKNESS s_multiSelectSquareThickness
		public static readonly Thickness MultiSelectSquareThickness = new(2.0, 2.0, 2.0, 2.0);

		// static const XTHICKNESS s_multiSelectRoundedSquareThickness
		public static readonly Thickness MultiSelectRoundedSquareThickness = new(1.0, 1.0, 1.0, 1.0);

		// static const XTHICKNESS s_multiSelectSquareInlineMargin
		public static readonly Thickness MultiSelectSquareInlineMargin = new(12.0, 0.0, 0.0, 0.0);

		// static const XTHICKNESS s_multiSelectRoundedSquareInlineMargin
		public static readonly Thickness MultiSelectRoundedSquareInlineMargin = new(14.0, 0.0, 0.0, 0.0);

		// static const XTHICKNESS s_multiSelectSquareOverlayMargin
		public static readonly Thickness MultiSelectSquareOverlayMargin = new(0.0, 2.0, 2.0, 0.0);

		// static const XTHICKNESS s_backplateMargin
		public static readonly Thickness BackplateMargin = new(4.0, 2.0, 4.0, 2.0);

		// static const XTHICKNESS s_borderThickness
		public static readonly Thickness BorderThickness = new(1.0, 1.0, 1.0, 1.0);

		// static const XTHICKNESS s_innerSelectionBorderThickness
		public static readonly Thickness InnerSelectionBorderThickness = new(1.0, 1.0, 1.0, 1.0);

		// static const XFLOAT s_listViewItemMultiSelectContentOffset
		public const float ListViewItemMultiSelectContentOffset = 32.0f;

		// static const XFLOAT s_multiSelectRoundedContentOffset
		public const float MultiSelectRoundedContentOffset = 28.0f;

		// static const XPOINTF s_checkMarkPoints[6]
		public static readonly Point[] CheckMarkPoints =
		{
			new(0.0f, 7.0f),
			new(2.2f, 4.3f),
			new(6.1f, 7.9f),
			new(12.4f, 0.0f),
			new(15.0f, 2.4f),
			new(6.6f, 13.0f),
		};

		// static const XFLOAT s_listViewItemFocusBorderThickness
		public const float ListViewItemFocusBorderThickness = 1.0f;

		// static const XFLOAT s_gridViewItemFocusBorderThickness
		public const float GridViewItemFocusBorderThickness = 2.0f;

		// static const XFLOAT s_checkMarkGlyphFontSize
		public const float CheckMarkGlyphFontSize = 16.0f;

		// public: static constexpr XCORNERRADIUS s_defaultSelectionIndicatorCornerRadius
		public static readonly CornerRadius DefaultSelectionIndicatorCornerRadius = new(1.5);

		// public: static constexpr XCORNERRADIUS s_defaultCheckBoxCornerRadius
		public static readonly CornerRadius DefaultCheckBoxCornerRadius = new(3.0);

		// public: static constexpr XTHICKNESS s_selectedBorderThicknessRounded
		public static readonly Thickness SelectedBorderThicknessRounded = new(2.0, 2.0, 2.0, 2.0);

		// public: static constexpr XTHICKNESS s_selectedBorderThickness
		public static readonly Thickness SelectedBorderThicknessNonRounded = new(0.0, 0.0, 0.0, 0.0);

		// CheckMark glyph character (Uno addition, not in header)
		public const string CheckMarkGlyph = "\uE73E";

		// static const XFLOAT s_cOpacityUnset (line 379)
		public const float OpacityUnset = -1.0f;

		// Default disabled opacity for rounded chrome.
		public const float DefaultDisabledOpacityRounded = 0.3f;

		// Default disabled opacity for non-rounded chrome.
		public const float DefaultDisabledOpacityNonRounded = 0.55f;

		// Default drag opacity.
		public const float DefaultDragOpacity = 0.8f;

		// Default ListView reorder hint offset.
		public const float DefaultListViewItemReorderHintOffset = 10.0f;

		// Default GridView reorder hint offset.
		public const float DefaultGridViewItemReorderHintOffset = 16.0f;
	}

	// Private fields (MUX: header lines 271-329)

	// CContentControl* m_pParentListViewBaseItemNoRef - removed, use GetParentListViewBaseItem() helper
	// CListViewBaseItemSecondaryChrome* m_pSecondaryChrome - removed, not needed in Uno
	// CGeometry* m_pCheckGeometryData - removed, checkmark path not used in Uno
	// CTextBlock* m_pDragItemsCountTextBlock - removed, drag count text block

	// xref_ptr<CBorder> m_multiSelectCheckBoxRectangle
	private Border? _multiSelectCheckBoxRectangle;

	// xref_ptr<CRectangleGeometry> m_multiSelectCheckBoxClip - removed, clip

	// xref_ptr<CFontIcon> m_multiSelectCheckGlyph
	private FontIcon? _multiSelectCheckGlyph;

	// xref_ptr<CBorder> m_backplateRectangle
	private Border? _backplateRectangle;

	// xref_ptr<CBorder> m_selectionIndicatorRectangle
	private Border? _selectionIndicatorRectangle;

	// xref_ptr<CBorder> m_innerSelectionBorder
	private Border? _innerSelectionBorder;

	// xref_ptr<CBorder> m_outerBorder
	private Border? _outerBorder;

	// xref_ptr<CBrush> m_currentCheckBrush
	private Brush? _currentCheckBrush;

	// xvector<ListViewBaseItemAnimationCommand*> m_animationCommands - skip, animation not ported
	// ListViewBaseItemAnimationCommand* m_pCurrentHighestPriorityCommand - skip, animation not ported

	// XFLOAT m_swipeHintCheckOpacity
	private float _swipeHintCheckOpacity = ChromeConstants.OpacityUnset;

	// XINT32 m_currentHighestCommandPriority - skip, animation not ported
	// XRECTF_RB m_checkGeometryBounds - skip

	// xref_ptr<CBrush> m_previousBackgroundBrush
	private Brush? _previousBackgroundBrush;

	// VisualStates m_visualStates
	private ChromeVisualStates _visualStates;

	// bool m_isFocusVisualDrawnByFocusManager
	private bool _isFocusVisualDrawnByFocusManager;

	// bool m_fFillBrushDirty
	private bool _fillBrushDirty;

	// bool m_shouldRenderChrome
	private bool _shouldRenderChrome;

	// bool m_isInIndicatorSelect - true when states are in MultiSelectStates_MultiSelectDisabled + SelectionIndicatorStates_SelectionIndicatorEnabled
	private bool _isInIndicatorSelect;

	// bool m_isInMultiSelect - true when states are in MultiSelectStates_MultiSelectEnabled + SelectionIndicatorStates_SelectionIndicatorDisabled
	private bool _isInMultiSelect;

	// static std::optional<bool> s_isRoundedListViewBaseItemChromeEnabled - not needed in Uno, always true

	// public CBrush* fields (lines 334-374) - Not needed in Uno, they correspond to DependencyProperties on the presenter (in Properties.cs)

	// XTHICKNESS m_contentMargin - corresponds to ContentMargin DependencyProperty
	// DirectUI::ListViewItemPresenterCheckMode m_checkMode - corresponds to CheckMode DependencyProperty
}
