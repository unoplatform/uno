// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/2.5.1

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Core.Scaling;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemPresenter
{
	// Dead WinUI code: s_selectionCheckMarkVisualSize, s_focusBorderThickness, s_checkmarkOffset, s_swipeHintOffset,
	// s_swipingCheckSteadyStateOpacity and s_selectingSwipingCheckSteadyStateOpacity are only used by the removed old-style chrome.

	// Special value for opacity not being set.
	internal const float s_cOpacityUnset = -1.0f;

	// Default corner radius of the outer selection border or backplate visual used when
	// GetCornerRadius() returns 0 and IsRoundedListViewBaseItemChromeForced() returns True.
	private const float s_generalCornerRadius = 4.0f;

	// Minimum corner radius of inner border visual.
	private const float s_innerBorderCornerRadius = 3.0f;

	// Size of selection indicator rectangular visual.
	private static readonly Size s_selectionIndicatorSize = new(3.0, 16.0);

	// Selection indicator height decrease when pressed.
	private const float s_selectionIndicatorHeightShrinkage = 6.0f;

	// Margin for the selection indicator rectangular visual.
	private static readonly Thickness s_selectionIndicatorMargin = new(4.0, 20.0, 0.0, 20.0);

	// Size of multiselect square visual.
	private static readonly Size s_multiSelectSquareSize = new(20.0, 20.0);

	// Border thickness for the multiselect square.
	private static readonly Thickness s_multiSelectSquareThickness = new(2.0, 2.0, 2.0, 2.0);

	// Border thickness for the multiselect square when rounded corners are applied.
	private static readonly Thickness s_multiSelectRoundedSquareThickness = new(1.0, 1.0, 1.0, 1.0);

	// Margin for the multiselect square for inline (ListView)
	private static readonly Thickness s_multiSelectSquareInlineMargin = new(12.0, 0.0, 0.0, 0.0);

	// Margin for the multiselect square for inline (ListView)
	private static readonly Thickness s_multiSelectRoundedSquareInlineMargin = new(14.0, 0.0, 0.0, 0.0);

	// Margin for the multiselect square for overlay (GridView)
	private static readonly Thickness s_multiSelectSquareOverlayMargin = new(0.0, 2.0, 2.0, 0.0);

	// Backplate margin.
	private static readonly Thickness s_backplateMargin = new(4.0, 2.0, 4.0, 2.0);

	// Unselected outer border thickness.
	private static readonly Thickness s_borderThickness = new(1.0, 1.0, 1.0, 1.0);

	// Extra inner selection border thickness compared to outer border.
	private static readonly Thickness s_innerSelectionBorderThickness = new(1.0, 1.0, 1.0, 1.0);

	// Offset of ListViewItem multiselect content.
	private const float s_listViewItemMultiSelectContentOffset = 32.0f;

	// Offset of ListViewItem multiselect content when rounded corners are applied.
	private const float s_multiSelectRoundedContentOffset = 28.0f;

	// Dead WinUI code: s_checkMarkPoints belongs to the removed checkmark path.

	// ListViewItem focus border thickness
	private const float s_listViewItemFocusBorderThickness = 1.0f;

	// ListViewItem focus border thickness
	private const float s_gridViewItemFocusBorderThickness = 2.0f;

	// FontSize of the CheckMark glyph
	private const float s_checkMarkGlyphFontSize = 16.0f;

	private const string c_strCheckMarkGlyphStorage = "";

	// Template specializations for string to enum mappings.
	// Dead WinUI code: the CommonStates, SelectionHintStates and SelectionStates tables.
	private static Array GetMap(Type enumType)
	{
		if (enumType == typeof(FocusStates))
		{
			return new MapItem<FocusStates>[]
			{
				new("Focused",        FocusStates.Focused),
				new("Unfocused",      FocusStates.Unfocused),
				new("PointerFocused", FocusStates.PointerFocused),
			};
		}

		if (enumType == typeof(DragStates))
		{
			return new MapItem<DragStates>[]
			{
				new("NotDragging",               DragStates.NotDragging),
				new("Dragging",                  DragStates.Dragging),
				new("DraggingTarget",            DragStates.DraggingTarget),
				new("MultipleDraggingPrimary",   DragStates.MultipleDraggingPrimary),
				new("MultipleDraggingSecondary", DragStates.MultipleDraggingSecondary),
				new("DraggedPlaceholder",        DragStates.DraggedPlaceholder),
				new("Reordering",                DragStates.Reordering),
				new("ReorderingTarget",          DragStates.ReorderingTarget),
				new("MultipleReorderingPrimary", DragStates.MultipleReorderingPrimary),
				new("ReorderedPlaceholder",      DragStates.ReorderedPlaceholder),
				new("DragOver",                  DragStates.DragOver),
			};
		}

		if (enumType == typeof(ReorderHintStates))
		{
			return new MapItem<ReorderHintStates>[]
			{
				new("NoReorderHint",     ReorderHintStates.NoReorderHint),
				new("BottomReorderHint", ReorderHintStates.BottomReorderHint),
				new("TopReorderHint",    ReorderHintStates.TopReorderHint),
				new("RightReorderHint",  ReorderHintStates.RightReorderHint),
				new("LeftReorderHint",   ReorderHintStates.LeftReorderHint),
			};
		}

		if (enumType == typeof(DataVirtualizationStates))
		{
			return new MapItem<DataVirtualizationStates>[]
			{
				new("DataAvailable",   DataVirtualizationStates.DataAvailable),
				new("DataPlaceholder", DataVirtualizationStates.DataPlaceholder),
			};
		}

		if (enumType == typeof(CommonStates2))
		{
			return new MapItem<CommonStates2>[]
			{
				new("Normal",              CommonStates2.Normal),
				new("PointerOver",         CommonStates2.PointerOver),
				new("Pressed",             CommonStates2.Pressed),
				new("Selected",            CommonStates2.Selected),
				new("PointerOverSelected", CommonStates2.PointerOverSelected),
				new("PressedSelected",     CommonStates2.PressedSelected),
			};
		}

		if (enumType == typeof(DisabledStates))
		{
			return new MapItem<DisabledStates>[]
			{
				new("Enabled",  DisabledStates.Enabled),
				new("Disabled", DisabledStates.Disabled),
			};
		}

		if (enumType == typeof(MultiSelectStates))
		{
			return new MapItem<MultiSelectStates>[]
			{
				new("MultiSelectDisabled", MultiSelectStates.MultiSelectDisabled),
				new("MultiSelectEnabled",  MultiSelectStates.MultiSelectEnabled),
			};
		}

		if (enumType == typeof(SelectionIndicatorStates))
		{
			return new MapItem<SelectionIndicatorStates>[]
			{
				new("SelectionIndicatorDisabled", SelectionIndicatorStates.SelectionIndicatorDisabled),
				new("SelectionIndicatorEnabled",  SelectionIndicatorStates.SelectionIndicatorEnabled),
			};
		}

		throw new ArgumentOutOfRangeException(nameof(enumType));
	}

	// Helpers

	private void LayoutRoundHelper(ref Size size)
	{
		size.Width = LayoutRound(size.Width);
		size.Height = LayoutRound(size.Height);
	}

	private void LayoutRoundHelper(ref Thickness thickness)
	{
		thickness.Left = LayoutRound(thickness.Left);
		thickness.Right = LayoutRound(thickness.Right);
		thickness.Top = LayoutRound(thickness.Top);
		thickness.Bottom = LayoutRound(thickness.Bottom);
	}

	private Rect LayoutRoundHelper(Rect bounds)
	{
		Rect newBounds = default;

		newBounds.X = LayoutRound(bounds.X);
		newBounds.Y = LayoutRound(bounds.Y);
		var right = LayoutRound(bounds.Width + bounds.X);
		var bottom = LayoutRound(bounds.Height + bounds.Y);
		newBounds.Width = right - newBounds.X;
		newBounds.Height = bottom - newBounds.Y;

		return newBounds;
	}

	private bool ShouldUseLayoutRounding()
	{
		// Similar to what Borders do, but we don't care about corner radius (ours is always 0).
		var scale = RootScale.GetRasterizationScaleForElement(this);
		return (scale != 1.0f) && GetUseLayoutRounding();
	}

	// Dead WinUI code: AddRectangle, AddBorder and AddChromeAssociatedPath (C:262-376) are ported with the rendering layers.

	// CListViewBaseItemChrome

	internal ListViewBaseItemPresenter()
	{
		m_isFocusVisualDrawnByFocusManager = false;
		m_shouldRenderChrome = true;
		m_isInIndicatorSelect = false;
		m_isInMultiSelect = false;

		m_visualStates.focusState = FocusStates.Unfocused;
		m_visualStates.dragState = DragStates.NotDragging;
		m_visualStates.reorderHintState = ReorderHintStates.NoReorderHint;
		m_visualStates.dataVirtualizationState = DataVirtualizationStates.DataAvailable;

		m_visualStates.commonState2 = CommonStates2.Normal;
		m_visualStates.disabledState = DisabledStates.Enabled;
		m_visualStates.multiSelectState = MultiSelectStates.MultiSelectDisabled;
		m_visualStates.selectionIndicatorState = SelectionIndicatorStates.SelectionIndicatorDisabled;
	}

	// TODO Uno: Original C++ destructor cleanup (disposes queued animation commands). Uno does not support cleanup via finalizers.

	// Given a string state name and a pointer to a visual state:
	// - tries to parse the string as the appropriate VisualState type.
	// - If parsing successful, updates the value at the pointer.
	// - If the value at the pointer was updated, return TRUE. FALSE otherwise.
	internal static bool UpdateVisualStateGroup<TVisualState>(string name, ref TVisualState state, out bool stateFound)
		where TVisualState : struct, Enum
	{
		var map = Mapping<TVisualState>.s_map;
		stateFound = false;

		for (var i = 0; i < map.Length; ++i)
		{
			ref readonly var item = ref map[i];

			if (string.Equals(item.m_pName, name, StringComparison.Ordinal))
			{
				stateFound = true;

				if (!EqualityComparer<TVisualState>.Default.Equals(item.m_pEnumValue, state))
				{
					state = item.m_pEnumValue;
					return true;
				}

				return false;
			}
		}

		return false;
	}

	// Dead WinUI code: AppendCheckmarkTransform and AppendEarmarkTransform (C:766-818).

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		// TODO Uno: MeasureNewStyle (C:2385-2500) is not ported yet; this only keeps its parent requirement.
		GetParentListViewBaseItemNoRef();

		return base.MeasureOverride(availableSize);
	}

	private protected sealed override bool HasTemplateChild() => GetTemplateChildIfExists() != null;

	private protected override void AddTemplateChild(UIElement child)
	{
		global::System.Diagnostics.Debug.Assert(GetTemplateChildIfExists() == null);

		AddChild(child, m_backplateRectangle is not null ? 1 : 0);
	}

	private protected override void RemoveTemplateChild()
	{
		var templateChild = GetTemplateChildIfExists();

		if (templateChild != null)
		{
			RemoveChild(templateChild);
		}
	}

	private protected override UIElement? GetTemplateChildNoRef() => GetTemplateChildIfExists();

	internal UIElement? GetTemplateChildIfExists()
	{
		var children = GetChildren();
		var templateChild = children.Count > 0 ? children[0] : null;

		if (m_backplateRectangle is not null && templateChild == m_backplateRectangle)
		{
			if (children.Count > 1)
			{
				templateChild = children[1];
			}
		}

		if (templateChild != m_pDragItemsCountTextBlock &&
			templateChild != m_pSecondaryChrome &&
			templateChild != m_multiSelectCheckBoxRectangle &&
			templateChild != m_selectionIndicatorRectangle &&
			templateChild != m_innerSelectionBorder &&
			templateChild != m_outerBorder &&
			templateChild != m_backplateRectangle)
		{
			return templateChild;
		}
		else
		{
			return null;
		}
	}

	// Raise exception if no appropriate parent is set.
	private void SetWrongParentError()
	{
		string? message = null;

		if (this is GridViewItemPresenter)
		{
			message = string.Format(global::System.Globalization.CultureInfo.InvariantCulture, c_wrongParentErrorFormat, "GridViewItemPresenter", "GridViewItem");
		}
		else if (this is ListViewItemPresenter)
		{
			message = string.Format(global::System.Globalization.CultureInfo.InvariantCulture, c_wrongParentErrorFormat, "ListViewItemPresenter", "ListViewItem");
		}

		throw message is null ? new InvalidOperationException() : new InvalidOperationException(message);
	}

	// AG_E_RUNTIME_CHROME_WRONG_PARENT (agcore.debug/CommonErrors.rc)
	private const string c_wrongParentErrorFormat = "{0} can only be used as the first child in the template for a {1}.";

	// Retrieve parent item or raise exception if no parent is set.
	private ContentControl GetParentListViewBaseItemNoRef()
	{
		if (m_pParentListViewBaseItemNoRef is null)
		{
			SetWrongParentError();
		}

		return m_pParentListViewBaseItemNoRef!;
	}

	private static ListViewBase? GetParentListViewBase(ContentControl? listViewBaseItem)
	{
		DependencyObject? parent = listViewBaseItem;
		while (parent != null && parent is not ListViewBase)
		{
			// TODO Uno: WinUI uses GetParentFollowPopups.
			parent = parent.GetParent() as DependencyObject;
		}

		return parent as ListViewBase;
	}

	// Dead WinUI code: DrawBaseLayer, DrawUnderContentLayer, DrawOverContentLayer and DrawDragOverlayLayer (C:995-1341).

	// The "ListViewBaseItemRoundedChromeEnabled" theme resource value is used to turn on/off the rendering with rounded corners.
	// For performance reasons, the resource is only evaluated once. TAEF tests can invalidate the cache by calling TestServices::Utilities::DeleteResourceDictionaryCaches().
	internal bool IsRoundedListViewBaseItemChromeEnabled()
	{
		s_isRoundedListViewBaseItemChromeEnabled ??= IsRoundedListViewBaseItemChromeEnabledStatic();

		return s_isRoundedListViewBaseItemChromeEnabled.Value;
	}

	internal bool IsChromeForGridViewItem() => m_pParentListViewBaseItemNoRef is GridViewItem;

	internal bool IsChromeForListViewItem() => m_pParentListViewBaseItemNoRef is ListViewItem;

	// Uses the ListViewItemPresenter.SelectionIndicatorVisualEnabled property as well as RuntimeEnabledFeature's DenySelectionIndicatorVisualEnabled/ForceSelectionIndicatorVisualEnabled
	// overwrites to determine whether a ListViewItem should render the selection indicator or not.
	internal bool IsSelectionIndicatorVisualEnabled()
	{
		if (!IsChromeForListViewItem() ||
			this is not ListViewItemPresenter listViewItemPresenter)
		{
			return false;
		}

		if (ListViewBaseItemChromeRuntimeFeatures.DenySelectionIndicatorVisualEnabled)
		{
			return false;
		}

		return (IsRoundedListViewBaseItemChromeEnabled() && listViewItemPresenter.SelectionIndicatorVisualEnabled) || ListViewBaseItemChromeRuntimeFeatures.ForceSelectionIndicatorVisualEnabled;
	}

	// Indicates whether the selection indicator is allowed to be rendered and the SelectionMode is either Single or Extended.
	internal bool IsInSelectionIndicatorMode()
	{
		if (!IsSelectionIndicatorVisualEnabled())
		{
			return false;
		}

		var listViewBaseNoRef = GetParentListViewBase(m_pParentListViewBaseItemNoRef);

		if (listViewBaseNoRef is null)
		{
			return false;
		}

		var selectionMode = listViewBaseNoRef.SelectionMode;

		return selectionMode == ListViewSelectionMode.Single || selectionMode == ListViewSelectionMode.Extended;
	}

	// Returns True when the GridViewItem's outer border is present and uses an opaque brush.
	private bool IsOuterBorderBrushOpaque()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());

		if (m_outerBorder is null)
		{
			return false;
		}

		var outerBorderBrush = m_outerBorder.BorderBrush;

		return outerBorderBrush is not null && IsOpaqueBrush(outerBorderBrush);
	}

	// Returns this ListViewBaseItemPresenter's CornerRadius when set,
	// or the default 4.0 value when rounded corner rendering is forced.
	internal CornerRadius GetGeneralCornerRadius()
	{
		var cornerRadius = CornerRadius;

		if (IsRoundedListViewBaseItemChromeForced() &&
			cornerRadius.BottomLeft == 0.0f &&
			cornerRadius.BottomRight == 0.0f &&
			cornerRadius.TopLeft == 0.0f &&
			cornerRadius.TopRight == 0.0f)
		{
			cornerRadius = new CornerRadius(s_generalCornerRadius);
		}

		return cornerRadius;
	}

	// Returns this ListViewBaseItemPresenter's CheckBoxCornerRadius when set,
	// or the default 3.0 value when rounded corner rendering is forced.
	internal CornerRadius GetCheckBoxCornerRadius()
	{
		CornerRadius cornerRadius = default;

		if (this is ListViewItemPresenter listViewItemPresenter)
		{
			cornerRadius = listViewItemPresenter.CheckBoxCornerRadius;

			if (IsRoundedListViewBaseItemChromeForced() &&
				cornerRadius.BottomLeft == 0.0f &&
				cornerRadius.BottomRight == 0.0f &&
				cornerRadius.TopLeft == 0.0f &&
				cornerRadius.TopRight == 0.0f)
			{
				cornerRadius = new CornerRadius(GetDefaultCheckBoxCornerRadius());
			}
		}

		return cornerRadius;
	}

	// Returns this ListViewBaseItemPresenter's SelectionIndicatorCornerRadius when set,
	// or the default 1.5 value when rounded corner rendering is forced.
	internal CornerRadius GetSelectionIndicatorCornerRadius()
	{
		CornerRadius cornerRadius = default;

		if (this is ListViewItemPresenter listViewItemPresenter)
		{
			cornerRadius = listViewItemPresenter.SelectionIndicatorCornerRadius;

			if (IsRoundedListViewBaseItemChromeForced() &&
				cornerRadius.BottomLeft == 0.0f &&
				cornerRadius.BottomRight == 0.0f &&
				cornerRadius.TopLeft == 0.0f &&
				cornerRadius.TopRight == 0.0f)
			{
				cornerRadius = new CornerRadius(GetDefaultSelectionIndicatorCornerRadius());
			}
		}

		return cornerRadius;
	}

	// Returns the selection indicator's desired height given the ListViewItem's available height.
	internal float GetSelectionIndicatorHeightFromAvailableHeight(float availableHeight)
	{
		if (availableHeight <= s_selectionIndicatorSize.Height)
		{
			return availableHeight;
		}

		return (float)Math.Max(s_selectionIndicatorSize.Height, availableHeight - s_selectionIndicatorMargin.Top - s_selectionIndicatorMargin.Bottom);
	}

	// Uses the ListViewItemPresenter.SelectionIndicatorMode property as well as RuntimeEnabledFeature's ForceSelectionIndicatorModeInline/ForceSelectionIndicatorModeOverlay
	// overwrites to determine whether the selection indicator must rendered inline or overlayed.
	internal ListViewItemPresenterSelectionIndicatorMode GetSelectionIndicatorMode()
	{
		global::System.Diagnostics.Debug.Assert(this is ListViewItemPresenter);

		if (ListViewBaseItemChromeRuntimeFeatures.ForceSelectionIndicatorModeInline)
		{
			return ListViewItemPresenterSelectionIndicatorMode.Inline;
		}

		if (ListViewBaseItemChromeRuntimeFeatures.ForceSelectionIndicatorModeOverlay)
		{
			return ListViewItemPresenterSelectionIndicatorMode.Overlay;
		}

		return (ListViewItemPresenterSelectionIndicatorMode)GetValue(ListViewItemPresenter.SelectionIndicatorModeProperty);
	}

	internal Brush? GetRevealBackgroundBrushNoRef()
	{
		if (this is not ListViewItemPresenter listViewItemPresenter)
		{
			return null;
		}

		return listViewItemPresenter.RevealBackground;
	}

	internal Brush? GetRevealBorderBrushNoRef()
	{
		if (this is not ListViewItemPresenter listViewItemPresenter)
		{
			return null;
		}

		return listViewItemPresenter.RevealBorderBrush;
	}

	internal bool GetRevealBackgroundShowsAboveContent()
	{
		if (this is not ListViewItemPresenter listViewItemPresenter)
		{
			return false;
		}

		return listViewItemPresenter.RevealBackgroundShowsAboveContent;
	}

	internal bool GetSelectionCheckMarkVisualEnabled()
		=> (bool)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.SelectionCheckMarkVisualEnabledProperty : GridViewItemPresenter.SelectionCheckMarkVisualEnabledProperty);

	internal Thickness GetRevealBorderThickness()
	{
		global::System.Diagnostics.Debug.Assert(this is ListViewItemPresenter);

		return (Thickness)GetValue(ListViewItemPresenter.RevealBorderThicknessProperty);
	}

	internal Thickness GetSelectedBorderThickness()
		=> (Thickness)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.SelectedBorderThicknessProperty : GridViewItemPresenter.SelectedBorderThicknessProperty);

	// Dead WinUI code: GetPointerOverBackgroundMargin (C:1860-1869) is only used by the removed old-style chrome.

	// WinUI quirk: the opacities and the offset are stored as floats.
	internal float GetDisabledOpacity()
		=> (float)(double)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.DisabledOpacityProperty : GridViewItemPresenter.DisabledOpacityProperty);

	internal float GetDragOpacity()
		=> (float)(double)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.DragOpacityProperty : GridViewItemPresenter.DragOpacityProperty);

	internal float GetReorderHintOffset()
		=> (float)(double)GetValue(this is ListViewItemPresenter ? ListViewItemPresenter.ReorderHintOffsetProperty : GridViewItemPresenter.ReorderHintOffsetProperty);

	// TODO Uno: NWSetContentDirty / NWCleanDirtyFlags (C:1904-1916) have no Uno dirty-flag render model.

	// Dead WinUI code: PrepareCheckPath, AddLineSegmentToSegmentCollection and GetCheckMarkBounds (C:1919-2007).

	// Lets us know we have a parent ListViewBaseItem. We don't take a ref.
	internal void SetChromedListViewBaseItem(UIElement? parent)
	{
		m_pParentListViewBaseItemNoRef = null;

		if (parent is not null)
		{
			m_pParentListViewBaseItemNoRef = parent as ContentControl ?? throw new InvalidOperationException("The chromed item must be a ContentControl.");
		}
	}

	// TODO Uno: GenerateContentBounds (C:2273-2286) has no Uno equivalent; the chrome draws within {0,0,ActualWidth,ActualHeight}.

	internal void InvalidateRender()
	{
		// TODO Uno: NWSetContentDirty on this, the parent item and the secondary chrome; the chrome rendering is not ported yet.
	}

	// all XCBBs are backed by a comp brush
	// For performance reason, if it's reveal and in fallback mode, comp brush is set to null
	// LVIP would detect this and not render it.
	internal static bool IsNullCompositionBrush(Brush? brush)
	{
		var nullCompositionBrush = false;
		// Uno-specific: WinUI matches the core XamlCompositionBrushBase type, which every app-defined subclass shares.
		if (brush is XamlCompositionBrushBase xamlCompositionBrush)
		{
			nullCompositionBrush = xamlCompositionBrush.GetValue(XamlCompositionBrushBase.CompositionBrushProperty) is null;
		}
		return nullCompositionBrush;
	}

	internal static bool IsOpaqueBrush(Brush brush)
	{
		if (brush is SolidColorBrush solidColorBrush)
		{
			return solidColorBrush.Color.A == 0xFF;
		}
		else if (brush is GradientBrush gradientBrush)
		{
			// MUX Reference gradient.cpp (CGradientBrush::IsOpaque)
			var gradientStops = gradientBrush.GradientStops;
			var stopHasAlpha = false;
			for (var i = 0; !stopHasAlpha && i < gradientStops.Count; i++)
			{
				if (gradientStops[i].Color.A != 0xFF)
				{
					stopHasAlpha = true;
				}
			}

			return gradientStops.Count > 0 && !stopHasAlpha;
		}
		return false;
	}

	// Invoked when a TAEF test calls TestServices::Utilities::DeleteResourceDictionaryCaches().
	internal static void ClearIsRoundedListViewBaseItemChromeEnabledCache()
		=> s_isRoundedListViewBaseItemChromeEnabled = null;

	internal static bool IsRoundedListViewBaseItemChromeEnabledStatic()
	{
		if (IsRoundedListViewBaseItemChromeForced())
		{
			return true;
		}

		return DependencyProperty.GetBooleanThemeResourceValue("ListViewBaseItemRoundedChromeEnabled");
	}

	internal static bool IsRoundedListViewBaseItemChromeForced()
	{
		if (ListViewBaseItemChromeRuntimeFeatures.DenyRoundedListViewBaseItemChrome)
		{
			return false;
		}

		return ListViewBaseItemChromeRuntimeFeatures.ForceRoundedListViewBaseItemChrome;
	}

	// Dead WinUI code: IsSelectionIndicatorVisualForced (DBG only, fallback colors).
}
