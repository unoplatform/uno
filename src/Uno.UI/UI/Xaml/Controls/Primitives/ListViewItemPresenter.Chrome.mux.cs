// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/1.8.4

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

using static Microsoft.UI.Xaml.Controls._Tracing;

#pragma warning disable CS0649, CS0414, IDE0051

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewItemPresenter
{
	// =====================================================================
	// LayoutRoundHelper overloads (lines 224-253) - SKIP in Uno
	// Not needed; use LayoutRound from base class if needed.
	// =====================================================================

	// =====================================================================
	// ShouldUseLayoutRounding (lines 255-260)
	// =====================================================================
	private bool ShouldUseLayoutRounding()
	{
		// Similar to what Borders do, but we don't care about corner radius (ours is always 0).
		return UseLayoutRounding;
	}

#if !HAS_UNO
	// TODO Uno: AddRectangle (lines 262-286) - native rendering helper.
	// Renders a filled rectangle with the given brush using the content renderer.
	// Not applicable in Uno since rendering goes through the XAML visual tree.
#endif

#if !HAS_UNO
	// TODO Uno: AddBorder (lines 288-330) - native rendering helper.
	// Renders a hollow rectangle (nine-grid border) using the content renderer.
	// Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: AddChromeAssociatedPath (lines 331-638) - native rendering helper.
	// Renders a path-based chrome visual (checkmark, earmark) with transform and opacity.
	// Not applicable in Uno.
#endif

	// =====================================================================
	// Constructor (lines 639-671) - Not needed
	// Presenter has its own constructor in Properties.cs.
	// Visual state defaults are initialized inline in field declarations.
	// =====================================================================

	// =====================================================================
	// Destructor (lines 672-734) - Not needed
	// TODO Uno: In WinUI, the destructor clears animation commands and releases
	// all brush references. In Uno, the GC handles disposal.
	// =====================================================================

	// =====================================================================
	// UpdateVisualStateGroup (lines 734-764) - Already in h.mux.cs
	// =====================================================================

#if !HAS_UNO
	// TODO Uno: AppendCheckmarkTransform (lines 768-798) - native rendering transform.
	// Updates a transformation matrix for the checkmark taking into account
	// layout rounding and RTL. Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: AppendEarmarkTransform (lines 799-820) - native rendering transform.
	// Updates a transformation matrix for the earmark. Not applicable in Uno.
#endif

	// =====================================================================
	// MeasureOverride (lines 821-839)
	// =====================================================================
	protected override Size MeasureOverride(Size availableSize)
	{
		return MeasureNewStyle(availableSize);
	}

	// =====================================================================
	// ArrangeOverride (lines 840-855)
	// =====================================================================
	protected override Size ArrangeOverride(Size finalSize)
	{
		return ArrangeNewStyle(finalSize);
	}

	// =====================================================================
	// HasTemplateChild (lines 857-860)
	// =====================================================================
	private new bool HasTemplateChild()
	{
		return GetChromeTemplateChild() != null;
	}

	// =====================================================================
	// AddTemplateChild (lines 862-869)
	// =====================================================================
	private void AddTemplateChild(UIElement child)
	{
		MUX_ASSERT(GetChromeTemplateChild() == null);

		if (_backplateRectangle != null)
		{
			AddChild(child, 1);
		}
		else
		{
			AddChild(child, 0);
		}
	}

	// =====================================================================
	// RemoveTemplateChild (lines 871-881)
	// =====================================================================
	private void RemoveTemplateChild()
	{
		var templateChild = GetChromeTemplateChild();

		if (templateChild != null)
		{
			RemoveChild(templateChild);
		}
	}

	// =====================================================================
	// GetTemplateChildNoRef (lines 883-886)
	// =====================================================================
	private UIElement GetTemplateChildNoRef()
	{
		return GetChromeTemplateChild();
	}

	// =====================================================================
	// GetTemplateChildIfExists (lines 888-917)
	// =====================================================================
	private UIElement GetChromeTemplateChild()
	{
		int childCount = VisualTreeHelper.GetChildrenCount(this);
		if (childCount == 0)
		{
			return null;
		}

		UIElement templateChild = VisualTreeHelper.GetChild(this, 0) as UIElement;

		if (_backplateRectangle != null && templateChild == _backplateRectangle)
		{
			if (childCount > 1)
			{
				templateChild = VisualTreeHelper.GetChild(this, 1) as UIElement;
			}
			else
			{
				templateChild = null;
			}
		}

		if (templateChild != null &&
			templateChild != _multiSelectCheckBoxRectangle &&
			templateChild != _selectionIndicatorRectangle &&
			templateChild != _backplateRectangle &&
			templateChild != _innerSelectionBorder &&
			templateChild != _outerBorder)
		{
			return templateChild;
		}

		return null;
	}

#if !HAS_UNO
	// TODO Uno: SetWrongParentError (lines 921-956) - raises an error if no appropriate parent is set.
	// Not applicable in Uno since the presenter IS the chrome.
#endif

	// =====================================================================
	// GetParentListViewBaseItemNoRef (lines 957-973)
	// =====================================================================
	private ContentControl GetParentListViewBaseItemNoRef()
	{
		DependencyObject current = VisualTreeHelper.GetParent(this);
		while (current != null)
		{
			if (current is ListViewItem || current is GridViewItem)
			{
				return (ContentControl)current;
			}
			current = VisualTreeHelper.GetParent(current);
		}
		return null;
	}

	// =====================================================================
	// GetParentListViewBase (lines 974-982)
	// =====================================================================
	private ListViewBase GetParentListViewBase(ContentControl listViewBaseItem)
	{
		DependencyObject parent = listViewBaseItem;
		while (parent != null && parent is not ListViewBase)
		{
			parent = VisualTreeHelper.GetParent(parent);
		}
		return parent as ListViewBase;
	}

	// =====================================================================
	// GoToChromedState (lines 985-993)
	// =====================================================================
	private void GoToChromedState(string stateName, bool useTransitions, ref bool wentToState)
	{
		GoToChromedStateNewStyle(stateName, useTransitions, ref wentToState);
	}

#if !HAS_UNO
	// TODO Uno: DrawBaseLayer (lines 997-1067) - native rendering.
	// Draws the base layer (swipe hint check mark). Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: DrawUnderContentLayer (lines 1068-1168) - native rendering.
	// Draws the below-content layer (selection visuals, pointer over background, focus border).
	// Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: DrawOverContentLayer (lines 1169-1312) - native rendering.
	// Draws the above-content layer (placeholder, control border, selected border, earmark, checkmark).
	// Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: DrawDragOverlayLayer (lines 1313-1343) - native rendering.
	// Draws the above-content drag overlay rectangle. Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: RenderLayer (lines 1344-1373) - native rendering.
	// Dispatches rendering to DrawUnderContentLayerNewStyle / DrawOverContentLayerNewStyle
	// based on layer position. Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: ShouldDrawBaseLayerHere (lines 1374-1380) - native rendering layer check.
#endif

#if !HAS_UNO
	// TODO Uno: ShouldDrawUnderContentLayerHere (lines 1381-1387) - native rendering layer check.
#endif

#if !HAS_UNO
	// TODO Uno: ShouldDrawOverContentLayerHere (lines 1388-1405) - native rendering layer check.
#endif

#if !HAS_UNO
	// TODO Uno: GetNextPendingAnimation (lines 1407-1420) - animation not ported.
	// Returns the next pending animation command from the queue.
#endif

#if !HAS_UNO
	// TODO Uno: LockLayersForAnimation (lines 1421-1456) - animation not ported.
	// Assigns layer positions for the animation targets and manages priority.
#endif

#if !HAS_UNO
	// TODO Uno: UnlockLayersForAnimationAndDisposeCommand (lines 1457-1470) - animation not ported.
	// Unlocks layers for the given command and releases it.
#endif

	// =====================================================================
	// UpdateBordersParenting (lines 1473-1521)
	// =====================================================================
	private void UpdateBordersParenting()
	{
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_outerBorder != null);

		if (_innerSelectionBorder == null)
		{
			return;
		}

		bool isOuterBorderBrushOpaque = IsOuterBorderBrushOpaque();
		bool areBordersNested = _outerBorder == VisualTreeHelper.GetParent(_innerSelectionBorder);

		if (isOuterBorderBrushOpaque != areBordersNested)
		{
			if (areBordersNested)
			{
				_outerBorder.RemoveChild(_innerSelectionBorder);
			}
			else
			{
				RemoveChild(_innerSelectionBorder);
			}

			if (isOuterBorderBrushOpaque)
			{
				_outerBorder.AddChild(_innerSelectionBorder);
			}
			else
			{
				AddChild(_innerSelectionBorder);
			}

			SetInnerSelectionBorderProperties();
		}
	}

#if !HAS_UNO
	// TODO Uno: EnsureTransitionTarget (lines 1522-1569) - transition targets for animation.
	// Creates TransitionTargets for animation targets. Not applicable in Uno.
