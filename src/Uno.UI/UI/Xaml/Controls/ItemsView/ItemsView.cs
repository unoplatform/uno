// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsView.cpp, tag winui3/release/1.5.0

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsView : Control
{
	// Change to 'true' to turn on debugging outputs in Output window
	//bool ItemsViewTrace.s_IsDebugOutputEnabled{ false };
	//bool ItemsViewTrace.s_IsVerboseDebugOutputEnabled{ false };

	// Number of CompositionTarget.Rendering event occurrences after bring-into-view completion before resetting m_bringIntoViewElement as the scroll anchoring element.
	const byte c_renderingEventsPostBringIntoView = 4;

	public ItemsView()
	{
#if __ANDROID__ || __IOS__
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error("ItemsView is not supported on this platform (iOS, Android). For more information, visit https://aka.platform.uno/notimplemented#m=ItemsView");
		}
#endif
		//ITEMSVIEW_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_ItemsView);

		//static  var s_ItemsViewItemContainerRevokersPropertyInit = []()
		//{
		//	s_ItemsViewItemContainerRevokersProperty =
		//		InitializeDependencyProperty(
		//			"ItemsViewItemContainerRevokers",
		//			name_of<object>(),
		//			name_of<UIElement>(),
		//			true /* isAttached */,
		//			null /* defaultValue */);
		//	return false;
		//}();

		//EnsureProperties();
		this.SetDefaultStyleKey();

		// Uno docs: We use OnLoaded and OnUnloaded overrides instead of event subscriptions.
		//m_loadedRevoker = Loaded(auto_revoke, { this, &OnLoaded });
		//m_unloadedRevoker = Unloaded(auto_revoke, { this, &OnUnloaded });
		m_selectionModel.SelectionChanged += OnSelectionModelSelectionChanged;
		m_selectionModelSelectionChangedRevoker.Disposable = new DisposableAction(() => m_selectionModel.SelectionChanged -= OnSelectionModelSelectionChanged);

		m_currentElementSelectionModel.SelectionChanged += OnCurrentElementSelectionModelSelectionChanged;
		m_currentElementSelectionModelSelectionChangedRevoker.Disposable = new DisposableAction(() => m_currentElementSelectionModel.SelectionChanged -= OnCurrentElementSelectionModelSelectionChanged);

		// m_currentElementSelectionModel tracks the single current element.
		m_currentElementSelectionModel.SingleSelect = true;

		UpdateSelector();
	}

	~ItemsView()
	{
		//ITEMSVIEW_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		//m_loadedRevoker.revoke();
		//m_unloadedRevoker.revoke();
		m_selectionModelSelectionChangedRevoker.Disposable = null;
		m_currentElementSelectionModelSelectionChangedRevoker.Disposable = null;

		UnhookCompositionTargetRendering();
		UnhookItemsRepeaterEvents(true /*isForDestructor*/);
		UnhookScrollViewEvents(true /*isForDestructor*/);
	}

	public ScrollView ScrollView => m_scrollView;

	// Returns the index of the closest realized item to a point specified by the two viewport ratio numbers.
	// See GetItemInternal for details.
	public bool TryGetItemIndex(
		double horizontalViewportRatio,
		double verticalViewportRatio,
		out int index)
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalViewportRatio, verticalViewportRatio);

		IndexBasedLayoutOrientation indexBasedLayoutOrientation = GetLayoutIndexBasedLayoutOrientation();
		bool isHorizontalDistanceFavored = indexBasedLayoutOrientation == IndexBasedLayoutOrientation.TopToBottom;
		bool isVerticalDistanceFavored = indexBasedLayoutOrientation == IndexBasedLayoutOrientation.LeftToRight;

		index = GetItemInternal(
					horizontalViewportRatio,
					verticalViewportRatio,
					isHorizontalDistanceFavored,
					isVerticalDistanceFavored,
					false /*useKeyboardNavigationReferenceHorizontalOffset*/,
					false /*useKeyboardNavigationReferenceVerticalOffset*/,
					false /*capItemEdgesToViewportRatioEdges*/,
					false /*forFocusableItemsOnly*/);
		return index != -1;
	}

	// Invokes UIElement.StartBringIntoView with the provided BringIntoViewOptions instance, if any,
	// for the element associated with the given data index.
	// If that element is currently virtualized, it is realized and synchronously layed out, before that StartBringIntoView call.
	// Note that because of lines 111-112 of ViewportManagerWithPlatformFeatures.GetLayoutVisibleWindow() and line 295 of
	// ViewportManagerWithPlatformFeatures.OnBringIntoViewRequested(...), animated bring-into-view operations are not supported.
	// ViewportManagerWithPlatformFeatures.GetLayoutVisibleWindow snaps the RealizationWindow to 0,0 while
	// ViewportManagerWithPlatformFeatures.OnBringIntoViewRequested turns off BringIntoViewRequestedEventArgs.AnimationDesired.
	public void StartBringItemIntoView(
		int index,
		BringIntoViewOptions options)
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		bool success = StartBringItemIntoViewInternal(true /*throwOutOfBounds*/, true /* throwOnAnyFailure */, index, options);
		MUX_ASSERT(success);
	}

	public IReadOnlyList<object> SelectedItems => m_selectionModel.SelectedItems;

	public void Select(int itemIndex)
	{
		m_selectionModel.Select(itemIndex);
	}

	public void Deselect(int itemIndex)
	{
		m_selectionModel.Deselect(itemIndex);
	}

	public bool IsSelected(int itemIndex)
	{
		bool? isItemIndexSelected = m_selectionModel.IsSelected(itemIndex);
		return isItemIndexSelected != null && isItemIndexSelected.Value;
	}

	public void SelectAll()
	{
		// TODO: Update once ItemsView has grouping.
		// This function assumes a flat list data source.
		m_selectionModel.SelectAllFlat();
	}

	public void DeselectAll()
	{
		m_selectionModel.ClearSelection();
	}

	public void InvertSelection()
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
			{
				var selectedIndices = m_selectionModel.SelectedIndices;
				int indexEnd = itemsSourceView.Count - 1;

				// We loop backwards through the selected indices so we can deselect as we go
				for (int i = selectedIndices.Count - 1; i >= 0; i--)
				{
					var indexPath = selectedIndices[i];
					// TODO: Update once ItemsView has grouping.
					int index = indexPath.GetAt(0);

					// Select all the unselected items
					if (index < indexEnd)
					{
						var startIndex = IndexPath.CreateFrom(index + 1);
						var endIndex = IndexPath.CreateFrom(indexEnd);
						m_selectionModel.SelectRange(startIndex, endIndex);
					}

					m_selectionModel.DeselectAt(indexPath);
					indexEnd = index - 1;
				}

				// Select the remaining unselected items at the beginning of the collection
				if (indexEnd >= 0)
				{
					var startIndex = IndexPath.CreateFrom(0);
					var endIndex = IndexPath.CreateFrom(indexEnd);
					m_selectionModel.SelectRange(startIndex, endIndex);
				}
			}
		}
	}

	#region IControlOverrides

#if DEBUG
	protected override void OnGotFocus(
		RoutedEventArgs args)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		base.OnGotFocus(args);
	}
#endif

	#endregion

	#region IFrameworkElementOverrides

	protected override void OnApplyTemplate()
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		RestoreOriginalVerticalScrollControllerAndVisibility();

		base.OnApplyTemplate();

		ScrollView scrollView = GetTemplateChild<ScrollView>(s_scrollViewPartName);

		UpdateScrollView(scrollView);

		ItemsRepeater itemsRepeater = GetTemplateChild<ItemsRepeater>(s_itemsRepeaterPartName);

		UpdateItemsRepeater(itemsRepeater);

		m_setVerticalScrollControllerOnLoaded = true;
	}

	#endregion

	#region IUIElementOverrides

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ItemsViewAutomationPeer(this);
	}

	#endregion

	// Invoked when a dependency property of this ItemsView has changed.
	void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var dependencyProperty = args.Property;

		//ITEMSVIEW_TRACE_VERBOSE_DBG(null, "%s(property: %s)\n", METH_NAME, DependencyPropertyToStringDbg(dependencyProperty).c_str());

		if (dependencyProperty == IsItemInvokedEnabledProperty)
		{
			OnIsItemInvokedEnabledChanged();
		}
		else if (dependencyProperty == ItemTemplateProperty)
		{
			OnItemTemplateChanged();
		}
		else if (dependencyProperty == SelectionModeProperty)
		{
			OnSelectionModeChanged();
		}
		else if (dependencyProperty == ItemsSourceProperty)
		{
			OnItemsSourceChanged();
		}
		else if (dependencyProperty == VerticalScrollControllerProperty)
		{
			OnVerticalScrollControllerChanged();
		}
#if DEBUG
		else if (dependencyProperty == LayoutProperty)
		{
			OnLayoutChangedDbg();
		}
