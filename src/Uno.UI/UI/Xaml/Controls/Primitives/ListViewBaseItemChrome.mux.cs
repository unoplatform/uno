// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/2.5.1

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DirectUI;
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

	private const string c_strCheckMarkGlyphStorage = "\uE73E";

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

	// TODO Uno: AddRectangle and AddBorder (C:262-329) come with the chrome rendering layers (C6).

	// Dead WinUI code: AddChromeAssociatedPath (C:331-376).

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
	protected override Size MeasureOverride(Size availableSize) => MeasureNewStyle(availableSize);

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize) => ArrangeNewStyle(finalSize);

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

	// Reparents the inner selection border if needed and updates its affected properties.
	// According to the visual design, the inner and outer borders are meant to be overlapping each other. In order to avoid bleed through at the borders' edges though,
	// we can afford to host a bloated inner border inside the outer border when the latter is opaque. That trick is not applicable when the outer border is semi-transparent,
	// and that is OK since the bleed through is not obvious in those cases.
	internal void UpdateBordersParenting()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		if (m_innerSelectionBorder is null)
		{
			return;
		}

		var isOuterBorderBrushOpaque = IsOuterBorderBrushOpaque();
		var areBordersNested = m_outerBorder == m_innerSelectionBorder.GetParent();

		if (isOuterBorderBrushOpaque != areBordersNested)
		{
			if (areBordersNested)
			{
				m_outerBorder!.Child = null;
			}
			else
			{
				RemoveChild(m_innerSelectionBorder);
			}

			if (isOuterBorderBrushOpaque)
			{
				m_outerBorder!.Child = m_innerSelectionBorder;
			}
			else
			{
				AddChild(m_innerSelectionBorder);
			}

			SetInnerSelectionBorderProperties();
		}
	}

	// TODO Uno: EnsureTransitionTargets (C:1520-1567) comes with the animation command queue.

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

	// Sets the border CornerRadius based on value returned by GetGeneralCornerRadius.
	private void SetGeneralCornerRadius(Border border)
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(border is not null);

		var cornerRadius = GetGeneralCornerRadius();

		border!.CornerRadius = cornerRadius;
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

	// Sets whether or not the drag overlay text block is shown.
	internal void SetDragOverlayTextBlockVisible(bool isVisible)
	{
		if (isVisible)
		{
			EnsureDragOverlayTextBlock();

			EnsureMultiSelectCheckBox();

			// For RS1, drag items count text will show inside a border, and a background is also applied to the text.
			// So, we re-use the multi-select checkbox to have the drag count textblock as its child. This will produce the effect of a border around the text.
			// WinUI quirk: the check box Clip and child are never restored when the overlay is hidden again.
			m_multiSelectCheckBoxRectangle!.Clip = null;
			m_multiSelectCheckBoxRectangle.Child = m_pDragItemsCountTextBlock;

			const string c_borderBrush = "SystemControlBackgroundChromeWhiteBrush";
			var borderBrush = Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(c_borderBrush);
			if (borderBrush != null)
			{
				m_multiSelectCheckBoxRectangle.BorderBrush = borderBrush as Brush;
			}

			const string c_background = "SystemControlBackgroundAccentBrush";
			var backgroundBrush = Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(c_background);
			if (backgroundBrush != null)
			{
				m_multiSelectCheckBoxRectangle.Background = backgroundBrush as Brush;
			}

			m_multiSelectCheckBoxRectangle.Visibility = Visibility.Visible;

			if (m_checkMode == ListViewItemPresenterCheckMode.Overlay)
			{
				// For GridView, the item count should show in the center just like in File Explorer
				m_multiSelectCheckBoxRectangle.BorderThickness = new Thickness(2.0, 2.0, 2.0, 2.0);

				m_multiSelectCheckBoxRectangle.VerticalAlignment = VerticalAlignment.Center;

				m_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Center;
			}

			InvalidateMeasure();
		}

		if (m_pDragItemsCountTextBlock is not null)
		{
			m_pDragItemsCountTextBlock.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	// Sets the drag count display.
	internal void SetDragItemsCount(uint dragItemsCount)
	{
		EnsureDragOverlayTextBlock();

		m_pDragItemsCountTextBlock!.Text = dragItemsCount.ToString(CultureInfo.InvariantCulture);
	}

	internal void SetSwipeHintCheckOpacity(float opacity)
	{
		if (m_swipeHintCheckOpacity != opacity)
		{
			m_swipeHintCheckOpacity = opacity;

			// TODO Uno: CContentControl::NWSetContentDirty(m_pParentListViewBaseItemNoRef, DirtyFlags::Bounds) has no Uno dirty-flag render model.
		}
	}

	// Sets up and adds the drag overlay text block to the tree.
	private void EnsureDragOverlayTextBlock()
	{
		if (m_pDragItemsCountTextBlock is null)
		{
			m_pDragItemsCountTextBlock = new TextBlock();
			SetDragOverlayTextBlockProperties();
		}
	}

	private void SetDragOverlayTextBlockProperties()
	{
		if (m_pDragItemsCountTextBlock is not null)
		{
			m_pDragItemsCountTextBlock.IsHitTestVisible = false;

			AutomationProperties.SetAccessibilityView(m_pDragItemsCountTextBlock, AccessibilityView.Raw);

			// Keep the drag items count textblock collapsed by default. It is made visible on demand when we change visual states.
			m_pDragItemsCountTextBlock.Visibility = Visibility.Collapsed;

			const string c_style = "CaptionTextBlockStyle";
			var style = Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(c_style);
			if (style != null)
			{
				m_pDragItemsCountTextBlock.Style = style as Style;
			}

			m_pDragItemsCountTextBlock.HorizontalAlignment = HorizontalAlignment.Center;

			m_pDragItemsCountTextBlock.VerticalAlignment = VerticalAlignment.Center;
		}
	}

	// Sets up the secondary chrome and adds it to the tree.
	internal void AddSecondaryChrome()
	{
		if (m_pSecondaryChrome is null)
		{
			m_pSecondaryChrome = new ListViewBaseItemSecondaryChrome();

			m_pSecondaryChrome.m_pPrimaryChromeNoRef = this;
			AddChild(m_pSecondaryChrome);
		}
	}

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

	// Uno-specific: CListViewBaseItemChrome::OnPropertyChanged (C:2377-2383) calls the base then OnPropertyChangedNewStyle;
	// Uno routes the presenter DPs through OnChromePropertyChanged and ContentPresenter.CornerRadius through OnCornerRadiusChanged.

	private Size MeasureNewStyle(Size availableSize)
	{
		var contentMargin = m_contentMargin;
		Thickness controlBorderThickness = default;
		Size totalSize = default;

		if (m_backplateRectangle is null && IsRoundedListViewBaseItemChromeEnabled())
		{
			EnsureBackplate();
		}

		var pTemplateChild = GetTemplateChildIfExists();

		// We can't be used without a parent LVB.
		var pParentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

		controlBorderThickness = pParentListViewBaseItemNoRef.BorderThickness;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref contentMargin);
			LayoutRoundHelper(ref controlBorderThickness);
		}

		if (pTemplateChild is not null)
		{
			var contentAvailableSize = availableSize;
			var contentPrefixWidth = 0.0f;

			// Size available for content is availableSize - content margin - control border
			CSizeUtil.Deflate(ref contentAvailableSize, contentMargin);
			CSizeUtil.Deflate(ref contentAvailableSize, controlBorderThickness);

			// Check to see if we need to offset the content due to the potential CheckBox in Inline MultiSelect state.
			if (GetSelectionCheckMarkVisualEnabled() &&
				m_visualStates.HasState(MultiSelectStates.MultiSelectEnabled) &&
				m_checkMode == ListViewItemPresenterCheckMode.Inline)
			{
				contentPrefixWidth = IsRoundedListViewBaseItemChromeEnabled() ? s_multiSelectRoundedContentOffset : s_listViewItemMultiSelectContentOffset;
			}

			if (IsInSelectionIndicatorMode() &&
				GetSelectionIndicatorMode() == ListViewItemPresenterSelectionIndicatorMode.Inline)
			{
				contentPrefixWidth = Math.Max(contentPrefixWidth, (float)(s_selectionIndicatorMargin.Left + s_selectionIndicatorSize.Width + s_selectionIndicatorMargin.Right));
			}

			// subtract the offset to have the child arrange using the new width
			contentAvailableSize.Width -= contentPrefixWidth;

			pTemplateChild.Measure(contentAvailableSize);
			// Uno-specific: as in ContentPresenter.MeasureOverride.
			pTemplateChild.EnsureLayoutStorage();

			// Use child's desired size for our size.
			totalSize = pTemplateChild.DesiredSize;

			// If SelectionMode is Multiple and a CheckBox is potentially visible (Inline mode), we add the buffer of the CheckBox back.
			// If SelectionMode is Single or Extended and a SelectionIndicator is potentially visible (Inline mode), we add the buffer of the SelectionIndicator back.
			totalSize.Width += contentPrefixWidth;
		}

		// border should be accounted for regardless if there is content.
		CSizeUtil.Inflate(ref totalSize, contentMargin);
		CSizeUtil.Inflate(ref totalSize, controlBorderThickness);

		if (GetSelectionCheckMarkVisualEnabled() && m_multiSelectCheckBoxRectangle is not null)
		{
			m_multiSelectCheckBoxRectangle.Measure(totalSize);
		}

		if (IsSelectionIndicatorVisualEnabled() && m_selectionIndicatorRectangle is not null)
		{
			m_selectionIndicatorRectangle.Measure(totalSize);
		}

		if (m_backplateRectangle is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			m_backplateRectangle.Measure(totalSize);
		}

		if (m_innerSelectionBorder is not null && m_outerBorder != m_innerSelectionBorder.GetParent())
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_innerSelectionBorder.Measure(totalSize);
		}

		if (m_outerBorder is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_outerBorder.Measure(totalSize);
		}

		// Minimum size
		{
			var minimumSize = new Size(pParentListViewBaseItemNoRef.MinWidth, pParentListViewBaseItemNoRef.MinHeight);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref minimumSize);
			}

			totalSize.Width = Math.Max(totalSize.Width, minimumSize.Width);
			totalSize.Height = Math.Max(totalSize.Height, minimumSize.Height);
		}

		return totalSize;
	}

	private Size ArrangeNewStyle(Size finalSize)
	{
		var contentMargin = m_contentMargin;
		var finalBounds = new Rect(0.0, 0.0, finalSize.Width, finalSize.Height);
		Rect controlBorderBounds = default;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref contentMargin);
		}


		// Peel off content margin whitespace.
		controlBorderBounds = finalBounds;
		CSizeUtil.Deflate(ref controlBorderBounds, contentMargin);

		var contentPrefixWidth = 0.0f;

		if (GetSelectionCheckMarkVisualEnabled() && m_visualStates.HasState(MultiSelectStates.MultiSelectEnabled))
		{
			var multiSelectSquareSize = s_multiSelectSquareSize;

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref multiSelectSquareSize);
			}

			// If checkmark is visible, make sure there's at least enough space to show it.
			finalBounds.Width = Math.Max(finalBounds.Width, multiSelectSquareSize.Width);
			finalBounds.Height = Math.Max(finalBounds.Height, multiSelectSquareSize.Height);

			// Check to see if we need to offset the content due to the CheckBox in MultiSelect state.
			if (m_checkMode == ListViewItemPresenterCheckMode.Inline)
			{
				contentPrefixWidth = IsRoundedListViewBaseItemChromeEnabled() ? s_multiSelectRoundedContentOffset : s_listViewItemMultiSelectContentOffset;
			}
		}

		if (IsInSelectionIndicatorMode())
		{
			var selectionIndicatorSize = new Size(s_selectionIndicatorMargin.Left + s_selectionIndicatorSize.Width + s_selectionIndicatorMargin.Right, s_selectionIndicatorHeightShrinkage + 1.0f);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref selectionIndicatorSize);
			}

			// If a selection indicator is potentially visible, make sure there's at least enough space to show it.
			finalBounds.Width = Math.Max(finalBounds.Width, selectionIndicatorSize.Width);
			finalBounds.Height = Math.Max(finalBounds.Height, selectionIndicatorSize.Height);

			// Check to see if we need to offset the content due to the Inline selection indicator.
			if (GetSelectionIndicatorMode() == ListViewItemPresenterSelectionIndicatorMode.Inline)
			{
				contentPrefixWidth = (float)Math.Max(contentPrefixWidth, selectionIndicatorSize.Width);
			}
		}

		if (contentPrefixWidth != 0)
		{
			// will be used by ArrangeTemplateChild
			controlBorderBounds.X += contentPrefixWidth;

			// subtract the offset to have the child arrange using the new width
			controlBorderBounds.Width -= contentPrefixWidth;
		}

		if (m_multiSelectCheckBoxRectangle is not null)
		{
			m_multiSelectCheckBoxRectangle.Arrange(finalBounds);
		}

		if (m_selectionIndicatorRectangle is not null)
		{
			var selectionIndicatorBounds = finalBounds;
			var selectionIndicatorHeight = GetSelectionIndicatorHeightFromAvailableHeight((float)finalBounds.Height);

			if (m_visualStates.HasState(CommonStates2.Pressed) ||
				m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				global::System.Diagnostics.Debug.Assert(selectionIndicatorHeight > s_selectionIndicatorHeightShrinkage);
				selectionIndicatorHeight -= s_selectionIndicatorHeightShrinkage;
			}

			var excessAvailableHeight = (float)finalBounds.Height - selectionIndicatorHeight;

			if (excessAvailableHeight > 0)
			{
				selectionIndicatorBounds.Y += excessAvailableHeight / 2.0f;
				selectionIndicatorBounds.Height -= excessAvailableHeight;
			}

			m_selectionIndicatorRectangle.Arrange(selectionIndicatorBounds);
		}

		if (m_backplateRectangle is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			m_backplateRectangle.Arrange(finalBounds);
		}

		if (m_innerSelectionBorder is not null && m_outerBorder != m_innerSelectionBorder.GetParent())
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_innerSelectionBorder.Arrange(finalBounds);
		}

		if (m_outerBorder is not null)
		{
			global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
			global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
			m_outerBorder.Arrange(finalBounds);
		}

		// TODO Uno: WinUI never lays out the secondary chrome; Uno does not render a never-arranged element, so it gets the primary bounds.
		m_pSecondaryChrome?.Arrange(new Rect(0.0, 0.0, finalBounds.Width, finalBounds.Height));

		var newFinalSize = new Size(finalBounds.Width, finalBounds.Height);

		ArrangeTemplateChild(controlBorderBounds);

		return newFinalSize;
	}

	private void ArrangeTemplateChild(Rect controlBorderBounds)
	{
		var pTemplateChild = GetTemplateChildIfExists();

		if (pTemplateChild is not null)
		{
			var contentAvailableSize = new Size(controlBorderBounds.Width, controlBorderBounds.Height);
			Size contentSize = default;
			Rect contentArrangedBounds = default;
			Thickness controlBorderThickness = default;

			var horizontalContentAlignment = HorizontalContentAlignment;
			var verticalContentAlignment = VerticalContentAlignment;

			// We can't be used without a parent LVB.
			var pParentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

			controlBorderThickness = pParentListViewBaseItemNoRef.BorderThickness;

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref controlBorderThickness);
			}

			// control border is not going to be available for content.
			CSizeUtil.Deflate(ref contentAvailableSize, controlBorderThickness);

			// If alignment is Stretch, use entire available size, otherwise control's desired size.
			contentSize.Width = (horizontalContentAlignment == HorizontalAlignment.Stretch) ? contentAvailableSize.Width : pTemplateChild.DesiredSize.Width;
			contentSize.Height = (verticalContentAlignment == VerticalAlignment.Stretch) ? contentAvailableSize.Height : pTemplateChild.DesiredSize.Height;

			FrameworkElement.ComputeAlignmentOffset(
				horizontalContentAlignment,
				verticalContentAlignment,
				contentAvailableSize,
				contentSize,
				out var offsetX,
				out var offsetY);
			contentArrangedBounds.X = offsetX;
			contentArrangedBounds.Y = offsetY;

			// making sure we are still within the boundaries of the control
			if (contentSize.Width > contentAvailableSize.Width)
			{
				contentSize.Width = contentAvailableSize.Width;
			}

			if (contentSize.Height > contentAvailableSize.Height)
			{
				contentSize.Height = contentAvailableSize.Height;
			}

			// Adjust top/left coordinate to account for space used for chrome.
			contentArrangedBounds.X += controlBorderThickness.Left + controlBorderBounds.X;
			contentArrangedBounds.Y += controlBorderThickness.Top + controlBorderBounds.Y;
			contentArrangedBounds.Width = contentSize.Width;
			contentArrangedBounds.Height = contentSize.Height;

			// Uno-specific: as in ContentPresenter.ArrangeOverride.
			pTemplateChild.EnsureLayoutStorage();
			pTemplateChild.Arrange(contentArrangedBounds);
		}
	}

	// Removes the multi-select checkbox from the tree.
	internal void RemoveMultiSelectCheckBox()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckBoxRectangle is not null);

		RemoveChild(m_multiSelectCheckBoxRectangle!);

		m_multiSelectCheckGlyph = null;
		m_multiSelectCheckBoxRectangle = null;
		m_multiSelectCheckBoxClip = null;
	}

	// Removes the selection indicator from the tree.
	internal void RemoveSelectionIndicator()
	{
		global::System.Diagnostics.Debug.Assert(m_selectionIndicatorRectangle is not null);

		RemoveChild(m_selectionIndicatorRectangle!);

		m_selectionIndicatorRectangle = null;
	}

	// Removes the inner selection border from the tree.
	internal void RemoveInnerSelectionBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

		// WinUI quirk: the DisabledStates branch calls RemoveOuterBorder first, so a nested inner border is then
		// "removed" from this presenter (a no-op) and stays inside the detached outer border.
		var areBordersNested = m_outerBorder == m_innerSelectionBorder!.GetParent();

		if (areBordersNested)
		{
			m_outerBorder!.Child = null;
		}
		else
		{
			RemoveChild(m_innerSelectionBorder);
		}

		m_innerSelectionBorder = null;
	}

	// Removes the outer border from the tree.
	internal void RemoveOuterBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		RemoveChild(m_outerBorder!);

		m_outerBorder = null;

		if (m_backplateRectangle is not null)
		{
			SetBackplateMargin();
		}
	}

	// Sets up and adds the multi-select checkbox to the tree.
	internal void EnsureMultiSelectCheckBox()
	{
		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		if (m_multiSelectCheckBoxRectangle is null)
		{
			var multiSelectSquareSize = s_multiSelectSquareSize;

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref multiSelectSquareSize);
			}

			m_multiSelectCheckBoxRectangle = new Border();

			m_multiSelectCheckBoxRectangle.IsHitTestVisible = false;

			m_multiSelectCheckBoxRectangle.MinWidth = multiSelectSquareSize.Width;

			m_multiSelectCheckBoxRectangle.Height = multiSelectSquareSize.Height;

			m_multiSelectCheckBoxRectangle.TransitionTarget = new TransitionTarget();

			AddChild(m_multiSelectCheckBoxRectangle);
		}

		// create checkmark glyph
		if (m_multiSelectCheckGlyph is null)
		{
			var strCheckMarkGlyph = c_strCheckMarkGlyphStorage;

			m_multiSelectCheckGlyph = new FontIcon();

			m_multiSelectCheckGlyph.IsHitTestVisible = false;

			m_multiSelectCheckGlyph.Opacity = selected ? 1.0 : 0.0;

			m_multiSelectCheckGlyph.FontSize = s_checkMarkGlyphFontSize;

			// Setting the glyph for the check mark
			m_multiSelectCheckGlyph.Glyph = strCheckMarkGlyph;
		}

		// add the glyph to the check box children
		m_multiSelectCheckBoxRectangle.Child = m_multiSelectCheckGlyph;

		SetMultiSelectCheckBoxProperties();
	}

	// Sets up and adds the selection indicator to the tree.
	internal void EnsureSelectionIndicator()
	{
		global::System.Diagnostics.Debug.Assert(IsInSelectionIndicatorMode());

		if (m_selectionIndicatorRectangle is null)
		{
			var selectionIndicatorSize = s_selectionIndicatorSize;
			var zeroThickness = default(Thickness);
			var selectionIndicatorMargin = new Thickness(s_selectionIndicatorMargin.Left, 0.0, s_selectionIndicatorMargin.Right, 0.0);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref selectionIndicatorSize);
				LayoutRoundHelper(ref selectionIndicatorMargin);
			}

			m_selectionIndicatorRectangle = new Border();

			m_selectionIndicatorRectangle.IsHitTestVisible = false;

			m_selectionIndicatorRectangle.Margin = selectionIndicatorMargin;

			m_selectionIndicatorRectangle.Width = selectionIndicatorSize.Width;

			m_selectionIndicatorRectangle.TransitionTarget = new TransitionTarget();

			m_selectionIndicatorRectangle.BorderBrush = null;

			m_selectionIndicatorRectangle.BorderThickness = zeroThickness;

			m_selectionIndicatorRectangle.VerticalAlignment = VerticalAlignment.Stretch;

			m_selectionIndicatorRectangle.HorizontalAlignment = HorizontalAlignment.Left;

			AddChild(m_selectionIndicatorRectangle);
		}

		SetSelectionIndicatorBackground();
		SetSelectionIndicatorCornerRadius();
	}

	// Sets up and adds the backplate to the tree.
	internal void EnsureBackplate()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());

		if (m_backplateRectangle is null)
		{
			var zeroThickness = default(Thickness);

			m_backplateRectangle = new Border();

			m_backplateRectangle.IsHitTestVisible = false;

			m_backplateRectangle.BorderBrush = null;

			if (IsChromeForListViewItem())
			{
				var backplateMargin = s_backplateMargin;

				if (ShouldUseLayoutRounding())
				{
					LayoutRoundHelper(ref backplateMargin);
				}

				m_backplateRectangle.Margin = backplateMargin;
			}

			m_backplateRectangle.BorderThickness = zeroThickness;

			m_backplateRectangle.VerticalAlignment = VerticalAlignment.Stretch;

			m_backplateRectangle.HorizontalAlignment = HorizontalAlignment.Stretch;

			// Inserting the backplate into first position so it is rendered underneath the content
			AddChild(m_backplateRectangle, 0);
		}

		SetBackplateCornerRadius();
		SetBackplateBackground();

		if (IsChromeForGridViewItem())
		{
			SetBackplateMargin();
		}
	}

	// Sets up and adds the inner selection border to the tree.
	internal void EnsureInnerSelectionBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());

		if (m_innerSelectionBorder is null)
		{
			m_innerSelectionBorder = new Border();

			m_innerSelectionBorder.IsHitTestVisible = false;

			m_innerSelectionBorder.Background = null;

			m_innerSelectionBorder.VerticalAlignment = VerticalAlignment.Stretch;

			m_innerSelectionBorder.HorizontalAlignment = HorizontalAlignment.Stretch;

			if (IsOuterBorderBrushOpaque())
			{
				// When the outer border is opaque, it hosts the inner border to avoid any bleed through at the edges.
				m_outerBorder!.Child = m_innerSelectionBorder;
			}
			else
			{
				// Appending the border into last position so it is rendered over the content
				AddChild(m_innerSelectionBorder);
			}
		}

		SetInnerSelectionBorderProperties();
	}

	// Sets up and adds the outer border to the tree.
	internal void EnsureOuterBorder()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());

		if (m_outerBorder is null)
		{
			m_outerBorder = new Border();

			m_outerBorder.IsHitTestVisible = false;
			m_outerBorder.Background = null;

			m_outerBorder.VerticalAlignment = VerticalAlignment.Stretch;

			m_outerBorder.HorizontalAlignment = HorizontalAlignment.Stretch;

			// Appending the border into last position so it is rendered over the content
			AddChild(m_outerBorder);
		}

		SetOuterBorderProperties();
	}

	// Sets up the multi-select checkbox to the tree. (colors, alignment, clip)
	internal void SetMultiSelectCheckBoxProperties()
	{
		var isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();

		var multiSelectSquareMargin = default(Thickness);

		if (m_checkMode == ListViewItemPresenterCheckMode.Inline)
		{
			// ListViewItemBase case
			if (m_multiSelectCheckBoxClip is null)
			{
				var multiSelectSquareBounds = new Rect(0.0, 0.0, s_multiSelectSquareSize.Width, s_multiSelectSquareSize.Height);

				m_multiSelectCheckBoxClip = new RectangleGeometry();

				m_multiSelectCheckBoxClip.Rect = multiSelectSquareBounds;
			}

			multiSelectSquareMargin = isRoundedListViewBaseItemChromeEnabled ? s_multiSelectRoundedSquareInlineMargin : s_multiSelectSquareInlineMargin;

			m_multiSelectCheckBoxRectangle!.VerticalAlignment = VerticalAlignment.Center;

			m_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Left;

			m_multiSelectCheckBoxRectangle.Clip = m_multiSelectCheckBoxClip;
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(m_checkMode == ListViewItemPresenterCheckMode.Overlay);

			// GridViewItemBase case
			if (isRoundedListViewBaseItemChromeEnabled)
			{
				var selectedBorderThickness = GetSelectedBorderThickness();

				multiSelectSquareMargin.Top = s_innerSelectionBorderThickness.Top + selectedBorderThickness.Top + 1.0;
				multiSelectSquareMargin.Right = s_innerSelectionBorderThickness.Right + selectedBorderThickness.Right + 1.0;
			}
			else
			{
				multiSelectSquareMargin = s_multiSelectSquareOverlayMargin;
			}

			m_multiSelectCheckBoxRectangle!.VerticalAlignment = VerticalAlignment.Top;

			m_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Right;
		}

		if (isRoundedListViewBaseItemChromeEnabled)
		{
			var cornerRadius = GetCheckBoxCornerRadius();

			m_multiSelectCheckBoxRectangle.CornerRadius = cornerRadius;
		}

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref multiSelectSquareMargin);
		}
		m_multiSelectCheckBoxRectangle.Margin = multiSelectSquareMargin;

		// set the check box background brush
		SetMultiSelectCheckBoxBackground();

		// set the check box border brush and thickness
		SetMultiSelectCheckBoxBorder();

		// set the glyph's foreground brush
		SetMultiSelectCheckBoxForeground();

		if (!isRoundedListViewBaseItemChromeEnabled)
		{
			SetForegroundBrush();
		}
	}

	// Sets up the inner selection border visual variable properties.
	internal void SetInnerSelectionBorderProperties()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

		SetInnerSelectionBorderBrush();
		SetInnerSelectionBorderCornerRadius();
		SetInnerSelectionBorderThickness();
	}

	// Sets up the outer border visual variable properties.
	internal void SetOuterBorderProperties()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		SetOuterBorderBrush();
		SetOuterBorderCornerRadius();
		SetOuterBorderThickness();
	}

	// Sets the backplate Background brush based on current visual state.
	internal void SetBackplateBackground()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(m_backplateRectangle is not null);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);
		var pointerOver =
			m_visualStates.HasState(CommonStates2.PointerOver) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected);
		var pressed =
			m_visualStates.HasState(CommonStates2.Pressed) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		Brush? backplateRectangleBackground = null;

		if (m_visualStates.HasState(DisabledStates.Disabled))
		{
			if (selected)
			{
				backplateRectangleBackground = m_pSelectedDisabledBackground;
			}
		}
		else if (pressed)
		{
			if (selected)
			{
				backplateRectangleBackground = m_pSelectedPressedBackground;
			}
			else
			{
				backplateRectangleBackground = m_pPressedBackground;
			}
		}
		else if (pointerOver)
		{
			if (selected)
			{
				backplateRectangleBackground = m_pSelectedPointerOverBackground;
			}
			else
			{
				backplateRectangleBackground = m_pPointerOverBackground;
			}
		}
		else if (selected)
		{
			backplateRectangleBackground = m_pSelectedBackground;
		}
		else
		{
			var parentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();

			if (parentListViewBaseItemNoRef is not null)
			{
				// WinUI quirk: item Background changes only invalidate render (LBI:1984-2010), so this stays stale until the next state change.
				backplateRectangleBackground = parentListViewBaseItemNoRef.Background;
			}
		}

		m_backplateRectangle!.Background = backplateRectangleBackground;
	}

	// Sets up the backplate CornerRadius.
	internal void SetBackplateCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(m_backplateRectangle is not null);

		SetGeneralCornerRadius(m_backplateRectangle!);
	}

	// Sets up the backplate Margin for GridViewItem.
	internal void SetBackplateMargin()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_backplateRectangle is not null);

		// When the outer border is present and opaque, the backplate gets a 1px margin to avoid any bleed through at the edges.
		var zeroThickness = default(Thickness);
		var oneThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
		var backplateMargin = IsOuterBorderBrushOpaque() ? oneThickness : zeroThickness;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref backplateMargin);
		}

		m_backplateRectangle!.Margin = backplateMargin;
	}

	// Sets the inner selection border's BorderBrush based on current SelectionBorderBrush property.
	internal void SetInnerSelectionBorderBrush()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

