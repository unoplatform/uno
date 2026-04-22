// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeater.h, commit 5f9e85113

using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsRepeater
{
	// StackLayout measurements are shortcut when m_stackLayoutMeasureCounter reaches this value
	// to prevent a layout cycle exception.
	// The XAML Framework's iteration limit is 250, but that limit has been reached in practice
	// with this value as small as 61. It was never reached with 60.
	private const uint s_maxStackLayoutIterations = 60u;

	internal static Point ClearedElementsArrangePosition = new Point(-10000.0f, -10000.0f);

	// A convention we use in the ItemsRepeater codebase for an invalid Rect value.
	internal static Rect InvalidRect = new Rect(-1, -1, -1, -1);

	internal IElementFactoryShim ItemTemplateShim => m_itemTemplateWrapper;

	internal ViewManager ViewManager => m_viewManager;

	internal TransitionManager TransitionManager => m_transitionManager;

	internal object LayoutState
	{
		get => m_layoutState;
		set => m_layoutState = value;
	}

	internal Rect VisibleWindow => m_viewportManager.GetLayoutVisibleWindow();

	internal Rect RealizationWindow => m_viewportManager.GetLayoutRealizationWindow();

	internal UIElement SuggestedAnchor => m_viewportManager.SuggestedAnchor;

	internal UIElement MadeAnchor => m_viewportManager.MadeAnchor;

	internal Point LayoutOrigin
	{
		get => m_layoutOrigin;
		set => m_layoutOrigin = value;
	}

	private bool IsProcessingCollectionChange => m_processingItemsSourceChange != null;

	TransitionManager m_transitionManager;
	ViewManager m_viewManager;
	ViewportManager m_viewportManager;

	ItemsSourceView m_itemsSourceView;

	IElementFactoryShim m_itemTemplateWrapper;

	VirtualizingLayoutContext m_layoutContext;
	object m_layoutState;
	// Value is different from null only while we are on the OnItemsSourceChanged call stack.
	NotifyCollectionChangedEventArgs m_processingItemsSourceChange;

	bool m_isLayoutInProgress;
	// The value of m_layoutOrigin is expected to be set by the layout
	// when it gets measured. It should not be used outside of measure.
	Point m_layoutOrigin;

	// Cached Event args to avoid creation cost every time
	private ItemsRepeaterElementPreparedEventArgs m_elementPreparedArgs;
	private ItemsRepeaterElementClearingEventArgs m_elementClearingArgs;
	private ItemsRepeaterElementIndexChangedEventArgs m_elementIndexChangedArgs;

	// Loaded events fire on the first tick after an element is put into the tree
	// while unloaded is posted on the UI tree and may be processed out of sync with subsequent loaded
	// events. We keep these counters to detect out-of-sync unloaded events and take action to rectify.
	int m_loadedCounter;
	int m_unloadedCounter;

	// Used to avoid layout cycles with StackLayout layouts where variable sized children prevent
	// the ItemsRepeater's layout to settle.
	uint m_stackLayoutMeasureCounter;

	// If no ItemCollectionTransitionProvider is explicitly provided, we'll retrieve a default one
	// from the Layout object. In that case, we'll want to know that we own that object and can
	// overwrite it if the Layout object changes.
	bool m_ownsTransitionProvider = true;

	// Bug where DataTemplate with no content causes a crash.
	// See: https://github.com/microsoft/microsoft-ui-xaml/issues/776
	// Solution: Have flag that is only true when DataTemplate exists but it is empty.
	bool m_isItemTemplateEmpty;

	// Tracks whether OnLayoutChanged has already been called or not so that
	// EnsureDefaultLayoutState does not trigger a second call after the control's creation.
	bool m_wasLayoutChangedCalled;

	// Tracks the global scale factor so that children can be re-measured when
	// it changes, for example when moving the app to another screen.
	double m_layoutRoundFactor;

	public event TypedEventHandler<ItemsRepeater, ItemsRepeaterElementPreparedEventArgs> ElementPrepared;
	public event TypedEventHandler<ItemsRepeater, ItemsRepeaterElementIndexChangedEventArgs> ElementIndexChanged;
	public event TypedEventHandler<ItemsRepeater, ItemsRepeaterElementClearingEventArgs> ElementClearing;
}