#endif
	}

	// Make sure the default ItemTemplate is used when the ItemsSource is non-null and the ItemTemplate is null.
	// Prevents ViewManager.GetElementFromElementFactory from setting the ItemsRepeater.ItemTemplate property 
	// which would clear the template-binding between the ItemsView & ItemsRepeater ItemTemplate properties.
	// This is not needed though when the ItemsSource points to UIElements instead of data items. In that case,
	// ViewManager.GetElementFromElementFactory will not set a default ItemTemplate. It must remain null.
	void EnsureItemTemplate()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (ItemsSource == null || ItemTemplate != null)
		{
			return;
		}

		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
			{
				var itemCount = itemsSourceView.Count;

				if (itemCount == 0)
				{
					return;
				}

				var data = itemsSourceView.GetAt(0);

				if (data is UIElement)
				{
					// No need to set a default ItemTemplate with an ItemContainer when the ItemsSource is a list of UIElements.
					return;
				}

				// The default ItemTemplate uses an ItemContainer for its root element since this is an ItemsView requirement for now.
				var defaultItemTemplate = XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ItemContainer><TextBlock Text='{Binding}'/></ItemContainer></DataTemplate>") as DataTemplate;

				ItemTemplate = defaultItemTemplate;
			}
		}
	}

	void UpdateItemsRepeater(
		ItemsRepeater itemsRepeater)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_keyboardNavigationReferenceResetPending = false;

		UnhookItemsRepeaterEvents(false /*isForDestructor*/);

		// Reset inner ItemsRepeater dependency properties
		if (m_itemsRepeater is { } oldItemsRepeater)
		{
			oldItemsRepeater.ClearValue(ItemsRepeater.ItemsSourceProperty);
			oldItemsRepeater.ClearValue(ItemsRepeater.ItemTemplateProperty);
			oldItemsRepeater.ClearValue(ItemsRepeater.LayoutProperty);
		}

		m_itemsRepeater = itemsRepeater;

		HookItemsRepeaterEvents();
		HookItemsSourceViewEvents();
	}

	void UpdateScrollView(
		ScrollView scrollView)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UnhookScrollViewEvents(false /*isForDestructor*/);

		m_scrollView = scrollView;

		SetValue(ScrollViewProperty, scrollView);

		HookScrollViewEvents();
	}

	void UpdateSelector()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, SelectionMode());

		m_selectionModel.SingleSelect = false;

		switch (SelectionMode)
		{
			case ItemsViewSelectionMode.None:
				{
					m_selectionModel.ClearSelection();
					m_selector = new NullSelector();
					break;
				}

			case ItemsViewSelectionMode.Single:
				{
					m_selectionModel.SingleSelect = true;

					m_selector = new SingleSelector();
					m_selector.SetSelectionModel(m_selectionModel);
					break;
				}

			case ItemsViewSelectionMode.Multiple:
				{
					m_selector = new MultipleSelector();
					m_selector.SetSelectionModel(m_selectionModel);
					break;
				}

			case ItemsViewSelectionMode.Extended:
				{
					m_selector = new ExtendedSelector();
					m_selector.SetSelectionModel(m_selectionModel);
					break;
				}
		}
	}

	bool CanRaiseItemInvoked(
		ItemContainerInteractionTrigger interactionTrigger,
		ItemContainer itemContainer)
	{
		MUX_ASSERT(itemContainer != null);

#if MUX_PRERELEASE
		ItemContainerUserInvokeMode canUserInvoke = itemContainer.CanUserInvoke();
#else
		ItemContainerUserInvokeMode canUserInvoke = itemContainer.CanUserInvokeInternal();
#endif

		if ((canUserInvoke & (ItemContainerUserInvokeMode.UserCannotInvoke | ItemContainerUserInvokeMode.UserCanInvoke)) != (ItemContainerUserInvokeMode.UserCanInvoke))
		{
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, "Returns false based on ItemContainer.CanUserInvoke.");

			return false;
		}

		bool cannotRaiseItemInvoked =
			(!IsItemInvokedEnabled ||
			(SelectionMode == ItemsViewSelectionMode.None && interactionTrigger == ItemContainerInteractionTrigger.DoubleTap) ||
			(SelectionMode != ItemsViewSelectionMode.None && (interactionTrigger == ItemContainerInteractionTrigger.Tap || interactionTrigger == ItemContainerInteractionTrigger.SpaceKey)));

		bool canRaiseItemInvoked = !cannotRaiseItemInvoked;

		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "Returns based on ItemsView.IsItemInvokedEnabled.", canRaiseItemInvoked);

		return canRaiseItemInvoked;
	}

	void RaiseItemInvoked(
		UIElement element)
	{
		if (ItemInvoked is not null)
		{
			bool itemInvokedFound = false;

			var itemInvoked = GetElementItem(element, ref itemInvokedFound);

			//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_PTR_INT, METH_NAME, this, itemInvoked, itemInvokedFound);

			if (itemInvokedFound)
			{
				var itemsViewItemInvokedEventArgs = new ItemsViewItemInvokedEventArgs(itemInvoked);

				ItemInvoked.Invoke(this, itemsViewItemInvokedEventArgs);
			}
		}
	}

	void RaiseSelectionChanged()
	{
		if (SelectionChanged is not null)
		{
			//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

			var itemsViewSelectionChangedEventArgs = new ItemsViewSelectionChangedEventArgs();

			SelectionChanged.Invoke(this, itemsViewSelectionChangedEventArgs);
		}
	}

	void HookItemsSourceViewEvents()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_itemsSourceViewChangedRevoker.Disposable = null;

		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
			{
				itemsSourceView.CollectionChanged += OnSourceListChanged;
				m_itemsSourceViewChangedRevoker.Disposable = new DisposableAction(() => itemsSourceView.CollectionChanged -= OnSourceListChanged);

				// Make sure the default ItemTemplate is used when the ItemsSource is non-null and the ItemTemplate is null.
				EnsureItemTemplate();
			}
		}
	}

	void HookItemsRepeaterEvents()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (m_itemsRepeater is { } itemsRepeater)
		{
			itemsRepeater.ElementPrepared += OnItemsRepeaterElementPrepared;
			m_itemsRepeaterElementPreparedRevoker.Disposable = new DisposableAction(() => itemsRepeater.ElementPrepared -= OnItemsRepeaterElementPrepared);

			itemsRepeater.ElementClearing += OnItemsRepeaterElementClearing;
			m_itemsRepeaterElementClearingRevoker.Disposable = new DisposableAction(() => itemsRepeater.ElementClearing -= OnItemsRepeaterElementClearing);

			itemsRepeater.ElementIndexChanged += OnItemsRepeaterElementIndexChanged;
			m_itemsRepeaterElementIndexChangedRevoker.Disposable = new DisposableAction(() => itemsRepeater.ElementIndexChanged -= OnItemsRepeaterElementIndexChanged);

			itemsRepeater.LayoutUpdated += OnItemsRepeaterLayoutUpdated;
			m_itemsRepeaterLayoutUpdatedRevoker.Disposable = new DisposableAction(() => itemsRepeater.LayoutUpdated -= OnItemsRepeaterLayoutUpdated);

			itemsRepeater.SizeChanged += OnItemsRepeaterSizeChanged;
			m_itemsRepeaterSizeChangedRevoker.Disposable = new DisposableAction(() => itemsRepeater.SizeChanged -= OnItemsRepeaterSizeChanged);

			var token = itemsRepeater.RegisterPropertyChangedCallback(ItemsRepeater.ItemsSourceProperty, OnItemsRepeaterItemsSourceChanged);
			m_itemsRepeaterItemsSourcePropertyChangedRevoker.Disposable = new DisposableAction(() => itemsRepeater.UnregisterPropertyChangedCallback(ItemsRepeater.ItemsSourceProperty, token));
		}
	}

	void UnhookItemsRepeaterEvents(
		bool isForDestructor)
	{
		if (isForDestructor)
		{
			//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
		}
		else
		{
			//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
		}

		//if (var itemRepeater = isForDestructor ? m_itemsRepeater.safe_get() : m_itemsRepeater.get())
		{
			m_itemsRepeaterElementPreparedRevoker.Disposable = null;
			m_itemsRepeaterElementClearingRevoker.Disposable = null;
			m_itemsRepeaterElementIndexChangedRevoker.Disposable = null;
			m_itemsRepeaterItemsSourcePropertyChangedRevoker.Disposable = null;
			m_itemsRepeaterLayoutUpdatedRevoker.Disposable = null;
			m_itemsRepeaterSizeChangedRevoker.Disposable = null;

			if (isForDestructor)
			{
				ClearAllItemsViewItemContainerRevokers();
			}
		}
	}

	void HookScrollViewEvents()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (m_scrollView is { } scrollView)
		{
			scrollView.AnchorRequested += OnScrollViewAnchorRequested;
			m_scrollViewAnchorRequestedRevoker.Disposable = new DisposableAction(() => scrollView.AnchorRequested -= OnScrollViewAnchorRequested);

			scrollView.BringingIntoView += OnScrollViewBringingIntoView;
			m_scrollViewBringingIntoViewRevoker.Disposable = new DisposableAction(() => scrollView.BringingIntoView -= OnScrollViewBringingIntoView);
#if DEBUG
			scrollView.ExtentChanged += OnScrollViewExtentChangedDbg;
			m_scrollViewExtentChangedRevokerDbg.Disposable = new DisposableAction(() => scrollView.ExtentChanged -= OnScrollViewExtentChangedDbg);
#endif

			scrollView.ScrollCompleted += OnScrollViewScrollCompleted;
			m_scrollViewScrollCompletedRevoker.Disposable = new DisposableAction(() => scrollView.ScrollCompleted -= OnScrollViewScrollCompleted);
		}
	}

	void UnhookScrollViewEvents(
		bool isForDestructor)
	{
		if (isForDestructor)
		{
			//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
		}
		else
		{
			//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
		}

		//if (var scrollView = isForDestructor ? m_scrollView.safe_get() : m_scrollView.get())
		{
			m_scrollViewAnchorRequestedRevoker.Disposable = null;
			m_scrollViewBringingIntoViewRevoker.Disposable = null;
#if DEBUG
			m_scrollViewExtentChangedRevokerDbg.Disposable = null;
#endif
			m_scrollViewScrollCompletedRevoker.Disposable = null;
		}
	}

	void HookCompositionTargetRendering()
	{
		if (m_renderingRevoker.Disposable is null)
		{
			CompositionTarget.Rendering += OnCompositionTargetRendering;
			m_renderingRevoker.Disposable = new DisposableAction(() => CompositionTarget.Rendering -= OnCompositionTargetRendering);
		}
	}

	void UnhookCompositionTargetRendering()
	{
		m_renderingRevoker.Disposable = null;
	}

	void CacheOriginalVerticalScrollControllerAndVisibility()
	{
		if (m_scrollView is { } scrollView)
		{
			if (scrollView.ScrollPresenter is { } scrollPresenter)
			{
				m_originalVerticalScrollController = scrollPresenter.VerticalScrollController;
			}

			m_originalVerticalScrollBarVisibility = scrollView.VerticalScrollBarVisibility;
		}

		//ITEMSVIEW_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_originalVerticalScrollController, "m_originalVerticalScrollController");
		//ITEMSVIEW_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_originalVerticalScrollBarVisibility", m_originalVerticalScrollBarVisibility);
	}

	// Restore the original ScrollView and ScrollController properties when
	// the ItemsView gets re-templated.
	void RestoreOriginalVerticalScrollControllerAndVisibility()
	{
		//ITEMSVIEW_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_originalVerticalScrollController, "m_originalVerticalScrollController");
		//ITEMSVIEW_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_originalVerticalScrollBarVisibility", m_originalVerticalScrollBarVisibility);

		if (m_scrollView is { } scrollView)
		{
			if (scrollView.ScrollPresenter is { } scrollPresenter)
			{
				scrollPresenter.VerticalScrollController = m_originalVerticalScrollController;
			}

			scrollView.VerticalScrollBarVisibility = m_originalVerticalScrollBarVisibility;
		}

		m_originalVerticalScrollController = null;
		m_originalVerticalScrollBarVisibility = ScrollingScrollBarVisibility.Auto;
	}

	void SetVerticalScrollControllerOnLoaded()
	{
		if (VerticalScrollController is not null)
		{
			// Apply the VerticalScrollController property value that was set
			// before the Loaded event.
			ApplyVerticalScrollController();
		}
		else
		{
			// The VerticalScrollController property was left null prior to the
			// Loaded event. Use the value from the inner ScrollPresenter.
			// This may invoke OnVerticalScrollControllerChanged and
			// ApplyVerticalScrollController but will be a no-op.
			VerticalScrollController = m_originalVerticalScrollController;
		}
	}

	// Propagates the ItemsView.VerticalScrollController property value to the
	// inner ScrollPresenter.VerticalScrollController property.
	// If the value is other than original value read in the Loaded event,
	// the inner ScrollView's VerticalScrollBarVisibility is set to Hidden.
	// Else, the original visibility is restored.
	void ApplyVerticalScrollController()
	{
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, VerticalScrollController(), "VerticalScrollController");

		if (m_scrollView is { } scrollView)
		{
			var verticalScrollController = VerticalScrollController;

			if (verticalScrollController == m_originalVerticalScrollController)
			{
				scrollView.VerticalScrollBarVisibility = m_originalVerticalScrollBarVisibility;
			}
			else
			{
				scrollView.VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden;
			}

			var scrollViewImpl = scrollView;

			if (scrollViewImpl.ScrollPresenter is { } scrollPresenter)
			{
				scrollPresenter.VerticalScrollController = verticalScrollController;
			}
		}
	}

	// Invoked at the beginning of a StartBringItemIntoView call to abort any potential bring-into-view operation, and a few ticks after a bring-into-view
	// operation completed, giving time for the new layout to settle and scroll anchoring to do its job of freezing its target element.
	void CompleteStartBringItemIntoView()
	{
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_bringIntoViewElement.get(), "m_bringIntoViewElement reset.");
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_bringIntoViewCorrelationId reset", m_bringIntoViewCorrelationId);
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_bringIntoViewElementRetentionCountdown reset", m_bringIntoViewElementRetentionCountdown);

		m_bringIntoViewElement = null;
		m_bringIntoViewElementRetentionCountdown = 0;
		m_bringIntoViewCorrelationId = -1;

		if (m_scrollViewHorizontalAnchorRatio != -1)
		{
			MUX_ASSERT(m_scrollViewVerticalAnchorRatio != -1);

			// Restore the pre-operation anchor settings.
			if (m_scrollView is { } scrollView)
			{
				scrollView.HorizontalAnchorRatio = m_scrollViewHorizontalAnchorRatio;
				scrollView.VerticalAnchorRatio = m_scrollViewVerticalAnchorRatio;
			}

			m_scrollViewHorizontalAnchorRatio = -1;
			m_scrollViewVerticalAnchorRatio = -1;
		}

		if (m_navigationKeyProcessingCountdown == 0)
		{
			UnhookCompositionTargetRendering();
		}
	}

	void OnItemsRepeaterElementPrepared(
		ItemsRepeater itemsRepeater,
		ItemsRepeaterElementPreparedEventArgs args)
	{
		if (args.Element is { } element)
		{
			var index = args.Index;

#if DEBUG_VERBOSE
			//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, index);
#endif

			MUX_ASSERT(index == GetElementIndex(element));

			var itemContainer = element as ItemContainer;

			if (itemContainer != null)
			{
#if MUX_PRERELEASE
				if (static_cast<int>(itemContainer.CanUserInvoke() & ItemContainerUserInvokeMode.Auto))
				{
					ItemContainerUserInvokeMode canUserInvoke = ItemContainerUserInvokeMode.Auto;

					canUserInvoke |= IsItemInvokedEnabled() ? ItemContainerUserInvokeMode.UserCanInvoke : ItemContainerUserInvokeMode.UserCannotInvoke;

					itemContainer.CanUserInvoke(canUserInvoke);
				}

				if (static_cast<int>(itemContainer.MultiSelectMode() & ItemContainerMultiSelectMode.Auto))
				{
					ItemContainerMultiSelectMode multiSelectMode = ItemContainerMultiSelectMode.Auto;

					switch (SelectionMode())
					{
						case ItemsViewSelectionMode.None:
						case ItemsViewSelectionMode.Single:
							multiSelectMode |= ItemContainerMultiSelectMode.Single;
							break;
						case ItemsViewSelectionMode.Extended:
							multiSelectMode |= ItemContainerMultiSelectMode.Extended;
							break;
						case ItemsViewSelectionMode.Multiple:
							multiSelectMode |= ItemContainerMultiSelectMode.Multiple;
							break;
					}

					itemContainer.MultiSelectMode(multiSelectMode);
				}

				if ((itemContainer.CanUserSelect() & ItemContainerUserSelectMode.Auto) != 0)
				{
					ItemContainerUserSelectMode canUserSelect = ItemContainerUserSelectMode.Auto;

					canUserSelect |= SelectionMode() == ItemsViewSelectionMode.None ? ItemContainerUserSelectMode.UserCannotSelect : ItemContainerUserSelectMode.UserCanSelect;

					itemContainer.CanUserSelect(canUserSelect);
				}
#else
				if (itemContainer is ItemContainer itemContainerImpl)
				{
					if ((itemContainerImpl.CanUserInvokeInternal() & ItemContainerUserInvokeMode.Auto) != 0)
					{
						ItemContainerUserInvokeMode canUserInvoke = ItemContainerUserInvokeMode.Auto;

						canUserInvoke |= IsItemInvokedEnabled ? ItemContainerUserInvokeMode.UserCanInvoke : ItemContainerUserInvokeMode.UserCannotInvoke;

						itemContainerImpl.CanUserInvokeInternal(canUserInvoke);
					}

					if ((itemContainerImpl.MultiSelectModeInternal() & ItemContainerMultiSelectMode.Auto) != 0)
					{
						ItemContainerMultiSelectMode multiSelectMode = ItemContainerMultiSelectMode.Auto;

						switch (SelectionMode)
						{
							case ItemsViewSelectionMode.None:
							case ItemsViewSelectionMode.Single:
								multiSelectMode |= ItemContainerMultiSelectMode.Single;
								break;
							case ItemsViewSelectionMode.Extended:
								multiSelectMode |= ItemContainerMultiSelectMode.Extended;
								break;
							case ItemsViewSelectionMode.Multiple:
								multiSelectMode |= ItemContainerMultiSelectMode.Multiple;
								break;
						}

						itemContainerImpl.MultiSelectModeInternal(multiSelectMode);
					}

					if ((itemContainerImpl.CanUserSelectInternal() & ItemContainerUserSelectMode.Auto) != 0)
					{
						ItemContainerUserSelectMode canUserSelect = ItemContainerUserSelectMode.Auto;

						canUserSelect |= SelectionMode == ItemsViewSelectionMode.None ? ItemContainerUserSelectMode.UserCannotSelect : ItemContainerUserSelectMode.UserCanSelect;

						itemContainerImpl.CanUserSelectInternal(canUserSelect);
					}
				}
#endif

				bool? isSelectionModelSelectedAsNullable = m_selectionModel.IsSelected(index);
				bool isSelectionModelSelected = isSelectionModelSelectedAsNullable != null && isSelectionModelSelectedAsNullable.Value;

				if (itemContainer.IsSelected)
				{
					// The ItemsSource may be a list of ItemContainers, some of them having IsSelected==True. Account for this situation
					// by updating the selection model accordingly. Only selected containers are pushed into the selection model to avoid
					// clearing any potential selections already present in that model, which are pushed into the ItemContainers next.
					if (!isSelectionModelSelected && SelectionMode != ItemsViewSelectionMode.None)
					{
						// When SelectionMode is None, ItemContainer.IsSelected will be reset below.
						// For all other selection modes, simply select the item.
						// No need to go through the SingleSelector, MultipleSelector or ExtendedSelector policy.
						m_selectionModel.Select(index);

						// Access the new selection status for the same ItemContainer so it can be updated accordingly below.
						isSelectionModelSelectedAsNullable = m_selectionModel.IsSelected(index);
						isSelectionModelSelected = isSelectionModelSelectedAsNullable != null && isSelectionModelSelectedAsNullable.Value;
					}
				}

				itemContainer.IsSelected = isSelectionModelSelected;

				SetItemsViewItemContainerRevokers(itemContainer);
			}
			else
			{
				throw new ArgumentException(s_invalidItemTemplateRoot);
			}

			if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
			{
				element.SetValue(AutomationProperties.PositionInSetProperty, index + 1);
				element.SetValue(AutomationProperties.SizeOfSetProperty, itemsSourceView.Count);
			}
		}
	}

	void OnItemsRepeaterElementClearing(
		ItemsRepeater itemsRepeater,
		ItemsRepeaterElementClearingEventArgs args)
	{
		if (args.Element is { } element)
		{
#if DEBUG_VERBOSE
			//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, GetElementIndex(element));
#endif

			var itemContainer = element as ItemContainer;

			if (itemContainer != null)
			{
				// Clear all the revokers first before touching ItemContainer properties to avoid side effects.
				// For example, if you clear IsSelected before clearing revokers, we will listen to that change and
				// update SelectionModel which is incorrect.
				ClearItemsViewItemContainerRevokers(itemContainer);

#if MUX_PRERELEASE
				if ((itemContainer.CanUserInvoke() & ItemContainerUserInvokeMode.Auto) != 0)
				{
					itemContainer.CanUserInvoke(ItemContainerUserInvokeMode.Auto);
				}

				if ((itemContainer.MultiSelectMode() & ItemContainerMultiSelectMode.Auto) != 0)
				{
					itemContainer.MultiSelectMode(ItemContainerMultiSelectMode.Auto);
				}

				if ((itemContainer.CanUserSelect() & ItemContainerUserSelectMode.Auto) != 0)
				{
					itemContainer.CanUserSelect(ItemContainerUserSelectMode.Auto);
				}
#else
				if (itemContainer is ItemContainer itemContainerImpl)
				{
					if ((itemContainerImpl.CanUserInvokeInternal() & ItemContainerUserInvokeMode.Auto) != 0)
					{
						itemContainerImpl.CanUserInvokeInternal(ItemContainerUserInvokeMode.Auto);
					}

					if ((itemContainerImpl.MultiSelectModeInternal() & ItemContainerMultiSelectMode.Auto) != 0)
					{
						itemContainerImpl.MultiSelectModeInternal(ItemContainerMultiSelectMode.Auto);
					}

					if ((itemContainerImpl.CanUserSelectInternal() & ItemContainerUserSelectMode.Auto) != 0)
					{
						itemContainerImpl.CanUserSelectInternal(ItemContainerUserSelectMode.Auto);
					}
				}
#endif

				itemContainer.IsSelected = false;
			}

			element.ClearValue(AutomationProperties.PositionInSetProperty);
			element.ClearValue(AutomationProperties.SizeOfSetProperty);
		}
	}

	void OnItemsRepeaterElementIndexChanged(
		ItemsRepeater itemsRepeater,
		ItemsRepeaterElementIndexChangedEventArgs args)
	{
		if (args.Element is { } element)
		{
			var newIndex = args.NewIndex;

			//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, newIndex);

			element.SetValue(AutomationProperties.PositionInSetProperty, newIndex + 1);
		}
	}

	void OnItemsRepeaterItemsSourceChanged(
		DependencyObject sender,
		DependencyProperty args)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		HookItemsSourceViewEvents();

		var itemsSource = ItemsSource;

		// Updating the selection model's ItemsSource here rather than earlier in OnPropertyChanged/OnItemsSourceChanged so that
		// Layout.OnItemsChangedCore is executed before OnSelectionModelSelectionChanged. Otherwise OnSelectionModelSelectionChanged
		// would operate on out-of-date ItemsRepeater children.
		m_selectionModel.Source = itemsSource;

		m_currentElementSelectionModel.Source = itemsSource;
	}

	void OnItemsRepeaterLayoutUpdated(
		object sender,
		object args)
	{
#if DEBUG_VERBOSE
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
#endif

		if (m_keyboardNavigationReferenceResetPending)
		{
			m_keyboardNavigationReferenceResetPending = false;
			UpdateKeyboardNavigationReference();
		}
	}

	void OnItemsRepeaterSizeChanged(
		object sender,
		SizeChangedEventArgs args)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, TypeLogging.SizeToString(args.PreviousSize()).c_str(), TypeLogging.SizeToString(args.NewSize()).c_str());

		UpdateKeyboardNavigationReference();
	}

	void OnScrollViewAnchorRequested(
		ScrollView scrollView,
		ScrollingAnchorRequestedEventArgs args)
	{
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "ScrollingAnchorRequestedEventArgs.AnchorCandidates.Size", args.AnchorCandidates().Size());

		if (m_bringIntoViewElement != null)
		{
			// During a StartBringItemIntoView operation, its target element is used as the scroll anchor so that any potential shuffling of the Layout does not disturb the final visual.
			// This anchor is used until the new layout has a chance to settle.
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_bringIntoViewElement.get(), "ScrollingAnchorRequestedEventArgs.AnchorElement set to m_bringIntoViewElement.");
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "at index", GetElementIndex(m_bringIntoViewElement.get()));

			args.AnchorElement = m_bringIntoViewElement;
		}
