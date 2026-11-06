// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsControl_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Input;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsControl : IGeneratorHost
{
	private ICustomGeneratorItemsHost CustomItemsHost => ItemsPanelRoot as ICustomGeneratorItemsHost;

	private IItemContainerMapping ItemContainerMapping => CustomItemsHost?.GetItemContainerMapping();

	internal IItemContainerMapping GetItemContainerMapping() => ItemContainerMapping;

	// This code gets called from Automation Provider e.g. ItemAutomationPeer and must not be called from any
	// internal APIs in the control itself. It basically returns the Container for the DataItem in case it exist.
	internal virtual UIElement UIA_GetContainerForDataItemOverride(object pItem, int itemIndex)
	{
		UIElement ppContainer = null;
		bool isOwnContainer = false;

		if (pItem is not null)
		{
			isOwnContainer = IsItemItsOwnContainer(pItem);

			if (isOwnContainer)
			{
				ppContainer = pItem as UIElement;
			}
		}

		if (!isOwnContainer)
		{
			// TODO: once UIA is handling null items propertly by passing indices (bug 366772) call to UIA_GetContainerForDataItemOverride
			// should set the itemIndex parameter appropriately.  For now, to fix the drag and drop of null items scenario this
			// behavior is special cased and old code is executed (ContainerFromItem) by passing -1.
			DependencyObject spContainerAsDO = itemIndex != -1 ? ContainerFromIndex(itemIndex) : ContainerFromItem(pItem);

			ppContainer = spContainerAsDO as UIElement;
		}

		return ppContainer;
	}

	private protected IGroupHeaderMapping GetGroupHeaderMapping() => CustomItemsHost?.GetGroupHeaderMapping();

	private void InitializeItemContainerGenerator() => CustomItemsHost?.RegisterItemsHost(this);

	// Different panel types can have different ICG setups, so we'd better make sure we have the right one.
	private protected virtual void OnItemsHostAvailable() => InitializeItemContainerGenerator();

	// TODO Uno: the source is observed through INotifyCollectionChanged and has already changed here, so a
	// batched change can't be replayed one item at a time; it becomes a reset. WinUI's source raises one change per item.
	private void NotifyCustomItemsHost(ICustomGeneratorItemsHost host, NotifyCollectionChangedEventArgs args, int section)
	{
		if (args is null)
		{
			UpdateTickContextCounters();
			m_resetItemsThisLayoutTick = true;
			m_loadedTick = 0;
			host.Refresh();
			return;
		}

		int ToFlatIndex(int indexInSection) =>
			IsGrouping ? GetIndexFromIndexPath(Uno.UI.IndexPath.FromRowSection(indexInSection, section)) : indexInSection;

		void Notify(CollectionChange change, int index)
		{
			UpdateTickContextCounters();
			switch (change)
			{
				case CollectionChange.Reset:
					m_resetItemsThisLayoutTick = true;
					// load should no longer come into play:
					m_loadedTick = 0;
					break;
				case CollectionChange.ItemInserted:
					m_elementCountAddedThisLayoutTick += 1;
					break;
				case CollectionChange.ItemRemoved:
					m_elementCountRemovedThisLayoutTick += 1;
					break;
			}

			IVectorChangedEventArgs e = new VectorChangedEventArgs(change, (uint)index);
			host.NotifyOfItemsChanging(e);
			host.NotifyOfItemsChanged(e);
		}

		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add when args.NewItems is { Count: 1 }:
				Notify(CollectionChange.ItemInserted, ToFlatIndex(args.NewStartingIndex));
				break;
			case NotifyCollectionChangedAction.Remove when args.OldItems is { Count: 1 }:
				Notify(CollectionChange.ItemRemoved, ToFlatIndex(args.OldStartingIndex));
				break;
			case NotifyCollectionChangedAction.Replace when args.NewItems is { Count: 1 }:
				Notify(CollectionChange.ItemChanged, ToFlatIndex(args.NewStartingIndex));
				break;
			case NotifyCollectionChangedAction.Move when args.NewItems is { Count: 1 }:
				Notify(CollectionChange.ItemRemoved, ToFlatIndex(args.OldStartingIndex));
				Notify(CollectionChange.ItemInserted, ToFlatIndex(args.NewStartingIndex));
				break;
			default:
				Notify(CollectionChange.Reset, 0);
				break;
		}
	}

	#region IGeneratorHost
	IList<object> IGeneratorHost.View => Items;

	ICollectionView IGeneratorHost.CollectionView => UnwrapItemsSource() as ICollectionView;

	bool IGeneratorHost.IsItemItsOwnContainer(object item) => IsItemItsOwnContainerOverride(item);

	DependencyObject IGeneratorHost.GetContainerForItem(object item, DependencyObject recycledContainer)
	{
		DependencyObject container;

		if (IsItemItsOwnContainerOverride(item))
		{
			// Or, if the item is its own container, just use it as is.
			container = item as DependencyObject ?? throw new InvalidOperationException("The item must be a UIElement to be its own container.");
		}
		else
		{
			// Otherwise create a visual to render the item (a ContentPresenter by default)
			// or use the passed in recycledContainer.
			container = recycledContainer is UIElement ? recycledContainer : GetContainerForItemOverride();

			if (container is not UIElement containerAsUIE)
			{
				throw new InvalidOperationException("GetContainerForItemOverride must return a UIElement.");
			}

			// Record that this container was generated. We need this so we can skip un-setting the container's Style in
			// CleanupContainerForItem.
			containerAsUIE.IsGeneratedContainer = true;
		}

		EnsureContainerItemsControlProperty(container);

		return container;
	}

	void IGeneratorHost.PrepareItemContainer(DependencyObject container, object item)
	{
		var virtualizationInformation = (container as UIElement)?.GetVirtualizationInformationOrNull();

		PreProcessContentPreparation(container, item);

		if (virtualizationInformation is null || !virtualizationInformation.MaySkipPreparation)
		{
			_containerBeingPrepared = container;
			try
			{
				PrepareContainerForItemOverride(container, item);
			}
			finally
			{
				_containerBeingPrepared = null;
			}

			if (virtualizationInformation is not null)
			{
				virtualizationInformation.IsPrepared = true;
			}
		}
	}

	private protected virtual void PreProcessContentPreparation(DependencyObject container, object item)
	{
	}

	void IGeneratorHost.ClearContainerForItem(DependencyObject container, object item)
	{
		CleanUpContainer(container);

		if ((container as UIElement)?.GetVirtualizationInformationOrNull() is { } virtualizationInformation)
		{
			virtualizationInformation.IsPrepared = false;
		}
	}

	bool IGeneratorHost.IsHostForItemContainer(DependencyObject container) => IsHostForItemContainer(container);

	GroupStyle IGeneratorHost.GetGroupStyle(ICollectionViewGroup group, uint level)
	{
		// TODO Uno: GroupStyleSelector is not implemented.

		// b. lookup in GroupStyle list
		var groupStyles = GroupStyle;
		var count = (uint)(groupStyles?.Count ?? 0);

		// use last entry for all higher levels
		return count > 0 ? groupStyles[(int)Math.Min(count - 1, level)] : null;
	}

	void IGeneratorHost.SetIsGrouping(bool isGrouping) => IsGrouping = isGrouping;

	DependencyObject IGeneratorHost.GetHeaderForGroup(object group)
	{
		// we do not support passing in your own groups yet
		var container = GetGroupHeaderContainer(group);

		// Record that this container was generated. We need this so we can skip un-setting the container's Style in
		// CleanupContainerForItem.
		container.IsGeneratedContainer = true;

		return container;
	}

	void IGeneratorHost.PrepareGroupContainer(DependencyObject container, ICollectionViewGroup group) => PrepareGroupContainer(container, group);

	private protected virtual void PrepareGroupContainer(DependencyObject container, ICollectionViewGroup group)
	{
		var contentControl = (ContentControl)container;
		var groupStyle = ((IGeneratorHost)this).GetGroupStyle(group, 0);

		// ordering here is important. First set content, so that the right content is sent
		// to the selector.

		// content
		contentControl.Content = group.Group;

		// template
		contentControl.ContentTemplate = groupStyle?.HeaderTemplate;

		// template selector
		contentControl.ContentTemplateSelector = groupStyle?.HeaderTemplateSelector;

		// style
		if (contentControl.ReadLocalValue(StyleProperty) == DependencyProperty.UnsetValue || contentControl.IsStyleSetFromItemsControl)
		{
			// If we have been given a style to set, let's do it
			if (groupStyle?.HeaderContainerStyle is { } headerStyle)
			{
				contentControl.Style = headerStyle;
				contentControl.IsStyleSetFromItemsControl = true;
			}
			else
			{
				// If we don't have one, clear the property and let the default/implicit take over
				contentControl.ClearValue(StyleProperty);
				contentControl.IsStyleSetFromItemsControl = false;
			}
		}
	}

	void IGeneratorHost.ClearGroupContainerForGroup(DependencyObject container, ICollectionViewGroup group) => ClearGroupContainerForGroup(container, group);

	private protected virtual void ClearGroupContainerForGroup(DependencyObject container, ICollectionViewGroup group)
	{
	}

	void IGeneratorHost.SetupContainerContentChangingAfterPrepare(DependencyObject container, object item, int itemIndex, Size measureSize)
		=> SetupContainerContentChangingAfterPrepare(container, item, itemIndex, measureSize);

	private protected virtual void SetupContainerContentChangingAfterPrepare(DependencyObject container, object item, int itemIndex, Size measureSize)
		=> ContainerPreparedForItem(item, container as SelectorItem, itemIndex);

	void IGeneratorHost.RegisterWorkFromArgs(ContainerContentChangingEventArgs args) => RegisterWorkFromArgs(args);

	private protected virtual void RegisterWorkFromArgs(ContainerContentChangingEventArgs args)
	{
	}

	void IGeneratorHost.RegisterWorkForContainer(UIElement container) => RegisterWorkForContainer(container);

	private protected virtual void RegisterWorkForContainer(UIElement container)
	{
	}

	// For certain situations (such as drag/drop), the host may hang onto a container for an extended
	// period of time. That particular container shouldn't ever be recycled as long as it's being used.
	// This method asks whether or not the given container is eligible for recycling.
	bool IGeneratorHost.CanRecycleContainer(DependencyObject container) => CanRecycleContainer(container);

	private protected virtual bool CanRecycleContainer(DependencyObject container) => true;

	// itemscontrol has no clue
	DependencyObject IGeneratorHost.SuggestContainerForContainerFromItemLookup() => SuggestContainerForContainerFromItemLookup();

	private protected virtual DependencyObject SuggestContainerForContainerFromItemLookup() => null;

	bool IGeneratorHost.ShouldRaiseChoosingItemContainer() => ShouldRaiseChoosingItemContainer();

	private protected virtual bool ShouldRaiseChoosingItemContainer() => false;

	void IGeneratorHost.RaiseChoosingItemContainer(ChoosingItemContainerEventArgs args) => RaiseChoosingItemContainer(args);

	private protected virtual void RaiseChoosingItemContainer(ChoosingItemContainerEventArgs args)
	{
	}

	void IGeneratorHost.RaiseContainerContentChangingOnRecycle(UIElement container, object item) => RaiseContainerContentChangingOnRecycle(container, item);

	private protected virtual void RaiseContainerContentChangingOnRecycle(UIElement container, object item)
	{
	}

	bool IGeneratorHost.ShouldRaiseChoosingGroupHeaderContainer() => ShouldRaiseChoosingGroupHeaderContainer();

	private protected virtual bool ShouldRaiseChoosingGroupHeaderContainer() => false;

	void IGeneratorHost.RaiseChoosingGroupHeaderContainer(ChoosingGroupHeaderContainerEventArgs args) => RaiseChoosingGroupHeaderContainer(args);

	private protected virtual void RaiseChoosingGroupHeaderContainer(ChoosingGroupHeaderContainerEventArgs args)
	{
	}

	void IGeneratorHost.VirtualizationFinished() => VirtualizationFinished();

	private protected virtual void VirtualizationFinished()
	{
	}

	Rect IGeneratorHost.OverrideContainerArrangeBounds(int index, Rect suggestedBounds) => OverrideContainerArrangeBounds(index, suggestedBounds);

	private protected virtual Rect OverrideContainerArrangeBounds(int index, Rect suggestedBounds) => suggestedBounds;
	#endregion

	// Compute the TransitionContext
	private ThemeTransitionContext GetCurrentTransitionContext()
	{
		ThemeTransitionContext themeTransitionContext = ThemeTransitionContext.None;

		if (m_resetItemsThisLayoutTick)
		{
			// if content is changed, the colelction is reset or dataSource is changed, this animation runs
			themeTransitionContext = ThemeTransitionContext.ContentTransition;
		}
		// Reorder Ticks checks are above add/remove because if there is reorder, it will update the ItemsSource
		// which eventually updates m_elementCountAddedThisLayoutTick/m_elementCountRemovedThisLayoutTick
		else if (m_elementCountReorderedThisLayoutTick == 1)
		{
			// if single reorder happens
			themeTransitionContext = ThemeTransitionContext.SingleReorderList;
		}
		else if (m_elementCountReorderedThisLayoutTick > 1)
		{
			// if multiple reorder happens
			themeTransitionContext = ThemeTransitionContext.MultipleReorderList;
		}
		else if (m_elementCountAddedThisLayoutTick == 1 && m_elementCountRemovedThisLayoutTick == 0)
		{
			// if only 1 item gets added or removed
			themeTransitionContext = ThemeTransitionContext.SingleAddList;
		}
		else if (m_elementCountAddedThisLayoutTick == 0 && m_elementCountRemovedThisLayoutTick == 1)
		{
			themeTransitionContext = ThemeTransitionContext.SingleDeleteList;
		}
		else if (m_elementCountAddedThisLayoutTick > 0)
		{
			themeTransitionContext = ThemeTransitionContext.MultipleAddList;
		}
		else if (m_elementCountAddedThisLayoutTick == 0 && m_elementCountRemovedThisLayoutTick > 0)
		{
			themeTransitionContext = ThemeTransitionContext.MultipleDeleteList;
		}

		return themeTransitionContext;
	}

	internal virtual ThemeTransitionContext GetCurrentTransitionContext(int layoutTickId)
	{
		ThemeTransitionContext themeTransitionContext = ThemeTransitionContext.None;

		// If new ID, just use currently calculated TransitionContext
		if (layoutTickId != m_previousTickCounterId)
		{
			UpdateTickContextCounters();

			// loaded is an async event, so the tick might not come in until later
			if (m_loadedTick == loadCounterToken)
			{
				m_loadedTick = layoutTickId;
			}

			// If layoutTick is same as the one during Load, call EntraceTransition
			if (m_loadedTick == layoutTickId)
			{
				// Entrance Transition should happen only on First time, this animation runs only first time
				themeTransitionContext = ThemeTransitionContext.Entrance;
			}
			else
			{
				themeTransitionContext = GetCurrentTransitionContext();
			}
			m_themeTransitionContext = themeTransitionContext;
		}

		// Store current id to use in the next cycle
		m_previousTickCounterId = layoutTickId;
		return m_themeTransitionContext;
	}

	// Any context related operation must be guarded with this call.
	// It will reset all context counters if we get new tick.
	// If the tick haven't changed then we keep increasing operation counters
	private void UpdateTickContextCounters()
	{
		short currentTickCounterId = CoreImports.LayoutManager_GetLayoutTickForTransition(this);

		// as we store one tick less from current we should adjust comparison.
		if (currentTickCounterId - 1 != m_previousTickCounterId)
		{
			m_elementCountAddedThisLayoutTick = 0;
			m_elementCountRemovedThisLayoutTick = 0;
			m_elementCountReorderedThisLayoutTick = 0;
			m_resetItemsThisLayoutTick = false;
		}

		// We should not store current id before we get GetCurrentTransitionContext call.
		// Otherwise we won't re-calculate a transition context.
		m_previousTickCounterId = currentTickCounterId - 1;
	}

	internal virtual Point GetDropOffsetToRoot() => default;

	// callback from ReorderItems operation. We use it to update reorder counts for transitions context.
	internal void OnItemsReordered(uint nCount)
	{
		UpdateTickContextCounters();
		m_elementCountReorderedThisLayoutTick += (int)nCount;

		if (ItemsPanelRoot is ICustomGeneratorItemsHost customItemsHost)
		{
			customItemsHost.NotifyOfItemsReordered(nCount);
		}
	}

	// indicates that we should defer setting the content
	private protected virtual bool IsPrepareContainerForItemDeferred(DependencyObject element) => false;

	// Helper to find out GroupItemIndex from GroupItem
	private protected void GetGroupItemIndex(object groupItem, out uint pGroupItemIndex, out bool pFound)
	{
		pFound = false;
		pGroupItemIndex = 0;

		// If groupItem is nullptr, return found= false
		if (groupItem is null)
		{
			return;
		}

		if (IsGrouping)
		{
			IList<object> spView = null;

			// TODO Uno: there is no legacy generator view, so the collection view groups serve both cases.
			if (CustomItemsHost is not null)
			{
				// If we have a modern panel, there are no nested ItemsControls, so we can get our collection view directly
				// We are probably going through a few extra steps to match ICG's IVector<IInspectable> view, but
				// I'd rather not risk breaking backwards compatibility on old apps
				if (UnwrapItemsSource() is ICollectionView spCollectionView)
				{
					spView = spCollectionView.CollectionGroups;
				}
			}
			else
			{
				spView = CollectionGroups;
			}

			if (spView is not null)
			{
				// Go through all CollectionViewGroups, get Group from it and check whether it is given group or not
				uint numGroups = (uint)spView.Count;
				for (uint index = 0; index < numGroups; ++index)
				{
					var spGroup = (ICollectionViewGroup)spView[(int)index];
					if (Equals(spGroup.Group, groupItem))
					{
						pFound = true;
						pGroupItemIndex = index;
						break;
					}
				}
			}
		}
	}

	public DependencyObject GroupHeaderContainerFromItemContainer(DependencyObject itemContainer)
	{
		// ICG doesn't implement IGHM interface, so we need to manually dispatch to the right handler
		// TODO Uno: nothing to hook up here, the Items collection always exists.
		var groupMapping = GetGroupHeaderMapping() ?? throw new InvalidOperationException("The items panel does not map group headers.");
		return groupMapping.GroupHeaderContainerFromItemContainer(itemContainer);
	}

	internal object GroupFromHeader(DependencyObject header)
	{
		var groupMapping = GetGroupHeaderMapping() ?? throw new InvalidOperationException("The items panel does not map group headers.");
		return groupMapping.GroupFromHeader(header);
	}

	internal DependencyObject HeaderFromGroup(object group)
	{
		var groupMapping = GetGroupHeaderMapping() ?? throw new InvalidOperationException("The items panel does not map group headers.");
		return groupMapping.HeaderFromGroup(group);
	}

	internal int IndexFromHeader(DependencyObject header, bool excludeHiddenEmptyGroups)
	{
		var groupMapping = GetGroupHeaderMapping() ?? throw new InvalidOperationException("The items panel does not map group headers.");
		return groupMapping.IndexFromHeader(header, excludeHiddenEmptyGroups);
	}

	internal DependencyObject HeaderFromIndex(int index)
	{
		var groupMapping = GetGroupHeaderMapping();
		if (groupMapping is not null)
		{
			return groupMapping.HeaderFromIndex(index);
		}

		// TODO Uno: the legacy ItemContainerGenerator.ContainerFromGroupIndex path is not ported.
		return null;
	}

	#region IContainerRecyclingContext
	private DataTemplateSelectorRecyclingContext DataSelectorRecyclingContext => m_dataSelectorRecyclingContext ??= new(this);

	void IContainerRecyclingContext.PrepareForItemRecycling(object item) => DataSelectorRecyclingContext.PrepareForItemRecycling(item);

	bool IContainerRecyclingContext.IsCompatible(UIElement candidate) => DataSelectorRecyclingContext.IsCompatible(candidate);

	void IContainerRecyclingContext.StopRecycling() => DataSelectorRecyclingContext.StopRecycling();

	UIElement IContainerRecyclingContext.SelectedContainer
	{
		get => DataSelectorRecyclingContext.SelectedContainer;
		set => DataSelectorRecyclingContext.SelectedContainer = value;
	}

	void IContainerRecyclingContext.ConfigureSelectedContainer(UIElement container) => DataSelectorRecyclingContext.ConfigureSelectedContainer(container);

	internal IContainerRecyclingContext GetRecyclingContext()
	{
		IContainerRecyclingContext ppReturnValue = null;

		// Return ItemsControl's IContainerRecyclingContext interface only if it has a ItemTemplateSelector implementing IDataTemplateSelector and NO ItemTemplate
		if (ItemTemplate is null)
		{
			var spDataTemplateSelector = ItemTemplateSelector;
			if (spDataTemplateSelector is not null)
			{
				DataSelectorRecyclingContext.SetDataTemplateSelector(spDataTemplateSelector);
				if (spDataTemplateSelector is not null)
				{
					ppReturnValue = this;
				}
				else
				{
					ppReturnValue = null;
				}
			}
		}

		return ppReturnValue;
	}

	// This helper class implements the selection of a recycling candidate based on its SelectedDataTemplate
	// As of today, ItemsControl simply delegates the corresponding calls to its unique
	// DataTemplateSelectorRecyclingContext member
	// (We are using a separate class as the lifecycle of private fields is the preparation of a single
	//  item and hence different from the ItemsControl itself. Anyway, as item's preparations are run
	//  on a single thread, we do not need to create separate instances and prefer to avoid the associated
	//  performance cost)
	// A recycling cycle is started with PrepareForItemRecycling and terminated with StopRecycling
	// (or the preparation of another item)
	private sealed class DataTemplateSelectorRecyclingContext
	{
		// Lifetime of DataTemplateSelectorRecyclingContext is identical to its owner
		private readonly ItemsControl m_pOwner;
		private DataTemplateSelector m_tpSelector;
		private UIElement m_tpSelectedContainer;
		private DataTemplate m_tpSelectedTemplate;
		private object m_tpDataItem;

		public DataTemplateSelectorRecyclingContext(ItemsControl pOwner) => m_pOwner = pOwner;

		public void SetDataTemplateSelector(DataTemplateSelector pSelector) => m_tpSelector = pSelector;

		public UIElement SelectedContainer
		{
			get => m_tpSelectedContainer;
			// Store the selection
			set => m_tpSelectedContainer = value;
		}

		// Store the selected data template in the VirtualizationInformation associated
		// to the UIElement in order to avoid calling another time DataTemplateSelector::SelectTemplate
		public void ConfigureSelectedContainer(UIElement container)
		{
			// Make sure that the UIEmement has a virtualization information associated
			container.InitVirtualizationInformation();

			// And get the corresponding pointer
			// if InitVirtualizationInformation succeeded, we are ensured that GetVirtualizationInformation
			// will not return NULL
			var pVirtualizationInformation = container.GetVirtualizationInformation();

			// m_tpSelectedTemplate should not be NULL but it is not a problem if it was:
			// it would simply mean that ContentControl will call DataTemplateSelector::SelectTemplate
			// which would be fine in this situation
			pVirtualizationInformation.SelectedTemplate = m_tpSelectedTemplate;
		}

		public void PrepareForItemRecycling(object item)
		{
			m_tpDataItem = item;
			m_tpSelectedContainer = null;
			m_tpSelectedTemplate = null;

			// We should never be called without a TemplateSelector being set but we check anyway
			// As the rest of the code is already resilient to the absence of a selected template
			if (m_tpSelector is not null)
			{
				m_tpSelectedTemplate = m_tpSelector.SelectTemplate(item);
			}
		}

		public void StopRecycling()
		{
			m_tpDataItem = null;
			m_tpSelectedContainer = null;
			m_tpSelectedTemplate = null;
		}

		public bool IsCompatible(UIElement candidate)
		{
			bool returnValue = true;

			// If we don't have a selectedTemplate, all containers are considered as "compatible"
			if (m_tpSelectedTemplate is not null)
			{
				var pVirtualizationInformation = candidate.GetVirtualizationInformationOrNull();
				if (pVirtualizationInformation is null)
				{
#if !HAS_UNO // TODO Uno: ContentControl has no SelectedContentTemplate, so every candidate is compatible.
					// The container has never been virtualized : we fallback asking the container for its SelectedDataTemplate
					var spContentControl = (ContentControl)candidate;
					var spSelectedDataTemplate = spContentControl.SelectedContentTemplate;
					returnValue = (spSelectedDataTemplate == m_tpSelectedTemplate);
#endif
				}
				else
				{
					returnValue = (pVirtualizationInformation.SelectedTemplate == m_tpSelectedTemplate);
				}
			}

			return returnValue;
		}
	}
	#endregion

