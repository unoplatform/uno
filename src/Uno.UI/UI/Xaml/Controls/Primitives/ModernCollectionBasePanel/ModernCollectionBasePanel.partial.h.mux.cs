// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

// Uncomment to output ItemsUpdatingScrollMode debugging information
//#define IUSM_DEBUG

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using DirectUI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract partial class ModernCollectionBasePanel :
	Panel,
	IInsertionPanel,
	IItemContainerMapping,
	IScrollSnapPointsInfo,
	IChildTransitionContextProvider,
	ICustomGeneratorItemsHost,
	IGroupHeaderMapping,
	IItemContainerGenerator2,
	IItemLookupPanel,
	IKeyboardHeaderNavigationPanel,
	IKeyboardNavigationPanel,
	IPaginatedPanel,
	ITreeBuilder
{
	internal const int ElementType_Count = 2;

	// Scrollviewer looks at its scrollbarvisibility settings and will substitute infinite
	// if those are set to visible or auto. This is an archaic design that is hard to change
	// However, certain features such as the new modern virtualizingpanels do not appreciate this.
	// They lack the extra communication that scrolldata presents to IScrollInfo implementors, so
	// in those cases we wish to go with a newer more modern approach of actually trusting layout to
	// pass in the correct values.
	// See WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility.

	// have Itemspresenter set an origin before measure occurs so we don't have to do double invalidations
	internal void SetOriginFromItemsPresenter(Point origin)
	{
		m_windowState.validWindowCalculation &= origin.X == m_originFromItemsPresenter.X && origin.Y == m_originFromItemsPresenter.Y;
		m_originFromItemsPresenter = origin;
	}

	// returns an array of indices of all pinned elements by type
	// See GetPinnedElementsIndexVector.

	// core virtualization code

	// This method will detect the visible window, generate/recycle, measure, and pre-arrange
	// See RunVirtualization.

	// Determine the window to show, based on either the user scrolling, or responding to a specific request
	// such as key navigation or ScrollIntoView, and then generate the appropriate anchors for the new view
	// See DetermineWindowAndAnchors.

	// Measure elements in garbage section.
	// See MeasureElementsInGarbageSection.

	// If the layout specifies a need for aspecial group and/or item,
	// ensure these items are generated, measured, and pinned.
	// See MeasureSpecialElements.

	// Determines a starting element and runs Generate() both forward and backwards from that point
	// See RunGenerate.

	// Given a start location and a direction: measure and generate all needed elements until the window is filled in that direction
	// See Generate.

	// Given a start location and a direction, remove/recycle all valid containers and headers.
	// This not only trims off dangling sentinels, but also recycles all elements in that direction
	// We do this so that we don't have chunks of realized elements that weren't accounted for in the measure pass
	// See RemoveMeasureLeftOvers.

	// Arrange all elements of a given type
	// See ArrangeElements.

	// This method works in two stages:
	// Stage 1: Determine if the new window is disconnected from either our headers or containers
	//          and figure out the closest valid elements from which to estimate
	// Stage 2: Calculate new anchor indices by estimating how many groups/items it would take for
	//          us to get from the old realized elements to the new window
	// See DetectAndHandleDisconnectedView.

	// Recycle containers if our recycle queue is empty and we have containers to recycle.
	// See EnsureRecycleContainerCandidate.

	// Recycle headers if our recycle queue is empty and we have headers to recycle.
	// alwaysRecycle: even if we have elements in the recyclequeue, still recycle.
	// See EnsureRecycleHeaderCandidate.

	// Return whether an element should be generated/placed/shown. Normally, this is always true
	// but can return false if we're hiding empty groups or collapsing groups
	// See ShouldElementBeVisible.

	// When generating or measuring backward, this method is used when we need to set the bounds for
	// a header of a non-empty group and the first container of that group haven't been realized yet.
	// See EstimateFloatingHeaderLocation.

	// Returns whether the last element of the data source was realized.
	// If that's the case, it will also return the panel's extent.
	// See IsLastElementRealized.

	// Internal interface to dxaml code.

	// Scrolls the given rect into view. If another Scroll*IntoView was called
	// with asynchronous scrolling specified, the previous scroll action is forced to
	// complete synchronously with this method.
	// targetRect -         Target rectangle.
	// forceSynchronous -   if TRUE, causes the full virtualization pass (generate containers, etc)
	//                      to run and the scroll offsets to be updated before the call returns.
	//                      Otherwise, we'll invalidate measure and process the scroll in the
	//                      subsequent Measure pass.
	// See ScrollRectIntoView.

	// Scrolls the given item into view. If another Scroll*IntoView was called
	// with asynchronous scrolling specified, the previous scroll action is forced to
	// complete synchronously with this method.
	// index            -   Target index in items collection.
	// alignment        -   Desired alignment of item with viewport edges.
	// offset           -   An additional delta that get applied to the final scroll location.
	// forceSynchronous -   if TRUE, causes the full virtualization pass (generate containers, etc)
	//                      to run and the scroll offsets to be updated before the call returns.
	//                      Otherwise, we'll invalidate measure and process the scroll in the
	//                      subsequent Measure pass.
	// animate          -   if TRUE, a DirectManipulation animation is processed instead of a jump.
	// See ScrollItemIntoView.

	// Provide a focus candidate. If a scroll into view happens, the panel will
	// keep that as a focus candidate. The listview can ask for this candidate
	// when it needs to focus.
	// See GetFocusCandidate.

	// Scrolls the given group header into view. If another Scroll*IntoView was called
	// with asynchronous scrolling specified, the previous scroll action is forced to
	// complete synchronously with this method.
	// index            -   Target grouped index in collection.
	// alignment        -   Desired alignment of group header with viewport edges.
	// offset           -   An additional delta that get applied to the final scroll location.
	// forceSynchronous -   if TRUE, causes the full virtualization pass (generate containers, etc)
	//                      to run and the scroll offsets to be updated before the call returns.
	//                      Otherwise, we'll invalidate measure and process the scroll in the
	//                      subsequent Measure pass.
	// See ScrollGroupHeaderIntoView.

	// Called when attempting to animate from the current group header (currentGroupIndex)
	// to another group header (newGroupIndex).
	// When animating down/right to a larger index, returns the last item's index in the visible
	// group prior to the target group.
	// When animating up/left to a smaller index, returns the first item's index in the target group.
	// See GetNeighboringItemIndex.

	#region ICustomItemsHostPanel

	// pass through to the cache manager
	// See RegisterItemsHost, DisconnectItemsHost.

	// our virtualization solution chooses to implement the mapping interfaces on the main panel,
	// since this is where the information on how to map is available.
	// See GetItemContainerMapping, GetGroupHeaderMapping.

	// Recycle all headers and containers, then empty all caches and queues and invalidate Measure.
	// See Refresh.

	// responds to a collection change on the itemssource before others look at it
	// See NotifyOfItemsChanging.
	// responds to a collection change on the itemssource after others look at it
	// See NotifyOfItemsChanged.

	// responds to a reordering. ListViewBase calls it when re-ordering happens.
	// See NotifyOfItemsReordered.

	// Returns a containers iterator for CCC's incremental visualization.
	// See GetContainersForIncrementalVisualization.

	#endregion

	// exposing the LayoutStrategy such that virtualizing controls that host these panels can
	// have a way to figure out the virtualization direction
	// See GetLayoutStrategy.

	// The specifics of the layout is governed by a layout strategy. Subclasses like ItemsWrapGrid
	// tell us which strategy to use via these methods.
	// See SetLayoutStrategyBase.

	// Called when the GroupHeaderPlacement property is changed. Now we need to translate that into a
	// GroupHeaderStrategy and update the layout strategy
	// See SetGroupHeaderPlacement.

	// Returns the thickess size in the virtualizing direction.
	// See SizeFromThicknessInVirtualizingDirection.

	// Returns the size of the potential GroupPadding in the virtualizing direction.
	private protected virtual double GroupPaddingSizeInVirtualizingDirection() => 0.0;

	// Only ItemsStackPanel and CalendarPanel support the maintain viewport behavior.
	private protected virtual ItemsUpdatingScrollMode GetItemsUpdatingScrollMode() => ItemsUpdatingScrollMode.KeepScrollOffset;
	private protected bool IsMaintainViewportSupportedAndEnabled() => GetItemsUpdatingScrollMode() != ItemsUpdatingScrollMode.KeepScrollOffset;

	// Set the strategy's header strategy based on whether or not we're grouping.
	// See ReevaluateGroupHeaderStrategy.

	// called when a collection mutates.
	// Our implementation does not do anything here, just for the inheritors.
	private protected virtual void OnCollectionChangeProcessed() { }

	//
	// Special items overrides
	//

	private protected virtual bool NeedsSpecialItem() => false;
	private protected virtual bool NeedsSpecialGroup() => false;

	private protected virtual int GetSpecialItemIndex()
	{
		global::System.Diagnostics.Debug.Assert(false);
		throw new InvalidOperationException();
	}

	private protected virtual int GetSpecialGroupIndex()
	{
		global::System.Diagnostics.Debug.Assert(false);
		throw new InvalidOperationException();
	}

	private protected virtual void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)
	{
		global::System.Diagnostics.Debug.Assert(false);
		throw new InvalidOperationException();
	}

	private protected virtual void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)
	{
		global::System.Diagnostics.Debug.Assert(false);
		throw new InvalidOperationException();
	}

	// IWG and ISG supports different header alignment.
	// This virtual method allows them to override their respective layout strategies.
	// In the future, other modern panels might not care about this. That's why we
	// provide a default implementation that does nothing.
	private protected virtual void SetGroupHeaderStrategy(GroupHeaderStrategy strategy) { }

	// Used to estimate tracked element reposition during items source updates. These should have been part of the
	// Microsoft.UI.Xaml.Controls.ILayoutStrategy interface.
	private protected virtual float GetAverageHeaderSize() => -1.0f;
	private protected virtual float GetAverageContainerSize() => -1.0f;

	// core virtualization helper methods

	// Returns the group header strategy enum value based on virtualization direction and group header placement.
	// See GetGroupHeaderStrategy.

	// Makes sure that whatever container we think is focused is still focused.
	// Otherwise, recycle it.
	// See VerifyStoredFocusedContainer.

	// Does the previously stored focused container still have focus?
	// See StoredFocusedContainerStillHasFocus.

	// If we have a stored focused container, release it back into the recycle queue.
	// See ReleaseStoredFocusedContainer.

	// Ensure that any pinned containers still have a reason to be pinned. If not, unpin them.
	// See PrunePinnedContainers.

	// move unloaded elements from unloaded queue to the visual tree.
	// See ReloadUnloadedElements.

	// implementation of ITreeBuilder

	// perform async work. In the case of a modernpanel that is:
	// 1. increase the cache
	// See BuildTree.

	// shuts down all the work we still have to do
	// in our case that is nothing
	internal void ShutDownDeferredWork() { }

	// embodies the conditions we have for increasing the cache.
	// See CanIncreaseCacheLength.

	// core virtualization helper classes

	// caches to speed up access to spChildren, spIHost and so on
	internal partial class CacheManager
	{
		// nested struct describing our grouping cache
		internal partial struct GroupCache
		{
			internal int startItemIndex;
			internal int endItemIndex;
			internal int indexOfGroup;
			internal int nonEmptyIndexOfGroup;
		}

		// simple iDispose-like pattern
		internal partial class StrongRefLifetime : IDisposable
		{
			internal StrongRefLifetime(CacheManager pOwner) => m_pOwner = pOwner;

			public void Dispose()
			{
				if (m_pOwner is not null)
				{
					m_pOwner.UnguaranteeCache();
					m_pOwner = null;
				}
			}

			private CacheManager? m_pOwner;
		}

		// the object is intended to be created just once (not per cache session)

		// Gets an instance of the lifetime manager for caches
		// See CacheStrongRefs.

		// start a cache session. Some information can stay relevant after the session
		// while in Guaranteed-state, the manager will not doubt the invariants like GeneratorHost, spChildren, etc.
		// See GuaranteeCache.
		// the manager will clear out some information it has relied on in the past (GeneratorHost, spChildren)
		// See UnguaranteeCache.

		// Reset the cached group info
		// This is very impactful, only do this if you need to
		// See ResetGroupCache.

		// Check if the group count in the cache is the same as in the CVS. If different, then reset the cache
		// See ResetGroupCacheIfGroupCountsMismatch.

		// Trying to limit the amount of api that we expose by collapsing all indices. You can only pass one actual index, the rest should be -1
		// See RenewCacheAfterMutation.

		// store the itemshost
		internal void RegisterItemsHost(IGeneratorHost spHost) => m_wrHost = new WeakReference<IGeneratorHost>(spHost);
		internal void DisconnectItemsHost() => m_wrHost = null;
		internal bool IsItemsHostRegistered() => m_wrHost is not null && m_wrHost.TryGetTarget(out _);

		// Getters for expensive things
		// See GetItemsHost, GetChildren.

		internal bool GetHidesIfEmpty() => m_hidesIfEmpty;
		internal void PutHidesIfEmpty(bool hidesIfEmpty) => m_hidesIfEmpty = hidesIfEmpty;

		internal int GetTotalItemCount() => m_cachedItemsCount;
		internal int GetTotalGroupCount() => m_cachedGroupInformation.Count;
		internal int GetNonEmptyGroupCount() => m_cachedNonEmptyGroupsCount;
		internal bool IsGrouping() => m_cachedIsGrouped;
		internal bool WasGrouping() => m_cachedWasGrouped;

		// With things like "hiding empty groups" or "collapsing items in a group", it's often a helpful abstraction to be able to
		// work in terms of "layout units", rather than "data units", where traversing a range in layout units will skip over elements that are hidden
		// See DataIndexToLayoutIndex, LayoutIndexToDataIndex, GetTotalLayoutGroupCount.

		// register that we want to interpret the reset event in a certain way
		internal void RegisterInterpretResetAsFullReset()
		{
			global::System.Diagnostics.Debug.Assert(m_interpretItemCollectionResetAsFullReset == false);
			m_interpretItemCollectionResetAsFullReset = true;
		}

		internal bool ShouldInterpretResetAsFullReset() => m_interpretItemCollectionResetAsFullReset;

		// Checks if the container or one of its descendants currently has the focus,
		// in which case it cannot be recycled
		// In a RunVirtualization call, we may need to recycle several items
		// and as we have to check the focus for each candidate, we could
		// spend a lot of time in FrameworkElement::HasFocus as it walks the
		// the visual tree up starting to the focused element.
		// Therefore, the first time IsFocusedChild is called in a virtualization
		// cycle, we walk the tree up and cache the result
		// Following calls will simply test the candidate against the cache
		// See IsFocusedChild.

		// Returns the ICollectionViewGroup reference from the grouped bound source corresponding to an index
		// See GetGroup.
		// Returns the IInspectable reference from the bound source corresponding to a index
		// See GetItem.
		// Returns the index from the bound source corresponding to an IInspectable reference
		// See GetIndex.

		// Returns the index of the last header/item in the data that can be displayed in layout.
		// See GetLastElementInLayout.

		private bool m_interpretItemCollectionResetAsFullReset;
		private bool m_metricsAreCached;
		private bool m_metricsWereCached;

		private List<GroupCache> m_cachedGroupInformation = new();
		private List<GroupCache> m_cachedHistoryGroupInformation = new();
		private bool m_cachedIsGrouped;
		private bool m_cachedWasGrouped;
		private int m_cachedItemsCount;
		private int m_cachedHistoryItemsCount;
		private int m_cachedNonEmptyGroupsCount;

		// Quick and dirty vector-of-int to avoid plumbing GroupCache or similar across ILayoutStrategy

		private List<uint>? m_spGroupSizes;
		private List<uint>? m_spNonEmptyGroupSizes;

		// reference back to the host
		private WeakReference<IGeneratorHost>? m_wrHost;

		// resolved references, only valid during guaranteed sessions
		private IGeneratorHost? m_strongHost;
		private IList<UIElement>? m_strongChildren;

		// Context for recycling: only valid during VirtualizationPhase
		private IContainerRecyclingContext? m_spContainerRecyclingContext;
		private uint m_recyclingItemIndex;

		// Indicates that the visual tree has been walked in the current virtualization cycle
		// to check if a container or one of its descendants has the focus
		// (see IsFocusedChild)
		private bool m_isFocusedChildValid;
		// container which has the focus (or one of its descendants)
		private UIElement? m_spFocusedChild;

		// Cache to accelerate GetGroup and GetItem methods
		private bool m_isCollectionCacheValid;
		// When grouped, the vector of groups
		private IList<object>? m_strongCollectionGroupsAsV;
		// Index of the cached group
		private int m_cachedGroupIndex;
		// cached group strong reference
		private ICollectionViewGroup? m_strongCurrentGroup;
		// When grouped, the vector containing items of the current group, when not group, the items themselves
		private IList<object>? m_strongView;

		// Caching HidesIfEmpty
		private bool m_hidesIfEmpty;

		// backtalk
		private readonly ModernCollectionBasePanel m_owner;

		// Initialize the strong collection cache
		// See InitCollectionsCache.
		// Clear the strong collection cache
		// See ClearCollectionsCache.
		// Cache a group's interfaces
		// See CacheGroup.
	}

	// provides data information for the layout strategies.
	internal partial class LayoutDataInfoProvider : ILayoutDataInfoProvider
	{
		// Raw reference to the modern collection base panel to avoid a cyclical reference.
		// MCBP unset this in its destructor.
		private ModernCollectionBasePanel? _pMCBP;
	}

	// Iterates over containers for CCC's incremental visualization.
	internal partial class ContainerContentChangingIterator : IContainerContentChangingIterator
	{
		// Raw reference because the panel keeps this alive.
		private ModernCollectionBasePanel m_pMCBP = null!;

		private PanelScrollingDirection m_direction;
		private int m_visibleStartInVector;
		private int m_visibleEndInVector;
		private int m_cacheEndInVector;
		private int m_currentIndex;
	}

	// manages containers in the children collection

	// Some notes on terminology.
	// Child index      - Index within Panel.Children.
	// Valid index      - Specific to type (header or container). With this in mind,
	//                    it's an index within the list of that particular type. I.e.,
	//                    container valid index 0 is the first realized container, and
	//                    header valid index 0 is the first realized header.
	// Item index       - Index in the ItemsSource.

	// A quick diagram showing layout in Children relative to our internal fields,
	// arranged in increasing index.

	// Index                    Significance
	// 0                        First realized header.
	// ...                      Bunch of realized headers.
	// m_validHeadersCount      First realized _CONTAINER_
	// ...                      Bunch of realized containers.
	// m_validHeadersCount+\
	//  m_validContainerCount   Start of garbage.
	// Pinned containers are interspersed inside the Children collection (potentially in the garbage section).

	internal partial class ContainerManager
	{
		// pinned items: containers we want to track the index of
		internal partial class PinnedElementInfo
		{
			internal PinnedElementInfo() { }

			internal int GetIndex()
			{
				global::System.Diagnostics.Debug.Assert(m_index > -1 || m_wrElement is null);
				return m_index;
			}

			internal UIElement? GetElement() => m_wrElement is not null && m_wrElement.TryGetTarget(out var element) ? element : null;
			internal void UpdateIndex(int index) => m_index = index;
			internal void UpdateElement(WeakReference<UIElement>? wrElement) => m_wrElement = wrElement;

			private int m_index = -1;
			private WeakReference<UIElement>? m_wrElement;
		}

		// Clear Pinning and Focus
		// See ClearPinAndFocus.
		// Clear all realized headers and containers (pinned or not).
		// See Refresh.
		// Re do indices, like a less destructive refresh
		// See Reset.

		// Element management and translation.

		// Given an the index of a valid element (i.e., 0 is always the first container in the valid range), return the element.
		internal UIElement? GetHeaderAtValidIndex(int indexInValidHeaders) => GetElementAtValidIndex(ElementType.GroupHeader, indexInValidHeaders);
		internal UIElement? GetContainerAtValidIndex(int indexInValidContainers) => GetElementAtValidIndex(ElementType.ItemContainer, indexInValidContainers);

		// Given a data index, return the element - could be null if unrealized
		internal UIElement? GetHeaderAtGroupIndex(int groupIndex) => GetElementAtDataIndex(ElementType.GroupHeader, groupIndex);
		internal UIElement? GetContainerAtItemIndex(int itemIndex) => GetElementAtDataIndex(ElementType.ItemContainer, itemIndex);

		// Gets the number of realized elements.
		internal int GetValidHeaderCount() => GetValidElementCount(ElementType.GroupHeader);
		internal int GetValidContainerCount() => GetValidElementCount(ElementType.ItemContainer);
		internal int GetValidElementCount(ElementType type) => m_validElements[(int)type].Count;

		// Translate a valid element index to an index in Items(Source).
		internal int GetGroupIndexFromValidIndex(int indexInValidHeaders) => GetDataIndexFromValidIndex(ElementType.GroupHeader, indexInValidHeaders);
		internal int GetItemIndexFromValidIndex(int indexInValidContainers) => GetDataIndexFromValidIndex(ElementType.ItemContainer, indexInValidContainers);
		internal int GetDataIndexFromValidIndex(ElementType type, int indexInValidElements) => indexInValidElements + m_indicesOfFirstValidElement[(int)type];

		// Translate an index in data Items(Source) to a "valid" element index.
		// Note that the index may not be really valid in the classic sense,
		// as the element may not actually be realized. Check it with
		// IsValidElementIndexWithinBounds.
		internal int GetValidHeaderIndexFromGroupIndex(int groupIndex) => GetValidElementIndexFromDataIndex(ElementType.GroupHeader, groupIndex);
		internal int GetValidContainerIndexFromItemIndex(int indexInItemCollection) => GetValidElementIndexFromDataIndex(ElementType.ItemContainer, indexInItemCollection);

		internal int GetValidElementIndexFromDataIndex(ElementType type, int dataIndexInCollection)
		{
			int indexOfFirstValidElement = m_indicesOfFirstValidElement[(int)type];
			global::System.Diagnostics.Debug.Assert(indexOfFirstValidElement >= -1);
			return indexOfFirstValidElement == -1 ? indexOfFirstValidElement : dataIndexInCollection - indexOfFirstValidElement;
		}

		// Do we actually have a realized element with this valid element index?
		internal bool IsValidContainerIndexWithinBounds(int indexInValidContainers) => IsValidElementIndexWithinBounds(ElementType.ItemContainer, indexInValidContainers);
		internal bool IsValidHeaderIndexWithinBounds(int indexInValidHeaders) => IsValidElementIndexWithinBounds(ElementType.GroupHeader, indexInValidHeaders);
		internal bool IsValidElementIndexWithinBounds(ElementType type, int indexInValidElements) => indexInValidElements >= 0 && indexInValidElements < GetValidElementCount(type);

		internal bool IsItemIndexWithinValidRange(int itemIndex) => IsValidContainerIndexWithinBounds(GetValidContainerIndexFromItemIndex(itemIndex));
		internal bool IsGroupIndexWithinValidRange(int groupIndex) => IsValidHeaderIndexWithinBounds(GetValidHeaderIndexFromGroupIndex(groupIndex));
		internal bool IsDataIndexWithinValidRange(ElementType type, int dataIndex) => IsValidElementIndexWithinBounds(type, GetValidElementIndexFromDataIndex(type, dataIndex));

		// Returns True if there is a realized item in the valid collection for the provided index.
		// See IsItemConnected.

		// Returns True if the group header for the provided index is connected.
		// See IsGroupHeaderConnected.

		// Returns True if there is a realized group header for the provided index.
		// See IsGroupHeaderRealized.

		// Inform the ContainerManager that the data index of the first realized element has changed
		// to the given value.
		internal void UpdateGroupIndexForFirstValidHeader(int groupIndexOfFirstValidHeader) => UpdateDataIndexForFirstValidElement(ElementType.GroupHeader, groupIndexOfFirstValidHeader);
		internal void UpdateItemIndexForFirstValidContainer(int itemIndexOfFirstValidContainer) => UpdateDataIndexForFirstValidElement(ElementType.ItemContainer, itemIndexOfFirstValidContainer);

		// Given the element and its index in the Items(Source), add the element (or move, if it's a pinned item/group)
		// to the proper place in the Children collection. Also updates the element's virtualization info to reflect
		// that it is realized.
		internal void PlaceInValidHeaders(int groupIndex, UIElement? spHeader) => PlaceInValidElements(ElementType.GroupHeader, groupIndex, spHeader);
		internal void PlaceInValidContainers(int itemIndex, UIElement? spContainer) => PlaceInValidElements(ElementType.ItemContainer, itemIndex, spContainer);

		// Insert a placeholder sentinel into the valid range
		internal void InsertSentinelContainer(int itemIndex) => InsertSentinelElement(ElementType.ItemContainer, itemIndex);
		internal void InsertSentinelHeader(int groupIndex) => InsertSentinelElement(ElementType.GroupHeader, groupIndex);

		// Removes an element from the valid range. It is now considered to be junked and we are probably calling this because we are recycling.
		// In some cases we are removing a item/group from the itemcollection and we do not want to update the dataindexFromFirstValidElement (itemBeingRemoved).
		// Generated elements get moved to the junk sections; ungenerated elements get removed from the children.
		// If specified, gateIndex represents the limit of ungenerated elements removal: it is used to ensure that the element being realized
		// does not get disjoint from the range of elements after this cleanup phase
		internal void RemoveFromValidHeaders(int indexInValidHeaders, bool isForGroupRemoval, int gateIndex = -1) => RemoveFromValidElements(ElementType.GroupHeader, indexInValidHeaders, isForGroupRemoval, gateIndex);
		internal void RemoveFromValidContainers(int indexInValidContainers, bool isForItemRemoval, int gateIndex = -1) => RemoveFromValidElements(ElementType.ItemContainer, indexInValidContainers, isForItemRemoval, gateIndex);

		// Mark all valid elements as junked
		internal void RemoveAllValidHeaders() => RemoveAllValidElements(ElementType.GroupHeader);
		internal void RemoveAllValidContainers() => RemoveAllValidElements(ElementType.ItemContainer);

		// place in garbage
		// See PlaceInGarbageRegionIfNotInChildren.

		// IICM and IGHM implementation
		internal DependencyObject? IICM_ContainerFromItem(object? item) => Interface_ElementFromDataItem(ElementType.ItemContainer, item);
		internal DependencyObject? IGHM_HeaderFromGroup(object? group) => Interface_ElementFromDataItem(ElementType.GroupHeader, group);

		internal int IICM_IndexFromContainer(DependencyObject container) => Interface_IndexFromElement(ElementType.ItemContainer, container, false /*excludeHiddenEmptyGroups*/);
		internal int IGHM_IndexFromHeader(DependencyObject header, bool excludeHiddenEmptyGroups) => Interface_IndexFromElement(ElementType.GroupHeader, header, excludeHiddenEmptyGroups);

		internal DependencyObject? IICM_ContainerFromIndex(int index) => Interface_ElementFromDataIndex(ElementType.ItemContainer, index);
		internal DependencyObject? IGHM_HeaderFromIndex(int index) => Interface_ElementFromDataIndex(ElementType.GroupHeader, index);

		internal object? IICM_ItemFromContainer(DependencyObject container) => Interface_DataItemFromElement(ElementType.ItemContainer, container);
		internal object? IGHM_GroupFromHeader(DependencyObject header) => Interface_DataItemFromElement(ElementType.GroupHeader, header);

		internal DependencyObject? IICM_GroupHeaderContainerFromItemContainerImpl(DependencyObject pItemContainer) => Interface_GroupHeaderContainerFromItemContainerImpl(pItemContainer);

		// Pinned Containers & Headers
		// These are containers or headers that are not necessarily part of the valid range,
		// but must be kept around for various reasons.

		// Return a vector of indices of all pinned elements
		internal List<uint> GetContainerPinnedElementsIndexVector() => GetPinnedElementsIndexVector(ElementType.ItemContainer);
		internal List<uint> GetHeaderHeaderPinnedElementsIndexVector() => GetPinnedElementsIndexVector(ElementType.GroupHeader);

		// Given an element index in the Items(Source), grab the actual pinned element (if it is actually pinned).
		internal UIElement? GetHeaderFromPinnedHeaders(int index) => GetElementFromPinnedElements(ElementType.GroupHeader, index);
		internal UIElement? GetContainerFromPinnedContainers(int index) => GetElementFromPinnedElements(ElementType.ItemContainer, index);

		// Given a data index in the Items(Source) and the element, add it to the pinned elements.
		internal void RegisterPinnedHeader(int index, UIElement spHeader) => RegisterPinnedElement(ElementType.GroupHeader, index, spHeader);
		internal void RegisterPinnedContainer(int index, UIElement spContainer) => RegisterPinnedElement(ElementType.ItemContainer, index, spContainer);

		// Given a data index in the Items(Source), remove the corresponding element from the pinned elements (if present).
		internal void UnregisterPinnedHeader(int index) => UnregisterPinnedElement(ElementType.GroupHeader, index);
		internal void UnregisterPinnedContainer(int index) => UnregisterPinnedElement(ElementType.ItemContainer, index);

		// Keep only the pinned containers that are approved by the given filter function.
		// The function takes the index and container as arguments, and should return TRUE for pinned containers to keep.
		// See FilterPinnedContainers.

		// Is the given container pinned?
		// Is the given header pinned?
		internal bool GetIsHeaderPinned(UIElement? spHeader) => GetIsElementPinned(ElementType.GroupHeader, spHeader, out _);
		internal bool GetIsContainerPinned(UIElement? spContainer) => GetIsElementPinned(ElementType.ItemContainer, spContainer, out _);

		// Temporary holding for elements being prepared
		// See HoldForPrepare, ResetAfterPrepare.

		// focus
		// store a container/index pair. Can only occur if there is no previously focused container (will fail if there is)
		// See SetFocusedContainer.
		internal bool HasFocusedContainer() => m_focusedContainer.GetIndex() > -1;

		internal bool HasFocusedHeader() => m_focusedHeaderContainer.GetIndex() > -1;

		internal bool GetIsIndexFocused(ElementType type, int index) => type == ElementType.ItemContainer && m_focusedContainer.GetIndex() == index;

		// Scroll into view and last focus.
		// Was the last scroll into view for item or header
		internal bool m_wasLastScrollIntoViewForHeader;
		internal bool m_isScrollIntoViewPending;
		internal bool m_isScrollIntoViewInProgress;

		// Collection change notifications

		// Called when a data item is added to Items(Source). We need to know about this so we can update various internal indexes.
		internal void OnGroupAdded(int index) => OnDataItemAdded(ElementType.GroupHeader, index);
		internal void OnItemAdded(int index) => OnDataItemAdded(ElementType.ItemContainer, index);

		// Called when a data item is removed from Items(Source). We need to know about this so we can update various internal indexes.
		internal void OnGroupRemoved(int index) => OnDataItemRemoved(ElementType.GroupHeader, index);
		internal void OnItemRemoved(int index) => OnDataItemRemoved(ElementType.ItemContainer, index);

		// Called when a data item is replaced from Items(Source).
		// See OnDataItemReplaced.

		// Return the start and size of various sections of the Children.
		internal int StartOfVisualSection(ElementType type) => type == ElementType.ItemContainer ? StartOfContainerVisualSection() : StartOfHeaderVisualSection();
		internal int SizeOfVisualSection(ElementType type) => m_elementIndicesForChildren[(int)type].Count;
		internal int EndOfVisualSection(ElementType type) => StartOfVisualSection(type) + SizeOfVisualSection(type);

		internal int StartOfHeaderVisualSection() => 0;
		internal int StartOfContainerVisualSection() => EndOfHeaderVisualSection(); // Containers sit right after the headers

		internal int SizeOfHeaderVisualSection() => SizeOfVisualSection(ElementType.GroupHeader);
		internal int SizeOfContainerVisualSection() => SizeOfVisualSection(ElementType.ItemContainer);
		internal int EndOfHeaderVisualSection() => EndOfVisualSection(ElementType.GroupHeader);
		internal int EndOfContainerVisualSection() => EndOfVisualSection(ElementType.ItemContainer);

		// Returns the index of the first element in Children that is in the garbage section.
		internal int StartOfGarbageSection() => SizeOfHeaderVisualSection() + SizeOfContainerVisualSection();

		// Get a visual child
		// NOTE: This method is much slower than getting elements from the valid ranges, because this is going to access the collection of visual children.
		// See GetVisualChild.

		// Trims sentinels from the ends of the collection
		// See TrimEdgeSentinels.

		// Returns whether a group is being reset.
		internal bool IsGroupBeingReset() => m_indexOfGroupBeingReset != -1;

		// Returns the index of the group being reset.
		internal int GetIndexOfGroupBeingReset()
		{
			global::System.Diagnostics.Debug.Assert(IsGroupBeingReset());
			return m_indexOfGroupBeingReset;
		}

		// Sets the index of the group being reset.
		internal void SetIndexOfGroupBeingReset(int index)
		{
			global::System.Diagnostics.Debug.Assert(index >= 0);
			m_indexOfGroupBeingReset = index;
		}

		// Invalidates the index of group being reset so that IsGroupBeingReset
		// returns false.
		internal void InvalidateIndexOfGroupBeingReset() => m_indexOfGroupBeingReset = -1;

		// Helper to remove a realized element from the visual children
		// See RemoveFromVisualChildren.

		// Helper to add a realized element to the visual children
		// See AddToVisualChildren.

		// Perform cleanup once element is unpinned and outside of valid window.
		// See ElementUnpinned.

		// Helper to trim one type of sentinel
		// See TrimEdgeSentinels(ElementType).

		// The valid range of containers and headers we use to maintain easy index-to-element lookups
		// NOTE: This collection may include sentinels and other hidden elements which won't participate in layout
		//       and won't ever be added to the visible children.
		private readonly Deque<WeakReference<UIElement>?>[] m_validElements = { new(), new() };

		// perf optimization: last found indices
		private readonly int[] m_lastFoundIndexElements = new int[ElementType_Count];

		// Maps each child element to their index in the item collection
		// Should always be the same size as the corresponding region in the children collection
		// We keep this map around to easily and quickly map elements to the chidlren collection
		// because the valid range might have "holes" in it due to sentinels and hidden elements
		private readonly Deque<int>[] m_elementIndicesForChildren = { new(), new() };

		// Index of first item/group represented by the beginning of the valid range
		private readonly int[] m_indicesOfFirstValidElement = new int[ElementType_Count];

		// storage for focused container, being focused counts as being pinned, even though we might not
		// have been put into the pinnedcontainers vector
		private PinnedElementInfo m_focusedContainer = new();
		private PinnedElementInfo m_focusedHeaderContainer = new();

		// backtalk
		private readonly ModernCollectionBasePanel m_owner;

		// pinned containers and headers
		private readonly List<PinnedElementInfo>[] m_pinnedElements = { new(), new() };

		// Temporary holding area for elements that are being prepared. PrepareContainerForItemOverride and its ilk
		// often call back into the mapping methods, so we need a place to hold the preparing container for the
		// lookups to succeed. I'm not using pinning/unpinning because they need to do cleanup that could interfere
		// with what we need.
		private struct PreparationHoldingArea
		{
			internal UIElement? m_element;
			internal int m_index;
			internal ElementType m_type;

			public PreparationHoldingArea() => Reset();

			internal void Reset()
			{
				m_element = null;
				m_index = -1;
				m_type = ElementType.ItemContainer;
			}

			internal readonly bool Valid() => m_index > -1;
		}

		private PreparationHoldingArea m_preparationHoldingArea = new();

		// We reset the group ourselves by deleting its items one by one.
		// We store its index because the cache manager doesn't have any information on the group.
		private int m_indexOfGroupBeingReset;

		// when a group is added, the order of events is:
		// 1. group gets added (notifyGroupChange)
		// 2. items get inserted in descending order
		// This is a very strange way of filling up groups and items, but it is what we have to work with.
		// This ties us into GroupedDataCollection's implementation
		// So, when a group is added, we create all the containers that are needed, put their indices in this vector
		// to remember and then _expect_ to get called with inserts. If we get an insert of one of the indices listed
		// in this vector, we will know how to treat it.
		// This vector should only exist temporarily while processing through a group and can be asserted to be null otherwise
		internal List<int> m_unlinkedFillers = new();
	}

	internal partial class DeferredModernPanelUnlinkActionPayload : DeferredUnlinkActionPayload
	{
		private ModernCollectionBasePanel? m_tpOwner;
	}

	// Iterates over a grouped or flat collection.
	// Preconditions: the collection must not be empty and must not change while iterating.
	internal partial struct CollectionIterator
	{
		internal struct ElementInfo
		{
			// No need to initialize the fields as it would be redundant
			// because CollectionInterator makes use of all of them.

			// Item and group information.
			internal int itemIndex;
			internal int groupIndex;
			internal int startItemIndex;
			internal int itemCountInGroup;
			internal ElementType type;

			// Is this a header?
			internal readonly bool isHeader() => type == ElementType.GroupHeader;

			internal readonly int IndexInGroup() => itemIndex - startItemIndex;
		}

		// Initializes the iterator at the beginning.
		// See CollectionIterator(CacheManager).

		// Initializes the iterator at a given location in the collection.
		// See CollectionIterator(CacheManager, int, ElementType).

		// Used to initializes or re-initializes the iterator.
		// See Init.

		// Try moving to the next element. Retruns true if it succeeds.
		// See MoveNext.

		// Try moving to the previous element. Retruns true if it succeeds.
		// See MovePrevious.

		// Returns the current element the iterator is pointing at.
		internal readonly ElementInfo GetCurrent() => m_current;

		// Uses the group index to update group information.
		private void UpdateCurrentGroupInformation()
		{
			m_cache.GetGroupInformationFromGroupIndex(
				m_current.groupIndex,
				out m_current.startItemIndex,
				out m_current.itemCountInGroup);

			m_current.itemIndex = m_current.startItemIndex;
		}

		private ElementInfo m_current;
		private readonly CacheManager m_cache;
	}

	// Actual implementation of ILayoutDataInfoProvider.

	// Implementation of ILayoutDataInfoProvider, that was also required in ListViewBaseHeaderItemAutomationPeer for SizeOfCount.
	// See GetTotalGroupCount.

	// Implementation of IICG2
	// All of these deal with real Items(Source) indexes.

	// Creates or recycles a container for the given index.
	// See GenerateContainerAtIndex.

	// Creates or recycles a header for the given index.
	// See GenerateHeaderAtGroupIndex.

	// Creates or recycles an element for the given type and index
	internal UIElement? GenerateElementAtDataIndex(ElementType type, int index)
	{
		switch (type)
		{
			case ElementType.GroupHeader:
				return GenerateHeaderAtGroupIndex(index);

			case ElementType.ItemContainer:
				return GenerateContainerAtIndex(index);

			default:
				global::System.Diagnostics.Debug.Assert(false, "Invalid enum value");
				throw new InvalidOperationException();
		}
	}

	// Is our container recycle queue empty?
	// See GetContainerRecycleQueueEmpty.

	// Call UnlinkContainerFromItem and ClearContainerForItem on the given container. If the item isn't
	// it's own container, return the container to the recycle queue.
	// For pinned items, this is a no-op.
	// See RecycleContainer.

	// Call UnlinkContainerFromItem and ClearContainerForItem on the given container. If the item isn't
	// it's own container, return the container to the recycle queue.
	// For pinned items, this is a no-op.
	// See TryRecycleContainer.

	// Calls RecycleContainer on all realized containers.
	// See RecycleAllContainers.

	// Is our header recycle queue empty?
	// See GetHeaderRecycleQueueEmpty.

	// Call UnlinkHeaderFromGroup and ClearContainerForGroup on the given header and return it to
	// the recycle queue (except for pinned headers, for which this is a no-op).
	// See RecycleHeader.

	// Calls RecycleHeaderImpl on all realized headers.
	// See RecycleAllHeaders.

	// Gets a candidate compatible with the item at the given index
	// See FindRecyclingCandidate.

	// Get the queue length
	// See GetQueueLength.

	// ICG related

	// Remove a container from queue. In most cases, it should be located at the end of it but we handle all cases
	// See RemoveFromQueue.

	// Remove a header from the header recycle queue.
	// See RemoveFromHeaderRecycleQueue.

	// Associates the item to the container.
	// See LinkContainerToItem.

	// Clears the item from the container.
	// See UnlinkContainerFromItem.

	// Associates the group to the header.
	// See LinkHeaderToGroup.

	// Clears the group from the header.
	// See UnlinkHeaderFromGroup.

	// Helper methods.

	// Prepare the container for recycling by unlinking and clearing, then recycle the container.
	// (recycling means different things depending on if the container is generated or not).
	// Generated containers are left in the tree and reused for another item, while
	// nongenerated containers are removed from the tree entirely (and will be re-added when the
	// virtualization window is appropriate for them).
	// Linked containers are containers which have had LinkContainerToItem and PrepareContainerForItem
	// called on them.
	// See RecycleLinkedContainer.

	// Callback for finishing unlinking from DeferredModernPanelUnlinkActionPayload when layout transition is complete.
	// Transition completion routine should use unloaded queue to avoid adding elements to the visual tree after the Layout completes.
	// See FinishUnlinkingContainer.

	// todo: should be moved into ICG2 interface

	// responds to a group change. Hooked up internally
	// See NotifyOfGroupsChanged.

	// Returns the first visible element's type, offset relative to the top and UIElement.
	// See GetFirstVisibleElementForSerialization.

	// Is the panel in a state where we cannot do a layout pass on it?
	internal bool IsLockedForLayout()
	{
		// 1. we are doing a collection change or
		// 2. we are in 'group filling' mode: when a group is added, we have to be prepared
		// to catch the following item inserts in OnItemAdded. We should _never_ be in this mode
		// during regular measure
		return m_inCollectionChange ||
			m_containerManager.m_unlinkedFillers.Count != 0;
	}

	internal bool CanScrollIntoView()
	{
		return !IsLockedForLayout() &&
			// If layout is in progress (i.e. during measure) and we have an
			// active window command, we can't scroll into view.
			// That's because we can't flush the current command during layout.
			// (e.g. calling UpdateLayout during layout is a no-op).
			!(m_layoutInProgress && (m_windowState.m_command is not null || m_containerManager.m_isScrollIntoViewInProgress));
	}

	// Item added in the collection
	// See OnItemAdded.

	// Item removed in the collection
	// See OnItemRemoved.

	// Group added to the collection.
	// See OnGroupAdded.

	// Group removed in the collection
	// See OnGroupRemoved.

	// Group reset in the collection
	// See OnGroupReset.

	// Data element replaced
	// See OnDataElementReplaced.

	// Collection refreshed
	// See OnCollectionReset.

	// Delegate the preparation of an element to the items host.
	// See PrepareContainerViaItemsHost, PrepareHeaderViaItemsHost.

	private void PrepareElementViaItemsHost(
		ElementType type,
		int indexInDataCollection,
		Size measureSize,
		UIElement spElement)
	{
		switch (type)
		{
			case ElementType.GroupHeader:
				PrepareHeaderViaItemsHost(indexInDataCollection, measureSize, spElement);
				break;

			case ElementType.ItemContainer:
				PrepareContainerViaItemsHost(indexInDataCollection, measureSize, spElement);
				break;

			default:
				global::System.Diagnostics.Debug.Assert(false, "Invalid enum value");
				throw new InvalidOperationException();
		}
	}

	// When groups are inserted, we get subsequent item insertions, but they arrive in reverse order!
	// This could be ugly if we don't correct for it.
	// For instance, if we are looking at group 0, items 0-10, and a new group comes in with 10 items,
	// we'll see a group header come in, but then we'll see an insertion at index 10, which will look to us
	// like we're inserting something right before the old index 10. Things will get messed up.
	// See CreateUnlinkedFillersForGroup.

	// A collection change is in progress
	private bool m_inCollectionChange;
	// layout is in progress
	private bool m_layoutInProgress;
	private bool m_refreshPendingLayout;

	// Interactions

	// IKeyboardNavigationPanel implementation.
	// See SupportsKeyNavigationAction, GetTargetIndexFromNavigationAction.

	// IKeyboardHeaderNavigationPanel implementation.
	// See GetTargetHeaderIndexFromNavigationAction.

	// Sentinel values for indices
	internal const int s_cNavigationIndexFirst = 0;
	internal const int s_cNavigationIndexLast = int.MaxValue;
	internal const int s_cNavigationIndexNone = -1;

	// IChildTransitionContextProvider implementation
	// See GetChildTransitionContext.

	// determines if mutations are going fast
	// See IsCollectionMutatingFast.

	// IScrollSnapPointsInfo implementation
	// See AreHorizontalSnapPointsRegular, AreVerticalSnapPointsRegular, GetIrregularSnapPoints, GetRegularSnapPoints, UpdateSnapPoints.

	internal partial class TransitionContextManager
	{
		internal void RegisterForIncrementalLoading()
		{
			// if we already load then ignore incremental loading.
			if (!m_isLoadingStarted)
			{
				m_shouldIncrementallyLoad = true;
			}
		}

		// Any context related operation must be guarded with this call.
		// It will reset all context counters if we get new tick.
		// If the tick haven't changed then we keep increasing operation counters
		// See UpdateContextCounters.

		// backtalk
		private readonly ModernCollectionBasePanel m_owner;

		// special value to workaround the async nature of the loaded event.
		// layoutticks are 16 bits wide, so 32_max will never be reached.
		private const int loadCounterToken = int.MaxValue;

		// Used by ThemeTransitionContext to compute Added elements count
		private uint m_elementCountAddedThisLayoutTick;

		// Used by ThemeTransitionContext to compute Removed elements count
		private uint m_elementCountRemovedThisLayoutTick;

		// Keep current counter
		private int m_currentTickCounterId;

		// Used by ThemeTransitionContext to save time when we do a mutation
		private uint m_currentMutationTime;
		private uint m_previousMutationTime;

		// Used by ThemeTransitionContext for loading period
		private bool m_isLoading;

		// Used by ThemeTransitionContext for incremental loading Entrance period
		private bool m_shouldIncrementallyLoad;

		// Used by ThemeTransitionContext to save the fact that we start loading
		private bool m_isLoadingStarted;

		// Used by ThemeTransitionContext to save time when we start loading
		private uint m_startIncrementallyLoadingTime;

		// Used by ThemeTransitionContext to find whether context is changed or not
		private bool m_resetItemsThisLayoutTick;

		// Used by ThemeTransitionContext to compute Reordered elements count
		private int m_elementCountReorderedThisLayoutTick;

		// How long we should show entrance animation.
		private const uint incrementalLoadingDuration = 500;

		// The threshold within which we mark collection changes as going fast
		private const uint fastmutationThreshold = 1000;
	}

	// calculation of panning direction
	// See PanningDirectionBase.

	// window state related
	// See ProcessOrientationChange, OnPropertyChanged2.

	// Spatial lookup helper methods

	// Given an index, calculate the index of the item one page from it.
	// WARNING: This method will potentially realize containers to figure
	// things out. Call with caution.
	// See GetTargetIndexFromNavigationActionByPage.

	// Calculates the next group header index for a given selection index and item navigation action.
	// See GetTargetHeaderIndexFromNavigationActionByItem.

	// Calculates the prior visible group header index from the provided index.
	// See GetPreviousHeaderIndex.

	// Calculates the last visible index, from either direction.
	// This is usually the last fully visible index, but if
	// there is no fully visible index, this returns the last
	// partly visible index.
	// fromLowerIndex - direction the search starts from. I.E., with a horizontal orientation,
	// fromLowerIndex = TRUE means the rightmost visible container.
	// See GetLastVisibleContainer.

	// Scroll by one visible viewport length,
	// forcing virtualization to run.
	// toIncreasingIndex - Governs the direction of scroll.
	// See SynchronousScrollByPage.

	// Finds an arbitrary realized item from within the group under the given point.
	// Non-grouped scenarios are treated as one large group.
	// See FindValidContainerInGroupForPoint.

	// Implementation of IItemLookupPanel

	// Get the closest element information to the point.
	// See GetClosestElementInfo.

	// Get the index where an item should be inserted if it were dropped at
	// the given position. This will be used by live reordering.
	// See GetInsertionIndex.

	// Gets a series of BOOLEAN values indicating whether a given index is
	// positioned on the leftmost, topmost, rightmost, or bottommost
	// edges of the layout.  This can be useful for both determining whether
	// to tilt items at the edges of rows or columns as well as providing
	// data for portal animations.
	// See IsLayoutBoundary.

	// Gets the bounds of the items.
	// See GetItemsBounds.

	// Gets the cache length.
	// Sets the cache length.
	// See CacheLengthBase.

	// Implementation of IInsertionPanel

	// Get the indexes where an item should be inserted if it were dropped at
	// the given position
	// See GetInsertionIndexes.

	// Implementation of IPaginatedPanel

	internal void GetGroupInformationFromGroupIndex(int groupIndex, out int startItemIndex, out int itemCountInGroup)
		=> m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out startItemIndex, out itemCountInGroup);

	internal void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)
		=> m_cacheManager.GetGroupInformationFromItemIndex(itemIndex, out indexOfGroup, out indexInsideGroup, out itemCountInGroup);

	// Virtual helper method to get the ItemsPerPage that can be overridden by derived classes.
	// See GetItemsPerPageImpl.

	// Implementation of IICM and IGHM

	private readonly TransitionContextManager m_transitionContextManager;
	private readonly CacheManager m_cacheManager;
	private readonly ContainerManager m_containerManager;
	private ILayoutStrategy m_spLayoutStrategy = null!;
	private IObservableVector<object>? m_tpObservableItemsSource;
	private readonly SerialDisposable m_observableItemsSourceChangedToken = new();

	// Window management concepts
	internal partial class WindowState
	{
		// Window commands set pNewVisibleWindow only when they want a view override, so callers seed it with an empty rect.
		internal delegate void WindowCommand(ref Rect pNewVisibleWindow);

		// Gets the currently known visible window. This is either the window "coerced" from a scroll request
		// like ScrollIntoView, if a request exists, or just the window calculated from the ScrollViewer otherwise.
		// See GetVisibleWindow.

		// Gets the realization window which was calculated from the current visible window
		internal Rect GetRealizationWindow() => m_currentRealizationWindow;
		internal void SetRealizationWindow(Rect arg) => m_currentRealizationWindow = arg;

		// Set the last known visible window as determined from the scroll state
		// Returns a BOOLEAN if this new scroll state differs from our current visible window
		// See SetScrollStateVisibleWindow(Rect, out bool).

		internal void SetScrollStateVisibleWindow(Rect newWindowFromScrollState) => SetScrollStateVisibleWindow(newWindowFromScrollState, out _);

		// Overrides/coerces the locally stored visible window for performing layout
		// If this window differs from the last window calculated from the ScrollViewer, we'll
		// send a new scroll offset to the ScrollViewer at the end of our layout.
		internal void SetCoercedVisibleWindow(Rect newCoercedWindow) => m_visibleWindowCoerced = newCoercedWindow;

		// Adjust the visible window if this was a mouse large increase or decrease click.
		// Note that we need to do this adjustment after the arrange, now that we have the
		// accurate visible window to calculate how much of the sticky header is occluding
		// the visible window.
		// See CorrectVisibleWindowForMouseLargeClick.

		internal Rect GetCoercedVisibleWindow() => m_visibleWindowCoerced;

		// Returns the current adjusment resulting for the possible CoercedVisibleWindow
		// Note : protected by apiset, never called on desktop, called only for Vertical Orientation on Phone
		// See GetCurrentAdjustmentY.

		// we can calculate our window on the fly, but in the case we were limited in some dimension by measure, we should
		// know that always
		internal Size lastAvailableSize;

		// Flag indicating whether the window is valid (ie, we were able to get correct width/height)
		internal bool validWindowCalculation;

		// Flag indicating we are done building up our cache
		internal bool cachePotentialReached;

		// Apply a correction to our window states
		// See ApplyAdjustment.

		// The last processed scroll offset
		internal Point m_lastScrollOffset;

		// The last processed zoom factor
		internal float m_lastZoomFactor;

		// The last scroll view change was caused by clicking on
		// the scroll bar's LargeIncrease or LargeDecrease
		// buttons. We need special handling for this specific case when
		// sticky headers are used to adjust the visible window since part
		// of the visible window is occluded by the sticky header
		internal bool m_lastScrollWasMouseLargeClick;

		// was the last scroll forward or backward
		internal bool m_lastScrollWasForward;

		// Window commands to override default window detection, e.g., ScrollIntoView
		// If pNewVisibleWindow is set to the empty rectangle, no view override
		// will be done.
		internal WindowCommand? m_command;

		// updated in arrange, used to determine panning direction
		internal int currentTickNumber;
		// updated in arrange, if currenttick is different
		internal int firstVisibleItemIndexOfPreviousTick;
		// updated in arrange
		internal PanelScrollingDirection m_panningDirectionBase;

		// The current amount of cache, in pixels, that will be applied on each side of the visible window
		// when calculating the realization window.
		// currentCacheBufferPerSide increases asynchronously at a lower priority than preparing the containers
		// that are in the visible window, and caps at (m_cacheLength * window length in virtualization direction) / 2.
		internal double currentCacheBufferPerSide;

		// This flag is set to true when scrolling so that if we notice that we (over/under)estimated the offset of our elements
		// during the virtualization pass, we shift both elements and the window to correct for the estimation error.
		// We don't shift the window if we are correcting due to a size or collection change.
		internal bool allowWindowShiftOnEstimationErrorCorrection;

		// We did something that might cause us to estimate incorrectly. Set this
		// so that the next measure corrects for estimation errors.
		// To scope the change, we are only re-estimating based on disconnected scrollviews. We considered basing the
		// correction purely on the delta in average container size - but that could case corrections to kick off very
		// often - possibly making thumb drag experience worse.
		internal bool m_estimationCorrectionPending;

		// Current window for us to fill with containers, including buffers
		private Rect m_currentRealizationWindow;

		// The ScrollViewer's viewport, projected onto our panel
		// Distinct from realization window for multiple reasons. For instance, realized buffered containers
		// can be immediately recycled, but visible containers need to perform a content transition. Also, page up/down
		// should scroll by the size of the visual set, not the realized set of containers.
		private Rect m_visibleWindowFromScrollState;
		private Rect m_visibleWindowCoerced;
	}

	// Viewport behavior settings.
	internal struct ViewportBehaviorInfo
	{
		// Default constructor
		public ViewportBehaviorInfo()
		{
			Reset();
#if IUSM_DEBUG
			ResetLastTracedDbg();
#endif
		}

		// Cancels tracking.
		internal void Reset()
		{
			Rect emptyRect = default;

			isTracking = false;
			isTrackingExtentEnd = false;
			originalExtent = currentExtent = 0.0f;
			elementBounds = emptyRect;
			type = ElementType.ItemContainer;
			index = -1;
			elementOffset = 0.0f;
			initialViewportEdge = 0.0f;
			isOffsetRelativeToSecondEdge = false;
			trackedElementShift = 0.0f;
			viewportOffsetDelta = 0.0f;
		}

#if IUSM_DEBUG
		internal void ResetLastTracedDbg()
		{
			lastTracedIsTrackingDbg = false;
			lastTracedIsTrackingExtentEndDbg = false;
			lastTracedIsOffsetRelativeToSecondEdgeDbg = false;
			lastTracedIndexDbg = -1;
			lastTracedElementOffsetDbg = 0.0f;
			lastTracedTrackedElementShiftDbg = 0.0f;
			lastTracedViewportOffsetDeltaDbg = 0.0f;
		}

		internal void TraceChangesDbg(string context)
		{
			bool isTrackingChanged = isTracking != lastTracedIsTrackingDbg;
			bool isTrackingExtentEndChanged = isTrackingExtentEnd != lastTracedIsTrackingExtentEndDbg;
			bool isOffsetRelativeToSecondEdgeChanged = isOffsetRelativeToSecondEdge != lastTracedIsOffsetRelativeToSecondEdgeDbg;
			bool indexChanged = index != lastTracedIndexDbg;
			bool elementOffsetChanged = elementOffset != lastTracedElementOffsetDbg;
			bool trackedElementShiftChanged = trackedElementShift != lastTracedTrackedElementShiftDbg;
			bool viewportOffsetDeltaChanged = viewportOffsetDelta != lastTracedViewportOffsetDeltaDbg;

			if (isTrackingChanged ||
				isTrackingExtentEndChanged ||
				isOffsetRelativeToSecondEdgeChanged ||
				indexChanged ||
				elementOffsetChanged ||
				trackedElementShiftChanged ||
				viewportOffsetDeltaChanged)
			{
				global::System.Diagnostics.Debug.WriteLine($"IUSM_DEBUG: ViewportBehaviorInfo::TraceDeltaDbg. {context}");

				if (isTrackingChanged)
				{
					global::System.Diagnostics.Debug.WriteLine($" -isTracking: {isTracking}");
					lastTracedIsTrackingDbg = isTracking;
				}

				if (isTrackingExtentEndChanged)
				{
					global::System.Diagnostics.Debug.WriteLine($" -isTrackingExtentEnd: {isTrackingExtentEnd}");
					lastTracedIsTrackingExtentEndDbg = isTrackingExtentEnd;
				}

				if (isOffsetRelativeToSecondEdgeChanged)
				{
					global::System.Diagnostics.Debug.WriteLine($" -isOffsetRelativeToSecondEdge: {isOffsetRelativeToSecondEdge}");
					lastTracedIsOffsetRelativeToSecondEdgeDbg = isOffsetRelativeToSecondEdge;
				}

				if (indexChanged)
				{
					global::System.Diagnostics.Debug.WriteLine($" -index: {index}");
					lastTracedIndexDbg = index;
				}

				if (elementOffsetChanged)
				{
					global::System.Diagnostics.Debug.WriteLine($" -elementOffset: {elementOffset}");
					lastTracedElementOffsetDbg = elementOffset;
				}

				if (trackedElementShiftChanged)
				{
					global::System.Diagnostics.Debug.WriteLine($" -trackedElementShift: {trackedElementShift}");
					lastTracedTrackedElementShiftDbg = trackedElementShift;
				}

				if (viewportOffsetDeltaChanged)
				{
					global::System.Diagnostics.Debug.WriteLine($" -viewportOffsetDelta: {viewportOffsetDelta}");
					lastTracedViewportOffsetDeltaDbg = viewportOffsetDelta;
				}
			}
		}

		internal bool lastTracedIsTrackingDbg;
		internal bool lastTracedIsTrackingExtentEndDbg;
		internal bool lastTracedIsOffsetRelativeToSecondEdgeDbg;
		internal int lastTracedIndexDbg;
		internal float lastTracedElementOffsetDbg;
		internal float lastTracedTrackedElementShiftDbg;
		internal float lastTracedViewportOffsetDeltaDbg;
#endif

		// True when we are tracking a container or a header.
		internal bool isTracking;

		// True when KeepLastItemInView is set and we are at the very end
		// of the list when we start tracking. We need to make sure that after
		// collection changes (e.g. some new items might get added at the end),
		// measure changes or size changes, we are still at the very end of the list.
		internal bool isTrackingExtentEnd;
		// Stores the panel's extent when we started tracking the end of it (isTrackingExtentEnd == true).
		internal float originalExtent;
		// Stores the most up to date extent while we are tracking the end of it (isTrackingExtentEnd == true).
		internal float currentExtent;

		// Bounds of the tracked element.
		// They get updated when there are changes before it, e.g. element (header or container) inserted before.
		internal Rect elementBounds;
		// Is the tracked element a header or a container?
		internal ElementType type;
		// The absolute index of the tracked element in the source collection.
		internal int index;
		// Offset of the viewport's top/left or bottom/right when the tracking started.
		internal float initialViewportEdge;
		// Offset between the viewport and the tracked element.
		// Example:
		// isOffsetRelativeToSecondEdge==False, ScrollViewer.VerticalOffset==1000,
		// tracked element position in ScrollViewer.Content==900, implies elementOffset==100.
		internal float elementOffset;
		// Is the offset calculated based on the first or second edge?
		internal bool isOffsetRelativeToSecondEdge;
		// During the Generate phase (which takes place during the measure pass),
		// we shift all the elements by the amount the tracked element moved.
		// We need to store that delta in case we need to elect a new tracked
		// element during Generate. This happens only when we track a group header
		// whose group was cleared and HidesIfEmpty equals true.
		internal float trackedElementShift;

		// If we start tracking outside layout (ie due to a collection change), we run the
		// risk that, by the time we get to arrange, the viewport offset we used when we started
		// tracking is stale (out of date) if the collection change happened while the viewport is
		// moving. viewportOffsetDelta is the amount of pixels we need to compensate for the viewport
		// drift.
		internal float viewportOffsetDelta;
	}

	// When called the first time, this method starts tracking the first
	// visible element. Otherwise, it does nothing.
	// See BeginTrackingFirstVisibleElement(bool).

	// Starts tracking the first visible element if it's not already the case.
	// If we already tracking something, it updates the tracked element so
	// we can deal with its removal or replacement.
	// See BeginTrackingFirstVisibleElement(bool, bool, int, CollectionChange).

	// Wrapper that calls BeginTrackingFirstVisibleElement when a collection change happens.
	// See BeginTrackingOnCollectionChange.

	// Wrapper that calls BeginTrackingFirstVisibleElement when an element changes size.
	// See BeginTrackingOnMeasureChange.

	// Wrapper that calls BeginTrackingFirstVisibleElement when we detect a viewport size change.
	// See BeginTrackingOnViewportSizeChange.

	// Wrapper that calls BeginTrackingFirstVisibleElement but also changes the location of that element and the window
	// See BeginTrackingOnOrientationChange.

	// Called during MeasureExistingElements to get the realized window accounting for the viewport shift.
	// This make sure ShouldContinueFillingUpSpace behaves correctly.
	// See GetRealizationWindowAfterViewportShift.

	// Called during the Generate pass to shift all the elements, including the
	// tracked element, to the new realization window.
	// See ApplyTrackedElementShift.

	// Returns whether a change happened before the tracked element.
	// See IsChangeBeforeTrackedElement.

	// If ItemsUpdatingScrollMode is set to KeepLastElementInView and the viewport is at the very end
	// of the list, we need to shift the viewport to the last element which might have been recently inserted.
	// See TrackLastElement.

	// Shifts the viewport to the new location of the first visible element.
	// See EndTrackingFirstVisibleElement.

	// Adjust layout transitions to account for viewport shift.
	// See AdjustLayoutTransitions, AdjustLayoutTransitionsForElement.

	// Returns the first visible element's type, index and UIElement.
	// See GetFirstVisibleElement.

	// Returns the first visible element's type, index and UIElement
	// in a given window.
	// See GetFirstElementInWindow.

	// Sets the offset between the tracked element and the visible window.
	// See SetTrackedElementOffsetRelativeToViewport.

	// Returns the tracked UIElement.
	// See GetTrackedElement.

	// When the tracked element gets removed, we need to elect a new one.
	// See ElectNewTrackedElement.

	// Returns where should the viewport move to 'follow' the tracked element.
	// The edge is relative to whether we are tracking the first element (top/left)
	// or the last element (bottom/right).
	// See GetNewViewportEdge.

	// Determine if a point is inside the window, or is before or after it in the virtualizing direction.
	// See GetReferenceDirectionFromWindow.

	// Calculates the visible window by analyzing our layout and our parent ScrollViewer's state.
	// See CalculateVisibleWindowFromScrollPoint.

	// Calculates the visible window from a set of scroll data
	// See CalculateVisibleWindowFromScrollData.

	// Before we get any ScrollViewChanging or ScrollViewChanged events, we need to proactively get a window
	// (instead of waiting for an event to tell us of a window).
	// This method gets a window from the ScrollViewer's offset, if possible. If a ScrollViewer isn't
	// available, we go with a default window.
	// See PrimeVisibleWindow.

	// Using the current visible window, calculate the realization window (taking into
	// account buffer space, etc).
	//
	// allowCache should be true during normal measure passes and false otherwise when initially setting the realization window.
	// The allowCache parameter allows us to incrementally build a cache by inflating the realization window during
	// measure passes.  When true, we will increase the realization window by CacheBufferPerSideInflationPixelDelta.
	// When false, we will reset the cache buffers to 0.
	// See SetRealizationWindowFromVisibleWindow.

	// Recycles alls containers (and headers if grouping) and removes them from the cache manager.
	// See RecycleAllContainersAndHeaders.

	// Given a window to fill with containers and headers, estimate
	// the location of the anchor container and header (if any),
	// call into ICG2 to get the container and header, set both of
	// their bounds, and add to valid containers and headers.
	// See GenerateAnchorsForWindow.

	// Given a group index and its reference, estimate its position,
	// call into ICG2 to get a header for the group, place the header
	// in the valid headers, and sets the header's bounds to the estimated
	// position.
	// See GenerateAnchorForGroup.

	// Given an item index and its references, estimate its position,
	// call into ICG2 to get a container, place the container
	// in the valid containers, and sets the container's bounds to the estimated
	// position.
	// See GenerateAnchorForItem.

	// window state

	// Triggers a synchronous layout if a command exists.
	// See FlushWindowStateCommand.

	// Estimates are inevitably wrong, so... when they are wrong, we need to adjust our containers' placement
	// on the panel so users can scroll exactly to the first/last elements, no more, no less. So, when we start
	// getting close to the edge, we need to see if we are wrong and perform the shift.
	// See CorrectForEstimationErrors.

	// This method actually performs the heavy lifting of ScrollItemIntoView.
	// See DoScrollItemIntoView.

	// This method actually performs the heavy lifting of ScrollGroupHeaderIntoView.
	// See DoScrollGroupHeaderIntoView.

	// Get or generate the container for the given index and set that
	// container as the focused container.
	// See GenerateAnchorAndSetFocus.

	// This method actually performs the heavy lifting of doing an orientation switch.
	// See DoProcessOrientation.

	// Estimate the extent of our panel
	// Since the last realized header and container represent the extent realized so far, we can
	// get more accurate results by estimating the remaining unrealized extent and tacking it on to the end of the realized extent
	// See EstimatePanelExtent.

	// Scrolls our ScrollViewer to the given area (synchronous).
	// See SetScrollViewerOffsetsTo.

	// Helper method to place a window around an element's rect
	// See PlaceWindowAroundElementRect.

	// builds up a referencestruct about an anchor that can be used to base generation on
	// See GetGenerationAnchorInformation.

	// Walk up the parent chain and find our ScrollViewer.
	// Attach its OnScrollViewChanging and ScrollViewChanged to our OnScrollViewChange method.
	// Also attach to our ItemsPresenter.
	// See AttachPanelComponents.

	// Clear our understanding of our parent ScrollViewer and ItemsPresenter.
	// See DetachPanelComponents.

	// Called when our ScrollViewer's view changes. Update our current visible window.
	// See OnScrollViewChanging.

	// Called when our ScrollViewer's has changed. Update our current visible window.
	// See OnScrollViewChanged.

	// Called when our ScrollViewer's size has changed. Re-evaluate our current visible window.
	// Cannot use measure available size because we might have changed in the virtualizing direction.
	// See OnScrollViewerSizeChanged.

	// Event handlers.
	// See OnPanelUnloaded, OnGroupStyleChanged.

	// GroupStyle helpers
	internal GroupStyle? GetGroupStyle() => m_tpGroupStyle;

	// Get a new GroupStyle from the items host
	// See UpdateGroupStyle.

	// Miscellaneous helper methods.

	private static VirtualizationInformation? GetVirtualizationInformationFromElement(UIElement spElement) => spElement.GetVirtualizationInformationOrNull();

	private static bool ElementHasVirtualizationInformation(UIElement spElement) => spElement.GetVirtualizationInformationOrNull() is not null;

	private static Rect GetBoundsFromElement(UIElement spElement) => spElement.GetVirtualizationInformationOrNull()!.Bounds;

	private static void SetElementIsRealized(UIElement spElement, bool isRealized) => spElement.GetVirtualizationInformationOrNull()!.IsRealized = isRealized;

	private static object? GetItemFromElement(UIElement spElement) => spElement.GetVirtualizationInformationOrNull()!.Item;

	private static void SetItemForElement(UIElement spElement, object? spItem) => spElement.GetVirtualizationInformationOrNull()!.Item = spItem;

	private static void SetElementIsGenerated(UIElement spElement, bool isGenerated) => spElement.GetVirtualizationInformationOrNull()!.IsGeneratedContainer = isGenerated;

	private static bool GetElementIsGenerated(UIElement spElement) => spElement.GetVirtualizationInformationOrNull()!.IsGeneratedContainer;

	private static void SetElementIsHeader(UIElement spElement, bool isHeader) => spElement.GetVirtualizationInformationOrNull()!.IsHeader = isHeader;

	private static bool GetElementIsHeader(UIElement spElement) => spElement.GetVirtualizationInformationOrNull()!.IsHeader;

	private static bool GetElementIsSentinel(UIElement? spElement) => spElement is null;

	private static void SetElementWantsToSkipContainerPreparation(UIElement spElement, bool shouldSkipContainerPreparation) => spElement.GetVirtualizationInformationOrNull()!.WantsToSkipContainerPreparation = shouldSkipContainerPreparation;

	private static void SetElementIsPrepared(UIElement spElement, bool isPrepared) => spElement.GetVirtualizationInformationOrNull()!.IsPrepared = isPrepared;

	private static bool GetElementIsPrepared(UIElement spElement) => spElement.GetVirtualizationInformationOrNull()!.IsPrepared;

	private static void SetIsContainerFromTemplateRoot(UIElement spElement, bool isFromRoot) => spElement.GetVirtualizationInformationOrNull()!.IsContainerFromTemplateRoot = isFromRoot;

	private static bool GetIsContainerFromTemplateRoot(UIElement spElement) => spElement.GetVirtualizationInformationOrNull()!.IsContainerFromTemplateRoot;

	private void RegisterSpecialElementSize(ElementType type, int index, Size desiredSize)
	{
		switch (type)
		{
			case ElementType.GroupHeader:
				RegisterSpecialHeaderSize(index, desiredSize);
				break;

			case ElementType.ItemContainer:
				RegisterSpecialContainerSize(index, desiredSize);
				break;

			default:
				global::System.Diagnostics.Debug.Assert(false, "Invalid enum value");
				throw new InvalidOperationException();
		}
	}

	private static Point GetOffScreenPosition() => new Point(-15000, -15000);

	// Struct component helper methods
	// See PointFromPointInVirtualizingDirection, PointFromSizeInVirtualizingDirection, SizeInVirtualizingDirection, SizeInNonVirtualizingDirection,
	// PointFromRectInVirtualizingDirection, PointFromRectInNonVirtualizingDirection, SizeFromRectInVirtualizingDirection.

	// Output data desired by ETW tracing
	// See TraceVirtualizationData.

	// Just returns E_FAIL inside an IFC_RETURN to help us
	// capture the current callstack in the error context
	private void ThrowEFAIL() => throw new InvalidOperationException();

	private void TraceGuardFailure(string failureReason)
	{
		global::System.Diagnostics.Debug.Assert(false);
#if !HAS_UNO // TODO Uno: no telemetry provider.
		TraceLoggingWrite(
			g_hTraceProvider,
			"ModernPanelGuard",
			TraceLoggingWideString(failureReason, "FailureReason"),
			TelemetryPrivacyDataTag(PDT_ProductAndServicePerformance),
			TraceLoggingKeyword(MICROSOFT_KEYWORD_TELEMETRY));
#endif

		// We cannot crash the app but we can store the callstack
		// in the stowed exceptions as a breadcrumb to help us debug
		// if this guard failure ultimately led to a crash
		try
		{
			ThrowEFAIL();
		}
		catch (InvalidOperationException)
		{
		}
	}

	// Creates a default layout reference.
	// See CreateDefaultLayoutReference.

	// Creates a default (and invalid) estimation reference.
	// See CreateDefaultEstimationReference.

	// Sticky Headers
	// Configure or update the sticky behavior of a header
	// See ConfigureStickyHeader.

	// Update all sticky header behaviors
	// See UpdateStickyHeaders.

	// Update the clipping region for items under sticky headers
	// See UpdateItemClippingForStickyHeaders.

	// Remove sticky header transformation
	// See RemoveStickyHeader.

	// Remove all sticky headers transformations
	// See RemoveStickyHeaders.

	// Computes the vertical position of a sticky header
	// See GetStickyHeaderPositionY.

	// compute the extent of the real header (as in not sticky) that is outside the visible window.
	// See GetRealHeaderExtentOutsideVisibleWindow.

	// Computes the offsets resulting from Sticky Headers
	// See CoerceStickyHeaderOffsets.

	// Provide the padding to the viewport in vertical direction - this occurs only when
	// there is a sticky header.
	// See GetVerticalViewportPadding.

	// Gets the default cache length
	// See GetDefaultCacheLength.

	// state

	// Resets the current cache buffer to 0 and invalidates measure.
	// See ResetCacheBuffers.

	// Stores the first visible element.
	// We need this method on orientation change to remember the first visible element in the visible window.
	// Then, when Generate() next runs (asynchronously) with the new orientation set and new realization window,
	// we can center the visible window around this element that we cached.
	// When switching orientations, we thus preserve the first element in the visible window.
	// Note that we do not preserve offset into the first visible element.
	// See CacheFirstVisibleElementBeforeOrientationChange.

	// Return the viewport size of our ScrollViewer.
	// See GetViewportSize.

	private protected ContainerManager GetContainerManager() => m_containerManager;

	// Basic fundamental state
	private readonly SerialDisposable m_epUnloadedHandler = new();

	// IICG2
	private IItemContainerGenerator2 m_icg2;   // just a pre-QI'd version of ourselves, to maintain the abstract facade we put up.

	// Queues of containers ready to link to items.
	private readonly List<WeakReference<UIElement>> m_recycleQueue = new();

	// List of elements which are being removed from the panel and are the target of an unload theme transition.
	private TrackerCollection<UIElement> m_unloadingElements;

	// Queues of elements which were unloaded after transitions completed.
	private TrackerCollection<UIElement> m_tpUnloadedElements;

	// Queues of headers ready to link to groups.
	private readonly List<WeakReference<UIElement>> m_recycleHeaderQueue = new();

	// Window management state
	private readonly WindowState m_windowState = new();

	// Our ScrollViewer.
	private WeakReference<ScrollViewer>? m_wrScrollViewer;

	// Our ItemsPresenter (if any).
	private WeakReference<ItemsPresenter>? m_wrItemsPresenter;

	// Viewport behavior management
	private ViewportBehaviorInfo m_viewportBehavior = new();

	// Event registrations on the ScrollViewer.
	private readonly SerialDisposable m_epScrollViewerViewChangingHandler = new();
	private readonly SerialDisposable m_epScrollViewerViewChangedHandler = new();
	private readonly SerialDisposable m_epScrollViewerSizeChangedHandler = new();

	// Note, need to invalidate sizes anytime we disconnect/anchor, and any time we change the collection
	// When we perform estimate corrections, however, we can just apply the same correction to the stored extent
	// we need to keep this for later anchoring when disconnected
	private Size m_estimatedSize;
	private int m_elementCountAtLastMeasure;

	// The GroupHeaderStrategy is stored on the layout strategy, but we need to store the
	// GroupHeaderPlacement here, so we can reevaluate the strategy if our orientation changes
	private GroupHeaderPlacement m_groupHeaderPlacement;

	// Manage GroupStyle state
	private GroupStyle? m_tpGroupStyle;
	private readonly SerialDisposable m_epGroupStyleChangedHandler = new();

	// Pixel delta by which to inflate the cache buffer on each side.  Rather than fill the entire
	// cache buffer all at once, we chunk the work to make the UI thread more responsive.  We inflate
	// the cache buffer from 0 to a max value determined by the CacheLength property.
	private const double CacheBufferPerSideInflationPixelDelta = 40;

	// only increase pixel delta if the time since last tick is small
	// the time in ms needed for this frame to be under in order to even consider increasing
	private const int PerformCacheInflationWhenTimeAvailable = 40;

	// Backing storage for ItemsWrapGrid.CacheLength and ItemsStackPanel.CacheLength properties.
	// Represents the amount of cache buffers in terms of viewports.
	private double m_cacheLength;

	// ItemsPresenter ensures that we have a nice origin before measure is called
	private Point m_originFromItemsPresenter;

	// Caches the first element in the visible window.
	private UIElement? m_tpFirstVisibleElementBeforeOrientationChange;

	// Caches the type of the first element in the visible window.
	private ElementType m_typeOfFirstVisibleElementBeforeOrientationChange;

	// Caches the index of the first element in the visible window.
	private int m_indexOfFirstVisibleElementBeforeOrientationChange;

	// SnapPoint state
	internal partial class SnapPointState
	{
		internal bool AreSnapPointsRegular(Orientation orientation) => m_orientation == orientation && m_isRegular;

		internal Orientation GetOrientation() => m_orientation;

		// Core state
		private bool m_snapPointsSet;
		private bool m_isRegular;
		private Orientation m_orientation;

		// Values
		private List<float> m_irregularSnapPointKeys = new();
		private float m_regularSpacing;
		private float m_nearOffset;
		private float m_farOffset;
	}

	private readonly SnapPointState m_snapPointState = new();

	private readonly List<Rect>[] m_arrangeRects = { new(), new() };

	private bool m_bLastItemRealized;