#if DEBUG
		else
		{
			//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, "ScrollingAnchorRequestedEventArgs.AnchorElement unset. m_bringIntoViewElement null.");
		}
#endif
	}

	void OnScrollViewBringingIntoView(
		ScrollView scrollView,
		ScrollingBringingIntoViewEventArgs args)
	{
#if DEBUG
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
		//	"ScrollingBringingIntoViewEventArgs.CorrelationId", args.CorrelationId);
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this,
		//	"ScrollingBringingIntoViewEventArgs.TargetHorizontalOffset", args.TargetHorizontalOffset());
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this,
		//	"ScrollingBringingIntoViewEventArgs.TargetVerticalOffset", args.TargetVerticalOffset());

		if (args.RequestEventArgs is { } requestEventArgs)
		{
			//ITEMSVIEW_TRACE_INFO(*this, "%s[0x%p](ScrollingBringingIntoViewEventArgs.RequestEventArgs: AnimationDesired:%d, H/V AlignmentRatio:%lf,%lf, H/V Offset:%f,%f, TargetRect:%s, TargetElement:0x%p)\n",
			//	METH_NAME, this,
			//	requestEventArgs.AnimationDesired(),
			//	requestEventArgs.HorizontalAlignmentRatio(), requestEventArgs.VerticalAlignmentRatio(),
			//	requestEventArgs.HorizontalOffset(), requestEventArgs.VerticalOffset(),
			//	TypeLogging.RectToString(requestEventArgs.TargetRect()).c_str(),
			//	requestEventArgs.TargetElement());

			if (requestEventArgs.AnimationDesired)
			{
				//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this,
				//	"ScrollingBringingIntoViewEventArgs.RequestEventArgs.AnimationDesired unexpectedly True");
			}
		}

		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this,
		//	m_bringIntoViewElement.get(), "m_bringIntoViewElement");
#endif

		if (m_bringIntoViewCorrelationId == -1 &&
			m_bringIntoViewElement != null &&
			args.RequestEventArgs != null &&
			m_bringIntoViewElement == args.RequestEventArgs.TargetElement)
		{
			// Record the CorrelationId for the bring-into-view operation so OnScrollViewScrollCompleted can trigger the countdown to a stable layout.
			m_bringIntoViewCorrelationId = args.CorrelationId;

			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "for m_bringIntoViewElement index", GetElementIndex(m_bringIntoViewElement.get()));
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_bringIntoViewCorrelationId set", m_bringIntoViewCorrelationId);
		}

		if (m_navigationKeyBringIntoViewPendingCount > 0)
		{
			m_navigationKeyBringIntoViewPendingCount--;
			// Record the CorrelationId for the navigation-key-triggered bring-into-view operation so OnScrollViewScrollCompleted can trigger the countdown
			// to a stable layout for large offset changes or immediately process queued navigation keys.
			m_navigationKeyBringIntoViewCorrelationId = args.CorrelationId;

			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_navigationKeyBringIntoViewPendingCount decremented", m_navigationKeyBringIntoViewPendingCount);
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_navigationKeyBringIntoViewCorrelationId set", m_navigationKeyBringIntoViewCorrelationId);
		}
	}

