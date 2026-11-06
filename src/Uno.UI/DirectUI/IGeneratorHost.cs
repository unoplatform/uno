// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IGeneratorHost.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace DirectUI;

internal interface IGeneratorHost
{
	IList<object>? View { get; }

	ICollectionView? CollectionView { get; }

	bool IsItemItsOwnContainer(object? item);

	DependencyObject GetContainerForItem(object? item, DependencyObject? container);

	void PrepareItemContainer(DependencyObject container, object? item);

	void ClearContainerForItem(DependencyObject container, object? item);

	bool IsHostForItemContainer(DependencyObject container);

	GroupStyle? GetGroupStyle(ICollectionViewGroup? group, uint level);

	void SetIsGrouping(bool isGrouping);

	DependencyObject GetHeaderForGroup(object? group);

	void PrepareGroupContainer(DependencyObject container, ICollectionViewGroup? group);

	void ClearGroupContainerForGroup(DependencyObject container, ICollectionViewGroup? group);

	void SetupContainerContentChangingAfterPrepare(DependencyObject container, object? item, int itemIndex, Size measureSize);

	void RegisterWorkFromArgs(ContainerContentChangingEventArgs args);

	void RegisterWorkForContainer(UIElement container);

	bool CanRecycleContainer(DependencyObject container);

	DependencyObject? SuggestContainerForContainerFromItemLookup();

	bool ShouldRaiseChoosingItemContainer();

	void RaiseChoosingItemContainer(ChoosingItemContainerEventArgs args);

	void RaiseContainerContentChangingOnRecycle(UIElement container, object? item);

	bool ShouldRaiseChoosingGroupHeaderContainer();

	void RaiseChoosingGroupHeaderContainer(ChoosingGroupHeaderContainerEventArgs args);

	void VirtualizationFinished();

	Rect OverrideContainerArrangeBounds(int index, Rect suggestedBounds);
}