#if !HAS_UNO // TODO Uno: x:Load deferral of items is not supported (no DeferredElementStateChange).
	private void NotifyDeferredElementStateChanged(
		KnownPropertyIndex propertyIndex,
		DeferredElementStateChange state,
		uint collectionIndex,
		DependencyObject realizedElement)
	{
		if (propertyIndex == KnownPropertyIndex.ItemsControl_Items)
		{
			object realizedElementAsIInspectable = realizedElement;

			switch (state)
			{
				case DeferredElementStateChange.Realized:
					m_tpItems.Insert((int)collectionIndex, realizedElementAsIInspectable);
					break;

				case DeferredElementStateChange.Deferred:
					{
						int index = m_tpItems.IndexOf(realizedElementAsIInspectable);

						if (index >= 0)
						{
							m_tpItems.RemoveAt(index);
						}
						// if not found, it's ok.  It might have been removed programatically.
					}
					break;

				default:
					Debug.Assert(false);
					break;
			}
		}
		else
		{
			// Should not be calling framework for any other properties
			Debug.Assert(false);
		}
	}
#endif

	// Gets first element that should take focus while processing a Tab keystroke.
	// Return nullptr to default to FocusManager logic.
	internal override DependencyObject GetFirstFocusableElementOverride()
	{
		bool usesDefaultControlTemplate = UsesDefaultControlTemplate();

		if (!usesDefaultControlTemplate)
		{
			// Prevent any custom handling from controls like the Pivot which has a more complex tree structure.
			return null;
		}

		return GetFirstOrLastFocusableElement(
			true  /*isForFirstFocusableElement*/,
			false /*isBackward*/,
			false /*skipHeader*/,
			false /*skipItemsAndGroupHeaders*/,
			false /*skipFooter*/);
	}

	// Gets last element that should take focus while processing a Shift-Tab keystroke.
	// Return nullptr to default to FocusManager logic.
	internal override DependencyObject GetLastFocusableElementOverride()
	{
		bool usesDefaultControlTemplate = UsesDefaultControlTemplate();

		if (!usesDefaultControlTemplate)
		{
			// Prevent any custom handling from controls like the Pivot which has a more complex tree structure.
			return null;
		}

		// When shift-tabbing into the ItemsControl, land on the first focusable element when its TabNavigation is Once,
		// and last focusable element for Local or Cycle, like a ListViewBase that never had focus.
		KeyboardNavigationMode navigationMode = TabNavigation;

		return GetFirstOrLastFocusableElement(
			navigationMode == KeyboardNavigationMode.Once /*isForFirstFocusableElement*/,
			true  /*isBackward*/,
			false /*skipHeader*/,
			false /*skipItemsAndGroupHeaders*/,
			false /*skipFooter*/);
	}

	// Returns a focusable child of this ItemsControl while processing ItemsControl::GetFirstFocusableElementOverride,
	// ItemsControl::GetLastFocusableElementOverride or ItemsControl::ProcessTabStopOverride.
	// isBackward: Indicates whether this is for a Tab or Shift-Tab processing.
	// skipHeader: Indicates whether returning the ItemsPresenter.Header or one of its focusable children is allowed.
	// skipItemsAndGroupHeaders: Indicates whether returning an item or group header is allowed.
	// skipFooter: Indicates whether returning the ItemsPresenter.Footer or one of its focusable children is allowed.
	// Because ItemsControl does not track a potential last focused child, it mostly acts like a ListViewBase which
	// never had focus:
	// - for TabNavigation==Once,
	//   * Tab targets the first focusable element within the first focusable item (isForFirstFocusableElement==True, isBackward==False).
	//   * Shift-Tab targets the last focusable element within the first focusable item (isForFirstFocusableElement==True, isBackward==True).
	// - for TabNavigation==Local/Cycle,
	//   * Tab targets the first focusable element within the first focusable item (isForFirstFocusableElement==True, isBackward==False).
	//   * Shift-Tab targets the last focusable element within the last focusable item (isForFirstFocusableElement==False, isBackward==True).
	private DependencyObject GetFirstOrLastFocusableElement(
		bool isForFirstFocusableElement,
		bool isBackward,
		bool skipHeader,
		bool skipItemsAndGroupHeaders,
		bool skipFooter)
	{
		if ((!isBackward && !skipHeader) || (isBackward && !skipFooter))
		{
			ContentControl focusableHeaderOrFooterContainer = GetFocusableHeaderOrFooterContainer(!isBackward /*isForHeader*/);

			if (focusableHeaderOrFooterContainer is not null)
			{
				if (isBackward)
				{
					// Get the last focusable element within the header/footer, or the header/footer itself.
					return FocusManager.FindLastFocusableElement(focusableHeaderOrFooterContainer);
				}
				else
				{
					// Get the first focusable element within the header/footer, or the header/footer itself.
					return FocusManager.FindFirstFocusableElement(focusableHeaderOrFooterContainer);
				}
			}
		}

		if (!skipItemsAndGroupHeaders)
		{
			// ShouldCustomizeTabNavigation returns whether the ItemsPanel is a ModernCollectionBasePanel or not (shouldCustomizeTabNavigation is set to True for modern panels).
			bool shouldCustomizeTabNavigation = ShouldCustomizeTabNavigation();

			bool scrollIntoViewAndPrepareContainer = false;

			// The first (for isForFirstFocusableElement==True) or last (for isForFirstFocusableElement==False) item may not be realized.
			// In those cases, that item must be realized and prepared first and selected as the focusable element.
			// This only needs to be done for modern panels as VirtualizingStackPanel and StackPanel do run into unrealized items.
			if (shouldCustomizeTabNavigation)
			{
#if !HAS_UNO // TODO Uno: FocusManager does not track IsMovingFocusToNextTabStop / IsMovingFocusToPreviousTabStop.
				var focusManager = VisualTree.GetFocusManagerForElement(this);

				Debug.Assert(focusManager is not null);

				if ((!isBackward && focusManager.IsMovingFocusToNextTabStop()) || (isBackward && focusManager.IsMovingFocusToPreviousTabStop()))
				{
					// The FocusManager is processing the Tab key to actually navigate to an element (as opposed to only accessing a tab stop).
					scrollIntoViewAndPrepareContainer = true;
				}
#endif
			}

			GroupItem groupItem = null;
			int itemCount = GetItemCount();

			int targetFocusedIndex = -1;

			if (itemCount > 0)
			{
				// Pick the first or last item, depending on isForFirstFocusableElement.
				targetFocusedIndex = isForFirstFocusableElement ? 0 : itemCount - 1;

				if (IsGrouping)
				{
					// Pick the group of the first or last item, depending on isForFirstFocusableElement.
					GetGroupFromItem(
						(uint)targetFocusedIndex,
						out _,
						out groupItem,
						out _,
						out _);
				}
			}

			DependencyObject focusableResult = null;

			if (groupItem is not null)
			{
				// This branch is handling the grouped VirtualizingStackPanel cases.
				// In particular, tabbing order is:
				// - ItemsPresenter.Header
				// - Group Header #1
				// - Item #1 in group #1
				// - Group Header #2
				// - Item #1 in group #2
				// ...
				// - Group Header #n
				// - Item #1 in group #n
				// Shift-tabbing goes in exact reverse order. Note that ItemsPresenter.Footer
				// is not supported.
				focusableResult = groupItem;
			}
			else if (targetFocusedIndex >= 0)
			{
				focusableResult = ContainerFromIndex(targetFocusedIndex);

				if (scrollIntoViewAndPrepareContainer)
				{
					DependencyObject itemContainerCandidate = focusableResult;

					// Make sure the target index is realized and prepared so it can be declared focusable and tabbed into.
					// Since this method brings the target index into view, it is only invoked when the FocusManager is processing
					// the Tab key to actually navigate to an element (as opposed to only accessing a tab stop).
					focusableResult = GetScrolledIntoViewAndPreparedContainer(targetFocusedIndex, itemContainerCandidate);
				}
			}

			if (focusableResult is not null)
			{
				if (isBackward)
				{
					// Get the last focusable element within the item/group header, or the item/group header itself.
					return FocusManager.FindLastFocusableElement(focusableResult);
				}
				else
				{
					// Get the first focusable element within the item/group header, or the item/group header itself.
					return FocusManager.FindFirstFocusableElement(focusableResult);
				}
			}
		}

		if ((!isBackward && !skipFooter) || (isBackward && !skipHeader))
		{
			ContentControl focusableHeaderOrFooterContainer = GetFocusableHeaderOrFooterContainer(isBackward /*isForHeader*/);

			if (focusableHeaderOrFooterContainer is not null)
			{
				if (isBackward)
				{
					// Get the last focusable element within the header/footer, or the header/footer itself.
					return FocusManager.FindLastFocusableElement(focusableHeaderOrFooterContainer);
				}
				else
				{
					// Get the first focusable element within the header/footer, or the header/footer itself.
					return FocusManager.FindFirstFocusableElement(focusableHeaderOrFooterContainer);
				}
			}
		}

		return null;
	}

	// Helper for determining if this ItemsControl uses the default control template made of a single CItemsPresenter child.
	// The ItemsControl::ProcessTabStopOverride, ItemsControl::GetFirstFocusableElementOverride and ItemsControl::GetLastFocusableElementOverride
	// methods only implement custom handling when usesDefaultControlTemplate is set to True. This is to preserve any particular handling that a sub-class control,
	// like the Pivot, may do while using a more complex ControlTemplate.
	private bool UsesDefaultControlTemplate()
	{
		int childrenCount = VisualTreeHelper.GetChildrenCount(this);

		if (childrenCount != 1)
		{
			return false;
		}

		DependencyObject child = VisualTreeHelper.GetChild(this, 0);

		return child is ItemsPresenter;
	}

	// Overriding default Tab key processing to account for potential
	// ItemsPresenter.Header and ItemsPresenter.Footer elements.
	internal override TabStopProcessingResult ProcessTabStopOverride(
		DependencyObject pFocusedElement,
		DependencyObject pCandidateTabStopElement,
		bool isBackward,
		bool didCycleFocusAtRootVisualScope)
	{
		DependencyObject ppNewTabStop = null;
		bool pIsTabStopOverridden = false;

		bool usesDefaultControlTemplate = UsesDefaultControlTemplate();

		if (!usesDefaultControlTemplate)
		{
			// Prevent any custom handling from controls like the Pivot which has a more complex tree structure.
			return new(pIsTabStopOverridden, ppNewTabStop);
		}

		ContentControl focusableHeaderOrFooterContainer = GetFocusableHeaderOrFooterContainer(true /*isForHeader*/);

		bool hasFocusableHeader = focusableHeaderOrFooterContainer is not null;

		focusableHeaderOrFooterContainer = GetFocusableHeaderOrFooterContainer(false /*isForHeader*/);

		bool hasFocusableFooter = focusableHeaderOrFooterContainer is not null;

		if (!hasFocusableHeader && !hasFocusableFooter)
		{
			// No special processing is required when there are no focusable header/footer.
			return new(pIsTabStopOverridden, ppNewTabStop);
		}

		GetElementType(pFocusedElement, out bool isCurrentAChild, out bool isCurrentInHeader, out bool isCurrentInFooter);
		Debug.Assert(isCurrentAChild);

		GetElementType(pCandidateTabStopElement, out bool isCandidateAChild, out _, out _);

		if (!isCandidateAChild)
		{
			// When tabbing, make sure the first focusable item, group header, or Footer if any gets focus, instead of exiting the ItemsControl.
			if (!isBackward && (isCurrentInHeader || (!isCurrentInFooter && hasFocusableFooter)))
			{
				ppNewTabStop = GetFirstOrLastFocusableElement(
					true /*isForFirstFocusableElement*/,
					false /*isBackward*/,
					true /*skipHeader*/,
					!isCurrentInHeader /*skipItemsAndGroupHeaders*/,
					false /*skipFooter*/);
			}
			// When shift-tabbing, make sure the first or last focusable item (depending on TabNavigation mode), group header, or Header if any gets focus, instead of exiting the ItemsControl.
			else if (isBackward && (isCurrentInFooter || (!isCurrentInHeader && hasFocusableHeader)))
			{
				KeyboardNavigationMode navigationMode = TabNavigation;

				ppNewTabStop = GetFirstOrLastFocusableElement(
					navigationMode == KeyboardNavigationMode.Once /*isForFirstFocusableElement*/,
					true /*isBackward*/,
					false /*skipHeader*/,
					!isCurrentInFooter /*skipItemsAndGroupHeaders*/,
					true /*skipFooter*/);
			}

			pIsTabStopOverridden = ppNewTabStop is not null;
		}

		return new(pIsTabStopOverridden, ppNewTabStop);
	}

	// Indicates whether the provided element is:
	// - a child of this ItemsControl,
	// - the ItemsPresenter.Header container or one of its children,
	// - the ItemsPresenter.Footer container or one of its children.
	private void GetElementType(
		DependencyObject element,
		out bool isAChild,
		out bool isInHeader,
		out bool isInFooter)
	{
		isAChild = false;
		isInHeader = false;
		isInFooter = false;

		DependencyObject current = element;
		ContentControl headerContainer = null;
		ContentControl footerContainer = null;

		ItemsPresenter itemsPresenter = ItemsPresenter;

		if (itemsPresenter is not null)
		{
			headerContainer = itemsPresenter.HeaderContainer;
			footerContainer = itemsPresenter.FooterContainer;
		}

		while (current is not null)
		{
			// We are interested in the closest element to the ItemsControl, therefore keep searching
			// until we are at the root of hierarchy or parent ItemsControl.

			if (ReferenceEquals(this, current))
			{
				isAChild = true;
				break;
			}

			// Is it a header?
			if (ReferenceEquals(headerContainer, current))
			{
				isAChild = true;
				isInHeader = true;
				break;
			}
			else
			{
				// Maybe a footer?
				if (ReferenceEquals(footerContainer, current))
				{
					isAChild = true;
					isInFooter = true;
					break;
				}
			}

			// Advance to parent.
			current = VisualTreeHelper.GetParent(current);
		}

		Debug.Assert(!(!isAChild && isInHeader));
		Debug.Assert(!(!isAChild && isInFooter));
	}

	// This method returns a value indicating whether the object is selectable.
	private protected static bool IsFocusableHelper(object pObject)
	{
		var spControl = pObject as Control;
		bool isFocusable = spControl is null;

		if (spControl is not null)
		{
			bool isEnabled = spControl.IsEnabled;
			bool isTabStop = spControl.IsTabStop;
			bool canHaveFocusableChildren = false;

			if (!isTabStop && isEnabled)
			{
				// If IsTabStop = FALSE, we cannot set focus on this element. But it may be possible to set focus on
				// an element in its subtree, in which case, it should still be selectable. FocusManager::CanHaveFocusableChildren
				// checks for a focusable child in the subtree.
				// The isEnabled check saves the p/invoke in the case that the item is not enabled, when it wouldn't be selectable anyway.
				canHaveFocusableChildren = FocusProperties.CanHaveFocusableChildren(spControl);
			}

			isFocusable = isEnabled && (isTabStop || canHaveFocusableChildren);
		}

		if (isFocusable)
		{
			if (pObject is UIElement spObjectAsIUIE)
			{
				isFocusable = spObjectAsIUIE.Visibility != Visibility.Collapsed;
			}
		}

		return isFocusable;
	}

	// This method is invoked by ItemsControl::GetScrolledIntoViewAndPreparedContainer, while tabbing or shift-tabbing
	// into the control. For an ItemsControl, it is only exercised when ItemsControl::ShouldCustomizeTabNavigation
	// returns True because an IModernCollectionBasePanel-implementing panel is used. For non-modern panels like the
	// StackPanel and VirtualizingStackPanel, the target element either already exists or is created synchronously by
	// the panel and then brought into view by CFocusManager through CInputPaneHandler::EnsureFocusedElementBringIntoView.
	private protected virtual void ScrollIntoView(
		uint index,
		bool isGroupItemIndex,
		bool isHeader,
		bool isFooter,
		bool isFromPublicAPI,
		bool ensureContainerRealized,
		bool animateIfBringIntoView,
		ScrollIntoViewAlignment alignment,
		double offset = 0.0,
		uint currentGroupIndex = 0)
	{
		// Our modern panel can deal with group headers and items, not with the header or footer
		if (isHeader || isFooter)
		{
			return;
		}

		Panel panel = ItemsPanelRoot;

		if (panel is not null)
		{
			var modernPanel = panel as ModernCollectionBasePanel;
			Debug.Assert(modernPanel is not null);

			if (isGroupItemIndex)
			{
				uint neighboringItemIndex = 0;
				if (animateIfBringIntoView)
				{
					neighboringItemIndex = modernPanel.GetNeighboringItemIndex(currentGroupIndex, index /*newGroupIndex*/);
				}

				modernPanel.ScrollGroupHeaderIntoView(
					index,
					alignment,
					offset,
					ensureContainerRealized /*forceSynchronous*/,
					animateIfBringIntoView /*animate*/,
					neighboringItemIndex);
			}
			else
			{
				modernPanel.ScrollItemIntoView(
					index,
					alignment,
					offset,
					ensureContainerRealized /*forceSynchronous*/,
					animateIfBringIntoView /*animate*/);
			}
		}
	}

	// Returns the Header or Footer container if it exists and is focusable.
	private protected ContentControl GetFocusableHeaderOrFooterContainer(bool isForHeader)
	{
		ItemsPresenter itemsPresenter = ItemsPresenter;

		if (itemsPresenter is null)
		{
			return null;
		}

		ContentControl contentContainer;

		if (isForHeader)
		{
			contentContainer = itemsPresenter.HeaderContainer;
		}
		else
		{
			itemsPresenter.LoadFooter(true);
			contentContainer = itemsPresenter.FooterContainer;
		}

		if (contentContainer is null)
		{
			return null;
		}

		bool isFocusable = IsFocusableHelper(contentContainer);

		return isFocusable ? contentContainer : null;
	}

	// Get the ICollectionViewGroup associated with an item.
	// TODO Uno: out parameters can't be omitted, so the visual tree shortcut and the group walk both always run.
	private protected void GetGroupFromItem(
		uint itemIndex,
		out ICollectionViewGroup ppGroup,
		out GroupItem ppGroupItem,
		out int pGroupIndex,
		out int pIndexInGroup)
	{
		ppGroup = null;
		pIndexInGroup = -1;
		ppGroupItem = null;
		pGroupIndex = -1;

		// First try to find the corresponding group by taking a short cut through
		// the visual tree. Skip this if we only need to find the index in the group.
		// This implementation cannot find the index in the group, so we must defer
		// to the other implementation below in that case.
		{
			GroupItem groupItem = null;

			// Get the current item's container
			DependencyObject container = ContainerFromIndex((int)itemIndex);

			// Walk up the visual tree looking for a GroupItem
			while (container is not null)
			{
				// Keep walking until we reach a GroupItem or we hit ourself
				groupItem = container as GroupItem;

				if (groupItem is not null || container == this)
				{
					// We found the item.
					ppGroupItem = groupItem;
					break;
				}

				container = VisualTreeHelper.GetParent(container);
			}

			// Use the GroupItem's Content if we were able to find it
			if (groupItem is not null)
			{
				ICollectionViewGroup contentGroup = groupItem.GetCollectionViewGroup();
				if (contentGroup is not null)
				{
					ppGroup = contentGroup;
				}
			}
		}

		// If we can't find the group using the visual tree (i.e., it hasn't been
		// realized yet), or we need to get the item's index within the group,
		// we'll just walk each of the groups checking their count to
		// see where the given item index belongs.
		{
			uint total = 0;

			var collectionGroups = (UnwrapItemsSource() as ICollectionView)?.CollectionGroups;

			// Walk through each of the groups to determine if it
			// contains the current item in which case we'll use it as
			// the destination
			uint size = (uint)collectionGroups.Count;

			for (uint i = 0; i < size; i++)
			{
				var currentGroup = (ICollectionViewGroup)collectionGroups[(int)i];
				uint groupSize = (uint)currentGroup.GroupItems.Count;

				total += groupSize;
				if (itemIndex < total)
				{
					ppGroup ??= currentGroup;
					pIndexInGroup = (int)(itemIndex - (total - groupSize));
					pGroupIndex = (int)i;
					break;
				}
			}
		}
	}

	// Realizes, scrolls into view and prepares the item at the provided index so it can be tabbed into.
	private protected DependencyObject GetScrolledIntoViewAndPreparedContainer(
		int index,
		DependencyObject itemContainerCandidate)
	{
		Debug.Assert(index >= 0);

		bool isPositionedInGarbageSection = false;

		DependencyObject itemContainer = itemContainerCandidate;

		if (itemContainer is not null)
		{
			// Check if the candidate item is currently off screen so it can be brought into view first.
			if (itemContainer is UIElement itemContainerUIE)
			{
				Panel itemsPanel = ItemsPanelRoot;

				if (itemsPanel is ModernCollectionBasePanel)
				{
					isPositionedInGarbageSection = ModernCollectionBasePanel.ElementIsPositionedInGarbageSection(itemContainerUIE);
				}
			}
		}

		if (itemContainer is null || isPositionedInGarbageSection)
		{
			// Synchronously realize item at the provided index.
			ScrollIntoView(
				(uint)index,
				false /*isGroupItemIndex*/,
				false /*isHeader*/,
				false /*isFooter*/,
				false /*isFromPublicAPI*/,
				true  /*ensureContainerRealized*/,
				false /*animateIfBringIntoView*/,
				ScrollIntoViewAlignment.Default);

			// Retrieve the item now that it was realized.
			itemContainer = ContainerFromIndex(index);

			// This last phase makes sure the target container is fully prepared to be eligible for focus.
			if (itemContainer is not null)
			{
				BuildTreeService buildTreeService = DXamlCore.Current.GetBuildTreeService();

				if (buildTreeService is not null)
				{
					BudgetManager budgetManager = DXamlCore.Current.GetBudgetManager();

					// Temporarily turning off the new container build budget to guarantee that the target item
					// at the provided index is fully prepared (gets its new Content and DataContext) so that it
					// can be declared focusable my the FocusManager. Otherwise the default 40ms allocation
					// comes short and it may be declared ineligible for focus, not having a Content set.
					budgetManager.SetUseUnbudgetedContainerBuild(true);

					try
					{
						bool workLeft;
						// Allow as many BuildTrees calls as there are active workers so that the ListViewBase worker
						// gets a chance to do its work as it is progressively moved into the front of the active list.
						int maxBuildTreesCalls = buildTreeService.ActiveWorkersCount;

						do
						{
							workLeft = buildTreeService.BuildTrees();
							maxBuildTreesCalls--;
						}
						while (maxBuildTreesCalls > 0 && workLeft);
					}
					finally
					{
						DXamlCore.Current.GetBudgetManager().SetUseUnbudgetedContainerBuild(false);
					}
				}
			}
		}

		return itemContainer;
	}

	// Helper for determining if this ItemsControl uses an IModernCollectionBasePanel-implementing panel.
	private protected bool ShouldCustomizeTabNavigation() => ItemsPanelRoot is ModernCollectionBasePanel;
}