#if DEBUG
		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);
		global::System.Diagnostics.Debug.Assert(selected);
#endif

		m_innerSelectionBorder!.BorderBrush = m_pSelectedInnerBorderBrush;
	}

	// Sets the inner selection border's CornerRadius.
	internal void SetInnerSelectionBorderCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

		var cornerRadius = GetGeneralCornerRadius();
		var areBordersNested = m_outerBorder == m_innerSelectionBorder!.GetParent();

		if (areBordersNested)
		{
			var selectedBorderThickness = GetSelectedBorderThickness();

			// Decrease inner border corner radius to account for outer border thickness.
			cornerRadius.BottomLeft = Math.Max(s_innerBorderCornerRadius, cornerRadius.BottomLeft - selectedBorderThickness.Left);
			cornerRadius.BottomRight = Math.Max(s_innerBorderCornerRadius, cornerRadius.BottomRight - selectedBorderThickness.Right);
			cornerRadius.TopLeft = Math.Max(s_innerBorderCornerRadius, cornerRadius.TopLeft - selectedBorderThickness.Left);
			cornerRadius.TopRight = Math.Max(s_innerBorderCornerRadius, cornerRadius.TopRight - selectedBorderThickness.Right);
		}

		m_innerSelectionBorder.CornerRadius = cornerRadius;
	}

	// Sets the outer selection border's BorderBrush based on current visual state.
	internal void SetOuterBorderBrush()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		global::System.Diagnostics.Debug.Assert(selected || (!m_visualStates.HasState(DisabledStates.Disabled) && m_visualStates.HasState(CommonStates2.PointerOver)));

		Brush? borderBrush = null;

		if (selected)
		{
			if (m_visualStates.HasState(DisabledStates.Disabled))
			{
				borderBrush = m_pSelectedDisabledBorderBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				borderBrush = m_pSelectedPressedBorderBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PointerOverSelected))
			{
				borderBrush = m_pSelectedPointerOverBorderBrush;
			}
			else
			{
				borderBrush = m_pSelectedBorderBrush;
			}
		}
		else
		{
			borderBrush = m_pPointerOverBorderBrush;
		}

		m_outerBorder!.BorderBrush = borderBrush;

		UpdateBordersParenting();

		if (m_backplateRectangle is not null)
		{
			SetBackplateMargin();
		}
	}

	// Sets the outer border's CornerRadius.
	internal void SetOuterBorderCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		SetGeneralCornerRadius(m_outerBorder!);
	}

	// Sets the inner selection border's BorderThickness based on current SelectedBorderThickness property.
	internal void SetInnerSelectionBorderThickness()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_innerSelectionBorder is not null);