#if DEBUG
	void OnScrollViewExtentChangedDbg(
		ScrollView scrollView,
		object args)
	{
		//if (m_scrollView is { } scrollView)
		//{
		//	ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "ScrollView.ExtentWidth", scrollView.ExtentWidth());
		//	ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "ScrollView.ExtentHeight", scrollView.ExtentHeight());
		//}
	}
#endif

	void OnScrollViewScrollCompleted(
		ScrollView scrollView,
		ScrollingScrollCompletedEventArgs args)
	{
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "ScrollingScrollCompletedEventArgs.CorrelationId", args.CorrelationId);

		if (args.CorrelationId == m_bringIntoViewCorrelationId)
		{
			MUX_ASSERT(m_bringIntoViewElement != null);

			m_bringIntoViewCorrelationId = -1;

			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, "m_bringIntoViewCorrelationId reset & m_bringIntoViewElementRetentionCountdown initialized to 4.");

			m_bringIntoViewElementRetentionCountdown = c_renderingEventsPostBringIntoView;

			// OnCompositionTargetRendering will decrement m_bringIntoViewElementRetentionCountdown until it reaches 0,
			// at which point m_bringIntoViewElement is reset to null and the ScrollView anchor alignments are restored.
			HookCompositionTargetRendering();
		}

		if (args.CorrelationId == m_navigationKeyBringIntoViewCorrelationId)
		{
			if (m_lastNavigationKeyProcessed == VirtualKey.Left ||
				m_lastNavigationKeyProcessed == VirtualKey.Right ||
				m_lastNavigationKeyProcessed == VirtualKey.Up ||
				m_lastNavigationKeyProcessed == VirtualKey.Down)
			{
				//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, "m_navigationKeyBringIntoViewCorrelationId reset.");

				m_navigationKeyBringIntoViewCorrelationId = -1;

				if (m_navigationKeyBringIntoViewPendingCount == 0)
				{
					// After a small offset change, for better perf, immediately process the remaining queued navigation keys as no item re-layout is likely.
					ProcessNavigationKeys();
				}
			}
			else
			{
				//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, "m_navigationKeyProcessingCountdown initialized to 4.");

				// After a large offset change for PageDown/PageUp/Home/End, wait a few ticks for the UI to settle before processing the remaining queued
				// navigation keys, so the content is properly layed out.
				m_navigationKeyProcessingCountdown = c_renderingEventsPostBringIntoView;

				HookCompositionTargetRendering();
			}
		}
	}

	void OnItemsViewItemContainerIsSelectedChanged(
		DependencyObject sender,
		DependencyProperty args)
	{
		var element = sender as UIElement;

		MUX_ASSERT(element != null);

		if (element != null)
		{
			int itemIndex = GetElementIndex(element);

			if (itemIndex != -1)
			{
				var itemContainer = element as ItemContainer;

				MUX_ASSERT(itemContainer != null);

				if (itemContainer != null)
				{
					bool? isSelectionModelSelectedAsNullable = m_selectionModel.IsSelected(itemIndex);
					bool isSelectionModelSelected = isSelectionModelSelectedAsNullable != null && isSelectionModelSelectedAsNullable.Value;

					if (itemContainer.IsSelected)
					{
						if (!isSelectionModelSelected)
						{
							if (SelectionMode == ItemsViewSelectionMode.None)
							{
								// Permission denied.
								itemContainer.IsSelected = false;
							}
							else
							{
								// For all other selection modes, simply select the item.
								// No need to go through the SingleSelector, MultipleSelector or ExtendedSelector policy.
								m_selectionModel.Select(itemIndex);
							}
						}
					}
					else
					{
						if (isSelectionModelSelected)
						{
							// For all selection modes, simply deselect the item & preserve the anchor if any.
							m_selector.DeselectWithAnchorPreservation(itemIndex);
						}
					}
				}
			}
		}
	}

