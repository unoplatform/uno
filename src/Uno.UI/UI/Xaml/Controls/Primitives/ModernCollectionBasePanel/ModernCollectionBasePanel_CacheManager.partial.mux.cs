// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_CacheManager_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

//#define MCBP_DEBUG

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal partial class CacheManager
	{
		internal CacheManager(ModernCollectionBasePanel owner)
		{
			m_owner = owner;    // no ref needed, cachemanager is not at all exposed to the outside world
			m_cachedIsGrouped = false;
			m_cachedWasGrouped = false;
			m_cachedItemsCount = 0;
			m_cachedHistoryItemsCount = 0;
			m_wrHost = null;
			m_strongHost = null;
			m_strongChildren = null;
			m_metricsAreCached = false;
			m_metricsWereCached = false;
			m_isCollectionCacheValid = false;
			m_isFocusedChildValid = false;
			m_recyclingItemIndex = unchecked((uint)-1);
			m_hidesIfEmpty = false;
			m_cachedNonEmptyGroupsCount = 0;
			m_interpretItemCollectionResetAsFullReset = false;
		}

		internal partial struct GroupCache
		{
			public GroupCache()
			{
				startItemIndex = -1;
				endItemIndex = -1;
				indexOfGroup = -1;
				nonEmptyIndexOfGroup = -1;
			}
		}

		// a bit on-orthodox. we want to return an instance of the StrongRefLifetime type here so we can store it in a using and
		// let it go off the stack at the end of usage. A failed GuaranteeCache throws, so the cache is released before it propagates.
		internal StrongRefLifetime CacheStrongRefs()
		{
			try
			{
				GuaranteeCache();
			}
			catch
			{
				UnguaranteeCache();
				throw;
			}

			return new StrongRefLifetime(this);
		}

		internal IGeneratorHost? GetItemsHost()
		{
			IGeneratorHost? spItemsHost;

			if (m_strongHost is not null)
			{
				// just take that one, we're in a guaranteed session
				spItemsHost = m_strongHost;
			}
			else
			{
				// resolve, but do not store, since we're not in a guaranteed session
				IGeneratorHost? resolvedHost = null;
				m_wrHost?.TryGetTarget(out resolvedHost);
				spItemsHost = resolvedHost;
			}

			return spItemsHost;
		}

		internal IList<UIElement> GetChildren()
		{
			IList<UIElement> spChildren;

			if (m_strongChildren is not null)
			{
				// just take that one, we're in a guaranteed session
				spChildren = m_strongChildren;
			}
			else
			{
				// resolve, but do not store, since we're not in a guaranteed session
				spChildren = m_owner.Children;
			}

			return spChildren;
		}

		// potentially creates grouping cache, and make strong versions of some important variables
		internal void GuaranteeCache()
		{
			ICollectionView? spCollectionView;
			IObservableVector<object>? spCollectionGroups = null;
			GroupStyle? spGroupStyle;
			ItemsControl? spItemsControl;

			// these will be released during Unguarantee
			m_strongHost = null;
			m_wrHost?.TryGetTarget(out m_strongHost);
			m_strongChildren = m_owner.Children;

			// Initialize the collection cache
			m_isCollectionCacheValid = false;

			// Will force to get focus information once per virtualization
			m_isFocusedChildValid = false;

			// Ensure our group size vectors are allocated
			m_spGroupSizes ??= new();
			m_spNonEmptyGroupSizes ??= new();

			if (!m_metricsAreCached)
			{
				// If no GroupStyle is set, then many of our decisions to perform grouped layout
				// will make no sense. Therefore, we'll just display the items as non-grouped.
				// This is also what ICG did for legacy panels
				spGroupStyle = m_owner.GetGroupStyle();
				if (spGroupStyle is not null)
				{
					spCollectionView = m_strongHost!.CollectionView;
					if (spCollectionView is not null)
					{
						spCollectionGroups = spCollectionView.CollectionGroups;
						if (spCollectionGroups is not null)
						{
							m_cachedIsGrouped = true;
							m_cachedItemsCount = 0;
						}
					}
				}

				m_cachedGroupInformation.Clear();
				m_spGroupSizes.Clear();
				m_spNonEmptyGroupSizes.Clear();

				if (m_cachedIsGrouped)
				{
					int size = 0;
					int emptyGroupsSoFar = 0;
					IList<object> spCollectionGroupsAsV;
					List<uint> groupSizes;
					List<uint> nonEmptyGroupSizes;

					spCollectionGroupsAsV = spCollectionGroups!;

					size = spCollectionGroupsAsV.Count;

					m_cachedGroupInformation.Capacity = Math.Max(m_cachedGroupInformation.Capacity, size);
					groupSizes = new(size);
					nonEmptyGroupSizes = new(size);

					for (int i = 0; i < size; ++i)
					{
						object spCurrent;
						ICollectionViewGroup spCurrentGroup;
						IObservableVector<object> spCurrentGroupItems;
						IList<object> spCurrentGroupItemsAsV;
						int groupSize = 0;
						GroupCache cache = new();
						spCurrent = spCollectionGroupsAsV[i];
						spCurrentGroup = (ICollectionViewGroup)spCurrent;
						spCurrentGroupItems = spCurrentGroup.GroupItems;
						spCurrentGroupItemsAsV = spCurrentGroupItems;
						groupSize = spCurrentGroupItemsAsV.Count;

						cache.startItemIndex = m_cachedItemsCount;
						cache.endItemIndex = m_cachedItemsCount + groupSize;
						cache.indexOfGroup = i;
						cache.nonEmptyIndexOfGroup = i - emptyGroupsSoFar;

						global::System.Diagnostics.Debug.Assert(m_cachedGroupInformation.Count < m_cachedGroupInformation.Capacity);
						m_cachedGroupInformation.Add(cache);

						m_cachedItemsCount += groupSize;
						if (groupSize == 0)
						{
							++emptyGroupsSoFar;
						}
						else
						{
							nonEmptyGroupSizes.Add((uint)groupSize);
						}
						groupSizes.Add((uint)groupSize);
					}

					m_cachedNonEmptyGroupsCount = size - emptyGroupsSoFar;
					m_spGroupSizes.AddRange(groupSizes);
					m_spNonEmptyGroupSizes.AddRange(nonEmptyGroupSizes);
				}
				else
				{
					int size = 0;
					IList<object> spView;
					m_cachedIsGrouped = false;

					spView = m_strongHost!.View!;
					size = spView.Count;
					m_cachedItemsCount = size;
					m_cachedNonEmptyGroupsCount = 0;
				}
				m_metricsAreCached = true;
			}

			m_strongHost!.SetIsGrouping(m_cachedIsGrouped);

			spItemsControl = m_strongHost as ItemsControl;
#if LISTVIEW_PORT_CUTOVER
			if (spItemsControl is not null)
			{
				m_spContainerRecyclingContext = spItemsControl.GetRecyclingContext();
			}
#endif
			m_recyclingItemIndex = unchecked((uint)-1);
		}

		internal void UnguaranteeCache()
		{
			// Reset focus cached information
			m_isFocusedChildValid = false;
			m_spFocusedChild = null;

			// kill off the resolved variables
			m_strongHost = null;
			m_strongChildren = null;

			if (m_spContainerRecyclingContext is not null)
			{
				try
				{
					m_spContainerRecyclingContext.StopRecycling();
				}
				catch
				{
					// IGNOREHR
				}
			}
			m_spContainerRecyclingContext = null;

			ClearCollectionsCache();

			// notice how we are not invalidating the grouping cache. This needs to be
			// tied into INCC notifications
		}

		// this is a very impactful slow operation (or rather, it will lead to slowness).
		// ***  Only call this method if you really mean it!!  ***
		internal void ResetGroupCache()
		{
			m_interpretItemCollectionResetAsFullReset = false;
			m_cachedWasGrouped = m_cachedIsGrouped;
			m_metricsWereCached = m_metricsAreCached;
			m_cachedHistoryItemsCount = m_cachedItemsCount;
			m_cachedHistoryGroupInformation = m_cachedGroupInformation;
			m_cachedGroupInformation = new();

			m_cachedIsGrouped = false;
			m_cachedItemsCount = 0;
			m_metricsAreCached = false;
		}

		internal void ResetGroupCacheIfGroupCountsMismatch()
		{
			if (m_metricsAreCached && m_cachedIsGrouped)
			{
				IGeneratorHost? strongHost = null;
				m_wrHost?.TryGetTarget(out strongHost);
				if (strongHost is not null)
				{
					ICollectionView? collectionView = strongHost.CollectionView;
					if (collectionView is not null)
					{
						IObservableVector<object>? collectionGroups = collectionView.CollectionGroups;
						if (collectionGroups is not null)
						{
							IList<object> collectionGroupsAsV;
							int size = 0;

							collectionGroupsAsV = collectionGroups;
							size = collectionGroupsAsV.Count;

							if (size != GetTotalGroupCount())
							{
								ResetGroupCache();
							}
						}
					}
				}
			}
		}

		// we expect this method to be only called for one event at a time
		// Instead of going hardcore and redoing the whole cache, just take the change and update the cache
		internal void RenewCacheAfterMutation(int itemIndexAdded, int itemIndexRemoved, int groupIndexAdded, int groupIndexRemoved)
		{
			m_cachedWasGrouped = m_cachedIsGrouped;
			m_metricsWereCached = m_metricsAreCached;
			m_cachedHistoryItemsCount = m_cachedItemsCount;
			// this should actually be pretty fast since those arrays are of the same size and std should be bitblasting this
			m_cachedHistoryGroupInformation = new(m_cachedGroupInformation);
			IObservableVector<object> spCollectionGroups;
			ICollectionView spCollectionView;

			// make sure that only one index is not -1
			global::System.Diagnostics.Debug.Assert(Math.Min(0, itemIndexAdded) + Math.Min(0, itemIndexRemoved) + Math.Min(0, groupIndexAdded) + Math.Min(0, groupIndexRemoved) == -3);

			// even though I asked you to give me a itemIndexAdded and Removed, really we only care about the index that was changed (so we
			// know that group is of interest)
			int itemIndexOfInterest = Math.Max(itemIndexAdded, itemIndexRemoved);

#if MCBP_DEBUG
			global::System.Diagnostics.Debug.WriteLine($"RenewCache: itemAdded {itemIndexAdded}, itemRemoved {itemIndexRemoved}, groupAdded {groupIndexAdded}, groupRemoved {groupIndexRemoved}");
#endif

			// instead of blindly iterating over all groups, lets update using the old cache

			// only need to do anything if we were already cached, otherwise the ensure will just build up everything
			if (m_metricsAreCached)
			{
				// if a group was added, we need to insert an empty cache node in the m_cachedGroupInformation list
				if (groupIndexAdded > -1)
				{
					GroupCache cache = new();
					cache.indexOfGroup = groupIndexAdded;
					m_cachedGroupInformation.Insert(groupIndexAdded, cache);
				}

				// if a group was removed, we need to remove that cache. From there, the iteration will just work as normal
				if (groupIndexRemoved > -1)
				{
					m_cachedGroupInformation.RemoveAt(groupIndexRemoved);
				}

				using var strongCache = CacheStrongRefs(); // Releases when it goes out of scope at the end of method

				if (m_cachedIsGrouped)
				{
					bool CacheWasNotConsistent = false;

					int size = 0;
					int emptyGroupsSoFar = 0;
					IList<object> spCollectionGroupsAsV;
					List<uint> groupSizes = new();
					List<uint> nonEmptyGroupSizes = new();
					spCollectionView = m_strongHost!.CollectionView!;

					// no if conditions checking the collectionview are needed. We got an ItemMutation,
					// which cannot have changed grouping
					spCollectionGroups = spCollectionView.CollectionGroups;

					spCollectionGroupsAsV = spCollectionGroups;
					size = spCollectionGroupsAsV.Count;
					// we will actually re-do these, but not m_cachedGroupInformation, as you might notice
					m_spGroupSizes!.Clear();
					m_spNonEmptyGroupSizes!.Clear();

					// When an item is inserted, we will first have gotten the group insert, so the sizes can not have changed
					// In some cases, we might have missed the group insert/removal event.
					CacheWasNotConsistent = m_cachedGroupInformation.Count != size;

					// going to use this as a counter
					m_cachedItemsCount = 0;

					// we are going to iterate over all the groups and use the cached information until we find the group
					// that might have the added item. If the index comes at the boundary between two groups, use the actual
					// collectionviewgroup api's to get the truth!
					// Once we've seen our mutation, we need to keep updating the cache after it

					for (int i = 0; i < size; ++i)
					{
						object spCurrent;
						ICollectionViewGroup spCurrentGroup;
						IObservableVector<object> spCurrentGroupItems;
						IList<object> spCurrentGroupItemsAsV;

						// if the cache sizes were not consistent, we want to be able to deal with that.
						// basically we are going into 'refresh' mode. We do need to make sure we have the
						// correct caches available, so lets fix that up here.
						if (CacheWasNotConsistent && (m_cachedGroupInformation.Count == 0 || m_cachedGroupInformation.Count <= i))
						{
#if MCBP_DEBUG
							global::System.Diagnostics.Debug.WriteLine("RenewCache: adding group cache on the fly");
#endif
							// create a cache on the fly
							GroupCache tempcache = new();
							tempcache.indexOfGroup = i;
							m_cachedGroupInformation.Add(tempcache);
						}

						GroupCache cache = m_cachedGroupInformation[i];
						int groupSize = cache.endItemIndex - cache.startItemIndex;

						// index can be either an item that was added or removed. As we are walking through the caches,
						// the basic strategy here is to actually do a (slow) api call for the group that _might_ be changed.
						// INSERT scenario:  item was added between beginning and end or item was appended to the end of the group
						// DELETE scenario:  item was removed from between and end
						//
						// empty groups: notice that there can be a few empty groups after each other and the index could be
						// part of any of those empty groups, so keep going until we find it (we _have_ to make the api calls)
						//
						// a group can be either removed or added or changed. As we are walking through the caches,
						// if a group is added or changed, we should just get the size from api. Since we have already removed
						// a cache from a group that was removed, the iteration will just work as normal for removed groups


						// notice endItemIndex is exclusive, so group 0 which has 10 items will have a start of 0, end of 10 instead of 9

						if ((cache.startItemIndex <= itemIndexOfInterest && itemIndexOfInterest <= cache.endItemIndex) ||
							i == groupIndexAdded || CacheWasNotConsistent)
						{
							int newGroupSize = 0;
							// this is the place where we might have inserted an item and it now belongs to
							// this group. Notice that it could also have been inserted at the beginning of the next group
							// In both cases, we want to get the actual numbers (groupsize is what we care about)
							spCurrent = spCollectionGroupsAsV[i];
							spCurrentGroup = (ICollectionViewGroup)spCurrent;
							spCurrentGroupItems = spCurrentGroup.GroupItems;
							spCurrentGroupItemsAsV = spCurrentGroupItems;
							newGroupSize = spCurrentGroupItemsAsV.Count;

							groupSize = newGroupSize;
						}

						cache.startItemIndex = m_cachedItemsCount;
						cache.endItemIndex = m_cachedItemsCount + groupSize;
						cache.indexOfGroup = i;
						cache.nonEmptyIndexOfGroup = i - emptyGroupsSoFar;
						m_cachedGroupInformation[i] = cache;

						m_cachedItemsCount += groupSize;
						if (groupSize == 0)
						{
							++emptyGroupsSoFar;
						}
						else
						{
							nonEmptyGroupSizes.Add((uint)groupSize);
						}
						groupSizes.Add((uint)groupSize);
					}

					m_cachedNonEmptyGroupsCount = size - emptyGroupsSoFar;
					m_spGroupSizes.AddRange(groupSizes);
					m_spNonEmptyGroupSizes.AddRange(nonEmptyGroupSizes);
				}
				else
				{
					// We have found that some collections do not raise the appropriate amount of events,
					// and thus a lookup is needed here always. This issue is working around a RI blocking issue on Facebook
					int size = 0;
					IList<object> spView;

					spView = m_strongHost!.View!;
					size = spView.Count;
					m_cachedItemsCount = size;
				}
			}
		}

		internal void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)
		{
			global::System.Diagnostics.Debug.Assert(m_metricsAreCached);
			global::System.Diagnostics.Debug.Assert(m_cachedIsGrouped && 0 <= itemIndex && itemIndex < m_cachedItemsCount);

			indexOfGroup = -1;
			indexInsideGroup = -1;
			itemCountInGroup = -1;

			if (m_cachedIsGrouped && 0 <= itemIndex && itemIndex < m_cachedItemsCount)
			{
				// very perf sensitive. notice the binary search
				int foundGroup = UpperBoundByEndItemIndex(m_cachedGroupInformation, itemIndex);

				if (foundGroup != m_cachedGroupInformation.Count)
				{
					GroupCache found = m_cachedGroupInformation[foundGroup];
					itemCountInGroup = found.endItemIndex - found.startItemIndex;
					indexInsideGroup = itemIndex - found.startItemIndex;
					indexOfGroup = found.indexOfGroup;
				}
			}
		}

		internal void GetGroupHistoryInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)
		{
			global::System.Diagnostics.Debug.Assert(m_metricsWereCached);
			global::System.Diagnostics.Debug.Assert(m_cachedWasGrouped && 0 <= itemIndex && itemIndex < m_cachedHistoryItemsCount);

			indexOfGroup = -1;
			indexInsideGroup = -1;
			itemCountInGroup = -1;

			if (m_cachedWasGrouped && 0 <= itemIndex && itemIndex < m_cachedHistoryItemsCount)
			{
				// very perf sensitive. notice the binary search
				int foundGroup = UpperBoundByEndItemIndex(m_cachedHistoryGroupInformation, itemIndex);

				if (foundGroup != m_cachedHistoryGroupInformation.Count)
				{
					GroupCache found = m_cachedHistoryGroupInformation[foundGroup];
					itemCountInGroup = found.endItemIndex - found.startItemIndex;
					indexInsideGroup = itemIndex - found.startItemIndex;
					indexOfGroup = found.indexOfGroup;
				}
			}
		}

		internal void GetGroupInformationFromGroupIndex(int groupIndex, out int startItemIndex, out int itemCountInGroup)
		{
			global::System.Diagnostics.Debug.Assert(m_metricsAreCached);
			global::System.Diagnostics.Debug.Assert(m_cachedIsGrouped && 0 <= groupIndex && groupIndex < GetTotalGroupCount());

			startItemIndex = -1;
			itemCountInGroup = -1;

			if (0 <= groupIndex && groupIndex < GetTotalGroupCount())
			{
				GroupCache cache = m_cachedGroupInformation[groupIndex];
				startItemIndex = cache.startItemIndex;
				itemCountInGroup = cache.endItemIndex - startItemIndex;
			}
		}

		// With things like "hiding empty groups" or "collapsing items in a group", it's often a helpful abstraction to be able to
		// work in terms of "layout units", rather than "data units", where traversing a range in layout units will skip over elements that are hidden
		internal int DataIndexToLayoutIndex(ElementType type, int index)
		{
			global::System.Diagnostics.Debug.Assert(m_metricsAreCached);
			int result = index;
			switch (type)
			{
				case ElementType.GroupHeader:
					{
						// If empty groups are hidden, figure out how many empty groups precede this one
						//ASSERT(0 <= index && index < GetTotalGroupCount());
						if (m_hidesIfEmpty && 0 <= index && index < GetTotalGroupCount())
						{
							// Get the empty group count first, because this is far faster than querying the groupstyle
							result = m_cachedGroupInformation[index].nonEmptyIndexOfGroup;

							result = Math.Min(result, GetTotalLayoutGroupCount() - 1);
							global::System.Diagnostics.Debug.Assert(result >= 0, "You should not be calling this when we have nothing to display");
						}

					}
					break;

				case ElementType.ItemContainer:
					// Nothing to do for items... for now...
					break;

				default:
					global::System.Diagnostics.Debug.Assert(false);
					break;
			}
			return result;
		}

		internal int LayoutIndexToDataIndex(ElementType type, int index)
		{
			global::System.Diagnostics.Debug.Assert(m_metricsAreCached);
			int result = index;
			switch (type)
			{
				case ElementType.GroupHeader:
					// Find the last occurrence of "index" in the m_groupIndexIgnoringEmpties array
					// This corresponds to the nonempty group that occupies this slot in the layout units
					// Reasoning: If the next group falls in a different layout slot, it's because the target group is visible
					{
						if (m_hidesIfEmpty && GetTotalGroupCount() != GetNonEmptyGroupCount())
						{
							int low = 0;
							int high = m_cachedGroupInformation.Count;
							while (low < high)
							{
								int mid = low + ((high - low) / 2);
								if (index < m_cachedGroupInformation[mid].nonEmptyIndexOfGroup)
								{
									high = mid;
								}
								else
								{
									low = mid + 1;
								}
							}
							result = low - 1;
						}
					}
					break;

				case ElementType.ItemContainer:
					// Nothing to do for items... for now...
					break;

				default:
					global::System.Diagnostics.Debug.Assert(false);
					break;
			}
			return result;
		}

		internal int GetTotalLayoutGroupCount() => m_hidesIfEmpty ? GetNonEmptyGroupCount() : GetTotalGroupCount();

		// Checks if the container or one of its descendants currently has the focus,
		// in which case it cannot be recycled
		// In a RunVirtualization call, we may need to recycle several items
		// and as we have to check the focus for each candidate, we could
		// spend a lot of time in FrameworkElement::HasFocus as it walks the
		// the visual tree up starting to the focused element.
		// Therefore, the first time IsFocusedChild is called in a virtualization
		// cycle, we walk the tree up and cache the result
		// Following calls will simply test the candidate against the cache
		internal bool IsFocusedChild(UIElement pCandidate)
		{
			if (!m_isFocusedChildValid)
			{
				DependencyObject? spFocused;
				DependencyObject? spCurrentAsDO;
				DependencyObject? spParentAsDO;
				DependencyObject spHostAsDO;

				if (m_strongChildren is null)
				{
					throw new InvalidOperationException();
				}
				// We walk the tree up to find a ContentControl which is an ancestor of the focused element
				// We stop either if we encounter the host or the root

				spHostAsDO = m_owner;

				spFocused = m_owner.XamlRoot is { } xamlRoot ? Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot) as DependencyObject : null;
				if (spFocused is null)
				{
					// No focused element
					m_isFocusedChildValid = true;
				}
				else
				{
					// we walk the tree up until we find a child of the host
					spCurrentAsDO = spFocused;

					while (!m_isFocusedChildValid)
					{
						spParentAsDO = VisualTreeHelper.GetParent(spCurrentAsDO);
						if (spParentAsDO is null)
						{
							// The focused control does not belong to the host
							// We release the pointers and the Focus test will always remain "NULL"
							m_isFocusedChildValid = true;
						}
						else if (ReferenceEquals(spParentAsDO, spHostAsDO))
						{
							// Found it !
							m_spFocusedChild = (UIElement)spCurrentAsDO;
							m_isFocusedChildValid = true;
						}
						else
						{
							// Parent is neither null nor host, continue up
							spCurrentAsDO = spParentAsDO;
						}
					}
				}
			}

			return ReferenceEquals(pCandidate, m_spFocusedChild);
		}

		internal IReadOnlyList<uint>? GetLayoutGroupSizes() => m_hidesIfEmpty ? m_spNonEmptyGroupSizes : m_spGroupSizes;

		// Initialize the strong collection cache
		private void InitCollectionsCache()
		{
			if (m_cachedIsGrouped)
			{
				ICollectionView spCollectionView;
				IObservableVector<object> spCollectionGroups;
				spCollectionView = m_strongHost!.CollectionView!;
				spCollectionGroups = spCollectionView.CollectionGroups;
				m_strongCollectionGroupsAsV = spCollectionGroups;
				// Current Index is Invalid
				m_cachedGroupIndex = -2;
			}
			else
			{
				m_strongView = m_strongHost!.View;
			}
			m_isCollectionCacheValid = true;
		}

		// Clear the strong collection cache
		private void ClearCollectionsCache()
		{
			m_strongCollectionGroupsAsV = null;
			m_strongCurrentGroup = null;
			m_strongView = null;
			m_isCollectionCacheValid = false;
		}

		// Cache a group's interfaces
		private void CacheGroup(int groupIndex, bool cacheItems)
		{
			if (!m_isCollectionCacheValid)
			{
				InitCollectionsCache();
			}

			if (m_cachedIsGrouped)
			{
				if (m_cachedGroupIndex != groupIndex)
				{
					object spCurrentGroupAsII;
					m_strongView = null;
					spCurrentGroupAsII = m_strongCollectionGroupsAsV![groupIndex];
					m_strongCurrentGroup = (ICollectionViewGroup)spCurrentGroupAsII;
					m_cachedGroupIndex = groupIndex;
				}
				// We need to get the items either if this is a new group or if items were not asked when we moved to this group
				if (cacheItems && (m_strongView is null))
				{
					IObservableVector<object> spCurrentGroupItems;
					spCurrentGroupItems = m_strongCurrentGroup!.GroupItems;
					m_strongView = spCurrentGroupItems;
				}
			}
		}

		// Returns the ICollectionViewGroup reference from the grouped bound source corresponding to an index
		internal ICollectionViewGroup? GetGroup(int groupIndex)
		{
			global::System.Diagnostics.Debug.Assert(0 <= groupIndex && groupIndex < GetTotalGroupCount());

			CacheGroup(groupIndex, false /* cacheItems */);
			return m_strongCurrentGroup;
		}

		// Returns the IInspectable reference from the bound source corresponding to a index
		internal object? GetItem(int indexInItemCollection)
		{
			int correctedItemIndex = indexInItemCollection;

			global::System.Diagnostics.Debug.Assert(0 <= indexInItemCollection && indexInItemCollection < m_cachedItemsCount);

			if (m_cachedIsGrouped)
			{
				int groupIndex = 0;
				GetGroupInformationFromItemIndex(indexInItemCollection, out groupIndex, out correctedItemIndex, out _);
				CacheGroup(groupIndex, true /* cacheItems */);
			}
			else
			{
				if (!m_isCollectionCacheValid)
				{
					InitCollectionsCache();
				}
			}
			if (m_strongView is null)
			{
				throw new InvalidOperationException();
			}
			return m_strongView[correctedItemIndex];
		}

		internal void GetIndex(object? pItem, out uint index, out bool isGroupIndex, out bool itemFound)
		{
			index = 0;
			isGroupIndex = false;
			itemFound = false;

			if (!m_isCollectionCacheValid)
			{
				InitCollectionsCache();
			}

			if (m_cachedIsGrouped)
			{
				ICollectionViewGroup? spItemAsCollectionViewGroup = pItem as ICollectionViewGroup;

				if (spItemAsCollectionViewGroup is not null)
				{
					object? spGroupContext;

					spGroupContext = spItemAsCollectionViewGroup.Group;

					// If this item is a CollectionViewGroup, then it represents a group header.
					// We'll just return the group index in this case.
					IEnumerable<object> spIterable;
					IEnumerator<object> spIter;

					int groupIndex = 0;
					bool areItemsRemaining = false;

					spIterable = m_strongCollectionGroupsAsV!;
					spIter = spIterable.GetEnumerator();
					areItemsRemaining = spIter.MoveNext();

					while (areItemsRemaining)
					{
						object spCurrentGroupAsII;
						ICollectionViewGroup spCurrentGroup;
						object? spCurrentGroupContext;

						spCurrentGroupAsII = spIter.Current;
						spCurrentGroup = (ICollectionViewGroup)spCurrentGroupAsII;
						spCurrentGroupContext = spCurrentGroup.Group;

						if (ReferenceEquals(spGroupContext, spCurrentGroupContext))
						{
							index = (uint)groupIndex;
							isGroupIndex = true;
							itemFound = true;

							break;
						}
						else
						{
							groupIndex++;
						}

						areItemsRemaining = spIter.MoveNext();
					}
				}
				else
				{
					// If this item is *not* a CollectionViewGroup, then we'll find which group it's located in,
					// and then return its index in that group plus that group's start index,
					// since that'll get us the index to scroll to, which is what we want.
					IEnumerable<object> spIterable;
					IEnumerator<object> spIter;

					int groupIndex = 0;
					bool areItemsRemaining = false;

					spIterable = m_strongCollectionGroupsAsV!;
					spIter = spIterable.GetEnumerator();
					areItemsRemaining = spIter.MoveNext();

					while (areItemsRemaining)
					{
						object spCurrentGroupAsII;
						ICollectionViewGroup spCurrentGroup;
						IObservableVector<object> spCurrentGroupItems;
						IList<object> spCurrentGroupItemsAsV;

						bool itemFoundInGroup = false;
						int indexInGroup = 0;

						spCurrentGroupAsII = spIter.Current;
						spCurrentGroup = (ICollectionViewGroup)spCurrentGroupAsII;
						spCurrentGroupItems = spCurrentGroup.GroupItems;

						spCurrentGroupItemsAsV = spCurrentGroupItems;
						indexInGroup = spCurrentGroupItemsAsV.IndexOf(pItem!);
						itemFoundInGroup = indexInGroup >= 0;

						if (itemFoundInGroup)
						{
							int indexCorrection = 0;

							GetGroupInformationFromGroupIndex(groupIndex, out indexCorrection, out _);

							index = (uint)(indexInGroup + indexCorrection);
							itemFound = true;

							break;
						}
						else
						{
							groupIndex++;
						}

						areItemsRemaining = spIter.MoveNext();
					}
				}
			}
			else
			{
				if (m_strongView is null)
				{
					throw new InvalidOperationException();
				}
				int indexInView = m_strongView.IndexOf(pItem!);
				itemFound = indexInView >= 0;
				index = itemFound ? (uint)indexInView : 0;
			}
		}

		internal IContainerRecyclingContext? GetContainerRecyclingContext(uint itemIndex)
		{
			if (m_spContainerRecyclingContext is not null && (itemIndex != m_recyclingItemIndex))
			{
				object? spItem;
				spItem = GetItem((int)itemIndex);
				m_spContainerRecyclingContext.PrepareForItemRecycling(spItem);
				m_recyclingItemIndex = itemIndex;
			}
			return m_spContainerRecyclingContext;
		}

		// Returns the index of the last header/item in the data that can be displayed in layout.
		internal void GetLastElementInLayout(out int elementIndex, out ElementType elementType)
		{
			elementIndex = -1;
			elementType = ElementType.ItemContainer;

			if (IsGrouping())
			{
				int targetGroupIndex = LayoutIndexToDataIndex(
					ElementType.GroupHeader,
					GetTotalLayoutGroupCount() - 1);

				int firstItemInAnchorGroup;
				int itemCountInAnchorGroup;
				GetGroupInformationFromGroupIndex(
					targetGroupIndex,
					out firstItemInAnchorGroup,
					out itemCountInAnchorGroup);

				if (itemCountInAnchorGroup == 0)
				{
					elementIndex = targetGroupIndex;
					elementType = ElementType.GroupHeader;
				}
				else
				{
					elementIndex = firstItemInAnchorGroup + itemCountInAnchorGroup - 1;
				}
			}
			else
			{
				elementIndex = GetTotalItemCount() - 1;
			}
		}

		// std::upper_bound over the groups' endItemIndex: first group whose endItemIndex is greater than workingIndex.
		private static int UpperBoundByEndItemIndex(List<GroupCache> groups, int workingIndex)
		{
			int low = 0;
			int high = groups.Count;
			while (low < high)
			{
				int mid = low + ((high - low) / 2);
				if (workingIndex < groups[mid].endItemIndex)
				{
					high = mid;
				}
				else
				{
					low = mid + 1;
				}
			}
			return low;
		}
	}
}