#endif

	// =====================================================================
	// IsRoundedListViewBaseItemChromeEnabled (lines 1570-1580)
	// =====================================================================
	private bool IsRoundedListViewBaseItemChromeEnabled()
	{
		// In Uno, rounded chrome is always enabled.
		return true;
	}

	// =====================================================================
	// IsChromeForGridViewItem (lines 1581-1586)
	// =====================================================================
	private bool IsChromeForGridViewItem()
	{
		return CheckMode == ListViewItemPresenterCheckMode.Overlay;
	}

	// =====================================================================
	// IsChromeForListViewItem (lines 1587-1593)
	// =====================================================================
	private bool IsChromeForListViewItem()
	{
		return CheckMode == ListViewItemPresenterCheckMode.Inline;
	}

	// =====================================================================
	// IsSelectionIndicatorVisualEnabled (lines 1595-1618)
	// =====================================================================
	private bool IsSelectionIndicatorVisualEnabled()
	{
		if (!IsChromeForListViewItem())
		{
			return false;
		}

		return IsRoundedListViewBaseItemChromeEnabled() && SelectionIndicatorVisualEnabled;
	}

	// =====================================================================
	// IsInSelectionIndicatorMode (lines 1619-1643)
	// =====================================================================
	private bool IsInSelectionIndicatorMode()
	{
		if (!IsSelectionIndicatorVisualEnabled())
		{
			return false;
		}

		var parentItem = GetParentListViewBaseItemNoRef();
		if (parentItem == null)
		{
			return false;
		}

		var listViewBase = GetParentListViewBase(parentItem);
		if (listViewBase == null)
		{
			return false;
		}

		var selectionMode = listViewBase.SelectionMode;
		return selectionMode == ListViewSelectionMode.Single || selectionMode == ListViewSelectionMode.Extended;
	}

	// =====================================================================
	// IsOuterBorderBrushOpaque (lines 1644-1660)
	// =====================================================================
	private bool IsOuterBorderBrushOpaque()
	{
		MUX_ASSERT(IsChromeForGridViewItem());

		if (_outerBorder == null)
		{
			return false;
		}

		var outerBorderBrush = _outerBorder.BorderBrush;
		return outerBorderBrush != null && IsOpaqueBrush(outerBorderBrush);
	}

	// =====================================================================
	// SetGeneralCornerRadius (lines 1663-1678)
	// =====================================================================
	private void SetGeneralCornerRadius(Border border)
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(border != null);

		border.CornerRadius = GetGeneralCornerRadius();
	}

	// =====================================================================
	// GetGeneralCornerRadius (lines 1679-1696)
	// =====================================================================
	private CornerRadius GetGeneralCornerRadius()
	{
		var cornerRadius = CornerRadius;

		if (IsRoundedListViewBaseItemChromeForced() &&
			cornerRadius.BottomLeft == 0.0 &&
			cornerRadius.BottomRight == 0.0 &&
			cornerRadius.TopLeft == 0.0 &&
			cornerRadius.TopRight == 0.0)
		{
			cornerRadius = new CornerRadius(ChromeConstants.GeneralCornerRadius);
		}

		return cornerRadius;
	}

	// =====================================================================
	// GetCheckBoxCornerRadius (lines 1697-1723)
	// =====================================================================
	private CornerRadius GetCheckBoxCornerRadius()
	{
		var cornerRadius = CheckBoxCornerRadius;

		if (IsRoundedListViewBaseItemChromeForced() &&
			cornerRadius.BottomLeft == 0.0 &&
			cornerRadius.BottomRight == 0.0 &&
			cornerRadius.TopLeft == 0.0 &&
			cornerRadius.TopRight == 0.0)
		{
			cornerRadius = ChromeConstants.DefaultCheckBoxCornerRadius;
		}

		return cornerRadius;
	}

	// =====================================================================
	// GetSelectionIndicatorCornerRadius (lines 1724-1749)
	// =====================================================================
	private CornerRadius GetSelectionIndicatorCornerRadius()
	{
		var cornerRadius = SelectionIndicatorCornerRadius;

		if (IsRoundedListViewBaseItemChromeForced() &&
			cornerRadius.BottomLeft == 0.0 &&
			cornerRadius.BottomRight == 0.0 &&
			cornerRadius.TopLeft == 0.0 &&
			cornerRadius.TopRight == 0.0)
		{
			cornerRadius = ChromeConstants.DefaultSelectionIndicatorCornerRadius;
		}

		return cornerRadius;
	}

	// =====================================================================
	// GetSelectionIndicatorHeightFromAvailableHeight (lines 1750-1761)
	// =====================================================================
	private float GetSelectionIndicatorHeightFromAvailableHeight(float availableHeight)
	{
		if (availableHeight <= ChromeConstants.SelectionIndicatorSize.Height)
		{
			return availableHeight;
		}

		return Math.Max((float)ChromeConstants.SelectionIndicatorSize.Height, availableHeight - (float)ChromeConstants.SelectionIndicatorMargin.Top - (float)ChromeConstants.SelectionIndicatorMargin.Bottom);
	}

	// =====================================================================
	// GetSelectionIndicatorMode (lines 1762-1784)
	// =====================================================================
	private ListViewItemPresenterSelectionIndicatorMode GetSelectionIndicatorMode()
	{
		return SelectionIndicatorMode;
	}

	// =====================================================================
	// GetRevealBackgroundBrushNoRef (lines 1785-1798)
	// =====================================================================
	private Brush GetRevealBackgroundBrushNoRef()
	{
		return RevealBackground;
	}

	// =====================================================================
	// GetRevealBorderBrushNoRef (lines 1799-1812)
	// =====================================================================
	private Brush GetRevealBorderBrushNoRef()
	{
		return RevealBorderBrush;
	}

	// =====================================================================
	// GetRevealBackgroundShowsAboveContent (lines 1813-1826)
	// =====================================================================
	private bool GetRevealBackgroundShowsAboveContent()
	{
		return RevealBackgroundShowsAboveContent;
	}

	// =====================================================================
	// GetSelectionCheckMarkVisualEnabled (lines 1827-1837)
	// =====================================================================
	private bool GetSelectionCheckMarkVisualEnabled()
	{
		return SelectionCheckMarkVisualEnabled;
	}

	// =====================================================================
	// GetRevealBorderThickness (lines 1838-1848)
	// =====================================================================
	private Thickness GetRevealBorderThickness()
	{
		return RevealBorderThickness;
	}

	// =====================================================================
	// GetSelectedBorderThickness (lines 1849-1859)
	// =====================================================================
	private Thickness GetSelectedBorderThickness()
	{
		return SelectedBorderThickness;
	}

	// =====================================================================
	// GetPointerOverBackgroundMargin (lines 1860-1870)
	// =====================================================================
	private Thickness GetPointerOverBackgroundMargin()
	{
		return PointerOverBackgroundMargin;
	}

	// =====================================================================
	// GetDisabledOpacity (lines 1871-1881)
	// =====================================================================
	private float GetDisabledOpacity()
	{
		return (float)DisabledOpacity;
	}

	// =====================================================================
	// GetDragOpacity (lines 1882-1892)
	// =====================================================================
	private float GetDragOpacity()
	{
		return (float)DragOpacity;
	}

	// =====================================================================
	// GetReorderHintOffset (lines 1893-1903)
	// =====================================================================
	private float GetReorderHintOffset()
	{
		return (float)ReorderHintOffset;
	}

#if !HAS_UNO
	// TODO Uno: NWSetContentDirty (lines 1904-1918) - native rendering dirty notification.
	// Marks the chrome and its parent dirty for re-rendering. Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: PrepareCheckPath (lines 1920-1979) - builds the checkmark path geometry.
	// Creates a PathGeometry from the checkmark points. Not applicable in Uno
	// since the checkmark is rendered via a FontIcon glyph.
#endif

#if !HAS_UNO
	// TODO Uno: AddLineSegmentToSegmentCollection (lines 1980-2002) - path geometry helper.
	// Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: GetCheckMarkBounds (lines 2003-2010) - returns bounds of the checkmark geometry.
	// Not applicable in Uno.
#endif

	// =====================================================================
	// SetDragOverlayTextBlockVisible (lines 2011-2074)
	// =====================================================================
	private void SetDragOverlayTextBlockVisible(bool isVisible)
	{
		// TODO Uno: Drag overlay text block not yet ported.
		// In WinUI, this creates/shows a text block inside the multi-select checkbox
		// for displaying the dragged items count.
	}

	// =====================================================================
	// SetDragItemsCount (lines 2075-2091)
	// =====================================================================
	private void SetDragItemsCount(uint dragItemsCount)
	{
		// TODO Uno: Drag items count display not yet ported.
	}

#if !HAS_UNO
	// TODO Uno: SetSwipeHintCheckOpacity (lines 2092-2108) - sets swipe hint opacity.
	// Not applicable in Uno.
#endif

	// =====================================================================
	// EnsureDragOverlayTextBlock (lines 2109-2120)
	// =====================================================================
	private void EnsureDragOverlayTextBlock()
	{
		// TODO Uno: Drag overlay text block not yet ported.
	}

	// =====================================================================
	// SetDragOverlayTextBlockProperties (lines 2121-2155)
	// =====================================================================
	private void SetDragOverlayTextBlockProperties()
	{
		// TODO Uno: Drag overlay text block properties not yet ported.
	}

#if !HAS_UNO
	// TODO Uno: AddSecondaryChrome (lines 2156-2178) - secondary chrome for drag animations.
	// Creates and adds a secondary chrome element used as an animation target
	// during drag operations. Not applicable in Uno.
#endif

	// =====================================================================
	// SetChromedListViewBaseItem (lines 2179-2195)
	// Not needed in Uno (presenter IS the chrome).
	// =====================================================================

#if !HAS_UNO
	// TODO Uno: EnqueueAnimationCommand (lines 2196-2207) - animation not ported.
	// Enqueues animation commands in FIFO order.
#endif

#if !HAS_UNO
	// TODO Uno: DequeueAnimationCommand (lines 2208-2224) - animation not ported.
	// Dequeues and returns the next animation command.
#endif

	// =====================================================================
	// ComputeReorderHintOffset (lines 2225-2254)
	// =====================================================================
	private Point ComputeReorderHintOffset(ChromeReorderHintStates state)
	{
		float reorderHintOffset = GetReorderHintOffset();
		float x = 0.0f;
		float y = 0.0f;

		switch (state)
		{
			case ChromeReorderHintStates.BottomReorderHint:
				y = reorderHintOffset;
				break;
			case ChromeReorderHintStates.TopReorderHint:
				y = -reorderHintOffset;
				break;
			case ChromeReorderHintStates.LeftReorderHint:
				x = -reorderHintOffset;
				break;
			case ChromeReorderHintStates.RightReorderHint:
				x = reorderHintOffset;
				break;
		}

		return new Point(x, y);
	}

	// =====================================================================
	// ComputeSwipeHintOffset (lines 2255-2273)
	// =====================================================================
	private Point ComputeSwipeHintOffset(ChromeSelectionHintStates state)
	{
		float x = 0.0f;
		float y = 0.0f;

		switch (state)
		{
			case ChromeSelectionHintStates.VerticalSelectionHint:
				y = (float)ChromeConstants.SwipeHintOffset.Y;
				break;
			case ChromeSelectionHintStates.HorizontalSelectionHint:
				x = (float)ChromeConstants.SwipeHintOffset.X;
				break;
		}

		return new Point(x, y);
	}

#if !HAS_UNO
	// TODO Uno: GenerateContentBounds (lines 2274-2288) - native rendering bounds.
	// Returns the content bounds (0,0,ActualWidth,ActualHeight). Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: HitTestLocalInternal (point) (lines 2289-2314) - native hit testing.
	// Custom hit test for the chrome. Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: HitTestLocalInternal (polygon) (lines 2315-2340) - native hit testing.
	// Custom hit test for the chrome with polygon. Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: HitTestLocalInternalPostChildren (point) (lines 2341-2351) - native hit testing.
	// Post-children hit test. Not applicable in Uno.
#endif

#if !HAS_UNO
	// TODO Uno: HitTestLocalInternalPostChildren (polygon) (lines 2352-2361) - native hit testing.
	// Post-children hit test with polygon. Not applicable in Uno.