#if DEBUG
	void OnLayoutMeasureInvalidatedDbg(
		Layout sender,
		object args)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
	}

	void OnLayoutArrangeInvalidatedDbg(
		Layout sender,
		object args)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);
	}
#endif

	void OnCompositionTargetRendering(
		object sender,
		object args)
	{
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_bringIntoViewElementRetentionCountdown", m_bringIntoViewElementRetentionCountdown);
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "m_navigationKeyProcessingCountdown", m_navigationKeyProcessingCountdown);

		MUX_ASSERT(m_bringIntoViewElementRetentionCountdown > 0 || m_navigationKeyProcessingCountdown > 0);

		if (m_bringIntoViewElementRetentionCountdown > 0)
		{
			// Waiting for the new layout to settle before discarding the bring-into-view target element and no longer using it as a scroll anchor.
			m_bringIntoViewElementRetentionCountdown--;

			if (m_bringIntoViewElementRetentionCountdown == 0)
			{
				CompleteStartBringItemIntoView();
			}
		}

		if (m_navigationKeyProcessingCountdown > 0)
		{
			// Waiting for the new layout to settle before processing remaining queued navigation keys.
			m_navigationKeyProcessingCountdown--;

			if (m_navigationKeyProcessingCountdown == 0)
			{
				if (m_bringIntoViewElementRetentionCountdown == 0)
				{
					UnhookCompositionTargetRendering();
				}

				//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, "m_navigationKeyBringIntoViewCorrelationId reset.");

				m_navigationKeyBringIntoViewCorrelationId = -1;

				if (m_navigationKeyBringIntoViewPendingCount == 0)
				{
					// With no pending OnScrollViewBringingIntoView calls, it is time to process the remaining queue navigation keys.
					ProcessNavigationKeys();
				}
			}
		}
	}

	void OnItemsSourceChanged()
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		// When the inner ItemsRepeater has not been loaded yet, set the selection models' Source
		// right away as OnItemsRepeaterItemsSourceChanged will not be invoked.
		// There is no reason to delay the updates to OnItemsRepeaterItemsSourceChanged
		// in this case since ItemsRepeater and its children do not exist yet.
		var itemsRepeater = m_itemsRepeater;

		if (itemsRepeater == null)
		{
			var itemsSource = ItemsSource;

			m_selectionModel.Source = itemsSource;
			m_currentElementSelectionModel.Source = itemsSource;
		}
	}

	void OnVerticalScrollControllerChanged()
	{
		//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, VerticalScrollController(), "VerticalScrollController");

		ApplyVerticalScrollController();
	}

#if DEBUG
	void OnLayoutChangedDbg()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		m_layoutMeasureInvalidatedDbg.Disposable = null;
		m_layoutArrangeInvalidatedDbg.Disposable = null;

		if (Layout is { } layout)
		{
			layout.MeasureInvalidated += OnLayoutMeasureInvalidatedDbg;
			m_layoutMeasureInvalidatedDbg.Disposable = new DisposableAction(() => layout.MeasureInvalidated -= OnLayoutMeasureInvalidatedDbg);

			layout.ArrangeInvalidated += OnLayoutArrangeInvalidatedDbg;
			m_layoutArrangeInvalidatedDbg.Disposable = new DisposableAction(() => layout.ArrangeInvalidated -= OnLayoutArrangeInvalidatedDbg);
		}
	}