#if DEBUG
		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);
		global::System.Diagnostics.Debug.Assert(selected);
#endif

		var areBordersNested = m_outerBorder == m_innerSelectionBorder!.GetParent();
		var innerSelectionBorderThickness = s_innerSelectionBorderThickness;

		if (areBordersNested)
		{
			// When the outer border is opaque, it hosts the inner border which is expanded all around by a pixel
			// to avoid any bleed through at the edges and in-between the borders.
			innerSelectionBorderThickness.Left += 1.0;
			innerSelectionBorderThickness.Top += 1.0;
			innerSelectionBorderThickness.Right += 1.0;
			innerSelectionBorderThickness.Bottom += 1.0;

			var innerSelectionBorderMargin = new Thickness(-1.0, -1.0, -1.0, -1.0);

			if (ShouldUseLayoutRounding())
			{
				LayoutRoundHelper(ref innerSelectionBorderMargin);
			}

			m_innerSelectionBorder.Margin = innerSelectionBorderMargin;
		}
		else
		{
			var selectedBorderThickness = GetSelectedBorderThickness();

			innerSelectionBorderThickness.Left += selectedBorderThickness.Left;
			innerSelectionBorderThickness.Top += selectedBorderThickness.Top;
			innerSelectionBorderThickness.Right += selectedBorderThickness.Right;
			innerSelectionBorderThickness.Bottom += selectedBorderThickness.Bottom;
		}

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref innerSelectionBorderThickness);
		}

		m_innerSelectionBorder.BorderThickness = innerSelectionBorderThickness;
	}

	// Sets the outer selection border's BorderThickness based on current visual state.
	internal void SetOuterBorderThickness()
	{
		global::System.Diagnostics.Debug.Assert(IsRoundedListViewBaseItemChromeEnabled());
		global::System.Diagnostics.Debug.Assert(IsChromeForGridViewItem());
		global::System.Diagnostics.Debug.Assert(m_outerBorder is not null);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		global::System.Diagnostics.Debug.Assert(selected || (!m_visualStates.HasState(DisabledStates.Disabled) && m_visualStates.HasState(CommonStates2.PointerOver)));

		var outerBorderThickness = selected ? GetSelectedBorderThickness() : s_borderThickness;

		if (ShouldUseLayoutRounding())
		{
			LayoutRoundHelper(ref outerBorderThickness);
		}

		m_outerBorder!.BorderThickness = outerBorderThickness;
	}

	// Sets the multi-select checkbox background brush.
	internal void SetMultiSelectCheckBoxBackground()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckBoxRectangle is not null);
		global::System.Diagnostics.Debug.Assert(m_checkMode == ListViewItemPresenterCheckMode.Inline || m_checkMode == ListViewItemPresenterCheckMode.Overlay);

		var selected =
			m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected);

		Brush? checkBoxBrush = null;

		if (IsRoundedListViewBaseItemChromeEnabled())
		{
			if (m_visualStates.HasState(DisabledStates.Disabled))
			{
				checkBoxBrush = selected ? m_pCheckBoxSelectedDisabledBrush : m_pCheckBoxDisabledBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				checkBoxBrush = m_pCheckBoxSelectedPressedBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.Pressed))
			{
				checkBoxBrush = m_pCheckBoxPressedBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PointerOverSelected))
			{
				checkBoxBrush = m_pCheckBoxSelectedPointerOverBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PointerOver))
			{
				checkBoxBrush = m_pCheckBoxPointerOverBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.Selected))
			{
				checkBoxBrush = m_pCheckBoxSelectedBrush;
			}
			else
			{
				checkBoxBrush = m_pCheckBoxBrush;
			}
		}
		else if (m_checkMode == ListViewItemPresenterCheckMode.Overlay)
		{
			checkBoxBrush = selected ? m_pSelectedBackground : m_pCheckBoxBrush;
		}

		m_multiSelectCheckBoxRectangle!.Background = checkBoxBrush;
	}

	// Sets the multi-select checkbox border brush and thickness.
	internal void SetMultiSelectCheckBoxBorder()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckBoxRectangle is not null);
		global::System.Diagnostics.Debug.Assert(m_checkMode == ListViewItemPresenterCheckMode.Inline || m_checkMode == ListViewItemPresenterCheckMode.Overlay);

		var isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();
		var zeroThickness = default(Thickness);
		var multiSelectSquareThickness = s_multiSelectRoundedSquareThickness;
		Thickness value;
		Brush? checkBoxBorderBrush = null;

		if (isRoundedListViewBaseItemChromeEnabled)
		{
			var selected =
				m_visualStates.HasState(CommonStates2.Selected) ||
				m_visualStates.HasState(CommonStates2.PointerOverSelected) ||
				m_visualStates.HasState(CommonStates2.PressedSelected);

			if (selected)
			{
				value = zeroThickness;
			}
			else
			{
				if (ShouldUseLayoutRounding())
				{
					LayoutRoundHelper(ref multiSelectSquareThickness);
				}

				value = multiSelectSquareThickness;

				if (m_visualStates.HasState(DisabledStates.Disabled))
				{
					checkBoxBorderBrush = m_pCheckBoxDisabledBorderBrush;
				}
				else if (m_visualStates.HasState(CommonStates2.Pressed))
				{
					checkBoxBorderBrush = m_pCheckBoxPressedBorderBrush;
				}
				else if (m_visualStates.HasState(CommonStates2.PointerOver))
				{
					checkBoxBorderBrush = m_pCheckBoxPointerOverBorderBrush;
				}
				else
				{
					checkBoxBorderBrush = m_pCheckBoxBorderBrush;
				}
			}
		}
		else
		{
			if (m_checkMode == ListViewItemPresenterCheckMode.Inline)
			{
				// ListViewItemBase case
				value = s_multiSelectSquareThickness;

				checkBoxBorderBrush = m_pCheckBoxBrush;
			}
			else
			{
				// GridViewItemBase case, m_checkMode == ListViewItemPresenterCheckMode.Overlay
				value = zeroThickness;
			}
		}

		m_multiSelectCheckBoxRectangle!.BorderThickness = value;
		m_multiSelectCheckBoxRectangle.BorderBrush = checkBoxBorderBrush;
	}

	// Sets the multi-select checkbox glyph's foreground brush.
	internal void SetMultiSelectCheckBoxForeground()
	{
		global::System.Diagnostics.Debug.Assert(m_multiSelectCheckGlyph is not null);

		// set the Glyph's brush to m_pCheckBrush, m_pCheckPressedBrush or m_pCheckDisabledBrush
		var checkBrush = m_pCheckBrush;

		if (IsRoundedListViewBaseItemChromeEnabled())
		{
			if (m_visualStates.HasState(DisabledStates.Disabled))
			{
				checkBrush = m_pCheckDisabledBrush;
			}
			else if (m_visualStates.HasState(CommonStates2.PressedSelected))
			{
				checkBrush = m_pCheckPressedBrush;
			}
		}

		m_multiSelectCheckGlyph!.Foreground = checkBrush!;
	}

	// Sets the selection indicator background brush.
	internal void SetSelectionIndicatorBackground()
	{
		global::System.Diagnostics.Debug.Assert(IsSelectionIndicatorVisualEnabled());
		global::System.Diagnostics.Debug.Assert(m_selectionIndicatorRectangle is not null);

		var disabled = m_visualStates.HasState(DisabledStates.Disabled);
		var pressed = m_visualStates.HasState(CommonStates2.PressedSelected);
		var pointerOver = m_visualStates.HasState(CommonStates2.PointerOverSelected);

		global::System.Diagnostics.Debug.Assert(m_visualStates.HasState(CommonStates2.Selected) || pointerOver || pressed);

		Brush? selectionIndicatorRectangleBackground = null;

		if (disabled)
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorDisabledBrush;
		}
		else if (pressed)
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorPressedBrush;
		}
		else if (pointerOver)
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorPointerOverBrush;
		}
		else
		{
			selectionIndicatorRectangleBackground = m_pSelectionIndicatorBrush;
		}

		m_selectionIndicatorRectangle!.Background = selectionIndicatorRectangleBackground;
	}

	// Sets the selection indicator corner radius.
	internal void SetSelectionIndicatorCornerRadius()
	{
		global::System.Diagnostics.Debug.Assert(IsSelectionIndicatorVisualEnabled());
		global::System.Diagnostics.Debug.Assert(m_selectionIndicatorRectangle is not null);

		var cornerRadius = GetSelectionIndicatorCornerRadius();

		m_selectionIndicatorRectangle!.CornerRadius = cornerRadius;
	}

	// Sets / clears content's foreground brush in Common visual states.
	// This function is only called for new styles
	internal void SetForegroundBrush()
	{
		// Uno-specific: stands in for the CValue's IsUnset() state.
		var isForegroundValueSet = false;
		Brush? foregroundValue = null;

		if (m_visualStates.HasState(CommonStates2.Selected) ||
			m_visualStates.HasState(CommonStates2.PressedSelected) ||
			m_visualStates.HasState(CommonStates2.PointerOverSelected))
		{
			if (m_pSelectedForeground is not null)
			{
				foregroundValue = m_pSelectedForeground;
				isForegroundValueSet = true;
			}
		}
		else if (m_visualStates.HasState(CommonStates2.PointerOver) ||
			m_visualStates.HasState(CommonStates2.Pressed))
		{
			// PointerOverForeground is a Threshold property added to ListViewItemPresenter
			// GridViewItemPresenter does not have the PointerOverForeground property
			// Calling IsPropertyDefault on a property that does not exist will cause a crash
			if (this is not GridViewItemPresenter)
			{
				// We want to explicitly set the brush in 1 of 2 cases
				// 1- Brush not NULL meaning it is set to some color
				// 2- Developer explicitly sets it to NULL (By default, if the developer does not set the property, it is NULL)
				if (m_pPointerOverForeground is not null || GetCurrentHighestValuePrecedence(ListViewItemPresenter.PointerOverForegroundProperty) != DependencyPropertyValuePrecedences.DefaultValue)
				{
					foregroundValue = m_pPointerOverForeground;
					isForegroundValueSet = true;
				}
			}
		}

		// if the value is not set, we clear the Brush value
		if (!isForegroundValueSet)
		{
			ClearValue(ForegroundProperty, DependencyPropertyValuePrecedences.Animations);

			if (!IsRoundedListViewBaseItemChromeEnabled() &&
				m_checkMode == ListViewItemPresenterCheckMode.Inline &&
				m_multiSelectCheckBoxRectangle is not null)
			{
				// set the CheckBox's brush
				m_multiSelectCheckBoxRectangle.BorderBrush = m_pCheckBoxBrush;

				// set the CheckMark Glyph's brush
				m_multiSelectCheckGlyph!.Foreground = m_pCheckBrush!;
			}
		}
		else
		{
			SetValue(ForegroundProperty, foregroundValue, DependencyPropertyValuePrecedences.Animations);

			// in the case of Selection or PointerOver, we want the CheckBox and the glyph to have the same color as the item's Foreground
			if (!IsRoundedListViewBaseItemChromeEnabled() &&
				m_checkMode == ListViewItemPresenterCheckMode.Inline &&
				m_multiSelectCheckBoxRectangle is not null)
			{
				// set the CheckBox's brush
				m_multiSelectCheckBoxRectangle.BorderBrush = foregroundValue;

				// set the CheckMark Glyph's brush
				m_multiSelectCheckGlyph!.Foreground = foregroundValue!;
			}
		}
	}

	// Uno-specific: GetValue / SetValue (C:4918-4996) forward the deprecated alias DPs, see ListViewBaseItemPresenter.ForwardAlias.

	// Handles the property changed for the new ListViewBaseItem style for Threshold
	// WinUI quirk: the cases are mostly LVIP ids, so most GVIP brush changes don't refresh the chrome children.
	private protected void OnPropertyChangedNewStyle(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		// Brushes used for both Threshold and Blue
		if (property == ListViewItemPresenter.SelectedForegroundProperty ||
			property == GridViewItemPresenter.SelectedForegroundProperty ||
			property == ListViewItemPresenter.PointerOverForegroundProperty)
		{
			SetForegroundBrush();
		}
		else if (property == ListViewItemPresenter.SelectionIndicatorVisualEnabledProperty ||
			property == ListViewItemPresenter.SelectionCheckMarkVisualEnabledProperty ||
			property == GridViewItemPresenter.SelectionCheckMarkVisualEnabledProperty ||
			property == ListViewItemPresenter.CheckHintBrushProperty ||
			property == GridViewItemPresenter.CheckHintBrushProperty ||
			property == ListViewItemPresenter.CheckSelectingBrushProperty ||
			property == GridViewItemPresenter.CheckSelectingBrushProperty ||
			property == ListViewItemPresenter.SelectionIndicatorModeProperty)
		{
			InvalidateRender();
		}
		else if (property == ListViewItemPresenter.CheckModeProperty ||
			property == ListViewItemPresenter.CheckBrushProperty ||
			property == ListViewItemPresenter.CheckPressedBrushProperty ||
			property == ListViewItemPresenter.CheckDisabledBrushProperty ||
			property == ListViewItemPresenter.CheckBoxBrushProperty ||
			property == ListViewItemPresenter.CheckBoxBorderBrushProperty ||
			property == ListViewItemPresenter.CheckBoxPressedBorderBrushProperty ||
			property == ListViewItemPresenter.CheckBoxDisabledBorderBrushProperty ||
			property == ListViewItemPresenter.CheckBoxCornerRadiusProperty ||
			property == ListViewItemPresenter.SelectedBackgroundProperty ||
			property == ListViewItemPresenter.SelectedPointerOverBackgroundProperty ||
			property == ListViewItemPresenter.SelectedPressedBackgroundProperty ||
			property == ListViewItemPresenter.SelectedDisabledBackgroundProperty)
		{
			// WinUI quirk: CheckBoxPointerOverBorderBrush has no case, so changing it does not refresh the check box.
			// only update changes if the checkbox already exists
			if (m_multiSelectCheckBoxRectangle is not null)
			{
				SetMultiSelectCheckBoxProperties();
				InvalidateRender();
			}

			if (m_backplateRectangle is not null &&
				(property == ListViewItemPresenter.SelectedBackgroundProperty ||
				 property == ListViewItemPresenter.SelectedPointerOverBackgroundProperty ||
				 property == ListViewItemPresenter.SelectedPressedBackgroundProperty ||
				 property == ListViewItemPresenter.SelectedDisabledBackgroundProperty))
			{
				SetBackplateBackground();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectedInnerBorderBrushProperty)
		{
			if (m_innerSelectionBorder is not null)
			{
				SetInnerSelectionBorderBrush();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.CheckBoxPointerOverBrushProperty ||
			property == ListViewItemPresenter.CheckBoxPressedBrushProperty ||
			property == ListViewItemPresenter.CheckBoxDisabledBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedPointerOverBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedPressedBrushProperty ||
			property == ListViewItemPresenter.CheckBoxSelectedDisabledBrushProperty)
		{
			if (m_multiSelectCheckBoxRectangle is not null)
			{
				SetMultiSelectCheckBoxBackground();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.PointerOverBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedPointerOverBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedPressedBorderBrushProperty ||
			property == ListViewItemPresenter.SelectedDisabledBorderBrushProperty)
		{
			if (m_outerBorder is not null)
			{
				SetOuterBorderBrush();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectedBorderThicknessProperty)
		{
			if (m_outerBorder is not null)
			{
				SetOuterBorderThickness();
				InvalidateRender();
			}
			if (m_innerSelectionBorder is not null)
			{
				SetInnerSelectionBorderThickness();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectionIndicatorBrushProperty ||
			property == ListViewItemPresenter.SelectionIndicatorPointerOverBrushProperty ||
			property == ListViewItemPresenter.SelectionIndicatorPressedBrushProperty)
		{
			// WinUI quirk: SelectionIndicatorDisabledBrush has no case, so changing it does not refresh the indicator.
			if (m_selectionIndicatorRectangle is not null)
			{
				SetSelectionIndicatorBackground();
				InvalidateRender();
			}
		}
		else if (property == ListViewItemPresenter.SelectionIndicatorCornerRadiusProperty)
		{
			if (m_selectionIndicatorRectangle is not null)
			{
				SetSelectionIndicatorCornerRadius();
				InvalidateRender();
			}
		}
		else if (property == CornerRadiusProperty)
		{
			if (IsRoundedListViewBaseItemChromeEnabled())
			{
				if (m_backplateRectangle is not null)
				{
					SetBackplateCornerRadius();
					InvalidateRender();
				}

				if (m_outerBorder is not null)
				{
					SetOuterBorderCornerRadius();
					InvalidateRender();
				}

				if (m_innerSelectionBorder is not null)
				{
					SetInnerSelectionBorderCornerRadius();
					InvalidateRender();
				}
			}
		}
	}

	// all XCBBs are backed by a comp brush
	// For performance reason, if it's reveal and in fallback mode, comp brush is set to null
	// LVIP would detect this and not render it.
	internal static bool IsNullCompositionBrush(Brush? brush)
	{
		var nullCompositionBrush = false;
		// Uno-specific: WinUI matches the core XamlCompositionBrushBase type, which every app-defined subclass shares.
		// RadialGradientBrush renders without setting CompositionBrush, unlike WinUI's which sets it in OnConnected.
		if (brush is XamlCompositionBrushBase xamlCompositionBrush && brush is not RadialGradientBrush)
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
