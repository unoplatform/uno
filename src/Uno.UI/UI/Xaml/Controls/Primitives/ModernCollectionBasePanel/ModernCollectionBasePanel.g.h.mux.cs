// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	// Fields.
	private int m_firstCacheGroupIndexBase;
	private int m_firstCacheIndexBase;
	private int m_firstVisibleGroupIndexBase;
	private int m_firstVisibleIndexBase;
	private bool m_isRegisteredForCallbacks;
	private int m_lastCacheGroupIndexBase;
	private int m_lastCacheIndexBase;
	private int m_lastVisibleGroupIndexBase;
	private int m_lastVisibleIndexBase;

	// Properties.
	internal int FirstCacheGroupIndexBase { get => m_firstCacheGroupIndexBase; set => m_firstCacheGroupIndexBase = value; }
	internal int FirstCacheIndexBase { get => m_firstCacheIndexBase; set => m_firstCacheIndexBase = value; }
	internal int FirstVisibleGroupIndexBase { get => m_firstVisibleGroupIndexBase; set => m_firstVisibleGroupIndexBase = value; }
	internal int FirstVisibleIndexBase { get => m_firstVisibleIndexBase; set => m_firstVisibleIndexBase = value; }
	internal bool IsRegisteredForCallbacks { get => m_isRegisteredForCallbacks; set => m_isRegisteredForCallbacks = value; }
	internal int LastCacheGroupIndexBase { get => m_lastCacheGroupIndexBase; set => m_lastCacheGroupIndexBase = value; }
	internal int LastCacheIndexBase { get => m_lastCacheIndexBase; set => m_lastCacheIndexBase = value; }
	internal int LastVisibleGroupIndexBase { get => m_lastVisibleGroupIndexBase; set => m_lastVisibleGroupIndexBase = value; }
	internal int LastVisibleIndexBase { get => m_lastVisibleIndexBase; set => m_lastVisibleIndexBase = value; }

	internal bool AreStickyGroupHeadersEnabledBase
	{
		get => (bool)GetValue(AreStickyGroupHeadersEnabledBaseProperty);
		set => SetValue(AreStickyGroupHeadersEnabledBaseProperty, value);
	}

	internal static DependencyProperty AreStickyGroupHeadersEnabledBaseProperty { get; } =
		DependencyProperty.Register(
			nameof(AreStickyGroupHeadersEnabledBase),
			typeof(bool),
			typeof(ModernCollectionBasePanel),
			new FrameworkPropertyMetadata(BoolBoxes.True));

	// Events.
	internal event EventHandler<object>? HorizontalSnapPointsChanged;
	internal event EventHandler<object>? VerticalSnapPointsChanged;
	internal event EventHandler<object>? VisibleIndicesUpdated;

	// Methods.
	bool IScrollSnapPointsInfo.AreHorizontalSnapPointsRegular => AreHorizontalSnapPointsRegular;
	bool IScrollSnapPointsInfo.AreVerticalSnapPointsRegular => AreVerticalSnapPointsRegular;

	event EventHandler<object> IScrollSnapPointsInfo.HorizontalSnapPointsChanged
	{
		add => HorizontalSnapPointsChanged += value;
		remove => HorizontalSnapPointsChanged -= value;
	}

	event EventHandler<object> IScrollSnapPointsInfo.VerticalSnapPointsChanged
	{
		add => VerticalSnapPointsChanged += value;
		remove => VerticalSnapPointsChanged -= value;
	}

	IReadOnlyList<float> IScrollSnapPointsInfo.GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment) => GetIrregularSnapPoints(orientation, alignment);

	float IScrollSnapPointsInfo.GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
	{
		GetRegularSnapPoints(orientation, alignment, out offset, out var spacing);
		return spacing;
	}

	void IInsertionPanel.GetInsertionIndexes(Point position, out int first, out int second) => GetInsertionIndexes(position, out first, out second);

	object? IItemContainerMapping.ItemFromContainer(DependencyObject container) => ItemFromContainer(container);
	DependencyObject? IItemContainerMapping.ContainerFromItem(object? item) => ContainerFromItem(item);
	int IItemContainerMapping.IndexFromContainer(DependencyObject container) => IndexFromContainer(container);
	DependencyObject? IItemContainerMapping.ContainerFromIndex(int index) => ContainerFromIndex(index);

	ThemeTransitionContext IChildTransitionContextProvider.GetChildTransitionContext(UIElement element, int layoutTickId) => GetChildTransitionContext(element, layoutTickId);
	Rect IChildTransitionContextProvider.GetChildTransitionBounds(UIElement element) => GetChildTransitionBounds(element);
	bool IChildTransitionContextProvider.IsCollectionMutatingFast() => IsCollectionMutatingFast();

	void ICustomGeneratorItemsHost.RegisterItemsHost(IGeneratorHost host) => RegisterItemsHost(host);
	void ICustomGeneratorItemsHost.DisconnectItemsHost() => DisconnectItemsHost();
	IItemContainerMapping ICustomGeneratorItemsHost.GetItemContainerMapping() => GetItemContainerMapping();
	IGroupHeaderMapping ICustomGeneratorItemsHost.GetGroupHeaderMapping() => GetGroupHeaderMapping();
	void ICustomGeneratorItemsHost.Refresh() => Refresh();
	void ICustomGeneratorItemsHost.NotifyOfItemsChanging(IVectorChangedEventArgs e) => NotifyOfItemsChanging(e);
	void ICustomGeneratorItemsHost.NotifyOfItemsChanged(IVectorChangedEventArgs e) => NotifyOfItemsChanged(e);
	void ICustomGeneratorItemsHost.NotifyOfItemsReordered(uint count) => NotifyOfItemsReordered(count);
	IContainerContentChangingIterator ICustomGeneratorItemsHost.GetContainersForIncrementalVisualization() => GetContainersForIncrementalVisualization();

	object? IGroupHeaderMapping.GroupFromHeader(DependencyObject header) => GroupFromHeader(header);
	DependencyObject? IGroupHeaderMapping.HeaderFromGroup(object? group) => HeaderFromGroup(group);
	int IGroupHeaderMapping.IndexFromHeader(DependencyObject header, bool excludeHiddenEmptyGroups) => IndexFromHeader(header, excludeHiddenEmptyGroups);
	DependencyObject? IGroupHeaderMapping.HeaderFromIndex(int index) => HeaderFromIndex(index);
	DependencyObject? IGroupHeaderMapping.GroupHeaderContainerFromItemContainer(DependencyObject itemContainer) => GroupHeaderContainerFromItemContainer(itemContainer);

	UIElement? IItemContainerGenerator2.GenerateContainerAtIndex(int index) => GenerateContainerAtIndex(index);
	UIElement? IItemContainerGenerator2.GenerateHeaderAtGroupIndex(int index) => GenerateHeaderAtGroupIndex(index);
	bool IItemContainerGenerator2.GetContainerRecycleQueueEmpty() => GetContainerRecycleQueueEmpty();
	void IItemContainerGenerator2.RecycleContainer(UIElement container) => RecycleContainer(container);
	bool IItemContainerGenerator2.TryRecycleContainer(UIElement container) => TryRecycleContainer(container);
	void IItemContainerGenerator2.RecycleAllContainers() => RecycleAllContainers();
	bool IItemContainerGenerator2.GetHeaderRecycleQueueEmpty() => GetHeaderRecycleQueueEmpty();
	void IItemContainerGenerator2.RecycleHeader(UIElement header) => RecycleHeader(header);
	void IItemContainerGenerator2.RecycleAllHeaders() => RecycleAllHeaders();
	bool IItemContainerGenerator2.FindRecyclingCandidate(int index) => FindRecyclingCandidate(index);
	uint IItemContainerGenerator2.GetQueueLength() => GetQueueLength();

	ElementInfo IItemLookupPanel.GetClosestElementInfo(Point position) => GetClosestElementInfo(position);
	int IItemLookupPanel.GetInsertionIndex(Point position) => GetInsertionIndex(position);
	void IItemLookupPanel.IsLayoutBoundary(int index, out bool isLeftBoundary, out bool isTopBoundary, out bool isRightBoundary, out bool isBottomBoundary)
		=> IsLayoutBoundary(index, out isLeftBoundary, out isTopBoundary, out isRightBoundary, out isBottomBoundary);
	Rect IItemLookupPanel.GetItemsBounds() => GetItemsBounds();

	void IKeyboardHeaderNavigationPanel.GetTargetHeaderIndexFromNavigationAction(uint sourceIndex, KeyNavigationAction action, out uint computedTargetIndex, out bool actionHandled)
		=> GetTargetHeaderIndexFromNavigationAction(sourceIndex, action, out computedTargetIndex, out actionHandled);

	bool IKeyboardNavigationPanel.SupportsKeyNavigationAction(KeyNavigationAction action) => SupportsKeyNavigationAction(action);
	void IKeyboardNavigationPanel.GetTargetIndexFromNavigationAction(uint sourceIndex, ElementType sourceType, KeyNavigationAction action, bool allowWrap, uint itemIndexHintForHeaderNavigation, out uint computedTargetIndex, out ElementType computedTargetType, out bool actionValidForSourceIndex)
		=> GetTargetIndexFromNavigationAction(sourceIndex, sourceType, action, allowWrap, itemIndexHintForHeaderNavigation, out computedTargetIndex, out computedTargetType, out actionValidForSourceIndex);

	int IPaginatedPanel.GetLastItemIndexInViewport(ScrollViewer scrollViewer) => GetLastItemIndexInViewport(scrollViewer);
	double IPaginatedPanel.GetItemsPerPage(ScrollViewer scrollViewer) => GetItemsPerPage(scrollViewer);

	bool ITreeBuilder.IsBuildTreeSuspended => IsBuildTreeSuspended;
	bool ITreeBuilder.IsRegisteredForCallbacks { get => IsRegisteredForCallbacks; set => IsRegisteredForCallbacks = value; }
	bool ITreeBuilder.BuildTree() => BuildTree();
	void ITreeBuilder.ShutDownDeferredWork() => ShutDownDeferredWork();
}
