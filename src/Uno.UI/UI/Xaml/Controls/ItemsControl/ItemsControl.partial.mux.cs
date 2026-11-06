// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsControl_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsControl : IGeneratorHost
{
	private ICustomGeneratorItemsHost CustomItemsHost => ItemsPanelRoot as ICustomGeneratorItemsHost;

	private IItemContainerMapping ItemContainerMapping => CustomItemsHost?.GetItemContainerMapping();

	private void InitializeItemContainerGenerator() => CustomItemsHost?.RegisterItemsHost(this);

	// TODO Uno: the source is observed through INotifyCollectionChanged and has already changed here, so a
	// batched change can't be replayed one item at a time; it becomes a reset. WinUI's source raises one change per item.
	private void NotifyCustomItemsHost(ICustomGeneratorItemsHost host, NotifyCollectionChangedEventArgs args, int section)
	{
		if (args is null)
		{
			host.Refresh();
			return;
		}

		int ToFlatIndex(int indexInSection) =>
			IsGrouping ? GetIndexFromIndexPath(Uno.UI.IndexPath.FromRowSection(indexInSection, section)) : indexInSection;

		void Notify(CollectionChange change, int index)
		{
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

	void IGeneratorHost.PrepareGroupContainer(DependencyObject container, ICollectionViewGroup group)
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

	void IGeneratorHost.ClearGroupContainerForGroup(DependencyObject container, ICollectionViewGroup group)
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
}
