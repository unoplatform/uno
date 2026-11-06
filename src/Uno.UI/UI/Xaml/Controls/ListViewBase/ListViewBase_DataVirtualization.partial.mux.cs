// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_DataVirtualization.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	// Called when the ScrollHost's scrolling offsets change.
	// Triggers a LoadMoreItems call if necessary.
	private void ProcessDataVirtualizationScrollOffsets()
	{
		var spItemsHost = ItemsPanelRoot;
		var spPaginatedPanel = spItemsHost as IPaginatedPanel;

		if (spPaginatedPanel is not null && m_tpScrollViewer is not null && !m_isLoadAsyncInProgress)
		{
			int lastItemInViewport = -1;

			// Get the index of the last item in the viewport.
			// If we're grouping this index will be the index to the group item therefore it must be compared against
			// group counts not item count
			lastItemInViewport = spPaginatedPanel.GetLastItemIndexInViewport(m_tpScrollViewer);

			if (m_scrollHostOffsetChangeAction == ScrollHostOffsetChangeAction.ScrollHostOffsetChangeAction_IncrementalEdgeTrigger)
			{
				uint itemsCount = 0;
				double pageLoadThreshold = 0;
				uint pagesFromEnd = 0;
				bool fIsGrouping = false;
				double itemsPerPage = 0;

				// Get the number of items per page.
				itemsPerPage = spPaginatedPanel.GetItemsPerPage(m_tpScrollViewer);

				// Determine if we're grouping to see what count of items we need to get
				fIsGrouping = IsGrouping;

				// Check if we are within the threshold for triggering an incremental load.
				if (fIsGrouping)
				{
					// Since we're grouping we need to get the number of groups
					var spCollectionView = GetItems() as ICollectionView;
					global::System.Diagnostics.Debug.Assert(spCollectionView is not null);
					if (spCollectionView is null)
					{
						throw new InvalidOperationException("The collection view is not available.");
					}

					var spGroups = spCollectionView.CollectionGroups;

					itemsCount = (uint)spGroups.Count;
				}
				else
				{
					// Get the total number of data items (different from realized UI containers).
					// Since we're not grouping then the count will be the number of items
					itemsCount = (uint)Items.Count;
				}

				// Get the increment load threshold specified in page units.
				pageLoadThreshold = IncrementalLoadingThreshold;

				if (itemsPerPage > 0)
				{
					// LastItemInViewport is obtained from the panel while itemsCount is from
					// the data source. It is possible that when items are deleted the data is
					// updated making the itemsCount smaller but the panel has not gotten the notifications
					// yet, so the lastItemInViewport is still old information. In that case you could
					// end up with lastItemInViewport being greater than the itemsCount. This can only
					// happen when you are towards the end. We want pagesFromEnd=0 for that case.
					if ((int)itemsCount >= lastItemInViewport + 1)
					{
						pagesFromEnd = (uint)((itemsCount - (uint)(lastItemInViewport + 1)) / itemsPerPage);
					}
				}

				if (pagesFromEnd <= pageLoadThreshold)
				{
					LoadMoreItemsOperation spLoadMoreItemsOperation = new();

					spLoadMoreItemsOperation.Init(this, /*isDataProcessing*/true);
					spLoadMoreItemsOperation.Completed = OnLoadAsyncCompleted;
					spLoadMoreItemsOperation.Start();

					// Within the threshold, trigger an incremental load.
					if (spLoadMoreItemsOperation.HasStatus())
					{
						AsyncStatus status = AsyncStatus.Completed;
						status = spLoadMoreItemsOperation.GetStatus();
						if (status == AsyncStatus.Started)
						{
							m_isLoadAsyncInProgress = true;
						}
						else
						{
							// Since the operation is completed we need to be done with it
							spLoadMoreItemsOperation.Cancel();
						}
					}
					else
					{
						// we're not using the operation. so we need to close it
						spLoadMoreItemsOperation.Cancel();
					}
				}
			}
		}
	}

	// Called when LoadAsync fires completed event
	private void OnLoadAsyncCompleted(IAsyncOperation<LoadMoreItemsResult> getOperation, AsyncStatus status)
	{
		// CheckThread();
		m_isLoadAsyncInProgress = false;

		if (IsInLiveTree)
		{
			// Re-try to see if we need to load more items if the listview,
			// We do that only as long as the ListView is in the live tree.
			// If the ListView is not live it will
			// not get measured and the last visible indexes will not be updated, which
			// will end up causing a loop where we keep calling load more items repeatedly.
			ProcessDataVirtualizationScrollOffsets();
		}
	}

	// Create an IAsyncOperation to load an additional DataFetchSize number of
	// items.
	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync()
	{
		LoadMoreItemsOperation spLoadMoreItemsOperation = new();

		spLoadMoreItemsOperation.Init(this, /*isDataProcessing*/false);
		spLoadMoreItemsOperation.Start();

		// OK to release spLoadMoreItemsOperation, object itself tracks its lifetime.
		return spLoadMoreItemsOperation.Operation;
	}

	// Internal method for starting an incremental load, used for both manual and
	// automatic incremental loads.
	internal IAsyncInfo? LoadMoreItemsAsyncInternal(LoadMoreItemsOperation? pChainedAsyncOperation)
	{
		IAsyncInfo? spAsyncInfo = null;

		var spItemsSource = ItemsSource;
		if (spItemsSource is not null)
		{
			// Check if the data source supports incremental loading.
			var spIncrementalLoadingVector = IncrementalLoadingAdapter.AsIncrementalLoading(spItemsSource);
			if (spIncrementalLoadingVector is not null)
			{
				// Check if the data source has more items to load.
				bool hasMoreItems = spIncrementalLoadingVector.HasMoreItems;
				if (hasMoreItems)
				{
					var spItemsHost = ItemsPanelRoot;
					var spPaginatedPanel = spItemsHost as IPaginatedPanel;
					if (spPaginatedPanel is not null)
					{
						int itemsToLoad = 0;
						double pagesToLoad = 0;
						double itemsPerPage = 0;

						// Calculate the items to load via pages-to-load and items-per
						// page values.
						itemsPerPage = spPaginatedPanel.GetItemsPerPage(m_tpScrollViewer!);
						pagesToLoad = DataFetchSize;
						itemsToLoad = (int)(pagesToLoad * itemsPerPage);
						if (itemsToLoad <= 0)
						{
							// We know that there's data to be loaded, at least load one item
							itemsToLoad = 1;
						}

						// Call out to the data source to load more items asynchronously.
						var spAsyncOperation = spIncrementalLoadingVector.LoadMoreItemsAsync((uint)itemsToLoad);
						if (spAsyncOperation is null)
						{
							throw new ArgumentNullException(nameof(spAsyncOperation));
						}

						// Set up a complete handler for the data source to call on when
						// it has completed the incremental load.
						IncrementalLoadAsyncCompleteHandler spLoadCompletedHandler = new();
						spLoadCompletedHandler.SetChainedAsyncOperation(pChainedAsyncOperation);
						spAsyncOperation.Completed = spLoadCompletedHandler.Invoke;

						// Do not start the operation yet. In case of manual incremental
						// load, this async operation is chained with another async
						// operation controlled by the public LoadMoreItemsAsync API. We
						// should wait for Start to be called on that AsyncOperation.
						spAsyncInfo = spAsyncOperation;
					}
				}
			}
		}

		return spAsyncInfo;
	}

	// overridden from ItemsControl (IGeneratorHost)
	private protected override void VirtualizationFinished() => InvokeDataSourceRangesChanged();

	// Stores a handle to the items source as an IItemsRangeInfo if possible
	private void InitializeDataSourceItemsRangeInfo()
	{
		var spItemsSourceAsIInspectable = ItemsSource;
		if (spItemsSourceAsIInspectable is not null)
		{
			var spItemsSourceAsIRI = spItemsSourceAsIInspectable as IItemsRangeInfo;
			if (spItemsSourceAsIRI is not null)
			{
				m_tpDataSourceAsItemsRangeInfo = spItemsSourceAsIRI;

				ItemIndexRange spLastPassedVisibleRange = new(0, 0);
				m_tpLastPassedVisibleRange = spLastPassedVisibleRange;

				// if it was already created, just clear the collection
				if (m_tpLastPassedTrackedRanges is not null)
				{
					m_tpLastPassedTrackedRanges.Clear();
				}
				else
				{
					TrackerCollection<ItemIndexRange> spLastPassedTrackedRanges = new();
					m_tpLastPassedTrackedRanges = spLastPassedTrackedRanges;
				}
			}
		}
	}

	// Used to inform the data source of the items it is tracking
	// in this function, we check if the new data is the same as the last passed ones
	// we invoke if the data is different
	private void InvokeDataSourceRangesChangedHelper(ItemIndexRange visibleRange, TrackerCollection<ItemIndexRange> trackedItems)
	{
		bool bAreRangesEqual = false;

		// we invoke only in the case where the ranges are not identical to the ones we already passed
		// first we test the visible range
		bAreRangesEqual = ItemIndexRange.AreItemIndexRangesEqual(m_tpLastPassedVisibleRange!, visibleRange);

		// if the above returned true, then we test the tracked items collection
		// otherwise, no need since we know that we have to invoke the RangesChanged regardless
		if (bAreRangesEqual)
		{
			bAreRangesEqual = ItemIndexRange.AreItemIndexRangeCollectionsEqual(m_tpLastPassedTrackedRanges!, trackedItems);
		}

		if (!bAreRangesEqual)
		{
			// getting the VectorView from the TrackerCollection
			// TODO Uno: TrackerCollection has no GetView, so the data source gets a snapshot list.
			IReadOnlyList<ItemIndexRange> spTrackedItemsAsIVV = new List<ItemIndexRange>(trackedItems);

			// invoking the RangesChanged method
			m_tpDataSourceAsItemsRangeInfo!.RangesChanged(visibleRange, spTrackedItemsAsIVV);

			// saving the new ranges
			m_tpLastPassedVisibleRange = visibleRange;
			m_tpLastPassedTrackedRanges = trackedItems;
		}
	}

	// Used to inform the data source of the items it is tracking
	// in this function, we collect the data
	private void InvokeDataSourceRangesChanged()
	{
		if (m_tpDataSourceAsItemsRangeInfo is not null)
		{
			var spItemsPanelRoot = ItemsPanelRoot;
			if (spItemsPanelRoot is not null)
			{
				var spIModernCollectionBasePanel = spItemsPanelRoot as ModernCollectionBasePanel;
				if (spIModernCollectionBasePanel is not null)
				{
					int firstVisibleIndex = -1;

					// get the data from the panel
					firstVisibleIndex = spIModernCollectionBasePanel.FirstVisibleIndexBase;

					if (firstVisibleIndex != -1)
					{
						int lastVisibleIndex = -1;
						int firstCachedIndex = -1;
						int lastCachedIndex = -1;
						List<uint> pinnedElementsIndices;
						ItemIndexRange spVisibleRange;
						TrackerCollection<ItemIndexRange> spTrackedItemsAsTC;

						// get the data from the panel
						lastVisibleIndex = spIModernCollectionBasePanel.LastVisibleIndexBase;
						firstCachedIndex = spIModernCollectionBasePanel.FirstCacheIndexBase;
						lastCachedIndex = spIModernCollectionBasePanel.LastCacheIndexBase;
						pinnedElementsIndices = spIModernCollectionBasePanel.GetPinnedElementsIndexVector(Controls.ElementType.ItemContainer);

						// visible range
						spVisibleRange = new(firstVisibleIndex, (uint)(lastVisibleIndex - firstVisibleIndex + 1));

						// create the tracked items collection
						spTrackedItemsAsTC = new();

						// cached range (includes visible range)
						AppendItemIndexRangeToItemIndexRangeCollection(firstCachedIndex, (uint)(lastCachedIndex - firstCachedIndex + 1), spTrackedItemsAsTC);

						// pinned elements
						if (pinnedElementsIndices.Count > 0)
						{
							// sorting the elements in ascending order
							pinnedElementsIndices.Sort();

							// creating ranges from the indices and appending them to the tracked items
							// TODO Uno: AppendItemIndexRangesFromSortedVectorToItemIndexRangeCollection returns a new list instead of appending.
							foreach (var range in new ItemIndexRange(0, 0).AppendItemIndexRangesFromSortedVectorToItemIndexRangeCollection(pinnedElementsIndices))
							{
								spTrackedItemsAsTC.Add(range);
							}
						}

						// focused item range
						AppendItemIndexRangeToItemIndexRangeCollection((int)GetLastFocusedIndex(), 1, spTrackedItemsAsTC);

						// compare the new ranges with the passed ones and invoke
						InvokeDataSourceRangesChangedHelper(spVisibleRange, spTrackedItemsAsTC);
					}
				}
			}
		}
	}

	// TODO Uno: ItemIndexRange's append helper is private static with a ref IList; this adapts it to the C++ call shape.
	private static void AppendItemIndexRangeToItemIndexRangeCollection(int startIndex, uint length, TrackerCollection<ItemIndexRange> pCollection)
		=> pCollection.Add(new ItemIndexRange(startIndex, length));
}