#endif

	// =====================================================================
	// InvalidateRender (lines 2362-2376)
	// =====================================================================
	private void InvalidateRender()
	{
		InvalidateArrange();
	}

	// =====================================================================
	// OnPropertyChanged (lines 2377-2384)
	// =====================================================================
	// In WinUI, OnPropertyChanged is a virtual override on CContentPresenter.
	// In Uno, we use a static property change callback registered in the DP metadata.
	private static void OnChromePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is ListViewItemPresenter presenter)
		{
			presenter.OnPropertyChangedNewStyle(args.Property);
		}
	}

	// =====================================================================
	// MeasureNewStyle (lines 2385-2500)
	// =====================================================================
	private Size MeasureNewStyle(Size availableSize)
	{
		Thickness contentMargin = ContentMargin;
		Thickness controlBorderThickness = default;
		Size totalSize = default;

		if (_backplateRectangle == null && IsRoundedListViewBaseItemChromeEnabled())
		{
			EnsureBackplate();
		}

		UIElement pTemplateChild = GetChromeTemplateChild();
		ContentControl parentItem = GetParentListViewBaseItemNoRef();

		if (parentItem != null)
		{
			controlBorderThickness = parentItem.BorderThickness;
		}

		Size desiredSize = availableSize;

		if (pTemplateChild != null)
		{
			Size contentAvailableSize = availableSize;
			float contentPrefixWidth = 0.0f;

			// Size available for content is availableSize - content margin - control border
			contentAvailableSize = DeflateSize(contentAvailableSize, contentMargin);
			contentAvailableSize = DeflateSize(contentAvailableSize, controlBorderThickness);

			// Check to see if we need to offset the content due to the potential CheckBox in Inline MultiSelect state.
			if (GetSelectionCheckMarkVisualEnabled() &&
				_visualStates.HasState(ChromeMultiSelectStates.MultiSelectEnabled) &&
				CheckMode == ListViewItemPresenterCheckMode.Inline)
			{
				contentPrefixWidth = IsRoundedListViewBaseItemChromeEnabled() ? ChromeConstants.MultiSelectRoundedContentOffset : ChromeConstants.ListViewItemMultiSelectContentOffset;
			}

			if (IsInSelectionIndicatorMode() &&
				GetSelectionIndicatorMode() == ListViewItemPresenterSelectionIndicatorMode.Inline)
			{
				contentPrefixWidth = Math.Max(contentPrefixWidth,
					(float)(ChromeConstants.SelectionIndicatorMargin.Left + ChromeConstants.SelectionIndicatorSize.Width + ChromeConstants.SelectionIndicatorMargin.Right));
			}

			// subtract the offset to have the child arrange using the new width
			contentAvailableSize = new Size(
				Math.Max(0, contentAvailableSize.Width - contentPrefixWidth),
				contentAvailableSize.Height);

			pTemplateChild.Measure(contentAvailableSize);

			// Use child's desired size for our size.
			totalSize = pTemplateChild.DesiredSize;

			// If SelectionMode is Multiple and a CheckBox is potentially visible (Inline mode), we add the buffer of the CheckBox back.
			// If SelectionMode is Single or Extended and a SelectionIndicator is potentially visible (Inline mode), we add the buffer of the SelectionIndicator back.
			totalSize = new Size(totalSize.Width + contentPrefixWidth, totalSize.Height);
		}

		// border should be accounted for regardless if there is content.
		totalSize = InflateSize(totalSize, contentMargin);
		totalSize = InflateSize(totalSize, controlBorderThickness);

		if (GetSelectionCheckMarkVisualEnabled() && _multiSelectCheckBoxRectangle != null)
		{
			_multiSelectCheckBoxRectangle.Measure(totalSize);
		}

		if (IsSelectionIndicatorVisualEnabled() && _selectionIndicatorRectangle != null)
		{
			_selectionIndicatorRectangle.Measure(totalSize);
		}

		if (_backplateRectangle != null)
		{
			MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
			_backplateRectangle.Measure(totalSize);
		}

		if (_innerSelectionBorder != null && _outerBorder != VisualTreeHelper.GetParent(_innerSelectionBorder))
		{
			MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
			MUX_ASSERT(IsChromeForGridViewItem());
			_innerSelectionBorder.Measure(totalSize);
		}

		if (_outerBorder != null)
		{
			MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
			MUX_ASSERT(IsChromeForGridViewItem());
			_outerBorder.Measure(totalSize);
		}

		// Minimum size
		if (parentItem != null)
		{
			double minWidth = parentItem.MinWidth;
			double minHeight = parentItem.MinHeight;

			totalSize = new Size(
				Math.Max(totalSize.Width, minWidth),
				Math.Max(totalSize.Height, minHeight));
		}

		return totalSize;
	}

	// =====================================================================
	// ArrangeNewStyle (lines 2502-2625)
	// =====================================================================
	private Size ArrangeNewStyle(Size finalSize)
	{
		Thickness contentMargin = ContentMargin;
		Rect finalBounds = new Rect(0, 0, finalSize.Width, finalSize.Height);
		Rect controlBorderBounds;

		// Peel off content margin whitespace.
		controlBorderBounds = DeflateRect(finalBounds, contentMargin);

		float contentPrefixWidth = 0.0f;

		if (GetSelectionCheckMarkVisualEnabled() && _visualStates.HasState(ChromeMultiSelectStates.MultiSelectEnabled))
		{
			Size multiSelectSquareSize = ChromeConstants.MultiSelectSquareSize;

			// If checkmark is visible, make sure there's at least enough space to show it.
			finalBounds = new Rect(
				finalBounds.X,
				finalBounds.Y,
				Math.Max(finalBounds.Width, multiSelectSquareSize.Width),
				Math.Max(finalBounds.Height, multiSelectSquareSize.Height));

			// Check to see if we need to offset the content due to the CheckBox in MultiSelect state.
			if (CheckMode == ListViewItemPresenterCheckMode.Inline)
			{
				contentPrefixWidth = IsRoundedListViewBaseItemChromeEnabled() ? ChromeConstants.MultiSelectRoundedContentOffset : ChromeConstants.ListViewItemMultiSelectContentOffset;
			}
		}

		if (IsInSelectionIndicatorMode())
		{
			float selectionIndicatorWidth = (float)(ChromeConstants.SelectionIndicatorMargin.Left + ChromeConstants.SelectionIndicatorSize.Width + ChromeConstants.SelectionIndicatorMargin.Right);
			float selectionIndicatorMinHeight = ChromeConstants.SelectionIndicatorHeightShrinkage + 1.0f;

			// If a selection indicator is potentially visible, make sure there's at least enough space to show it.
			finalBounds = new Rect(
				finalBounds.X,
				finalBounds.Y,
				Math.Max(finalBounds.Width, selectionIndicatorWidth),
				Math.Max(finalBounds.Height, selectionIndicatorMinHeight));

			// Check to see if we need to offset the content due to the Inline selection indicator.
			if (GetSelectionIndicatorMode() == ListViewItemPresenterSelectionIndicatorMode.Inline)
			{
				contentPrefixWidth = Math.Max(contentPrefixWidth, selectionIndicatorWidth);
			}
		}

		if (contentPrefixWidth > 0)
		{
			// will be used by ArrangeTemplateChild
			controlBorderBounds = new Rect(
				controlBorderBounds.X + contentPrefixWidth,
				controlBorderBounds.Y,
				Math.Max(0, controlBorderBounds.Width - contentPrefixWidth),
				controlBorderBounds.Height);
		}

		if (_multiSelectCheckBoxRectangle != null)
		{
			_multiSelectCheckBoxRectangle.Arrange(finalBounds);
		}

		if (_selectionIndicatorRectangle != null)
		{
			Rect selectionIndicatorBounds = finalBounds;
			float selectionIndicatorHeight = GetSelectionIndicatorHeightFromAvailableHeight((float)finalBounds.Height);

			if (_visualStates.HasState(ChromeCommonStates2.Pressed) ||
				_visualStates.HasState(ChromeCommonStates2.PressedSelected))
			{
				MUX_ASSERT(selectionIndicatorHeight > ChromeConstants.SelectionIndicatorHeightShrinkage);
				selectionIndicatorHeight -= ChromeConstants.SelectionIndicatorHeightShrinkage;
			}

			float excessAvailableHeight = (float)finalBounds.Height - selectionIndicatorHeight;

			if (excessAvailableHeight > 0)
			{
				selectionIndicatorBounds = new Rect(
					selectionIndicatorBounds.X,
					selectionIndicatorBounds.Y + excessAvailableHeight / 2.0f,
					selectionIndicatorBounds.Width,
					selectionIndicatorBounds.Height - excessAvailableHeight);
			}

			_selectionIndicatorRectangle.Arrange(selectionIndicatorBounds);
		}

		if (_backplateRectangle != null)
		{
			MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
			_backplateRectangle.Arrange(finalBounds);
		}

		if (_innerSelectionBorder != null && _outerBorder != VisualTreeHelper.GetParent(_innerSelectionBorder))
		{
			MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
			MUX_ASSERT(IsChromeForGridViewItem());
			_innerSelectionBorder.Arrange(finalBounds);
		}

		if (_outerBorder != null)
		{
			MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
			MUX_ASSERT(IsChromeForGridViewItem());
			_outerBorder.Arrange(finalBounds);
		}

		Size newFinalSize = new Size(finalBounds.Width, finalBounds.Height);

		ArrangeTemplateChild(controlBorderBounds);

		return newFinalSize;
	}

	// =====================================================================
	// ArrangeTemplateChild (lines 2627-2689)
	// =====================================================================
	private void ArrangeTemplateChild(Rect controlBorderBounds)
	{
		UIElement pTemplateChild = GetChromeTemplateChild();

		if (pTemplateChild != null)
		{
			Size contentAvailableSize = new Size(controlBorderBounds.Width, controlBorderBounds.Height);
			Thickness controlBorderThickness = default;

			ContentControl parentItem = GetParentListViewBaseItemNoRef();
			HorizontalAlignment horizontalContentAlignment = HorizontalContentAlignment;
			VerticalAlignment verticalContentAlignment = VerticalContentAlignment;

			if (parentItem != null)
			{
				controlBorderThickness = parentItem.BorderThickness;
			}

			// control border is not going to be available for content.
			contentAvailableSize = DeflateSize(contentAvailableSize, controlBorderThickness);

			// If alignment is Stretch, use entire available size, otherwise control's desired size.
			double contentWidth = (horizontalContentAlignment == HorizontalAlignment.Stretch) ? contentAvailableSize.Width : pTemplateChild.DesiredSize.Width;
			double contentHeight = (verticalContentAlignment == VerticalAlignment.Stretch) ? contentAvailableSize.Height : pTemplateChild.DesiredSize.Height;

			double offsetX = 0;
			double offsetY = 0;

			// Compute alignment offset
			switch (horizontalContentAlignment)
			{
				case HorizontalAlignment.Center:
					offsetX = (contentAvailableSize.Width - contentWidth) / 2.0;
					break;
				case HorizontalAlignment.Right:
					offsetX = contentAvailableSize.Width - contentWidth;
					break;
				case HorizontalAlignment.Left:
				case HorizontalAlignment.Stretch:
				default:
					offsetX = 0;
					break;
			}

			switch (verticalContentAlignment)
			{
				case VerticalAlignment.Center:
					offsetY = (contentAvailableSize.Height - contentHeight) / 2.0;
					break;
				case VerticalAlignment.Bottom:
					offsetY = contentAvailableSize.Height - contentHeight;
					break;
				case VerticalAlignment.Top:
				case VerticalAlignment.Stretch:
				default:
					offsetY = 0;
					break;
			}

			// making sure we are still within the boundaries of the control
			if (contentWidth > contentAvailableSize.Width)
			{
				contentWidth = contentAvailableSize.Width;
			}

			if (contentHeight > contentAvailableSize.Height)
			{
				contentHeight = contentAvailableSize.Height;
			}

			// Adjust top/left coordinate to account for space used for chrome.
			Rect contentArrangedBounds = new Rect(
				offsetX + controlBorderThickness.Left + controlBorderBounds.X,
				offsetY + controlBorderThickness.Top + controlBorderBounds.Y,
				contentWidth,
				contentHeight);

			pTemplateChild.Arrange(contentArrangedBounds);
		}
	}

	// =====================================================================
	// GoToChromedStateNewStyle (lines 2691-3336)
	// =====================================================================
	private void GoToChromedStateNewStyle(string stateName, bool useTransitions, ref bool wentToState)
	{
		bool dirty = false;
		bool needsMeasure = false;
		bool needsArrange = false;
		bool disabledChanged = false;
		UIElement templateChild = GetChromeTemplateChild();

		bool isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();
		ChromeCommonStates2 oldCommonState2 = _visualStates.CommonState2;
		double listViewItemSelectionIndicatorContentOffset = ChromeConstants.SelectionIndicatorMargin.Left + ChromeConstants.SelectionIndicatorSize.Width + ChromeConstants.SelectionIndicatorMargin.Right;

		if (UpdateVisualStateGroup(stateName, s_commonStates2Map, ref _visualStates.CommonState2, ref wentToState))
		{
			bool selected =
				_visualStates.HasState(ChromeCommonStates2.Selected) ||
				_visualStates.HasState(ChromeCommonStates2.PressedSelected) ||
				_visualStates.HasState(ChromeCommonStates2.PointerOverSelected);

			bool roundedGridViewItem =
				isRoundedListViewBaseItemChromeEnabled && IsChromeForGridViewItem();

			if (selected)
			{
				if (_multiSelectCheckGlyph != null)
				{
					_multiSelectCheckGlyph.Opacity = 1.0;
				}

				if (roundedGridViewItem)
				{
					if (_outerBorder == null)
					{
						EnsureOuterBorder();
					}
					else
					{
						SetOuterBorderBrush();
						SetOuterBorderThickness();
					}

					if (_innerSelectionBorder == null)
					{
						EnsureInnerSelectionBorder();
					}
					else
					{
						SetInnerSelectionBorderBrush();
					}
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
					if (_innerSelectionBorder != null)
					{
						RemoveInnerSelectionBorder();
					}

					bool renderOuterBorder = !_visualStates.HasState(ChromeDisabledStates.Disabled) && _visualStates.HasState(ChromeCommonStates2.PointerOver);

					if (_outerBorder == null)
					{
						if (renderOuterBorder)
						{
							EnsureOuterBorder();
						}
					}
					else
					{
						if (!renderOuterBorder)
						{
							RemoveOuterBorder();
						}
						else
						{
							SetOuterBorderBrush();
							SetOuterBorderThickness();
						}
					}
				}
			}

			if (_multiSelectCheckBoxRectangle != null)
			{
				SetMultiSelectCheckBoxBackground();
				if (isRoundedListViewBaseItemChromeEnabled)
				{
					SetMultiSelectCheckBoxBorder();
				}
			}

			bool isInSelectionIndicatorMode = IsInSelectionIndicatorMode();

			if (isInSelectionIndicatorMode || _selectionIndicatorRectangle != null)
			{
				bool oldPressed =
					oldCommonState2 == ChromeCommonStates2.Pressed ||
					oldCommonState2 == ChromeCommonStates2.PressedSelected;
				bool pressed =
					_visualStates.HasState(ChromeCommonStates2.Pressed) ||
					_visualStates.HasState(ChromeCommonStates2.PressedSelected);

				bool updateSelectionIndicatorVisibility = true;
				bool showSelectionIndicator = true;

				if (isInSelectionIndicatorMode)
				{
					bool oldSelected = oldCommonState2 == ChromeCommonStates2.Selected || oldCommonState2 == ChromeCommonStates2.PressedSelected || oldCommonState2 == ChromeCommonStates2.PointerOverSelected;

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

					// TODO Uno: Enqueue animation command (SelectionIndicatorVisibility)
					// Apply steady state directly:
					if (_selectionIndicatorRectangle != null)
					{
						_selectionIndicatorRectangle.Opacity = showSelectionIndicator ? 1.0 : 0.0;
					}
				}
				else if (_selectionIndicatorRectangle != null && showSelectionIndicator && oldPressed != pressed)
				{
					// TODO Uno: Enqueue animation command (SelectionIndicatorVisibility shrink/expand)
					// In WinUI, this triggers an animation to shrink/expand the selection indicator.
					// Apply steady state directly via arrange.
				}

				if (showSelectionIndicator)
				{
					bool pointerOver = _visualStates.HasState(ChromeCommonStates2.PointerOverSelected);
					bool updateSelectionIndicatorBackground = updateSelectionIndicatorVisibility;

					if (!updateSelectionIndicatorVisibility)
					{
						updateSelectionIndicatorBackground = ((oldCommonState2 == ChromeCommonStates2.PointerOverSelected) != pointerOver) || ((oldCommonState2 == ChromeCommonStates2.PressedSelected) != pressed);
					}

					if (updateSelectionIndicatorBackground)
					{
						SetSelectionIndicatorBackground();
					}
				}

				if (oldPressed != pressed)
				{
					needsArrange = true;
				}
			}

			SetForegroundBrush();

			// MUX Reference ListViewBaseItemChrome.cpp, lines 2902-2938
			if (!isRoundedListViewBaseItemChromeEnabled)
			{
				if (_visualStates.HasState(ChromeCommonStates2.Pressed) ||
					_visualStates.HasState(ChromeCommonStates2.PressedSelected))
				{
					// TODO Uno: Enqueue ListViewBaseItemAnimationCommand_Pressed(pressed=true, isStarting=true, !useTransitions)
					// In WinUI, this triggers a PointerDownThemeAnimation on the template child.
				}
				else if (_visualStates.HasState(ChromeCommonStates2.Normal) ||
					_visualStates.HasState(ChromeCommonStates2.PointerOver) ||
					_visualStates.HasState(ChromeCommonStates2.Selected) ||
					_visualStates.HasState(ChromeCommonStates2.PointerOverSelected))
				{
					if (oldCommonState2 == ChromeCommonStates2.Pressed ||
						oldCommonState2 == ChromeCommonStates2.PressedSelected)
					{
						// TODO Uno: Enqueue ListViewBaseItemAnimationCommand_Pressed(pressed=false, isStarting=true, !useTransitions)
						// In WinUI, this triggers a PointerUpThemeAnimation on the template child.
					}
				}
				else
				{
					// TODO Uno: Enqueue ListViewBaseItemAnimationCommand_Pressed(pressed=false, isStarting=false, !useTransitions)
				}
			}

			if (_backplateRectangle != null)
			{
				MUX_ASSERT(isRoundedListViewBaseItemChromeEnabled);
				SetBackplateBackground();
			}

			dirty = true;
		}

		if (!wentToState &&
			UpdateVisualStateGroup(stateName, s_disabledStatesMap, ref _visualStates.DisabledState, ref wentToState))
		{
			bool roundedGridViewItem = isRoundedListViewBaseItemChromeEnabled && IsChromeForGridViewItem();

			if (roundedGridViewItem)
			{
				bool disabled = _visualStates.HasState(ChromeDisabledStates.Disabled);
				bool pointerOver = _visualStates.HasState(ChromeCommonStates2.PointerOver);
				bool selected2 =
					_visualStates.HasState(ChromeCommonStates2.Selected) ||
					_visualStates.HasState(ChromeCommonStates2.PressedSelected) ||
					_visualStates.HasState(ChromeCommonStates2.PointerOverSelected);

				if (_outerBorder == null)
				{
					if (selected2 || (!disabled && pointerOver))
					{
						EnsureOuterBorder();
					}
				}
				else
				{
					if (selected2 || (!disabled && pointerOver))
					{
						SetOuterBorderBrush();
					}
					else
					{
						RemoveOuterBorder();
					}
				}

				if (_innerSelectionBorder == null)
				{
					if (selected2)
					{
						EnsureInnerSelectionBorder();
					}
				}
				else
				{
					if (selected2)
					{
						SetInnerSelectionBorderBrush();
					}
					else
					{
						RemoveInnerSelectionBorder();
					}
				}
			}

			if (isRoundedListViewBaseItemChromeEnabled)
			{
				if (_backplateRectangle != null)
				{
					SetBackplateBackground();
				}

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

			disabledChanged = true;
			dirty = true;
		}

		if (!wentToState &&
			UpdateVisualStateGroup(stateName, s_focusStatesMap, ref _visualStates.FocusState, ref wentToState))
		{
			dirty = true;
		}

		if (!wentToState &&
			UpdateVisualStateGroup(stateName, s_multiSelectStatesMap, ref _visualStates.MultiSelectState, ref wentToState))
		{
			bool entering = _visualStates.HasState(ChromeMultiSelectStates.MultiSelectEnabled);
			double contentTranslationX = isRoundedListViewBaseItemChromeEnabled ? ChromeConstants.MultiSelectRoundedContentOffset : ChromeConstants.ListViewItemMultiSelectContentOffset;

			if (entering)
			{
				if (_isInIndicatorSelect)
				{
					contentTranslationX -= listViewItemSelectionIndicatorContentOffset;
				}
				_isInIndicatorSelect = false;
				_isInMultiSelect = true;
				EnsureMultiSelectCheckBox();
			}
			else if (IsInSelectionIndicatorMode())
			{
				contentTranslationX -= listViewItemSelectionIndicatorContentOffset;
			}

			// TODO Uno: Enqueue animation command (MultiSelect)
			// In WinUI, a multi-select animation is enqueued to slide the checkbox and content.
			// Apply steady state directly - the measure/arrange will handle positioning.

			needsMeasure = true;
			needsArrange = true;
			dirty = true;
		}

		if (!wentToState &&
			UpdateVisualStateGroup(stateName, s_selectionIndicatorStatesMap, ref _visualStates.SelectionIndicatorState, ref wentToState))
		{
			bool entering = _visualStates.HasState(ChromeSelectionIndicatorStates.SelectionIndicatorEnabled);
			bool enqueueAnimationCommand = !_isInMultiSelect;

			if (entering)
			{
				_isInIndicatorSelect = true;
				_isInMultiSelect = false;
			}
			else if (_visualStates.HasState(ChromeMultiSelectStates.MultiSelectDisabled))
			{
				_isInIndicatorSelect = false;
				_isInMultiSelect = false;
			}

			if (enqueueAnimationCommand)
			{
				// TODO Uno: Enqueue animation command (IndicatorSelect)
				// In WinUI, an indicator select animation is enqueued.
				// Apply steady state directly - the measure/arrange will handle positioning.
			}

			needsMeasure = true;
			needsArrange = true;
			dirty = true;
		}

		ChromeReorderHintStates oldReorderHintState = _visualStates.ReorderHintState;

		if (!wentToState &&
			UpdateVisualStateGroup(stateName, s_reorderHintStatesMap, ref _visualStates.ReorderHintState, ref wentToState))
		{
			if (_visualStates.HasState(ChromeReorderHintStates.NoReorderHint))
			{
				// TODO Uno: Enqueue animation command (ReorderHint stop)
				// Apply steady state directly.
			}
			else
			{
				if (oldReorderHintState != ChromeReorderHintStates.NoReorderHint)
				{
					// TODO Uno: Enqueue animation command (ReorderHint clear old)
					// Apply steady state directly.
				}

				// TODO Uno: Enqueue animation command (ReorderHint start)
				// Apply steady state directly.
			}
			dirty = true;
		}

		ChromeDragStates oldDragDropState = _visualStates.DragState;

		if (!wentToState &&
			UpdateVisualStateGroup(stateName, s_dragStatesMap, ref _visualStates.DragState, ref wentToState))
		{
			// MUX Reference ListViewBaseItemChrome.cpp, lines 3169-3282
			bool dragCountTextBlockVisible = false;

			if (_visualStates.HasState(ChromeDragStates.NotDragging))
			{
				// TODO Uno: Enqueue ListViewBaseItemAnimationCommand_DragDrop(DragDropState_Target, isStarting=false)
			}
			else
			{
				// TODO Uno: In WinUI, AddSecondaryChrome() is called here for the fade target.
				// Secondary chrome is not ported in Uno.

				if (_visualStates.HasState(ChromeDragStates.Dragging))
				{
					// DragDropState_SinglePrimary
				}
				else if (_visualStates.HasState(ChromeDragStates.MultipleDraggingPrimary))
				{
					dragCountTextBlockVisible = true;
					// DragDropState_MultiPrimary
				}
				else if (_visualStates.HasState(ChromeDragStates.MultipleDraggingSecondary))
				{
					// DragDropState_MultiSecondary
					// In WinUI, pFadeOutAnimationTarget = this (fade out self)
				}
				else if (_visualStates.HasState(ChromeDragStates.DraggingTarget))
				{
					// DragDropState_Target
				}
				else if (_visualStates.HasState(ChromeDragStates.DraggedPlaceholder))
				{
					// DragDropState_DraggedPlaceholder
					// In WinUI, pFadeOutAnimationTarget = this (fade out self)

					if (_visualStates.HasState(ChromeMultiSelectStates.MultiSelectEnabled))
					{
						EnsureMultiSelectCheckBox();
					}
					else if (_multiSelectCheckBoxRectangle != null)
					{
						// Extended selection mode, we suppress item count border for the placeholder
						RemoveMultiSelectCheckBox();
					}
				}
				else if (_visualStates.HasState(ChromeDragStates.Reordering))
				{
					// DragDropState_ReorderingSinglePrimary
				}
				else if (_visualStates.HasState(ChromeDragStates.MultipleReorderingPrimary))
				{
					dragCountTextBlockVisible = true;
					// DragDropState_ReorderingMultiPrimary
				}
				else if (_visualStates.HasState(ChromeDragStates.ReorderingTarget))
				{
					// DragDropState_ReorderingTarget
				}
				else if (_visualStates.HasState(ChromeDragStates.ReorderedPlaceholder))
				{
					// DragDropState_ReorderedPlaceholder
					// In WinUI, pFadeOutAnimationTarget = this (fade out self)

					if (_visualStates.HasState(ChromeMultiSelectStates.MultiSelectEnabled))
					{
						EnsureMultiSelectCheckBox();
					}
					else if (_multiSelectCheckBoxRectangle != null)
					{
						// Extended selection mode, we suppress item count border for the placeholder
						RemoveMultiSelectCheckBox();
					}
				}
				else if (_visualStates.HasState(ChromeDragStates.DragOver))
				{
					// DragDropState_DragOver
				}

				if (oldDragDropState != ChromeDragStates.NotDragging)
				{
					// Gotta clear out the old state first.
					// TODO Uno: Enqueue ListViewBaseItemAnimationCommand_DragDrop(isStarting=false, steadyStateOnly=true)
				}

				// TODO Uno: Enqueue ListViewBaseItemAnimationCommand_DragDrop(isStarting=true, !useTransitions)
			}

			SetDragOverlayTextBlockVisible(dragCountTextBlockVisible);
			dirty = true;
		}

		if (!wentToState &&
			UpdateVisualStateGroup(stateName, s_dataVirtualizationStatesMap, ref _visualStates.DataVirtualizationState, ref wentToState))
		{
			dirty = true;
		}

		if (disabledChanged)
		{
			float opacityValue;

			if (_visualStates.HasState(ChromeDisabledStates.Disabled))
			{
				opacityValue = GetDisabledOpacity();
			}
			else
			{
				MUX_ASSERT(_visualStates.HasState(ChromeDisabledStates.Enabled));
				opacityValue = 1.0f;
			}

			if (isRoundedListViewBaseItemChromeEnabled)
			{
				if (templateChild != null)
				{
					templateChild.Opacity = opacityValue;
				}
			}
			else
			{
				ContentControl parentListViewBaseItemNoRef = GetParentListViewBaseItemNoRef();
				if (parentListViewBaseItemNoRef != null)
				{
					parentListViewBaseItemNoRef.Opacity = opacityValue;
				}
			}
		}

		if (needsMeasure)
		{
			InvalidateMeasure();
		}

		if (needsArrange)
		{
			InvalidateArrange();
		}

		if (dirty)
		{
			InvalidateRender();
		}
	}

#if !HAS_UNO
	// TODO Uno: DrawUnderContentLayerNewStyle (lines 3339-3446) - native rendering.
	// Draws the below-content layer for new-style chrome (background rectangle,
	// reveal background below content). Not applicable in Uno since the backplate
	// Border element handles this via the visual tree.
#endif

#if !HAS_UNO
	// TODO Uno: DrawOverContentLayerNewStyle (lines 3447-3704) - native rendering.
	// Draws the above-content layer for new-style chrome (placeholder, control border,
	// overlay border, focus rectangle, reveal background/border above content).
	// Not applicable in Uno since Border elements handle this via the visual tree.
#endif

	// =====================================================================
	// RemoveMultiSelectCheckBox (lines 3705-3726)
	// =====================================================================
	private void RemoveMultiSelectCheckBox()
	{
		MUX_ASSERT(_multiSelectCheckBoxRectangle != null);

		RemoveChild(_multiSelectCheckBoxRectangle);

		_multiSelectCheckGlyph = null;
		_multiSelectCheckBoxRectangle = null;
	}

	// =====================================================================
	// RemoveSelectionIndicator (lines 3727-3746)
	// =====================================================================
	private void RemoveSelectionIndicator()
	{
		MUX_ASSERT(_selectionIndicatorRectangle != null);

		RemoveChild(_selectionIndicatorRectangle);

		_selectionIndicatorRectangle = null;
	}

	// =====================================================================
	// RemoveInnerSelectionBorder (lines 3747-3776)
	// =====================================================================
	private void RemoveInnerSelectionBorder()
	{
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_innerSelectionBorder != null);

		bool areBordersNested = _outerBorder == VisualTreeHelper.GetParent(_innerSelectionBorder);

		if (areBordersNested)
		{
			_outerBorder.RemoveChild(_innerSelectionBorder);
		}
		else
		{
			RemoveChild(_innerSelectionBorder);
		}

		_innerSelectionBorder = null;
	}

	// =====================================================================
	// RemoveOuterBorder (lines 3777-3802)
	// =====================================================================
	private void RemoveOuterBorder()
	{
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_outerBorder != null);

		RemoveChild(_outerBorder);

		_outerBorder = null;

		if (_backplateRectangle != null)
		{
			SetBackplateMargin();
		}
	}

	// =====================================================================
	// EnsureMultiSelectCheckBox (lines 3803-3882)
	// =====================================================================
	private void EnsureMultiSelectCheckBox()
	{
		bool selected =
			_visualStates.HasState(ChromeCommonStates2.Selected) ||
			_visualStates.HasState(ChromeCommonStates2.PointerOverSelected) ||
			_visualStates.HasState(ChromeCommonStates2.PressedSelected);

		if (_multiSelectCheckBoxRectangle == null)
		{
			Size multiSelectSquareSize = ChromeConstants.MultiSelectSquareSize;

			_multiSelectCheckBoxRectangle = new Border();

			_multiSelectCheckBoxRectangle.IsHitTestVisible = false;
			_multiSelectCheckBoxRectangle.MinWidth = multiSelectSquareSize.Width;
			_multiSelectCheckBoxRectangle.Height = multiSelectSquareSize.Height;

			AddChild(_multiSelectCheckBoxRectangle);
		}

		// create checkmark glyph
		if (_multiSelectCheckGlyph == null)
		{
			_multiSelectCheckGlyph = new FontIcon();

			_multiSelectCheckGlyph.IsHitTestVisible = false;
			_multiSelectCheckGlyph.Opacity = selected ? 1.0 : 0.0;
			_multiSelectCheckGlyph.FontSize = ChromeConstants.CheckMarkGlyphFontSize;
			_multiSelectCheckGlyph.Glyph = ChromeConstants.CheckMarkGlyph;
		}

		// add the glyph to the check box children
		_multiSelectCheckBoxRectangle.Child = _multiSelectCheckGlyph;

		SetMultiSelectCheckBoxProperties();
	}

	// =====================================================================
	// EnsureSelectionIndicator (lines 3883-3943)
	// =====================================================================
	private void EnsureSelectionIndicator()
	{
		MUX_ASSERT(IsInSelectionIndicatorMode());

		if (_selectionIndicatorRectangle == null)
		{
			Size selectionIndicatorSize = ChromeConstants.SelectionIndicatorSize;
			Thickness selectionIndicatorMargin = new Thickness(
				ChromeConstants.SelectionIndicatorMargin.Left, 0,
				ChromeConstants.SelectionIndicatorMargin.Right, 0);

			_selectionIndicatorRectangle = new Border();

			_selectionIndicatorRectangle.IsHitTestVisible = false;
			_selectionIndicatorRectangle.Margin = selectionIndicatorMargin;
			_selectionIndicatorRectangle.Width = selectionIndicatorSize.Width;

			_selectionIndicatorRectangle.BorderBrush = null;
			_selectionIndicatorRectangle.BorderThickness = default;

			_selectionIndicatorRectangle.VerticalAlignment = VerticalAlignment.Stretch;
			_selectionIndicatorRectangle.HorizontalAlignment = HorizontalAlignment.Left;

			AddChild(_selectionIndicatorRectangle);
		}

		SetSelectionIndicatorBackground();
		SetSelectionIndicatorCornerRadius();
	}

	// =====================================================================
	// EnsureBackplate (lines 3944-4005)
	// =====================================================================
	private void EnsureBackplate()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());

		if (_backplateRectangle == null)
		{
			_backplateRectangle = new Border();

			_backplateRectangle.IsHitTestVisible = false;
			_backplateRectangle.BorderBrush = null;

			if (IsChromeForListViewItem())
			{
				_backplateRectangle.Margin = ChromeConstants.BackplateMargin;
			}

			_backplateRectangle.BorderThickness = default;
			_backplateRectangle.VerticalAlignment = VerticalAlignment.Stretch;
			_backplateRectangle.HorizontalAlignment = HorizontalAlignment.Stretch;

			// Inserting the backplate into first position so it is rendered underneath the content
			AddChild(_backplateRectangle, 0);
		}

		SetBackplateCornerRadius();
		SetBackplateBackground();

		if (IsChromeForGridViewItem())
		{
			SetBackplateMargin();
		}
	}

	// =====================================================================
	// EnsureInnerSelectionBorder (lines 4006-4053)
	// =====================================================================
	private void EnsureInnerSelectionBorder()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());

		if (_innerSelectionBorder == null)
		{
			_innerSelectionBorder = new Border();

			_innerSelectionBorder.IsHitTestVisible = false;
			_innerSelectionBorder.Background = null;
			_innerSelectionBorder.VerticalAlignment = VerticalAlignment.Stretch;
			_innerSelectionBorder.HorizontalAlignment = HorizontalAlignment.Stretch;

			if (IsOuterBorderBrushOpaque())
			{
				// When the outer border is opaque, it hosts the inner border to avoid any bleed through at the edges.
				_outerBorder.AddChild(_innerSelectionBorder);
			}
			else
			{
				// Appending the border into last position so it is rendered over the content
				AddChild(_innerSelectionBorder);
			}
		}

		SetInnerSelectionBorderProperties();
	}

	// =====================================================================
	// EnsureOuterBorder (lines 4054-4092)
	// =====================================================================
	private void EnsureOuterBorder()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());

		if (_outerBorder == null)
		{
			_outerBorder = new Border();

			_outerBorder.IsHitTestVisible = false;
			_outerBorder.Background = null;
			_outerBorder.VerticalAlignment = VerticalAlignment.Stretch;
			_outerBorder.HorizontalAlignment = HorizontalAlignment.Stretch;

			// Appending the border into last position so it is rendered over the content
			AddChild(_outerBorder);
		}

		SetOuterBorderProperties();
	}

	// =====================================================================
	// SetMultiSelectCheckBoxProperties (lines 4093-4188)
	// =====================================================================
	private void SetMultiSelectCheckBoxProperties()
	{
		bool isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();

		Thickness multiSelectSquareMargin = default;

		if (CheckMode == ListViewItemPresenterCheckMode.Inline)
		{
			// ListViewItemBase case
			multiSelectSquareMargin = isRoundedListViewBaseItemChromeEnabled ? ChromeConstants.MultiSelectRoundedSquareInlineMargin : ChromeConstants.MultiSelectSquareInlineMargin;

			_multiSelectCheckBoxRectangle.VerticalAlignment = VerticalAlignment.Center;
			_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Left;
		}
		else
		{
			MUX_ASSERT(CheckMode == ListViewItemPresenterCheckMode.Overlay);

			// GridViewItemBase case
			if (isRoundedListViewBaseItemChromeEnabled)
			{
				Thickness selectedBorderThickness = GetSelectedBorderThickness();

				multiSelectSquareMargin = new Thickness(
					0,
					ChromeConstants.InnerSelectionBorderThickness.Top + selectedBorderThickness.Top + 1.0,
					ChromeConstants.InnerSelectionBorderThickness.Right + selectedBorderThickness.Right + 1.0,
					0);
			}
			else
			{
				multiSelectSquareMargin = ChromeConstants.MultiSelectSquareOverlayMargin;
			}

			_multiSelectCheckBoxRectangle.VerticalAlignment = VerticalAlignment.Top;
			_multiSelectCheckBoxRectangle.HorizontalAlignment = HorizontalAlignment.Right;
		}

		if (isRoundedListViewBaseItemChromeEnabled)
		{
			_multiSelectCheckBoxRectangle.CornerRadius = GetCheckBoxCornerRadius();
		}

		_multiSelectCheckBoxRectangle.Margin = multiSelectSquareMargin;

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

	// =====================================================================
	// SetInnerSelectionBorderProperties (lines 4189-4210)
	// =====================================================================
	private void SetInnerSelectionBorderProperties()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_innerSelectionBorder != null);

		SetInnerSelectionBorderBrush();
		SetInnerSelectionBorderCornerRadius();
		SetInnerSelectionBorderThickness();
	}

	// =====================================================================
	// SetOuterBorderProperties (lines 4211-4232)
	// =====================================================================
	private void SetOuterBorderProperties()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_outerBorder != null);

		SetOuterBorderBrush();
		SetOuterBorderCornerRadius();
		SetOuterBorderThickness();
	}

	// =====================================================================
	// SetBackplateBackground (lines 4233-4309)
	// =====================================================================
	private void SetBackplateBackground()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(_backplateRectangle != null);

		bool selected =
			_visualStates.HasState(ChromeCommonStates2.Selected) ||
			_visualStates.HasState(ChromeCommonStates2.PointerOverSelected) ||
			_visualStates.HasState(ChromeCommonStates2.PressedSelected);
		bool pointerOver =
			_visualStates.HasState(ChromeCommonStates2.PointerOver) ||
			_visualStates.HasState(ChromeCommonStates2.PointerOverSelected);
		bool pressed =
			_visualStates.HasState(ChromeCommonStates2.Pressed) ||
			_visualStates.HasState(ChromeCommonStates2.PressedSelected);

		Brush backplateRectangleBackground = null;

		if (_visualStates.HasState(ChromeDisabledStates.Disabled))
		{
			if (selected)
			{
				backplateRectangleBackground = SelectedDisabledBackground;
			}
		}
		else if (pressed)
		{
			if (selected)
			{
				backplateRectangleBackground = SelectedPressedBackground;
			}
			else
			{
				backplateRectangleBackground = PressedBackground;
			}
		}
		else if (pointerOver)
		{
			if (selected)
			{
				backplateRectangleBackground = SelectedPointerOverBackground;
			}
			else
			{
				backplateRectangleBackground = PointerOverBackground;
			}
		}
		else if (selected)
		{
			backplateRectangleBackground = SelectedBackground;
		}
		else
		{
			ContentControl parentItem = GetParentListViewBaseItemNoRef();

			if (parentItem != null)
			{
				backplateRectangleBackground = parentItem.Background;
			}
		}

		_backplateRectangle.Background = backplateRectangleBackground;
	}

	// =====================================================================
	// SetBackplateCornerRadius (lines 4310-4328)
	// =====================================================================
	private void SetBackplateCornerRadius()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(_backplateRectangle != null);

		SetGeneralCornerRadius(_backplateRectangle);
	}

	// =====================================================================
	// SetBackplateMargin (lines 4329-4360)
	// =====================================================================
	private void SetBackplateMargin()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_backplateRectangle != null);

		// When the outer border is present and opaque, the backplate gets a 1px margin to avoid any bleed through at the edges.
		Thickness backplateMargin = IsOuterBorderBrushOpaque() ? new Thickness(1) : default;

		_backplateRectangle.Margin = backplateMargin;
	}

	// =====================================================================
	// SetInnerSelectionBorderBrush (lines 4361-4388)
	// =====================================================================
	private void SetInnerSelectionBorderBrush()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_innerSelectionBorder != null);

		_innerSelectionBorder.BorderBrush = SelectedInnerBorderBrush;
	}

	// =====================================================================
	// SetInnerSelectionBorderCornerRadius (lines 4389-4424)
	// =====================================================================
	private void SetInnerSelectionBorderCornerRadius()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_innerSelectionBorder != null);

		CornerRadius cornerRadius = GetGeneralCornerRadius();
		bool areBordersNested = _outerBorder == VisualTreeHelper.GetParent(_innerSelectionBorder);

		if (areBordersNested)
		{
			Thickness selectedBorderThickness = GetSelectedBorderThickness();

			// Decrease inner border corner radius to account for outer border thickness.
			cornerRadius = new CornerRadius(
				Math.Max(ChromeConstants.InnerBorderCornerRadius, cornerRadius.TopLeft - selectedBorderThickness.Left),
				Math.Max(ChromeConstants.InnerBorderCornerRadius, cornerRadius.TopRight - selectedBorderThickness.Right),
				Math.Max(ChromeConstants.InnerBorderCornerRadius, cornerRadius.BottomRight - selectedBorderThickness.Right),
				Math.Max(ChromeConstants.InnerBorderCornerRadius, cornerRadius.BottomLeft - selectedBorderThickness.Left));
		}

		_innerSelectionBorder.CornerRadius = cornerRadius;
	}

	// =====================================================================
	// SetOuterBorderBrush (lines 4425-4484)
	// =====================================================================
	private void SetOuterBorderBrush()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_outerBorder != null);

		bool selected =
			_visualStates.HasState(ChromeCommonStates2.Selected) ||
			_visualStates.HasState(ChromeCommonStates2.PointerOverSelected) ||
			_visualStates.HasState(ChromeCommonStates2.PressedSelected);

		Brush borderBrush = null;

		if (selected)
		{
			if (_visualStates.HasState(ChromeDisabledStates.Disabled))
			{
				borderBrush = SelectedDisabledBorderBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.PressedSelected))
			{
				borderBrush = SelectedPressedBorderBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.PointerOverSelected))
			{
				borderBrush = SelectedPointerOverBorderBrush;
			}
			else
			{
				borderBrush = SelectedBorderBrush;
			}
		}
		else
		{
			borderBrush = PointerOverBorderBrush;
		}

		_outerBorder.BorderBrush = borderBrush;

		UpdateBordersParenting();

		if (_backplateRectangle != null)
		{
			SetBackplateMargin();
		}
	}

	// =====================================================================
	// SetOuterBorderCornerRadius (lines 4485-4503)
	// =====================================================================
	private void SetOuterBorderCornerRadius()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(_outerBorder != null);

		SetGeneralCornerRadius(_outerBorder);
	}

	// =====================================================================
	// SetInnerSelectionBorderThickness (lines 4504-4570)
	// =====================================================================
	private void SetInnerSelectionBorderThickness()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_innerSelectionBorder != null);

		bool areBordersNested = _outerBorder == VisualTreeHelper.GetParent(_innerSelectionBorder);
		Thickness innerSelectionBorderThickness = ChromeConstants.InnerSelectionBorderThickness;

		if (areBordersNested)
		{
			// When the outer border is opaque, it hosts the inner border which is expanded all around by a pixel
			// to avoid any bleed through at the edges and in-between the borders.
			innerSelectionBorderThickness = new Thickness(
				innerSelectionBorderThickness.Left + 1.0,
				innerSelectionBorderThickness.Top + 1.0,
				innerSelectionBorderThickness.Right + 1.0,
				innerSelectionBorderThickness.Bottom + 1.0);

			_innerSelectionBorder.Margin = new Thickness(-1, -1, -1, -1);
		}
		else
		{
			Thickness selectedBorderThickness = GetSelectedBorderThickness();

			innerSelectionBorderThickness = new Thickness(
				innerSelectionBorderThickness.Left + selectedBorderThickness.Left,
				innerSelectionBorderThickness.Top + selectedBorderThickness.Top,
				innerSelectionBorderThickness.Right + selectedBorderThickness.Right,
				innerSelectionBorderThickness.Bottom + selectedBorderThickness.Bottom);
		}

		_innerSelectionBorder.BorderThickness = innerSelectionBorderThickness;
	}

	// =====================================================================
	// SetOuterBorderThickness (lines 4571-4607)
	// =====================================================================
	private void SetOuterBorderThickness()
	{
		MUX_ASSERT(IsRoundedListViewBaseItemChromeEnabled());
		MUX_ASSERT(IsChromeForGridViewItem());
		MUX_ASSERT(_outerBorder != null);

		bool selected =
			_visualStates.HasState(ChromeCommonStates2.Selected) ||
			_visualStates.HasState(ChromeCommonStates2.PointerOverSelected) ||
			_visualStates.HasState(ChromeCommonStates2.PressedSelected);

		Thickness outerBorderThickness = selected ? GetSelectedBorderThickness() : ChromeConstants.BorderThickness;

		_outerBorder.BorderThickness = outerBorderThickness;
	}

	// =====================================================================
	// SetMultiSelectCheckBoxBackground (lines 4608-4669)
	// =====================================================================
	private void SetMultiSelectCheckBoxBackground()
	{
		MUX_ASSERT(_multiSelectCheckBoxRectangle != null);

		bool selected =
			_visualStates.HasState(ChromeCommonStates2.Selected) ||
			_visualStates.HasState(ChromeCommonStates2.PointerOverSelected) ||
			_visualStates.HasState(ChromeCommonStates2.PressedSelected);

		Brush checkBoxBrush = null;

		if (IsRoundedListViewBaseItemChromeEnabled())
		{
			if (_visualStates.HasState(ChromeDisabledStates.Disabled))
			{
				checkBoxBrush = selected ? CheckBoxSelectedDisabledBrush : CheckBoxDisabledBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.PressedSelected))
			{
				checkBoxBrush = CheckBoxSelectedPressedBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.Pressed))
			{
				checkBoxBrush = CheckBoxPressedBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.PointerOverSelected))
			{
				checkBoxBrush = CheckBoxSelectedPointerOverBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.PointerOver))
			{
				checkBoxBrush = CheckBoxPointerOverBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.Selected))
			{
				checkBoxBrush = CheckBoxSelectedBrush;
			}
			else
			{
				checkBoxBrush = CheckBoxBrush;
			}
		}
		else if (CheckMode == ListViewItemPresenterCheckMode.Overlay)
		{
			checkBoxBrush = selected ? SelectedBackground : CheckBoxBrush;
		}

		_multiSelectCheckBoxRectangle.Background = checkBoxBrush;
	}

	// =====================================================================
	// SetMultiSelectCheckBoxBorder (lines 4670-4749)
	// =====================================================================
	private void SetMultiSelectCheckBoxBorder()
	{
		MUX_ASSERT(_multiSelectCheckBoxRectangle != null);

		bool isRoundedListViewBaseItemChromeEnabled = IsRoundedListViewBaseItemChromeEnabled();
		Thickness multiSelectSquareThickness;
		Brush checkBoxBorderBrush = null;

		if (isRoundedListViewBaseItemChromeEnabled)
		{
			bool selected =
				_visualStates.HasState(ChromeCommonStates2.Selected) ||
				_visualStates.HasState(ChromeCommonStates2.PointerOverSelected) ||
				_visualStates.HasState(ChromeCommonStates2.PressedSelected);

			if (selected)
			{
				multiSelectSquareThickness = default;
			}
			else
			{
				multiSelectSquareThickness = ChromeConstants.MultiSelectRoundedSquareThickness;

				if (_visualStates.HasState(ChromeDisabledStates.Disabled))
				{
					checkBoxBorderBrush = CheckBoxDisabledBorderBrush;
				}
				else if (_visualStates.HasState(ChromeCommonStates2.Pressed))
				{
					checkBoxBorderBrush = CheckBoxPressedBorderBrush;
				}
				else if (_visualStates.HasState(ChromeCommonStates2.PointerOver))
				{
					checkBoxBorderBrush = CheckBoxPointerOverBorderBrush;
				}
				else
				{
					checkBoxBorderBrush = CheckBoxBorderBrush;
				}
			}
		}
		else
		{
			if (CheckMode == ListViewItemPresenterCheckMode.Inline)
			{
				// ListViewItemBase case
				multiSelectSquareThickness = ChromeConstants.MultiSelectSquareThickness;
				checkBoxBorderBrush = CheckBoxBrush;
			}
			else
			{
				// GridViewItemBase case
				multiSelectSquareThickness = default;
			}
		}

		_multiSelectCheckBoxRectangle.BorderThickness = multiSelectSquareThickness;
		_multiSelectCheckBoxRectangle.BorderBrush = checkBoxBorderBrush;
	}

	// =====================================================================
	// SetMultiSelectCheckBoxForeground (lines 4750-4782)
	// =====================================================================
	private void SetMultiSelectCheckBoxForeground()
	{
		MUX_ASSERT(_multiSelectCheckGlyph != null);

		// set the Glyph's brush to CheckBrush, CheckPressedBrush or CheckDisabledBrush
		Brush checkBrush = CheckBrush;

		if (IsRoundedListViewBaseItemChromeEnabled())
		{
			if (_visualStates.HasState(ChromeDisabledStates.Disabled))
			{
				checkBrush = CheckDisabledBrush;
			}
			else if (_visualStates.HasState(ChromeCommonStates2.PressedSelected))
			{
				checkBrush = CheckPressedBrush;
			}
		}

		_multiSelectCheckGlyph.Foreground = checkBrush;
	}

	// =====================================================================
	// SetSelectionIndicatorBackground (lines 4783-4826)
	// =====================================================================
	private void SetSelectionIndicatorBackground()
	{
		MUX_ASSERT(IsSelectionIndicatorVisualEnabled());
		MUX_ASSERT(_selectionIndicatorRectangle != null);

		bool disabled = _visualStates.HasState(ChromeDisabledStates.Disabled);
		bool pressed = _visualStates.HasState(ChromeCommonStates2.PressedSelected);
		bool pointerOver = _visualStates.HasState(ChromeCommonStates2.PointerOverSelected);

		Brush selectionIndicatorRectangleBackground = null;

		if (disabled)
		{
			selectionIndicatorRectangleBackground = SelectionIndicatorDisabledBrush;
		}
		else if (pressed)
		{
			selectionIndicatorRectangleBackground = SelectionIndicatorPressedBrush;
		}
		else if (pointerOver)
		{
			selectionIndicatorRectangleBackground = SelectionIndicatorPointerOverBrush;
		}
		else
		{
			selectionIndicatorRectangleBackground = SelectionIndicatorBrush;
		}

		_selectionIndicatorRectangle.Background = selectionIndicatorRectangleBackground;
	}

	// =====================================================================
	// SetSelectionIndicatorCornerRadius (lines 4827-4850)
	// =====================================================================
	private void SetSelectionIndicatorCornerRadius()
	{
		MUX_ASSERT(IsSelectionIndicatorVisualEnabled());
		MUX_ASSERT(_selectionIndicatorRectangle != null);

		_selectionIndicatorRectangle.CornerRadius = GetSelectionIndicatorCornerRadius();
	}

	// =====================================================================
	// SetForegroundBrush (lines 4851-4917)
	// =====================================================================
	private void SetForegroundBrush()
	{
		Brush foregroundBrush = null;
		bool hasForeground = false;

		if (_visualStates.HasState(ChromeCommonStates2.Selected) ||
			_visualStates.HasState(ChromeCommonStates2.PressedSelected) ||
			_visualStates.HasState(ChromeCommonStates2.PointerOverSelected))
		{
			if (SelectedForeground != null)
			{
				foregroundBrush = SelectedForeground;
				hasForeground = true;
			}
		}
		else if (_visualStates.HasState(ChromeCommonStates2.PointerOver) ||
				 _visualStates.HasState(ChromeCommonStates2.Pressed))
		{
			// PointerOverForeground is a property on ListViewItemPresenter (not on GridViewItemPresenter).
			// We check if the property is explicitly set.
			if (PointerOverForeground != null || ReadLocalValue(PointerOverForegroundProperty) != DependencyProperty.UnsetValue)
			{
				foregroundBrush = PointerOverForeground;
				hasForeground = true;
			}
		}

		// if the value is not set, we clear the Brush value
		if (!hasForeground)
		{
			ClearValue(ForegroundProperty);

			if (!IsRoundedListViewBaseItemChromeEnabled() &&
				CheckMode == ListViewItemPresenterCheckMode.Inline &&
				_multiSelectCheckBoxRectangle != null)
			{
				// set the CheckBox's brush
				_multiSelectCheckBoxRectangle.BorderBrush = CheckBoxBrush;

				// set the CheckMark Glyph's brush
				if (_multiSelectCheckGlyph != null)
				{
					_multiSelectCheckGlyph.Foreground = CheckBrush;
				}
			}
		}
		else
		{
			Foreground = foregroundBrush;

			// in the case of Selection or PointerOver, we want the CheckBox and the glyph to have the same color as the item's Foreground
			if (!IsRoundedListViewBaseItemChromeEnabled() &&
				CheckMode == ListViewItemPresenterCheckMode.Inline &&
				_multiSelectCheckBoxRectangle != null)
			{
				// set the CheckBox's brush
				_multiSelectCheckBoxRectangle.BorderBrush = foregroundBrush;

				// set the CheckMark Glyph's brush
				if (_multiSelectCheckGlyph != null)
				{
					_multiSelectCheckGlyph.Foreground = foregroundBrush;
				}
			}
		}
	}