#endif

	void OnIsItemInvokedEnabledChanged()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		// For all ItemContainer children, update the CanUserInvoke property according to the new ItemsView.IsItemInvokedEnabled property.
		if (m_itemsRepeater is { } itemsRepeater)
		{
			var count = VisualTreeHelper.GetChildrenCount(itemsRepeater);
			ItemContainerUserInvokeMode canUserInvoke = IsItemInvokedEnabled ?
				ItemContainerUserInvokeMode.Auto | ItemContainerUserInvokeMode.UserCanInvoke :
				ItemContainerUserInvokeMode.Auto | ItemContainerUserInvokeMode.UserCannotInvoke;

			for (int childIndex = 0; childIndex < count; childIndex++)
			{
				var elementAsDO = VisualTreeHelper.GetChild(itemsRepeater, childIndex);
				var itemContainer = elementAsDO as ItemContainer;

				if (itemContainer != null)
				{
#if MUX_PRERELEASE
					if ((itemContainer.CanUserInvoke() & ItemContainerUserInvokeMode.Auto) != 0)
					{
						itemContainer.CanUserInvoke(canUserInvoke);
					}
#else
					if (itemContainer is ItemContainer itemContainerImpl)
					{
						if ((itemContainerImpl.CanUserInvokeInternal() & ItemContainerUserInvokeMode.Auto) != 0)
						{
							itemContainerImpl.CanUserInvokeInternal(canUserInvoke);
						}
					}
#endif
				}
			}
		}
	}

	void OnItemTemplateChanged()
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		// Make sure the default ItemTemplate is used when the ItemsSource is non-null and the ItemTemplate is null.
		EnsureItemTemplate();
	}

	void OnSelectionModeChanged()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UpdateSelector();

		// For all ItemContainer children, update the MultiSelectMode and CanUserSelect properties according to the new ItemsView.SelectionMode property.
		if (m_itemsRepeater is { } itemsRepeater)
		{
			var count = VisualTreeHelper.GetChildrenCount(itemsRepeater);
			var selectionMode = SelectionMode;
			ItemContainerMultiSelectMode multiSelectMode = ItemContainerMultiSelectMode.Auto;

			switch (selectionMode)
			{
				case ItemsViewSelectionMode.None:
				case ItemsViewSelectionMode.Single:
					multiSelectMode |= ItemContainerMultiSelectMode.Single;
					break;
				case ItemsViewSelectionMode.Extended:
					multiSelectMode |= ItemContainerMultiSelectMode.Extended;
					break;
				case ItemsViewSelectionMode.Multiple:
					multiSelectMode |= ItemContainerMultiSelectMode.Multiple;
					break;
			}

			ItemContainerUserSelectMode canUserSelect = selectionMode == ItemsViewSelectionMode.None ?
				ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCannotSelect :
				ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect;

			for (int childIndex = 0; childIndex < count; childIndex++)
			{
				var elementAsDO = VisualTreeHelper.GetChild(itemsRepeater, childIndex);
				var itemContainer = elementAsDO as ItemContainer;

				if (itemContainer != null)
				{
#if MUX_PRERELEASE
					if ((itemContainer.MultiSelectMode() & ItemContainerMultiSelectMode.Auto) != 0)
					{
						itemContainer.MultiSelectMode(multiSelectMode);
					}

					if ((itemContainer.CanUserSelect() & ItemContainerUserSelectMode.Auto) != 0)
					{
						itemContainer.CanUserSelect(canUserSelect);
					}
#else
					if (itemContainer is ItemContainer itemContainerImpl)
					{
						if ((itemContainerImpl.MultiSelectModeInternal() & ItemContainerMultiSelectMode.Auto) != 0)
						{
							itemContainerImpl.MultiSelectModeInternal(multiSelectMode);
						}

						if ((itemContainerImpl.CanUserSelectInternal() & ItemContainerUserSelectMode.Auto) != 0)
						{
							itemContainerImpl.CanUserSelectInternal(canUserSelect);
						}
					}
#endif
				}
			}
		}
	}

	void OnSelectionModelSelectionChanged(
		SelectionModel selectionModel,
		SelectionModelSelectionChangedEventArgs args)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		// Unfortunately using an internal hook to see whether this notification orginated from a collection change or not.
		bool selectionInvalidatedDueToCollectionChange =
			selectionModel.SelectionInvalidatedDueToCollectionChange();

		/*
		Another option, besides a public API on SelectionModel, would have been to apply the changes
		asynchronously like below. But that seems fragile compared to delaying the application until
		the synchronous call to OnSourceListChanged that is about to occur.
		DispatcherQueue().TryEnqueue(
			DispatcherQueuePriority.Low,
			DispatcherQueueHandler([weakThis{ get_weak() }]()
			{
				if (var strongThis = weakThis.get())
				{
					strongThis->ApplySelectionModelSelectionChange();
				}
			}));
		*/

		if (selectionInvalidatedDueToCollectionChange)
		{
			// Delay the SelectionModel's selection changes until the upcoming OnSourceListChanged
			// call because neither m_itemsRepeater's Children nor m_itemsRepeater's ItemsSourceView have been updated yet.
			// ApplySelectionModelSelectionChange which uses both is thus delayed, but is still going to be called synchronously.
			m_applySelectionChangeOnSourceListChanged = true;
		}
		else
		{
			ApplySelectionModelSelectionChange();
		}
	}

	void ApplySelectionModelSelectionChange()
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		// Update ItemsView properties
		SelectedItem = m_selectionModel.SelectedItem;

		// For all ItemContainer children, update the IsSelected property according to the new ItemsView.SelectionModel property.
		if (m_itemsRepeater is { } itemsRepeater)
		{
			var count = VisualTreeHelper.GetChildrenCount(itemsRepeater);

#if DEBUG
			if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
			{
				var itemsSourceViewCount = itemsSourceView.Count;
			}
#endif

			for (int childIndex = 0; childIndex < count; childIndex++)
			{
				var elementAsDO = VisualTreeHelper.GetChild(itemsRepeater, childIndex);
				var itemContainer = elementAsDO as ItemContainer;

				if (itemContainer != null)
				{
					int itemIndex = itemsRepeater.GetElementIndex(itemContainer);

					if (itemIndex >= 0)
					{
						bool? isItemContainerSelected = m_selectionModel.IsSelected(itemIndex);
						bool isSelected = isItemContainerSelected != null && isItemContainerSelected.Value;

						if (isSelected)
						{
#if MUX_PRERELEASE
							ItemContainerUserSelectMode canUserSelect = itemContainer.CanUserSelect();
#else
							ItemContainerUserSelectMode canUserSelect = itemContainer.CanUserSelectInternal();
#endif

							if (!m_isProcessingInteraction || (canUserSelect & ItemContainerUserSelectMode.UserCannotSelect) == 0)
							{
								itemContainer.IsSelected = true;
							}
							else
							{
								// Processing a user interaction while ItemContainer.CanUserSelect is ItemContainerUserSelectMode.UserCannotSelect. Deselect that item
								// in the selection model without touching the potential anchor.
								m_selector.DeselectWithAnchorPreservation(itemIndex);
							}
						}
						else
						{
							itemContainer.IsSelected = false;
						}
					}
				}
			}
		}

		RaiseSelectionChanged();
	}

	// Raised when the current element index changed.
	void OnCurrentElementSelectionModelSelectionChanged(
		SelectionModel selectionModel,
		SelectionModelSelectionChangedEventArgs args)
	{
		int currentElementIndex = GetCurrentElementIndex();

		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, currentElementIndex);

		CurrentItemIndex = currentElementIndex;
	}

	// Raised when the ItemsSource collection changed.
	void OnSourceListChanged(
		object dataSource,
		NotifyCollectionChangedEventArgs args)
	{
		if (m_applySelectionChangeOnSourceListChanged)
		{
			m_applySelectionChangeOnSourceListChanged = false;

			// Finally apply the SelectionModel's changes notified in OnSelectionModelSelectionChanged
			// now that both m_itemsRepeater's Children & m_itemsRepeater's ItemsSourceView are up-to-date.
			ApplySelectionModelSelectionChange();
		}

		// When the item count goes from 0 to strictly positive, the ItemTemplate property may
		// have to be set to a default template which includes an ItemContainer.
		EnsureItemTemplate();

		m_keyboardNavigationReferenceResetPending = true;

		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
			{
				var count = itemsSourceView.Count;

				for (var index = 0; index < count; index++)
				{
					if (itemsRepeater.TryGetElement(index) is { } element)
					{
						element.SetValue(AutomationProperties.SizeOfSetProperty, count);
					}
				}
			}
		}
	}

	private protected override void OnLoaded(
		//object sender,
		//RoutedEventArgs args
		)
	{
		if (m_setVerticalScrollControllerOnLoaded)
		{
			// First occurrence of the Loaded event after template
			// application. Now that the inner ScrollView and ScrollPresenter
			// are loaded, cache their original vertical scroll
			// controller visibility and value for potential restoration.
			m_setVerticalScrollControllerOnLoaded = false;

			CacheOriginalVerticalScrollControllerAndVisibility();

			// Either push the VerticalScrollController value already set
			// to the inner control or set VerticalScrollController to the
			// inner control's value.
			SetVerticalScrollControllerOnLoaded();
		}
	}

	private protected override void OnUnloaded(
		//object sender,
		//RoutedEventArgs args
		)
	{
		//ITEMSVIEW_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (!IsLoaded)
		{
			UnhookCompositionTargetRendering();

			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, "Keyboard navigation fields reset.");

			m_navigationKeyBringIntoViewPendingCount = 0;
			m_navigationKeyBringIntoViewCorrelationId = -1;
			m_navigationKeyProcessingCountdown = 0;
			m_navigationKeysToProcess.Clear();

			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR, METH_NAME, this, "Bring-into-view fields reset.");

			m_bringIntoViewElement = null;
			m_bringIntoViewCorrelationId = -1;
			m_bringIntoViewElementRetentionCountdown = 0;
		}
	}

	// When isForTopLeftItem is True, returns the top/left focusable item in the viewport. Returns the bottom/right item instead.
	// Fully displayed items are favored over partially displayed ones. 
	int GetCornerFocusableItem(
		bool isForTopLeftItem)
	{
		// When FlowDirection is FlowDirection.RightToLeft, the top/right item is returned instead of top/left (and bottom/left instead of bottom/right).
		// GetItemInternal's input is unchanged as it handles FlowDirection itself.

		IndexBasedLayoutOrientation indexBasedLayoutOrientation = GetLayoutIndexBasedLayoutOrientation();
		int itemIndex = GetItemInternal(
			isForTopLeftItem ? 0.0 : 1.0 /*horizontalViewportRatio*/,
			isForTopLeftItem ? 0.0 : 1.0 /*verticalViewportRatio*/,
			indexBasedLayoutOrientation == IndexBasedLayoutOrientation.TopToBottom /*isHorizontalDistanceFavored*/,
			indexBasedLayoutOrientation == IndexBasedLayoutOrientation.LeftToRight /*isVerticalDistanceFavored*/,
			false /*useKeyboardNavigationReferenceHorizontalOffset*/,
			false /*useKeyboardNavigationReferenceVerticalOffset*/,
			true /*capItemEdgesToViewportRatioEdges*/,
			true /*forFocusableItemsOnly*/);

		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, itemIndex);

		return itemIndex;
	}

	int GetElementIndex(
		UIElement element)
	{
		MUX_ASSERT(element != null);

		if (m_itemsRepeater is { } itemsRepeater)
		{
			return itemsRepeater.GetElementIndex(element);
		}

		return -1;
	}

	IndexPath GetElementIndexPath(
		UIElement element,
		ref bool isValid)
	{
		MUX_ASSERT(element != null);

		int index = GetElementIndex(element);

		isValid = index >= 0;

		return IndexPath.CreateFrom(index);
	}

	object GetElementItem(
		UIElement element,
		ref bool valueReturned)
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
			{
				int itemIndex = GetElementIndex(element);

				if (itemIndex >= 0 && itemsSourceView.Count > itemIndex)
				{
					valueReturned = true;
					return itemsSourceView.GetAt(itemIndex);
				}
			}
		}

		return null;
	}

	// isHorizontalDistanceFavored==True:
	//  - Means distance on the horizontal axis supercedes the one on the vertical axis. i.e. the vertical distance is only considered when the horizontal distance is identical.
	// isVerticalDistanceFavored==True:
	//  - Means distance on the vertical axis supercedes the one on the horizontal axis. i.e. the horizontal distance is only considered when the vertical distance is identical.
	// useKeyboardNavigationReferenceHorizontalOffset:
	//  - Instead of using horizontalViewportRatio, m_keyboardNavigationReferenceIndex/m_keyboardNavigationReferenceRect define a target vertical line. The distance from the middle
	//    of the items to that line is considered.
	// useKeyboardNavigationReferenceVerticalOffset:
	//  - Instead of using verticalViewportRatio, m_keyboardNavigationReferenceIndex/m_keyboardNavigationReferenceRect define a target horizontal line. The distance from the middle
	//    of the items to that line is considered.
	// capItemEdgesToViewportRatioEdges==False:
	//  - Means
	//    - horizontalViewportRatio <= 0.5: find item with left edge closest to viewport ratio edge
	//    - horizontalViewportRatio > 0.5: find item with right edge closest to viewport ratio edge
	//    - verticalViewportRatio <= 0.5: find item with top edge closest to viewport ratio edge
	//    - verticalViewportRatio > 0.5: find item with bottom edge closest to viewport ratio edge
	// capItemEdgesToViewportRatioEdges==True:
	//  - Means that the two item edges used for distance measurements must be closer to the center of the viewport than viewport ratio edges.
	//  - Used for PageUp/PageDown operations which respectively look for items with their near/far edge on the viewport center side of the viewport ratio edges.
	//  - Additional restrictions compared to capItemEdgesToViewportRatioEdges==False above:
	//    - horizontalViewportRatio <= 0.5: item left edge larger than viewport ratio edge
	//    - horizontalViewportRatio > 0.5: item right edge smaller than viewport ratio edge
	//    - verticalViewportRatio <= 0.5: item top edge larger than viewport ratio edge
	//    - verticalViewportRatio > 0.5: item bottom edge smaller than viewport ratio edge
	// Returns -1 ItemsRepeater or ScrollView part are null, or the data source is empty, or when no item fulfills the restrictions imposed by capItemEdgesToViewportRatioEdges and/or forFocusableItemsOnly.
	int GetItemInternal(
		double horizontalViewportRatio,
		double verticalViewportRatio,
		bool isHorizontalDistanceFavored,
		bool isVerticalDistanceFavored,
		bool useKeyboardNavigationReferenceHorizontalOffset,
		bool useKeyboardNavigationReferenceVerticalOffset,
		bool capItemEdgesToViewportRatioEdges,
		bool forFocusableItemsOnly)
	{
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalViewportRatio, verticalViewportRatio);
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, isHorizontalDistanceFavored, isVerticalDistanceFavored);
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, capItemEdgesToViewportRatioEdges, forFocusableItemsOnly);

		MUX_ASSERT(!isHorizontalDistanceFavored || !isVerticalDistanceFavored);
		MUX_ASSERT(!useKeyboardNavigationReferenceHorizontalOffset || horizontalViewportRatio == 0.0);
		MUX_ASSERT(!useKeyboardNavigationReferenceVerticalOffset || verticalViewportRatio == 0.0);

		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (m_scrollView is { } scrollView)
			{
				float zoomFactor = scrollView.ZoomFactor;
				bool useHorizontalItemNearEdge = false;
				bool useVerticalItemNearEdge = false;
				double horizontalTarget = default;
				double verticalTarget = default;

				if (!useKeyboardNavigationReferenceHorizontalOffset)
				{
					double horizontalScrollOffset = scrollView.HorizontalOffset;
					double viewportWidth = scrollView.ViewportWidth;
					double horizontalViewportOffset = horizontalViewportRatio * viewportWidth;
					double horizontalExtent = scrollView.ExtentWidth * (double)zoomFactor;

					horizontalTarget = horizontalScrollOffset + horizontalViewportOffset;
					horizontalTarget = Math.Max(0.0, horizontalTarget);
					horizontalTarget = Math.Min(horizontalExtent, horizontalTarget);

					useHorizontalItemNearEdge = horizontalViewportRatio <= 0.5;
				}

				if (!useKeyboardNavigationReferenceVerticalOffset)
				{
					double verticalScrollOffset = scrollView.VerticalOffset;
					double viewportHeight = scrollView.ViewportHeight;
					double verticalViewportOffset = verticalViewportRatio * viewportHeight;
					double verticalExtent = scrollView.ExtentHeight * (double)zoomFactor;

					verticalTarget = verticalScrollOffset + verticalViewportOffset;
					verticalTarget = Math.Max(0.0, verticalTarget);
					verticalTarget = Math.Min(verticalExtent, verticalTarget);

					useVerticalItemNearEdge = verticalViewportRatio <= 0.5;
				}

				float keyboardNavigationReferenceOffset = -1.0f;

				if (useKeyboardNavigationReferenceHorizontalOffset || useKeyboardNavigationReferenceVerticalOffset)
				{
					Point keyboardNavigationReferenceOffsetPoint = GetUpdatedKeyboardNavigationReferenceOffset();
					keyboardNavigationReferenceOffset = useKeyboardNavigationReferenceHorizontalOffset ? (float)keyboardNavigationReferenceOffsetPoint.X : (float)keyboardNavigationReferenceOffsetPoint.Y;

					MUX_ASSERT(keyboardNavigationReferenceOffset != -1.0f);
				}

				double roundingScaleFactor = GetRoundingScaleFactor(scrollView);
				int childrenCount = VisualTreeHelper.GetChildrenCount(itemsRepeater);
				UIElement closestElement = null;
				double smallestFavoredDistance = double.MaxValue;
				double smallestUnfavoredDistance = double.MaxValue;

				for (int childIndex = 0; childIndex < childrenCount; childIndex++)
				{
					var elementAsDO = VisualTreeHelper.GetChild(itemsRepeater, childIndex);
					var element = elementAsDO as UIElement;

					if (element != null)
					{
						if (forFocusableItemsOnly && !SharedHelpers.IsFocusableElement(element))
						{
							continue;
						}

						Rect elementRect = GetElementRect(element, itemsRepeater);
						Rect elementZoomedRect = new Rect(elementRect.X * zoomFactor, elementRect.Y * zoomFactor, elementRect.Width * zoomFactor, elementRect.Height * zoomFactor);
						double signedHorizontalDistance = default;
						double signedVerticalDistance = default;

						if (useKeyboardNavigationReferenceHorizontalOffset)
						{
							signedHorizontalDistance = elementZoomedRect.X + elementZoomedRect.Width / 2.0f - keyboardNavigationReferenceOffset * zoomFactor;
						}
						else
						{
							signedHorizontalDistance = useHorizontalItemNearEdge ? elementZoomedRect.X - horizontalTarget : horizontalTarget - elementZoomedRect.X - elementZoomedRect.Width;

							if (capItemEdgesToViewportRatioEdges && signedHorizontalDistance < -1.0 / roundingScaleFactor)
							{
								continue;
							}
						}

						if (useKeyboardNavigationReferenceVerticalOffset)
						{
							signedVerticalDistance = elementZoomedRect.Y + elementZoomedRect.Height / 2.0f - keyboardNavigationReferenceOffset * zoomFactor;
						}
						else
						{
							signedVerticalDistance = useVerticalItemNearEdge ? elementZoomedRect.Y - verticalTarget : verticalTarget - elementZoomedRect.Y - elementZoomedRect.Height;

							if (capItemEdgesToViewportRatioEdges && signedVerticalDistance < -1.0 / roundingScaleFactor)
							{
								continue;
							}
						}

						double horizontalDistance = Math.Abs(signedHorizontalDistance);
						double verticalDistance = Math.Abs(signedVerticalDistance);

						if (!isHorizontalDistanceFavored && !isVerticalDistanceFavored)
						{
							horizontalDistance = Math.Pow(horizontalDistance, 2.0);
							verticalDistance = Math.Pow(verticalDistance, 2.0);

							smallestUnfavoredDistance = 0.0;

							if (horizontalDistance + verticalDistance < smallestFavoredDistance)
							{
								smallestFavoredDistance = horizontalDistance + verticalDistance;
								closestElement = element;
							}
						}
						else if (isHorizontalDistanceFavored)
						{
							if (horizontalDistance < smallestFavoredDistance)
							{
								smallestFavoredDistance = horizontalDistance;
								smallestUnfavoredDistance = verticalDistance;
								closestElement = element;
							}
							else if (horizontalDistance == smallestFavoredDistance && verticalDistance < smallestUnfavoredDistance)
							{
								smallestUnfavoredDistance = verticalDistance;
								closestElement = element;
							}
						}
						else
						{
							MUX_ASSERT(isVerticalDistanceFavored);

							if (verticalDistance < smallestFavoredDistance)
							{
								smallestFavoredDistance = verticalDistance;
								smallestUnfavoredDistance = horizontalDistance;
								closestElement = element;
							}
							else if (verticalDistance == smallestFavoredDistance && horizontalDistance < smallestUnfavoredDistance)
							{
								smallestUnfavoredDistance = horizontalDistance;
								closestElement = element;
							}
						}

						if (smallestFavoredDistance == 0.0 && smallestUnfavoredDistance == 0.0)
						{
							break;
						}
					}
				}

				return closestElement == null ? -1 : itemsRepeater.GetElementIndex(closestElement);
			}
		}

		return -1;
	}

	double GetRoundingScaleFactor(
		UIElement xamlRootReference)
	{
		if (xamlRootReference.XamlRoot is { } xamlRoot)
		{
			return (float)xamlRoot.RasterizationScale;
		}
		return 1.0; // Assuming a 1.0 scale factor when no XamlRoot is available at the moment.
	}

	bool SetCurrentElementIndex(
		int index,
		FocusState focusState,
		bool forceKeyboardNavigationReferenceReset,
		bool startBringIntoView = false,
		bool expectBringIntoView = false)
	{
		int currentElementIndex = GetCurrentElementIndex();

		if (index != currentElementIndex)
		{
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "current element index", currentElementIndex);
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "index", index);

			if (index == -1)
			{
				m_currentElementSelectionModel.ClearSelection();
			}
			else
			{
				m_currentElementSelectionModel.Select(index);
			}

			if (index == -1 || m_keyboardNavigationReferenceRect.X == -1.0 || forceKeyboardNavigationReferenceReset)
			{
				UpdateKeyboardNavigationReference();
			}
		}

		return SetFocusElementIndex(index, focusState, startBringIntoView, expectBringIntoView);
	}

	// Returns True when a bring-into-view operation was initiated, and False otherwise. 
	bool StartBringItemIntoViewInternal(
		bool throwOutOfBounds,
		bool throwOnAnyFailure,
		int index,
		BringIntoViewOptions options)
	{
#if DEBUG
		//ITEMSVIEW_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "index", index);

		if (options != null)
		{
			//ITEMSVIEW_TRACE_INFO(*this, "%s[0x%p](AnimationDesired:%d, H/V AlignmentRatio:%lf,%lf, H/V Offset:%f,%f, TargetRect:%s)\n",
			//	METH_NAME, this,
			//	options.AnimationDesired(),
			//	options.HorizontalAlignmentRatio(), options.VerticalAlignmentRatio(),
			//	options.HorizontalOffset(), options.VerticalOffset(),
			//	options.TargetRect() == null ? "nul" : TypeLogging.RectToString(options.TargetRect().Value()).c_str());
		}
#endif

		var itemsRepeater = m_itemsRepeater;

		if (itemsRepeater == null)
		{
			// StartBringIntoView operation cannot be initiated without ItemsRepeater part.
			if (throwOnAnyFailure)
			{
				throw new InvalidOperationException(s_missingItemsRepeaterPart);
			}

			return false;
		}

		bool isItemIndexValid = ValidateItemIndex(throwOutOfBounds || throwOnAnyFailure, index);

		if (!isItemIndexValid)
		{
			MUX_ASSERT(!throwOutOfBounds && !throwOnAnyFailure);

			return false;
		}

		// Reset the fields changed by any potential bring-into-view operation still in progress.
		CompleteStartBringItemIntoView();

		MUX_ASSERT(m_bringIntoViewElement == null);
		MUX_ASSERT(m_bringIntoViewElementRetentionCountdown == 0);
		MUX_ASSERT(m_bringIntoViewCorrelationId == -1);
		MUX_ASSERT(m_scrollViewHorizontalAnchorRatio == -1);
		MUX_ASSERT(m_scrollViewVerticalAnchorRatio == -1);

		// Access or create the target element so its position within the ItemsRepeater can be evaluated.
		UIElement element = itemsRepeater.GetOrCreateElement(index);

		MUX_ASSERT(element != null);

		var scrollView = m_scrollView;

		// During the initial position evaluation, scroll anchoring is turned off to avoid shifting offsets
		// which may result in an incorrect final scroll offset.
		if (scrollView is not null)
		{
			// Current anchoring settings are recorded for post-operation restoration.
			m_scrollViewHorizontalAnchorRatio = scrollView.HorizontalAnchorRatio;
			m_scrollViewVerticalAnchorRatio = scrollView.VerticalAnchorRatio;

			scrollView.HorizontalAnchorRatio = DoubleUtil.NaN;
			scrollView.VerticalAnchorRatio = DoubleUtil.NaN;
		}

		//ITEMSVIEW_TRACE_VERBOSE_DBG(*this, TRACE_MSG_METH_METH, METH_NAME, this, "UIElement.UpdateLayout");

		// Ensure the item is given a valid position within the ItemsRepeater. It will determine the target scroll offset.
		element.UpdateLayout();

		// Make sure that the target index is still valid after the UpdateLayout call.
		isItemIndexValid = ValidateItemIndex(false /*throwIfInvalid*/, index);

		if (!isItemIndexValid)
		{
			// Restore scrollView.HorizontalAnchorRatio/VerticalAnchorRatio properties with m_scrollViewHorizontalAnchorRatio/m_scrollViewVerticalAnchorRatio cached values.
			CompleteStartBringItemIntoView();

			// StartBringIntoView operation cannot be initiated without ItemsRepeater part or in-bounds item index.
			if (throwOnAnyFailure)
			{
				throw new InvalidOperationException(s_indexOutOfBounds);
			}
			return false;
		}

		if (scrollView is not null)
		{
			// Turn on scroll anchoring during and after the scroll operation so target repositionings have no effect on the final visual.
			// The value 0.5 is used rather than 0.0 or 1.0 to avoid near and far edge anchoring which only take effect when the content
			// hits the viewport boundary. With any value between 0 and 1, anchoring is also effective in those extreme cases. 
			const double anchorRatio = 0.5;

			scrollView.HorizontalAnchorRatio = anchorRatio;
			scrollView.VerticalAnchorRatio = anchorRatio;
		}

		// Access the element's index to account for the rare event where it was recycled during the layout.
		if (index != GetElementIndex(element))
		{
			// Access the target element which was created during the layout pass. Its position is already set.
			element = itemsRepeater.GetOrCreateElement(index);

			if (GetElementIndex(element) == -1)
			{
				// This situation arises when the ItemsView is not parented.
				// Restore the ScrollView's HorizontalAnchorRatio/VerticalAnchorRatio properties.
				CompleteStartBringItemIntoView();

				if (throwOnAnyFailure)
				{
					throw new InvalidOperationException(s_itemsViewNotParented);
				}

				return false;
			}

			MUX_ASSERT(index == GetElementIndex(element));

			// Make sure animation is turned off in this rare case.
			if (options != null)
			{
				options.AnimationDesired = false;
			}
		}

		m_bringIntoViewElement = element;

		// Trigger the actual bring-into-view scroll - it'll cause OnScrollViewBringingIntoView and OnScrollViewScrollCompleted calls.
		if (options == null)
		{
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_bringIntoViewElement.get(), "m_bringIntoViewElement set. Invoking UIElement.StartBringIntoView without options.");

			element.StartBringIntoView();
		}
		else
		{
			//ITEMSVIEW_TRACE_INFO_DBG(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this, m_bringIntoViewElement.get(), "m_bringIntoViewElement set. Invoking UIElement.StartBringIntoView with options.");

			element.StartBringIntoView(options);
		}

		return true;
	}

	UIElement TryGetElement(int index)
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			return itemsRepeater.TryGetElement(index);
		}

		return null;
	}

	void SetItemsViewItemContainerRevokers(
		ItemContainer itemContainer)
	{
		var itemContainerRevokers = new ItemContainerRevokers();

		itemContainer.KeyDown += OnItemsViewElementKeyDown;
		itemContainerRevokers.m_keyDownRevoker.Disposable = new DisposableAction(() => itemContainer.KeyDown -= OnItemsViewElementKeyDown);

		itemContainer.GettingFocus += OnItemsViewElementGettingFocus;
		itemContainerRevokers.m_gettingFocusRevoker.Disposable = new DisposableAction(() => itemContainer.GettingFocus -= OnItemsViewElementGettingFocus);

#if DEBUG
		itemContainer.LosingFocus += OnItemsViewElementLosingFocusDbg;
		itemContainerRevokers.m_losingFocusRevoker.Disposable = new DisposableAction(() => itemContainer.LosingFocus -= OnItemsViewElementLosingFocusDbg);
#endif

		itemContainer.ItemInvoked += OnItemsViewItemContainerItemInvoked;
		itemContainerRevokers.m_itemInvokedRevoker.Disposable = new DisposableAction(() => itemContainer.ItemInvoked -= OnItemsViewItemContainerItemInvoked);

		var token = itemContainer.RegisterPropertyChangedCallback(ItemContainer.IsSelectedProperty, OnItemsViewItemContainerIsSelectedChanged);
		itemContainerRevokers.m_isSelectedPropertyChangedRevoker.Disposable = new DisposableAction(() => itemContainer.UnregisterPropertyChangedCallback(ItemContainer.IsSelectedProperty, token));

#if DEBUG
		itemContainer.SizeChanged += OnItemsViewItemContainerSizeChangedDbg;
		itemContainerRevokers.m_sizeChangedRevokerDbg.Disposable = new DisposableAction(() => itemContainer.SizeChanged -= OnItemsViewItemContainerSizeChangedDbg);
#endif

		itemContainer.SetValue(ItemsViewItemContainerRevokersProperty, itemContainerRevokers);

		m_itemContainersWithRevokers.Add(itemContainer);
	}

	void ClearItemsViewItemContainerRevokers(
		ItemContainer itemContainer)
	{
		RevokeItemsViewItemContainerRevokers(itemContainer);
		itemContainer.SetValue(ItemsViewItemContainerRevokersProperty, null);
		m_itemContainersWithRevokers.Remove(itemContainer);
	}

	void ClearAllItemsViewItemContainerRevokers()
	{
		foreach (var itemContainer in m_itemContainersWithRevokers)
		{
			// ClearAllItemsViewItemContainerRevokers is only called in the destructor, where exceptions cannot be thrown.
			// If the associated ItemsView items have not yet been cleaned up, we must detach these revokers or risk a call into freed
			// memory being made.  However if they have been cleaned up these calls will throw. In this case we can ignore
			// those exceptions.
			try
			{
				RevokeItemsViewItemContainerRevokers(itemContainer);
				itemContainer.SetValue(ItemsViewItemContainerRevokersProperty, null);
			}
			catch
			{
			}
		}
		m_itemContainersWithRevokers.Clear();
	}

	void RevokeItemsViewItemContainerRevokers(
		ItemContainer itemContainer)
	{
		if (itemContainer.GetValue(ItemsViewItemContainerRevokersProperty) is { } revokers)
		{
			if (revokers is ItemContainerRevokers itemContainerRevokers)
			{
				itemContainerRevokers.RevokeAll(itemContainer);
			}
		}
	}

	bool ValidateItemIndex(
		bool throwIfInvalid,
		int index)
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (itemsRepeater.ItemsSourceView == null)
			{
				if (throwIfInvalid)
				{
					throw new InvalidOperationException(s_itemsSourceNull);
				}
				return false;
			}

			if (index < 0 || index >= itemsRepeater.ItemsSourceView.Count)
			{
				if (throwIfInvalid)
				{
					throw new IndexOutOfRangeException(s_indexOutOfBounds);
				}
				return false;
			}

			return true;
		}

		return false;
	}

	// Invoked by ItemsViewTestHooks
	internal Point GetKeyboardNavigationReferenceOffset()
	{
		if (m_keyboardNavigationReferenceRect.X == -1.0f)
		{
			return new Point(-1.0f, -1.0f);
		}

		return new Point(
			m_keyboardNavigationReferenceRect.X + m_keyboardNavigationReferenceRect.Width / 2.0f,
			m_keyboardNavigationReferenceRect.Y + m_keyboardNavigationReferenceRect.Height / 2.0f);
	}

	int GetCurrentElementIndex()
	{
		IndexPath currentElementIndexPath = m_currentElementSelectionModel.SelectedIndex;

		MUX_ASSERT(currentElementIndexPath == null || currentElementIndexPath.GetSize() == 1);

		int currentElementIndex = currentElementIndexPath == null ? -1 : currentElementIndexPath.GetAt(0);

		return currentElementIndex;
	}

	internal ScrollView GetScrollViewPart()
	{
		return m_scrollView;
	}

	internal ItemsRepeater GetItemsRepeaterPart()
	{
		return m_itemsRepeater;
	}

	internal SelectionModel GetSelectionModel()
	{
		return m_selectionModel;
	}

#if false

	string DependencyPropertyToStringDbg(
		DependencyProperty dependencyProperty)
	{
		if (dependencyProperty == ItemsSourceProperty)
		{
			return "ItemsSource";
		}
		else if (dependencyProperty == ItemTemplateProperty)
		{
			return "ItemTemplate";
		}
		else if (dependencyProperty == LayoutProperty)
		{
			return "Layout";
		}
		else if (dependencyProperty == SelectionModeProperty)
		{
			return "SelectionMode";
		}
		else if (dependencyProperty == ScrollViewProperty)
		{
			return "ScrollView";
		}
		else
		{
			return "UNKNOWN";
		}
	}

#endif
}
