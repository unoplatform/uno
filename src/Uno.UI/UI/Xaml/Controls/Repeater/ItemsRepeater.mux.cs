// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeater.cpp, commit 4b206bce3

#pragma warning disable 105 // remove when moving to WinUI tree

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsRepeater
{
	public ItemsRepeater()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_ItemsRepeater);

		// Uno specific: ItemsRepeater hosts children in a managed UIElementCollection
		// so that Uno's IPanel contract can be satisfied.
		_repeaterChildren = new UIElementCollection(this);
		m_transitionManager = new TransitionManager(this);
		m_viewManager = new ViewManager(this);
		//if (SharedHelpers.IsRS5OrHigher())
		{
			m_viewportManager = new ViewportManagerWithPlatformFeatures(this);
		}
		//else
		//{
		//	m_viewportManager = std.new ViewportManagerDownLevel(this);
		//}

		AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
		TabFocusNavigation = KeyboardNavigationMode.Once;
		XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		LayoutUpdated += OnLayoutUpdated;
	}

	// #pragma region IUIElementOverrides

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new RepeaterAutomationPeer(this);
	}

	// #pragma endregion

	// #pragma region IUIElementOverrides7

	protected override IEnumerable<DependencyObject> GetChildrenInTabFocusOrder()
	{
		return CreateChildrenInTabFocusOrderIterable();
	}

	// #pragma endregion

	// #pragma region IUIElementOverrides8

	protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs e)
	{
		m_viewportManager.OnBringIntoViewRequested(e);
	}

	// #pragma endregion

	// #pragma region IFrameworkElementOverrides

	protected override Size MeasureOverride(Size availableSize)
	{
		if (m_isLayoutInProgress)
		{
			throw new InvalidOperationException("Reentrancy detected during layout.");
		}

		if (IsProcessingCollectionChange)
		{
			throw new InvalidOperationException("Cannot run layout in the middle of a collection change.");
		}

		var layout = GetEffectiveLayout();

		if (layout != null)
		{
			var stackLayout = layout as StackLayout;

			if (stackLayout != null && ++m_stackLayoutMeasureCounter >= s_maxStackLayoutIterations)
			{
				REPEATER_TRACE_INFO("MeasureOverride shortcut - %d\n", m_stackLayoutMeasureCounter);
				// Shortcut the apparent layout cycle by returning the previous desired size.
				// This can occur when children have variable sizes that prevent the ItemsPresenter's desired size from settling.
				Rect layoutExtent = m_viewportManager.GetLayoutExtent();
				var localDesiredSize = new Size(layoutExtent.Width - layoutExtent.X, layoutExtent.Height - layoutExtent.Y);
				return localDesiredSize;
			}
		}

		m_viewportManager.OnOwnerMeasuring();

		m_isLayoutInProgress = true;
		using var layoutInProgress = Disposable.Create(() => m_isLayoutInProgress = false);

		m_viewManager.PrunePinnedElements();
		Rect extent = default;
		Size desiredSize = default;

		if (layout != null)
		{
			var layoutContext = GetLayoutContext();

			// Expensive operation, do it only in debug builds.
#if DEBUG
			var virtualContext = (VirtualizingLayoutContext)layoutContext;
			virtualContext.Indent = Indent();
#endif

			// Checking if we have an data template and it is empty
			if (m_isItemTemplateEmpty)
			{
				// Has no content, so we will draw nothing and request zero space
				extent = new Rect(m_layoutOrigin.X, m_layoutOrigin.Y, 0, 0);
			}
			else
			{
				desiredSize = layout.Measure(layoutContext, availableSize);
				extent = new Rect(m_layoutOrigin.X, m_layoutOrigin.Y, desiredSize.Width, desiredSize.Height);
			}

			// Clear var recycle candidate elements that have not been kept alive by layout - i.e layout did not
			// call GetElementAt(index).
			var children = Children;
			for (var i = 0; i < children.Count; ++i)
			{
				if (children[i] is not { } element)
				{
					continue;
				}

				var virtInfo = GetVirtualizationInfo(element);

				if (virtInfo.Owner == ElementOwner.Layout &&
					virtInfo.AutoRecycleCandidate &&
					!virtInfo.KeepAlive)
				{
					REPEATER_TRACE_INFO("AutoClear - %d \n", virtInfo.Index);
					ClearElementImpl(element);
				}
			}
		}

		m_viewportManager.SetLayoutExtent(extent);

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (m_isLayoutInProgress)
		{
			throw new InvalidOperationException("Reentrancy detected during layout.");
		}

		if (IsProcessingCollectionChange)
		{
			throw new InvalidOperationException("Cannot run layout in the middle of a collection change.");
		}

		m_isLayoutInProgress = true;
		using var layoutInProgress = Disposable.Create(() => m_isLayoutInProgress = false);

		Size arrangeSize = default;

		if (GetEffectiveLayout() is { } layout)
		{
			arrangeSize = layout.Arrange(GetLayoutContext(), finalSize);
		}

		// The view manager might clear elements during this call.
		// That's why we call it before arranging cleared elements
		// off screen.
		m_viewManager.OnOwnerArranged();

		var children = Children;
		for (var i = 0; i < children.Count; ++i)
		{
			if (children[i] is not { } element)
			{
				continue;
			}

			var virtInfo = GetVirtualizationInfo(element);
			virtInfo.KeepAlive = false;

			if (virtInfo.Owner == ElementOwner.ElementFactory ||
				virtInfo.Owner == ElementOwner.PinnedPool)
			{
				// Toss it away. And arrange it with size 0 so that XYFocus won't use it.
				element.Arrange(new Rect(
					ClearedElementsArrangePosition.X - (float)(element.DesiredSize.Width),
					ClearedElementsArrangePosition.Y - (float)(element.DesiredSize.Height),
					0.0,
					0.0));
			}
			else
			{
				var newBounds = CachedVisualTreeHelpers.GetLayoutSlot(element as FrameworkElement);

				if (virtInfo.ArrangeBounds != ItemsRepeater.InvalidRect &&
					newBounds != virtInfo.ArrangeBounds)
				{
					m_transitionManager.OnElementBoundsChanged(element, virtInfo.ArrangeBounds, newBounds);
				}

				virtInfo.ArrangeBounds = newBounds;
			}
		}

		m_viewportManager.OnOwnerArranged();
		m_transitionManager.OnOwnerArranged();

		return arrangeSize;
	}

	// #pragma endregion

	// #pragma region IRepeater interface.

	public ItemsSourceView ItemsSourceView => m_itemsSourceView;

	public int GetElementIndex(UIElement element)
	{
		return GetElementIndexImpl(element);
	}

	public UIElement TryGetElement(int index)
	{
		return GetElementFromIndexImpl(index);
	}

	// Pinning APIs
	internal void PinElement(UIElement element)
	{
		m_viewManager.UpdatePin(element, true /* addPin */);
	}

	internal void UnpinElement(UIElement element)
	{
		m_viewManager.UpdatePin(element, false /* addPin */);
	}

	public UIElement GetOrCreateElement(int index)
	{
		return GetOrCreateElementImpl(index);
	}

	// #pragma endregion

	public UIElement GetElementImpl(int index, bool forceCreate, bool suppressAutoRecycle)
	{
		var element = m_viewManager.GetElement(index, forceCreate, suppressAutoRecycle);
		return element;
	}

	public void ClearElementImpl(UIElement element)
	{
		// Clearing an element due to a collection change
		// is more strict in that pinned elements will be forcibly
		// unpinned and sent back to the view generator.
		bool isClearedDueToCollectionChange =
			IsProcessingCollectionChange &&
			(m_processingItemsSourceChange.Action == NotifyCollectionChangedAction.Remove ||
				m_processingItemsSourceChange.Action == NotifyCollectionChangedAction.Replace ||
				m_processingItemsSourceChange.Action == NotifyCollectionChangedAction.Reset);

		m_viewManager.ClearElement(element, isClearedDueToCollectionChange);
		m_viewportManager.OnElementCleared(element);
	}

	int GetElementIndexImpl(UIElement element)
	{
		// Verify that element is actually a child of this ItemsRepeater
		var parent = VisualTreeHelper.GetParent(element);
		if (parent == this)
		{
			var virtInfo = TryGetVirtualizationInfo(element);
			return m_viewManager.GetElementIndex(virtInfo);
		}

		return -1;
	}

	UIElement GetElementFromIndexImpl(int index)
	{
		UIElement result = null;

		var children = Children;
		for (int i = 0; i < children.Count && result == null; ++i)
		{
			if (children[i] is not { } element)
			{
				continue;
			}

			var virtInfo = TryGetVirtualizationInfo(element);
			if (virtInfo != null && virtInfo.IsRealized && virtInfo.Index == index)
			{
				result = element;
			}
		}

		return result;
	}

	UIElement GetOrCreateElementImpl(int index)
	{
		if (ItemsSourceView == null)
		{
			throw new InvalidOperationException("ItemSource doesn't have a value");
		}

		if (index >= 0 && index >= ItemsSourceView.Count)
		{
			throw new ArgumentException(nameof(index), "Argument index is invalid.");
		}

		if (m_isLayoutInProgress)
		{
			throw new InvalidOperationException("GetOrCreateElement invocation is not allowed during layout.");
		}

		var element = GetElementFromIndexImpl(index);
		bool isAnchorOutsideRealizedRange = element == null;

		if (isAnchorOutsideRealizedRange)
		{
			if (GetEffectiveLayout() == null)
			{
				throw new InvalidOperationException("Cannot make an Anchor when there is no attached layout.");
			}

			element = GetLayoutContext().GetOrCreateElementAt(index);
			element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		}

		m_viewportManager.OnMakeAnchor(element, isAnchorOutsideRealizedRange);
		InvalidateMeasure();

		return element;
	}

	/*static*/
	internal static VirtualizationInfo TryGetVirtualizationInfo(UIElement element)
	{
		var value = element.GetValue(VirtualizationInfoProperty);
		return (VirtualizationInfo)value;
	}

	/*static*/
	internal static VirtualizationInfo GetVirtualizationInfo(UIElement element)
	{
		var result = TryGetVirtualizationInfo(element);

		if (result == null)
		{
			result = CreateAndInitializeVirtualizationInfo(element);
		}

		return result;
	}

	/* static */
	internal static VirtualizationInfo CreateAndInitializeVirtualizationInfo(UIElement element)
	{
		global::System.Diagnostics.Debug.Assert(TryGetVirtualizationInfo(element) == null);
		var result = new VirtualizationInfo();
		element.SetValue(VirtualizationInfoProperty, result);
		return result;
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		if (property == ItemsSourceProperty)
		{
			if (args.NewValue != args.OldValue)
			{
				var newValue = args.NewValue;
				var newDataSource = newValue as ItemsSourceView;
				if (newValue != null && newDataSource == null)
				{
					newDataSource = new InspectingDataSource(newValue);
				}

				OnDataSourcePropertyChanged(m_itemsSourceView, newDataSource);
			}
		}
		else if (property == ItemTemplateProperty)
		{
			OnItemTemplateChanged(args.OldValue, args.NewValue);
		}
		else if (property == LayoutProperty)
		{
			OnLayoutChanged(args.OldValue as Layout, args.NewValue as Layout);
		}
		else if (property == ItemTransitionProviderProperty)
		{
			OnTransitionProviderChanged(args.OldValue as ItemCollectionTransitionProvider, args.NewValue as ItemCollectionTransitionProvider);
		}
		else if (property == HorizontalCacheLengthProperty)
		{
			m_viewportManager.HorizontalCacheLength = (double)args.NewValue;
		}
		else if (property == VerticalCacheLengthProperty)
		{
			m_viewportManager.VerticalCacheLength = (double)args.NewValue;
		}
	}

	internal void OnElementPrepared(UIElement element, int index)
	{
		m_viewportManager.OnElementPrepared(element);
		var m_elementPreparedEventSource = ElementPrepared;
		if (m_elementPreparedEventSource != null)
		{
			if (m_elementPreparedArgs == null)
			{
				m_elementPreparedArgs = new ItemsRepeaterElementPreparedEventArgs(element, index);
			}
			else
			{
				m_elementPreparedArgs.Update(element, index);
			}

			m_elementPreparedEventSource(this, m_elementPreparedArgs);
		}
	}

	internal void OnElementClearing(UIElement element)
	{
		var m_elementClearingEventSource = ElementClearing;
		if (m_elementClearingEventSource != null)
		{
			if (m_elementClearingArgs == null)
			{
				m_elementClearingArgs = new ItemsRepeaterElementClearingEventArgs(element);
			}
			else
			{
				m_elementClearingArgs.Update(element);
			}

			m_elementClearingEventSource(this, m_elementClearingArgs);
		}
	}

	internal void OnElementIndexChanged(UIElement element, int oldIndex, int newIndex)
	{
		var m_elementIndexChangedEventSource = ElementIndexChanged;
		if (m_elementIndexChangedEventSource != null)
		{
			if (m_elementIndexChangedArgs == null)
			{
				m_elementIndexChangedArgs = new ItemsRepeaterElementIndexChangedEventArgs(element, oldIndex, newIndex);
			}
			else
			{
				m_elementIndexChangedArgs.Update(element, oldIndex, newIndex);
			}

			m_elementIndexChangedEventSource(this, m_elementIndexChangedArgs);
		}
	}

	// Provides an indentation based on repeater elements in the UI Tree that
	// can be used to make logging a little easier to read.
	internal int Indent()
	{
		int indent = 1;

		// Expensive, so we do it only in debug builds.
#if DEBUG
		var parent = this.Parent as FrameworkElement;
		while (parent != null && !(parent is ItemsRepeater))
		{
			parent = parent.Parent as FrameworkElement;
		}

		if (parent is ItemsRepeater parentRepeater)
		{
			indent = parentRepeater.Indent();
		}
#endif

		return indent * 4;
	}

	void OnLoaded(object sender, RoutedEventArgs args)
	{
#if !UNO_HAS_ENHANCED_LIFECYCLE
		// On native Uno targets we always detach from the scroller on unload, so we force a
		// layouting pass to re-subscribe to scroller and update viewport (in a single pass).
#else
		// If we skipped an unload event, reset the scrollers now and invalidate measure so that we get a new
		// layout pass during which we will hookup new scrollers.
		// The potential cache buffer is also reset so that the realization window regrows from scratch.
		if (m_loadedCounter > m_unloadedCounter)
#endif
		{
			InvalidateMeasure();
			m_viewportManager.ResetScrollers();
			m_viewportManager.ResetLayoutRealizationWindowCacheBuffer();
		}

		++m_loadedCounter;

#if HAS_UNO
		OnLoadedUno();
#endif
	}

	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		m_stackLayoutMeasureCounter = 0u;
		++m_unloadedCounter;