#if !HAS_UNO // TODO Uno: no DManip secondary content, sticky headers take the manual update path.
	private ISecondaryContentRelationshipStatics m_tpSecondaryContentRelationshipStatics;
#endif
	private bool m_bUseStickyHeaders;
	private double m_currentHeaderHeight;
	// Represents how much space sticky headers are taking away from the visible window (as of the last arrange).
	private float m_lastVisibleWindowClippingHeight;
#pragma warning disable CS0414 // Upstream only ever assigns it.
	private double m_cachedPannableExtent;
#pragma warning restore CS0414

	internal struct StickyHeaderInfo : IEquatable<StickyHeaderInfo>
	{
		internal double m_offset;
		internal double m_size;

		internal StickyHeaderInfo(double offset, double size)
		{
			m_offset = offset;
			m_size = size;
		}

		public readonly bool Equals(StickyHeaderInfo rhs) => m_offset == rhs.m_offset && m_size == rhs.m_size;
		public override readonly bool Equals(object? obj) => obj is StickyHeaderInfo rhs && Equals(rhs);
		public override readonly int GetHashCode() => HashCode.Combine(m_offset, m_size);
		public static bool operator ==(StickyHeaderInfo left, StickyHeaderInfo right) => left.Equals(right);
		public static bool operator !=(StickyHeaderInfo left, StickyHeaderInfo right) => !left.Equals(right);
	}

	private List<StickyHeaderInfo> m_currentStickyHeaderLocations = new();
	private List<StickyHeaderInfo> m_newStickyHeaderLocations = new();

	private CompositeTransform? m_tpItemClippingTransform;
