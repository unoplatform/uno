// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsView.h, tag winui3/release/1.5.0
using System.Collections.Generic;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Windows.Foundation;
using Windows.System;

namespace Windows.UI.Xaml.Controls;

partial class ItemsView
{
	// Properties' default values.
	const ItemsViewSelectionMode s_defaultSelectionMode = ItemsViewSelectionMode.Single;

	private static DependencyProperty ItemsViewItemContainerRevokersProperty { get; } = DependencyProperty.RegisterAttached(
		"ItemsViewItemContainerRevokers",
		typeof(object),
		typeof(UIElement),
		new FrameworkPropertyMetadata(defaultValue: null));

	private const string s_itemsRepeaterPartName = "PART_ItemsRepeater";
	private const string s_scrollViewPartName = "PART_ScrollView";
	private const string s_indexOutOfBounds = "Index is out of bounds.";
	private const string s_invalidItemTemplateRoot = "ItemTemplate's root element must be an ItemContainer.";
	private const string s_itemsSourceNull = "ItemsSource does not have a value.";
	private const string s_itemsViewNotParented = "ItemsView is not parented.";
	private const string s_missingItemsRepeaterPart = "ItemsRepeater part is not available.";

	// CorrelationId of the bring-into-view scroll resulting from a navigation key processing.
	private int m_navigationKeyBringIntoViewCorrelationId = -1;
	// Ticks count left before the next queued navigation key is processed,
	// after a navigation-key-induced bring-into-view scroll completed a large offset change.
	private byte m_navigationKeyProcessingCountdown = 0;
	// Incremented in SetFocusElementIndex when a navigation key processing causes a new item to get focus.
	// This will trigger an OnScrollViewBringingIntoView call where it is decremented.
	// Used to delay a navigation key processing until the content has settled on a new viewport.
	private byte m_navigationKeyBringIntoViewPendingCount = 0;
	// Caches the most recent navigation key processed.
	private VirtualKey m_lastNavigationKeyProcessed;

	// CorrelationId of the bring-into-view scroll resulting from a StartBringItemIntoView call.
	private int m_bringIntoViewCorrelationId = -1;
	// Ticks count left after bring-into-view scroll completed before the m_bringIntoViewElement
	// field is reset and no longer returned in OnScrollViewAnchorRequested.
	private byte m_bringIntoViewElementRetentionCountdown = 0;
	// ScrollView anchor ratios to restore after a StartBringItemIntoView operation completes.
	private double m_scrollViewHorizontalAnchorRatio = -1.0;
	private double m_scrollViewVerticalAnchorRatio = -1.0;

	// Set to True when a ResetKeyboardNavigationReference() call is delayed until the next ItemsView::OnItemsRepeaterLayoutUpdated occurrence.
	private bool m_keyboardNavigationReferenceResetPending = false;
	// Caches the element index used to define m_keyboardNavigationReferenceRect.
	private int m_keyboardNavigationReferenceIndex = -1;
	// Bounds of the reference element for directional keyboard navigations.
	private Rect m_keyboardNavigationReferenceRect = new Rect(-1.0f, -1.0f, -1.0f, -1.0f);

	// Set to True during a user action processing which updates selection.
	private bool m_isProcessingInteraction = false;

	private bool m_setVerticalScrollControllerOnLoaded = false;

	// Set to True in ItemsView::OnSelectionModelSelectionChanged to delay the application
	// of the selection changes until the imminent ItemsView::OnSourceListChanged call.
	private bool m_applySelectionChangeOnSourceListChanged = false;

	private SelectorBase m_selector;

	SerialDisposable m_renderingRevoker = new();
	SerialDisposable m_selectionModelSelectionChangedRevoker = new();
	SerialDisposable m_currentElementSelectionModelSelectionChangedRevoker = new();
	SerialDisposable m_itemsRepeaterElementPreparedRevoker = new();
	SerialDisposable m_itemsRepeaterElementClearingRevoker = new();
	SerialDisposable m_itemsRepeaterElementIndexChangedRevoker = new();
	SerialDisposable m_itemsRepeaterLayoutUpdatedRevoker = new();
	SerialDisposable m_itemsRepeaterSizeChangedRevoker = new();
	//SerialDisposable m_unloadedRevoker;
	//SerialDisposable m_loadedRevoker;
	SerialDisposable m_itemsSourceViewChangedRevoker = new();
#if DEBUG
	SerialDisposable m_layoutMeasureInvalidatedDbg = new();
	SerialDisposable m_layoutArrangeInvalidatedDbg = new();
#endif
	SerialDisposable m_scrollViewAnchorRequestedRevoker = new();
	SerialDisposable m_scrollViewBringingIntoViewRevoker = new();
	SerialDisposable m_scrollViewScrollCompletedRevoker = new();
#if DEBUG
	SerialDisposable m_scrollViewExtentChangedRevokerDbg = new();
#endif
	SerialDisposable m_itemsRepeaterItemsSourcePropertyChangedRevoker = new();

	// Tracks selected elements.
	SelectionModel m_selectionModel = new();
	// Tracks current element.
	SelectionModel m_currentElementSelectionModel = new();

	// ScrollView's vertical scrollbar visibility to restore in the event VerticalScrollController gets assigned back to
	// its original value (m_originalVerticalScrollController read in OnApplyTemplate) after being set to a custom value.
	ScrollingScrollBarVisibility m_originalVerticalScrollBarVisibility = ScrollingScrollBarVisibility.Auto;
	// Original VerticalScrollController read in OnApplyTemplate, which by default is the ScrollView's ScrollBarController.
	IScrollController m_originalVerticalScrollController;

	ItemsRepeater m_itemsRepeater;
	ScrollView m_scrollView;
	UIElement m_bringIntoViewElement;

	List<VirtualKey> m_navigationKeysToProcess = new();
	HashSet<ItemContainer> m_itemContainersWithRevokers = new();
	//Dictionary<ItemContainer, PointerInfo<ItemsView>> m_itemContainersPointerInfos; // Uno docs: This is unused in WinUI
};