#if !HAS_UNO
	// TODO Uno: GetValue (lines 4918-4951) - DP value interception.
	// Forwards legacy ListViewItemPresenter/GridViewItemPresenter properties
	// to their ContentPresenter equivalents. Not applicable in Uno since
	// legacy property forwarding is handled in Properties.cs.
#endif

#if !HAS_UNO
	// TODO Uno: SetValue (lines 4952-4998) - DP value interception.
	// Forwards legacy ListViewItemPresenter/GridViewItemPresenter properties
	// to their ContentPresenter equivalents. Not applicable in Uno since
	// legacy property forwarding is handled in Properties.cs.
#endif

	// =====================================================================
	// OnPropertyChangedNewStyle (lines 4999-5164)
	// =====================================================================
	private void OnPropertyChangedNewStyle(DependencyProperty dp)
	{
		// SelectedForeground, PointerOverForeground
		if (dp == SelectedForegroundProperty ||
			dp == PointerOverForegroundProperty)
		{
			SetForegroundBrush();
			return;
		}

		// SelectionIndicatorVisualEnabled, SelectionCheckMarkVisualEnabled, SelectionIndicatorMode
		if (dp == SelectionIndicatorVisualEnabledProperty ||
			dp == SelectionCheckMarkVisualEnabledProperty ||
			dp == SelectionIndicatorModeProperty)
		{
			InvalidateRender();
			return;
		}

		// CheckMode, CheckBrush, CheckPressedBrush, CheckDisabledBrush,
		// CheckBoxBrush, CheckBoxBorderBrush, CheckBoxPressedBorderBrush, CheckBoxDisabledBorderBrush,
		// CheckBoxCornerRadius, SelectedBackground, SelectedPointerOverBackground,
		// SelectedPressedBackground, SelectedDisabledBackground
		if (dp == CheckModeProperty ||
			dp == CheckBrushProperty ||
			dp == CheckPressedBrushProperty ||
			dp == CheckDisabledBrushProperty ||
			dp == CheckBoxBrushProperty ||
			dp == CheckBoxBorderBrushProperty ||
			dp == CheckBoxPressedBorderBrushProperty ||
			dp == CheckBoxDisabledBorderBrushProperty ||
			dp == CheckBoxCornerRadiusProperty ||
			dp == SelectedBackgroundProperty ||
			dp == SelectedPointerOverBackgroundProperty ||
			dp == SelectedPressedBackgroundProperty ||
			dp == SelectedDisabledBackgroundProperty)
		{
			// only update changes if the checkbox already exists
			if (_multiSelectCheckBoxRectangle != null)
			{
				SetMultiSelectCheckBoxProperties();
				InvalidateRender();
			}

			if (_backplateRectangle != null &&
				(dp == SelectedBackgroundProperty ||
				 dp == SelectedPointerOverBackgroundProperty ||
				 dp == SelectedPressedBackgroundProperty ||
				 dp == SelectedDisabledBackgroundProperty))
			{
				SetBackplateBackground();
				InvalidateRender();
			}
			return;
		}

		// SelectedInnerBorderBrush
		if (dp == SelectedInnerBorderBrushProperty)
		{
			if (_innerSelectionBorder != null)
			{
				SetInnerSelectionBorderBrush();
				InvalidateRender();
			}
			return;
		}

		// CheckBoxPointerOverBrush, CheckBoxPressedBrush, CheckBoxDisabledBrush,
		// CheckBoxSelectedBrush, CheckBoxSelectedPointerOverBrush, CheckBoxSelectedPressedBrush, CheckBoxSelectedDisabledBrush
		if (dp == CheckBoxPointerOverBrushProperty ||
			dp == CheckBoxPressedBrushProperty ||
			dp == CheckBoxDisabledBrushProperty ||
			dp == CheckBoxSelectedBrushProperty ||
			dp == CheckBoxSelectedPointerOverBrushProperty ||
			dp == CheckBoxSelectedPressedBrushProperty ||
			dp == CheckBoxSelectedDisabledBrushProperty)
		{
			if (_multiSelectCheckBoxRectangle != null)
			{
				SetMultiSelectCheckBoxBackground();
				InvalidateRender();
			}
			return;
		}

		// PointerOverBorderBrush, SelectedBorderBrush, SelectedPointerOverBorderBrush,
		// SelectedPressedBorderBrush, SelectedDisabledBorderBrush
		if (dp == PointerOverBorderBrushProperty ||
			dp == SelectedBorderBrushProperty ||
			dp == SelectedPointerOverBorderBrushProperty ||
			dp == SelectedPressedBorderBrushProperty ||
			dp == SelectedDisabledBorderBrushProperty)
		{
			if (_outerBorder != null)
			{
				SetOuterBorderBrush();
				InvalidateRender();
			}
			return;
		}

		// SelectedBorderThickness
		if (dp == SelectedBorderThicknessProperty)
		{
			if (_outerBorder != null)
			{
				SetOuterBorderThickness();
				InvalidateRender();
			}
			if (_innerSelectionBorder != null)
			{
				SetInnerSelectionBorderThickness();
				InvalidateRender();
			}
			return;
		}

		// SelectionIndicatorBrush, SelectionIndicatorPointerOverBrush, SelectionIndicatorPressedBrush
		if (dp == SelectionIndicatorBrushProperty ||
			dp == SelectionIndicatorPointerOverBrushProperty ||
			dp == SelectionIndicatorPressedBrushProperty)
		{
			if (_selectionIndicatorRectangle != null)
			{
				SetSelectionIndicatorBackground();
				InvalidateRender();
			}
			return;
		}

		// SelectionIndicatorCornerRadius
		if (dp == SelectionIndicatorCornerRadiusProperty)
		{
			if (_selectionIndicatorRectangle != null)
			{
				SetSelectionIndicatorCornerRadius();
				InvalidateRender();
			}
			return;
		}

		// CornerRadius (from ContentPresenter)
		if (dp == CornerRadiusProperty)
		{
			if (IsRoundedListViewBaseItemChromeEnabled())
			{
				if (_backplateRectangle != null)
				{
					SetBackplateCornerRadius();
					InvalidateRender();
				}

				if (_outerBorder != null)
				{
					SetOuterBorderCornerRadius();
					InvalidateRender();
				}

				if (_innerSelectionBorder != null)
				{
					SetInnerSelectionBorderCornerRadius();
					InvalidateRender();
				}
			}
			return;
		}
	}