#if !UNO_HAS_ENHANCED_LIFECYCLE
		// Uno specific on native targets: always reset; we don't validate the count in the loaded.
#else
		// Only reset the scrollers if this unload event is in-sync.
		// The potential cache buffer is also reset so that the realization window regrows from scratch.
		if (m_unloadedCounter == m_loadedCounter)
#endif
		{
			m_viewportManager.ResetScrollers();
			m_viewportManager.ResetLayoutRealizationWindowCacheBuffer();
		}

#if HAS_UNO
		OnUnloadedUno();
#endif
	}

	private void OnLayoutUpdated(object sender, object e)
	{
		// Now that the layout has settled, reset the measure counter to detect the next potential StackLayout layout cycle.
		m_stackLayoutMeasureCounter = 0u;

		EnsureDefaultLayoutState();
	}

	private void OnDataSourcePropertyChanged(ItemsSourceView oldValue, ItemsSourceView newValue)
	{
		if (m_isLayoutInProgress)
		{
			throw new InvalidOperationException("Cannot set ItemsSourceView during layout.");
		}

		EnsureDefaultLayoutState();

#if HAS_UNO
		// Uno specific: tear down the previous data-source subscription before replacing the view
		// so the revoker is in a clean state before we re-subscribe.
		if (oldValue != null)
		{
			_dataSourceSubscriptionsRevoker.Disposable = null;
		}
#endif

		m_itemsSourceView = newValue;

		if (newValue != null)
		{
			newValue.CollectionChanged += OnItemsSourceViewChanged;
#if HAS_UNO
			_dataSourceSubscriptionsRevoker.Disposable = Disposable.Create(() =>
			{
				newValue.CollectionChanged -= OnItemsSourceViewChanged;
			});
#endif
		}

		if (GetEffectiveLayout() is { } layout)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			using var processingChange = Disposable.Create(() => m_processingItemsSourceChange = null);
			m_processingItemsSourceChange = args;

			if (layout is VirtualizingLayout virtualLayout)
			{
				// After a data source change, reset the potential cache buffer so that the realization window regrows from scratch.
				m_viewportManager.ResetLayoutRealizationWindowCacheBuffer();

				virtualLayout.OnItemsChangedCore(GetLayoutContext(), newValue, args);
			}
			else if (layout is NonVirtualizingLayout nonVirtualLayout)
			{
				// Walk through all the elements and make sure they are cleared for
				// non-virtualizing layouts.
				{
					// As we will be clearing up the elements from the parent at the end
					// we will avoid owners be assigned to items while recycling so that
					// there won't be crashes later when items are retrieved from recycle pool
					m_viewManager.RecycleWithoutOwner(true);
					using var processingClear = Disposable.Create(() => m_viewManager.RecycleWithoutOwner(false));
					foreach (var element in Children)
					{
						if (GetVirtualizationInfo(element).IsRealized)
						{
							ClearElementImpl(element);
						}
					}
				}

				Children.Clear();
			}

			InvalidateMeasure();
		}
	}

	void OnItemTemplateChanged(object oldValue, object newValue)
	{
#if HAS_UNO
		if (Uno.UI.TemplateManager.IsDataTemplateDynamicUpdateEnabled)
		{
			// ARCHITECTURE NOTE: Dynamic template update handling
			// This delegates to ItemsRepeater.Templates.cs (partial file) which contains
			// intentionally duplicated wrapper update logic to bypass layout constraints.
			// See ItemsRepeater.Templates.cs header for detailed architecture explanation.
			if (HandleDynamicTemplateUpdate(oldValue, newValue))
			{
				return;
			}
		}
#endif

		if (m_isLayoutInProgress && oldValue != null)
		{
			throw new InvalidOperationException("ItemTemplate cannot be changed during layout.");
		}

		EnsureDefaultLayoutState();

		// Since the ItemTemplate has changed, we need to re-evaluate all the items that
		// have already been created and are now in the tree. The easiest way to do that
		// would be to do a reset.. Note that this has to be done before we change the template
		// so that the cleared elements go back into the old template.
		if (GetEffectiveLayout() is { } layout)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			using var processingChange = Disposable.Create(() => m_processingItemsSourceChange = null);
			m_processingItemsSourceChange = args;

			if (layout is VirtualizingLayout virtualLayout)
			{
				virtualLayout.OnItemsChangedCore(GetLayoutContext(), newValue, args);
			}
			else if (layout is NonVirtualizingLayout nonVirtualLayout)
			{
				// Walk through all the elements and make sure they are cleared for
				// non-virtualizing layouts.
				foreach (var child in Children)
				{
					if (GetVirtualizationInfo(child).IsRealized)
					{
						ClearElementImpl(child);
					}
				}
			}
		}