#if !HAS_UNO // TODO Uno: no DManip secondary content, sticky headers take the manual update path.
	private SecondaryContentRelationship m_tpItemClippingRelationship;
#endif

	// Debug state.
#if DEBUG
	// We can temporarily enter an invalid state during the processing of item collection changes-
	// if we enter RunVirtualization somehow during this processing (probably via user code),
	// we'll have problems matching up indexes.
	// We may need to fix this mismatching issue in the future, but for now we have this field
	// to catch when it happens.
	private bool m_debugIsInTemporaryInvalidState;
#endif

	// Default value for the CacheLength property.  CacheLength defines the maximum amount of cache buffer.
	private const double WindowsDefaultCacheLength = 4.0;   // Total cache; not per-side
	private const double PhoneDefaultCacheLength = 2.0;   // Total cache; not per-side

	// DataTemplateSelector related constants
	// When no container in the queue currently has the DataTemplate we are looking for
	// if the number of containers in the queue is less than QueueLengthBeforeFallback,
	// we create a new container
	// if it is greater, then we select a container even if it means changing its template
	// These values should be tuned through performance tests
	private const uint QueueLengthBeforeFallback = 30;
	// When we are looking in valid containers outside the virtualization window which
	// have the DataTemplate we are looking for, we will have to recycle all containers
	// between the extremity and the container (if any) with the right template
	// and this can have an immediate cost greater than switching a template
	// ValidContainersBeforeFallback is the maximum number of containers we will examine
	// before creating a new one instead of trying to recycle
	private const uint ValidContainersBeforeFallback = 30;

	// Elements are arranged one after the other and they may overlay over a fine line.
	// For example, in a vertical list, if item 1 is arranged at location (0, 0) width a size
	// of (100, 50), then item 2 is arranged at location (0, 50). They overlap over line y = 50.
	// This impacts the calculation of the first and last visible elements, and we need this
	// small delta to remove the edge overlay.
	// Used to make elements bounds not overlay.
	private const float EdgeOverlayDisambiguationDelta = 0.01f;

	// Counts for tracking container request/generation for telemetry purposes
	private uint m_containerRequestedCount;
	private uint m_containerCreatedCount;

	// Provides information about the data source for the layout strategies.
	private ILayoutDataInfoProvider m_spLayoutDataInfoProvider;

	// Iterator used for CCC's incremental visualization.
	private ContainerContentChangingIterator? m_spContainerContentChangingIterator;

	// In order to avoid the cost of repeated and unnecessary allocations,
	// we'll allocate exactly one instance of ChoosingItemContainerEventArgs
	// and will then just reuse it every single time ChoosingItemContainer is raised.
	private ChoosingItemContainerEventArgs? m_trChoosingItemContainerEventArgs;

	// In order to avoid the cost of repeated and unnecessary allocations,
	// we'll allocate exactly one instance of ChoosingGroupHeaderContainerEventArgs
	// and will then just reuse it every single time ChoosingGroupHeaderContainer is raised.
	private ChoosingGroupHeaderContainerEventArgs? m_trChoosingGroupHeaderContainerEventArgs;
}