#if !HAS_UNO
	// TODO Uno: DrawRevealBackground (lines 5165-5188) - native rendering.
	// Draws the reveal background brush excluding the reveal border area.
	// Not applicable in Uno.
#endif

	// =====================================================================
	// IsNullCompositionBrush (lines 5189-5200)
	// =====================================================================
	private static bool IsNullCompositionBrush(Brush brush)
	{
		// In Uno, we simply check for null.
		return brush == null;
	}

	// =====================================================================
	// IsOpaqueBrush (lines 5201-5218)
	// =====================================================================
	private static bool IsOpaqueBrush(Brush brush)
	{
		if (brush is SolidColorBrush solidColorBrush)
		{
			return solidColorBrush.Color.A == 255;
		}
		else if (brush is GradientBrush gradientBrush)
		{
			// Check if all gradient stops are opaque
			if (gradientBrush.GradientStops != null)
			{
				foreach (var stop in gradientBrush.GradientStops)
				{
					if (stop.Color.A != 255)
					{
						return false;
					}
				}
				return gradientBrush.GradientStops.Count > 0;
			}
			return false;
		}
		return false;
	}

	// =====================================================================
	// ClearIsRoundedListViewBaseItemChromeEnabledCache (lines 5220-5225)
	// Not needed in Uno, always true.
	// =====================================================================

	// =====================================================================
	// IsRoundedListViewBaseItemChromeEnabled (static) (lines 5227-5242)
	// =====================================================================
	private static bool IsRoundedListViewBaseItemChromeEnabledStatic()
	{
		// In Uno, rounded chrome is always enabled.
		return true;
	}

	// =====================================================================
	// IsRoundedListViewBaseItemChromeForced (lines 5243-5257)
	// =====================================================================
	private static bool IsRoundedListViewBaseItemChromeForced()
	{
		return false;
	}

	// =====================================================================
	// IsSelectionIndicatorVisualForced (lines 5258-end)
	// =====================================================================
	private static bool IsSelectionIndicatorVisualForced()
	{
		return false;
	}

	// =====================================================================
	// Helper methods for size/rect inflation/deflation
	// =====================================================================
	private static Size DeflateSize(Size size, Thickness thickness)
	{
		return new Size(
			Math.Max(0, size.Width - thickness.Left - thickness.Right),
			Math.Max(0, size.Height - thickness.Top - thickness.Bottom));
	}

	private static Size InflateSize(Size size, Thickness thickness)
	{
		return new Size(
			size.Width + thickness.Left + thickness.Right,
			size.Height + thickness.Top + thickness.Bottom);
	}

	private static Rect DeflateRect(Rect rect, Thickness thickness)
	{
		return new Rect(
			rect.X + thickness.Left,
			rect.Y + thickness.Top,
			Math.Max(0, rect.Width - thickness.Left - thickness.Right),
			Math.Max(0, rect.Height - thickness.Top - thickness.Bottom));
	}
}