#if HAS_UNO
		if (!SharedHelpers.IsRS5OrHigher())
		{
			// Bug in framework's reference tracking causes crash during
			// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
			m_itemTemplate = newValue as IElementFactory; // DataTemplate of DataTemplateSelector
		}
#endif

		// Clear flag for bug #776
		m_isItemTemplateEmpty = false;
		m_itemTemplateWrapper = newValue as IElementFactoryShim;
		if (m_itemTemplateWrapper == null)
		{
			// ItemTemplate set does not implement IElementFactoryShim. We also
			// want to support DataTemplate and DataTemplateSelectors automagically.
			if (newValue is DataTemplate dataTemplate) // Implements IElementFactory but not IElementFactoryShim
			{
				m_itemTemplateWrapper = new ItemTemplateWrapper(dataTemplate);
				if (dataTemplate.LoadContent() is FrameworkElement content)
				{
					// Due to bug https://github.com/microsoft/microsoft-ui-xaml/issues/3057, we need to get the framework
					// to take ownership of the extra implicit ref that was returned by LoadContent. The simplest way to do
					// this is to add it to a Children collection and immediately remove it.
					var children = Children;
					children.Add(content);
					children.RemoveAt(children.Count - 1);
				}
				else
				{
					// We have a DataTemplate which is empty, so we need to set it to true
					m_isItemTemplateEmpty = true;
				}
			}
			else if (newValue is DataTemplateSelector selector) // Implements IElementFactory but not IElementFactoryShim
			{
				m_itemTemplateWrapper = new ItemTemplateWrapper(selector);
			}
			else
			{
				throw new ArgumentException("ItemTemplate", "ItemTemplate");
			}
		}

		InvalidateMeasure();
	}

	void OnLayoutChanged(Layout oldValue, Layout newValue)
	{
		bool isInitialSetup = !m_wasLayoutChangedCalled;
		m_wasLayoutChangedCalled = true;

		if (m_isLayoutInProgress)
		{
			throw new InvalidOperationException("Layout cannot be changed during layout.");
		}

		m_viewManager.OnLayoutChanging();
		m_transitionManager.OnLayoutChanging();

		if (oldValue == null && !isInitialSetup)
		{
			oldValue = GetDefaultLayout();
		}
		if (newValue == null)
		{
			newValue = GetDefaultLayout();
		}

		if (oldValue != null)
		{
			oldValue.UninitializeForContext(GetLayoutContext());
#if HAS_UNO
			_layoutSubscriptionsRevoker.Disposable = null;
#endif
			m_stackLayoutMeasureCounter = 0u;

			// Walk through all the elements and make sure they are cleared
			var children = Children;
			for (int i = 0; i < children.Count; ++i)
			{
				if (children[i] is not { } element)
				{
					continue;
				}

				if (GetVirtualizationInfo(element).IsRealized)
				{
					ClearElementImpl(element);
				}
			}

			m_layoutState = null;
		}

#if HAS_UNO
		if (!SharedHelpers.IsRS5OrHigher())
		{
			// Bug in framework's reference tracking causes crash during
			// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
			m_layout = newValue;
		}
#endif

		if (newValue != null)
		{
			// Uno specific: use the weak-reference RegisterMeasureInvalidated / RegisterArrangeInvalidated
			// helpers so that the long-lived default StackLayout (thread-local singleton) does not keep
			// ItemsRepeater instances alive. _layoutSubscriptionsRevoker is reset before resubscribing.
			_layoutSubscriptionsRevoker.Disposable = null;

			newValue.InitializeForContext(GetLayoutContext());

			var disposables = new CompositeDisposable();
			newValue.RegisterMeasureInvalidated(InvalidateMeasureForLayout).DisposeWith(disposables);
			newValue.RegisterArrangeInvalidated(InvalidateArrangeForLayout).DisposeWith(disposables);
			_layoutSubscriptionsRevoker.Disposable = disposables;

			if (m_ownsTransitionProvider)
			{
				m_transitionManager.OnTransitionProviderChanged(newValue.CreateDefaultItemTransitionProvider());
			}
		}

		bool isVirtualizingLayout = newValue is VirtualizingLayout;
		m_viewportManager.OnLayoutChanged(isVirtualizingLayout);
		InvalidateMeasure();
	}

	void OnTransitionProviderChanged(ItemCollectionTransitionProvider oldValue, ItemCollectionTransitionProvider newValue)
	{
		m_ownsTransitionProvider = false;
		m_transitionManager.OnTransitionProviderChanged(newValue);
	}

	void OnItemsSourceViewChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		if (m_isLayoutInProgress)
		{
			// Bad things will follow if the data changes while we are in the middle of a layout pass.
			throw new InvalidOperationException("Changes in data source are not allowed during layout.");
		}

#if HAS_UNO
		if (args.Action == NotifyCollectionChangedAction.Move)
		{
			// As ItemsRepeater originally relied on VectorChange, internal structure does not support the Move action.
			// So we decompose it as early as possible.
			OnItemsSourceViewChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, args.OldItems, args.OldStartingIndex));
			OnItemsSourceViewChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, args.NewItems, args.NewStartingIndex));
			return;
		}
#endif

		if (IsProcessingCollectionChange)
		{
			throw new InvalidOperationException("Changes in the data source are not allowed during another change in the data source.");
		}

		m_processingItemsSourceChange = args;
		using var processingChange = Disposable.Create(() => m_processingItemsSourceChange = null);

		m_transitionManager.OnItemsSourceChanged(sender, args);
		m_viewManager.OnItemsSourceChanged(sender, args);

		if (GetEffectiveLayout() is { } layout)
		{
			if (layout is VirtualizingLayout virtualLayout)
			{
				virtualLayout.OnItemsChangedCore(GetLayoutContext(), sender, args);
			}
			else
			{
				// NonVirtualizingLayout
				InvalidateMeasure();
			}
		}
	}

	void InvalidateMeasureForLayout(Layout sender, object args)
	{
		if (UseLayoutRounding)
		{
			if (XamlRoot is { } xamlRoot)
			{
				double layoutRoundFactor = xamlRoot.RasterizationScale;

				if (layoutRoundFactor != m_layoutRoundFactor)
				{
					if (m_layoutRoundFactor != 0.0)
					{
						// Invoke InvalidateMeasure for all children owned by the layout so that they
						// get re-measured using the new global scale factor.
						// Otherwise they keep using their old DesiredSize based on the old factor
						// which may be slightly different.
						// This could have unwanted effects, like StackLayoutState::m_areElementsMeasuredRegular
						// being incorrectly set to False in the StackLayout case.
						InvalidateChildrenMeasure();
					}

					// ItemsRepeater has its own m_layoutRoundFactor field to avoid:
					//  - the need for a new public ItemsRepeater API,
					//  - the need for an internal ItemsRepeater/Layout communication.
					m_layoutRoundFactor = layoutRoundFactor;
				}
			}
			else
			{
				m_layoutRoundFactor = 0.0;
			}
		}
		else
		{
			m_layoutRoundFactor = 0.0;
		}

		InvalidateMeasure();
	}

	void InvalidateArrangeForLayout(Layout sender, object args)
	{
		InvalidateArrange();
	}

	// Invalidates all children owned by the layout.
	void InvalidateChildrenMeasure()
	{
		//ITEMSREPEATER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		var children = Children;
		var childrenCount = children.Count;

		for (var childIndex = 0; childIndex < childrenCount; childIndex++)
		{
			if (children[childIndex] is { } element)
			{
				if (GetVirtualizationInfo(element) is { } virtInfo)
				{
					if (virtInfo.Owner == ElementOwner.Layout)
					{
						element.InvalidateMeasure();
					}
				}
			}
		}
	}

	void EnsureDefaultLayoutState()
	{
		if (!m_wasLayoutChangedCalled)
		{
			// Initialize the cached layout to the default value
			// OnLayoutChanged has not been called yet for this ItemsRepeater.
			// This is the first call for the default VirtualizingLayout layout after the control's creation.
			var layout = GetEffectiveLayout() as VirtualizingLayout;
			OnLayoutChanged(null, layout);
		}
	}

	private VirtualizingLayoutContext GetLayoutContext()
	{
		if (m_layoutContext == null)
		{
			m_layoutContext = new RepeaterLayoutContext(this);
		}

		return m_layoutContext;
	}

	IEnumerable<DependencyObject> CreateChildrenInTabFocusOrderIterable()
	{
		var children = Children;
		if (children.Count > 0u)
		{
			return new ChildrenInTabFocusOrderIterable(this);
		}

		return null;
	}

	Layout GetEffectiveLayout()
	{
		if (Layout is { } layout)
		{
			return layout;
		}
		else
		{
			return GetDefaultLayout();
		}
	}

	Layout GetDefaultLayout()
	{
		// Default to StackLayout if the Layout property was not set.
		// We use ThreadStatic here to get a unique instance per thread, since Layout objects
		// are not sharable across different xaml threads.
		s_defaultLayout ??= new StackLayout();
		return s_defaultLayout;
	}

	[global::System.ThreadStatic]
	private static Layout s_defaultLayout;
}
