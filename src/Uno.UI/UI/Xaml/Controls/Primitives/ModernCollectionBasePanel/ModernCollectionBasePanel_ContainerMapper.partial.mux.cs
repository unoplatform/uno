// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_ContainerMapper_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
#if !HAS_UNO // TODO Uno: only used by the containment gate in GetPinnedElementsIndexVector.
	// Bug 62542953: [2.0 servicing] Reduce allocations by reserving vector space
	private const int WINAPPSDK_CHANGEID_62542953 = 62542953;
#endif

	internal partial class ContainerManager
	{
		internal ContainerManager(ModernCollectionBasePanel owner)
		{
			m_owner = owner;    // no ref needed, containermapper is not at all exposed to the outside world
			m_focusedContainer = new();
			m_focusedHeaderContainer = new();
			m_unlinkedFillers = new();
			m_preparationHoldingArea = new();
			m_indexOfGroupBeingReset = -1;
			m_wasLastScrollIntoViewForHeader = false;
			m_isScrollIntoViewPending = false;
			m_isScrollIntoViewInProgress = false;

			Array.Fill(m_indicesOfFirstValidElement, -1);
			Array.Fill(m_lastFoundIndexElements, -1);
		}

		// naming confusion: for long we have shipped with a Refresh that did the work of what I would consider
		// a reset. This has seeped into public api, and therefore I can't rectify that. So understand these definitions:
		// *******    Refresh:   destructive, time consuming
		// *******    Reset:     maintain state as much as possible. We can't trust our concept of indices anymore and want to redo them
		internal void Refresh()
		{
			// Invalidate all the container offets, as if we were recycling all
			RemoveAllValidContainers();
			RemoveAllValidHeaders();

			ClearPinAndFocus();
		}

		// immediately clears pin and focused container without prejudice
		internal void ClearPinAndFocus()
		{
			// Remove the pinned containers
			m_pinnedElements[(int)ElementType.GroupHeader].Clear();
			m_pinnedElements[(int)ElementType.ItemContainer].Clear();

			// we remember when we want to recycle a focused container
			// but we start off without one.
			m_focusedContainer = new();
		}

		// naming confusion: for long we have shipped with a Refresh that did the work of what I would consider
		// a reset. This has seeped into public api, and therefore I can't rectify that. So understand these definitions:
		// *******    Refresh:   destructive, time consuming
		// *******    Reset:     maintain state as much as possible. We can't trust our concept of indices anymore and want to redo them
		internal void Reset()
		{
			// a refresh really means that indices are no longer valid. but that does not mean that the link between
			// container and item needs to be lost.

			bool isGroupHeader = false;
			bool found = false;
			uint newItemIndex = 0;

			PinnedElementInfo focusedContainerInfo = m_focusedContainer;
			var focusedContainer = m_focusedContainer.GetElement();
			// temporary, so that the pinned containers do not also include m_focusedContainer when we ask for 'isPinned'
			m_focusedContainer = new();

			m_owner.m_cacheManager.GetChildren();

			// if we ever get a a new elementtype, we probably need to update this code
			global::System.Diagnostics.Debug.Assert(ElementType_Count == 2);
			for (int typeindex = 0; typeindex < ElementType_Count; ++typeindex)
			{
				ElementType elementType = (ElementType)typeindex;

				var pinnedContainers = m_pinnedElements[(int)elementType];
				int iter = 0;

				// update and remove as we go
				while (iter != pinnedContainers.Count)
				{
					PinnedElementInfo info = pinnedContainers[iter];
					UIElement? oldContainer = info.GetElement();
					UIElement? newContainer;

					bool isPinnedContainerAlsoFocused = (oldContainer == focusedContainer);
					if (isPinnedContainerAlsoFocused)
					{
						// Clear focusedContainerInfo to avoid a double reset on this container.
						focusedContainerInfo = new();
					}

					object? spDataItem = Interface_DataItemFromElement(elementType, oldContainer!);
					m_owner.m_cacheManager.GetIndex(spDataItem, out newItemIndex, out isGroupHeader, out found);
					if (found)
					{
						// get rid of it since recycling will not work if we are still pinned
						pinnedContainers.RemoveAt(iter);
						Size measureSize = m_owner.m_spLayoutStrategy.GetElementMeasureSize(
							elementType,
							(int)newItemIndex,
							m_owner.m_windowState.GetRealizationWindow());

						if (isGroupHeader)
						{
							m_owner.RecycleHeader(info.GetElement()!);
						}
						else
						{
							m_owner.RecycleContainer(info.GetElement()!);
						}

						newContainer = m_owner.GenerateElementAtDataIndex(elementType, (int)newItemIndex);

						info.UpdateIndex((int)newItemIndex);
						if (info.GetElement() != newContainer)
						{
							// new container
							info.UpdateElement(newContainer is null ? null : new WeakReference<UIElement>(newContainer));

							// items need to be in the visual tree to be measured correctly
							// however, this does not mean they have to be in the valid part.
							PlaceInGarbageRegionIfNotInChildren(newContainer!);
						}

						m_owner.PrepareElementViaItemsHost(elementType, (int)newItemIndex, measureSize, newContainer!);

						// reinsert the pin at the same location
						pinnedContainers.Insert(iter, info);

						if (isPinnedContainerAlsoFocused)
						{
							// We finished the reset of the pinned container.
							// It's present in the new data source.
							// Make sure we also update m_focusedContainer if
							// this pinned container is also focused.
							m_focusedContainer = info;
						}

						++iter;
					}
					else
					{
						// in new datasource, apparently this doesn't exist anymore
						pinnedContainers.RemoveAt(iter);

						ElementUnpinned(
							elementType,
							info.GetIndex(),
							info.GetElement()!,
							true /* removeNonGeneratedContainer */);

						// let it recycle
						if (elementType == ElementType.GroupHeader)
						{
							m_owner.RecycleHeader(info.GetElement()!);
						}
						else
						{
							m_owner.RecycleContainer(info.GetElement()!);
						}
					}
				}
			}

			// focused containers are not necessarily in the pinned container collection, but do act as though they are pinned
			if (focusedContainerInfo.GetIndex() > -1)
			{
				object? spDataItem = Interface_DataItemFromElement(ElementType.ItemContainer, focusedContainerInfo.GetElement()!);
				m_owner.m_cacheManager.GetIndex(spDataItem, out newItemIndex, out isGroupHeader, out found);
				if (found)
				{
					if (GetIsContainerPinned(focusedContainerInfo.GetElement()))
					{
						// Focused container is also the pinned container. We already took care
						// of pinned containers, so just update the index of the focused container.
						focusedContainerInfo.UpdateIndex((int)newItemIndex);
					}
					else
					{
						Size measureSize = m_owner.m_spLayoutStrategy.GetElementMeasureSize(
							ElementType.ItemContainer,
							(int)newItemIndex,
							m_owner.m_windowState.GetRealizationWindow());

						m_owner.RecycleLinkedContainer(focusedContainerInfo.GetElement()!);
						UIElement? spContainer = m_owner.GenerateContainerAtIndex((int)newItemIndex);
						focusedContainerInfo.UpdateIndex((int)newItemIndex);

						if (focusedContainerInfo.GetElement() != spContainer)
						{
							// new container
							focusedContainerInfo.UpdateElement(spContainer is null ? null : new WeakReference<UIElement>(spContainer));

							// items need to be in the visual tree to be measured correctly
							// however, this does not mean they have to be in the valid part.
							PlaceInGarbageRegionIfNotInChildren(spContainer!);
						}

						m_owner.PrepareElementViaItemsHost(ElementType.ItemContainer, (int)newItemIndex, measureSize, spContainer!);
					}

					m_focusedContainer = focusedContainerInfo;
				}
				else
				{
					// in new datasource, apparently this doesn't exist anymore
					m_owner.RecycleLinkedContainer(focusedContainerInfo.GetElement()!);
				}
			}
		}

		// Given an the index of a valid element (i.e., 0 is always the first container in the valid range), return the element.
		internal UIElement? GetElementAtValidIndex(ElementType type, int indexInValidElements)
		{
			global::System.Diagnostics.Debug.Assert(IsValidElementIndexWithinBounds(type, indexInValidElements));

			return ResolveWeakElement(m_validElements[(int)type][indexInValidElements]);
		}

		internal UIElement? GetElementAtDataIndex(ElementType type, int dataIndex)
		{
			WeakReference<UIElement>? temp = null;

			int validIndex = GetValidElementIndexFromDataIndex(type, dataIndex);
			if (IsValidElementIndexWithinBounds(type, validIndex))
			{
				temp = m_validElements[(int)type][validIndex];
			}

			return ResolveWeakElement(temp);
		}

		// Returns True if there is a realized item in the valid collection for the provided index.
		internal bool IsItemConnected(uint index)
		{
			if (GetValidContainerCount() > 0 &&
				index >= (uint)GetItemIndexFromValidIndex(0) &&
				index <= (uint)GetItemIndexFromValidIndex(GetValidContainerCount() - 1))
			{
				UIElement? spContainer = GetContainerAtItemIndex((int)index);
				return spContainer is not null;
			}
			return false;
		}

		// Returns True if the group header for the provided group index is connected.
		// neighboringItemIndex is either the last item of the previous visible group or the first item of the provided group.
		// It depends on whether the known connected area is above or below the provided group index.
		internal bool IsGroupHeaderConnected(uint groupIndex, uint neighboringItemIndex)
			=> IsGroupHeaderRealized(groupIndex) && IsItemConnected(neighboringItemIndex);

		// Returns True if there is a realized group header for the provided index.
		internal bool IsGroupHeaderRealized(uint groupIndex)
		{
			// Deal with empty groups
			groupIndex = (uint)m_owner.m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, (int)groupIndex);
			groupIndex = (uint)m_owner.m_cacheManager.LayoutIndexToDataIndex(ElementType.GroupHeader, (int)groupIndex);

			// Find out if we've already realized this header, and if not, who the closest header is
			if (GetValidHeaderCount() > 0)
			{
				uint firstValidGroupIndex = (uint)GetGroupIndexFromValidIndex(0);
				uint lastValidGroupIndex = (uint)GetGroupIndexFromValidIndex(GetValidHeaderCount() - 1);

				if (groupIndex < firstValidGroupIndex || groupIndex > lastValidGroupIndex)
				{
					return false;
				}
				else
				{
					UIElement? spHeader = GetHeaderAtGroupIndex((int)groupIndex);
					return spHeader is not null;
				}
			}
			return false;
		}

		// Inform the ContainerManager that the index of the first realized element has changed
		// to the given value.
		internal void UpdateDataIndexForFirstValidElement(ElementType type, int dataIndexOfFirstValidElement)
		{
			// in cases where we insert or remove a data item before the collection, the dataIndexes need to reflect the change
			int difference = dataIndexOfFirstValidElement - m_indicesOfFirstValidElement[(int)type];
			m_indicesOfFirstValidElement[(int)type] = dataIndexOfFirstValidElement;

			var indices = m_elementIndicesForChildren[(int)type];
			for (int i = 0; i < indices.Count; ++i)
			{
				indices[i] += difference;
			}
		}

		// Given the element and its data index in the Items(Source), add the element (or move, if it's)
		// to the proper place in the Children collection. Also updates the elements's virtualization info to reflect
		// that it is realized.
		internal void PlaceInValidElements(ElementType type, int dataIndex, UIElement? spElement)
		{
#if !HAS_UNO // TODO Uno: no ETW tracing.
			TracePlaceElementBegin(dataIndex);

			using var guard = Uno.Disposables.Disposable.Create(() => TracePlaceElementEnd());
#endif

			WeakReference<UIElement>? weakRef = spElement is null ? null : new WeakReference<UIElement>(spElement);

			var validElements = m_validElements[(int)type];

			// Sanity-check the index so we don't end up corrupting memory
			// Our item index can be up to 1 element beyond the valid range.
			if (!(validElements.Count == 0 ||
				(m_indicesOfFirstValidElement[(int)type] - 1 <= dataIndex && dataIndex <= m_indicesOfFirstValidElement[(int)type] + GetValidElementCount(type))))
			{
				throw new InvalidOperationException();
			}

			// Figure out if we are inserting a new element in the valid range, or just replacing an existing one
			bool isValidRangeInsertion =
				validElements.Count == 0 || // Realizing our first element
				dataIndex == m_indicesOfFirstValidElement[(int)type] - 1 || // Placing at beginning of the range
				dataIndex == m_indicesOfFirstValidElement[(int)type] + GetValidElementCount(type); // Placing at end of the range

			if (isValidRangeInsertion)
			{
				int indexInValidElements = validElements.Count == 0 ? 0 : dataIndex - m_indicesOfFirstValidElement[(int)type];

				// If we're inserting just before the valid range, use the "begin" iterator
				int insertIterator = Math.Max(0, indexInValidElements);

				// Note: we are allowed to place a null on the edge here during generation, under the assumption that
				// the panel's generate will clean this sentinel up if another non-sentinel isn't placed after it.
				// However, it doesn't do us any good to have a valid range consisting entirely of a sentinel, so no-op
				if (weakRef is not null || validElements.Count != 0)
				{
					validElements.Insert(insertIterator, weakRef);

					// We inserted in front of the range, so update the first item index
					if (indexInValidElements <= 0)
					{
						m_indicesOfFirstValidElement[(int)type] = dataIndex;
					}
				}
			}
			else
			{
				// Just replacing an existing element
				int indexInValidElements = dataIndex - m_indicesOfFirstValidElement[(int)type];
				global::System.Diagnostics.Debug.Assert(0 <= indexInValidElements && indexInValidElements < GetValidElementCount(type));

				global::System.Diagnostics.Debug.Assert(validElements[indexInValidElements] is null || weakRef is null);

				if (validElements[indexInValidElements] is not null)
				{
					// If we're replacing an existing element with a sentinel, let's pull the old one out of the visual collection
					RemoveFromVisualChildren(
						type,
						dataIndex,
						ResolveWeakElement(validElements[indexInValidElements]),
						false /* isForElementRemoval */);
				}

				if (validElements.Count > 1 || weakRef is not null)
				{
					// If we're placing a valid, non-sentinel, or we have other elements in the valid range
					// Go ahead and place it
					validElements[indexInValidElements] = weakRef;
				}
				else
				{
					// If we're replacing the only element in the range with a sentinel, let's just blow away the range
					global::System.Diagnostics.Debug.Assert(validElements.Count == 1 && weakRef is null);
					global::System.Diagnostics.Debug.Assert(m_elementIndicesForChildren[(int)type].Count == 0);
					validElements.Clear();
					m_indicesOfFirstValidElement[(int)type] = -1;
				}
			}

			AddToVisualChildren(type, dataIndex, spElement);

			// Make sure that we either have an empty collection (adding a sentinel to an empty collection should remain empty)
			// or that our range is contiguous and valid
			global::System.Diagnostics.Debug.Assert((spElement is null && validElements.Count == 0 && m_elementIndicesForChildren[(int)type].Count == 0) ||
				(m_indicesOfFirstValidElement[(int)type] <= dataIndex) && (dataIndex < m_indicesOfFirstValidElement[(int)type] + GetValidElementCount(type))); // test for contiguousness
		}

		// Place a sentinel object into the valid element block
		internal void InsertSentinelElement(ElementType type, int dataIndex)
		{
			// We should only be doing this if it needs to go into the interior or edge of the valid block
			global::System.Diagnostics.Debug.Assert(m_indicesOfFirstValidElement[(int)type] <= dataIndex && dataIndex <= m_indicesOfFirstValidElement[(int)type] + GetValidElementCount(type));

			m_validElements[(int)type].Insert(GetValidElementIndexFromDataIndex(type, dataIndex), null);
		}

		// Removes an element from the valid range. It is now considered to be junked and we are probably calling this because we are recycling.
		// In some cases we are removing a item/group from the itemcollection and we do not want to update the dataindexFromFirstValidElement (itemBeingRemoved).
		// Generated elements get moved to the junk sections; ungenerated elements get removed from the children.
		// If specified, gateIndex represents the limit of ungenerated elements removal: it is used to ensure that the element being realized
		// does not get disjoint from the range of elements after this cleanup phase
		internal void RemoveFromValidElements(
			ElementType type,
			int indexInValidElements,
			bool isForDataRemoval,
			int gateIndex)
		{
			int dataIndex = GetDataIndexFromValidIndex(type, indexInValidElements);

			UIElement? spElement = GetElementAtValidIndex(type, indexInValidElements);
			RemoveFromVisualChildren(type, dataIndex, spElement, isForDataRemoval);

			var validElements = m_validElements[(int)type];

			// Remove the slot from our realized containers deque
			// Careful now... If we remove from an end, we'll also clean up any nulls left over on that end
			// While cleaning the nulls left over, ensure that we are not trimming too much
			// and making the element being realized and the range disjoints
			// But we're not going to clean up the other end, because it's probably in the middle of generating things
			if (indexInValidElements == 0)
			{
				validElements.RemoveFromFront();
				if (!isForDataRemoval)
				{
					// Only do this check on the first removal
					// The others are placeholders that we are popping off the range
					++m_indicesOfFirstValidElement[(int)type];
				}
				while (validElements.Count != 0 && validElements[0] is null
					&& ((gateIndex == -1) || (m_indicesOfFirstValidElement[(int)type] < gateIndex)))
				{
					validElements.RemoveFromFront();
					++m_indicesOfFirstValidElement[(int)type];
				}
			}
			else if (indexInValidElements == validElements.Count - 1)
			{
				do
				{
					validElements.RemoveFromBack();
				} while (validElements.Count != 0 && validElements[validElements.Count - 1] is null
					&& ((gateIndex == -1) || (m_indicesOfFirstValidElement[(int)type] + (validElements.Count - 1) > gateIndex)));
			}
			else
			{
				validElements.RemoveAt(indexInValidElements);
			}

			// Update the following indices in the child map
			if (isForDataRemoval)
			{
				var indices = m_elementIndicesForChildren[(int)type];
				for (int i = 0; i < indices.Count; ++i)
				{
					global::System.Diagnostics.Debug.Assert(indices[i] != dataIndex);
					if (dataIndex < indices[i])
					{
						--indices[i];
					}
				}
			}

			// Since our set of elements is empty, let's make sure our other data is consistent with this state
			if (validElements.Count == 0)
			{
				m_indicesOfFirstValidElement[(int)type] = -1;
				global::System.Diagnostics.Debug.Assert(m_elementIndicesForChildren[(int)type].Count == 0);
			}
		}

		// Mark all valid containers as junked
		internal void RemoveAllValidElements(ElementType type)
		{
			var spChildren = m_owner.m_cacheManager.GetChildren();

			// notify that it is no longer of interest
			while (SizeOfVisualSection(type) > 0)
			{
				int childCollectionIndex = StartOfVisualSection(type) + SizeOfVisualSection(type) - 1;
				UIElement spChild = spChildren[childCollectionIndex];
				ModernCollectionBasePanel.SetElementIsRealized(spChild, false);

				bool forceRemove = false;
				if (!ModernCollectionBasePanel.GetElementIsGenerated(spChild))
				{
					forceRemove = !GetIsElementPinned(type, spChild, out _);
				}

				if (type == ElementType.ItemContainer && forceRemove)
				{
					spChildren.RemoveAt(childCollectionIndex);
				}
				else
				{
					((UIElementCollection)spChildren).MoveInternal(childCollectionIndex, StartOfGarbageSection());
				}
				m_elementIndicesForChildren[(int)type].RemoveFromBack();
			}

			m_indicesOfFirstValidElement[(int)type] = -1;
			m_validElements[(int)type].Clear();
			global::System.Diagnostics.Debug.Assert(m_elementIndicesForChildren[(int)type].Count == 0);
		}

		// Place the container in the garbage section
		internal void PlaceInGarbageRegionIfNotInChildren(UIElement spElement)
		{
			var spChildren = m_owner.m_cacheManager.GetChildren();
			bool found = spChildren.IndexOf(spElement) >= 0;
			if (!found)
			{
				spChildren.Add(spElement);
			}
		}

		// NOTE: This method is much slower than getting elements from the valid ranges, because this is going to access the collection of visual children.
		internal UIElement? GetVisualChild(int visualChildIndex)
		{
			var spChildren = m_owner.m_cacheManager.GetChildren();
			UIElement spChild = spChildren[visualChildIndex];

			return spChild;
		}

		// Trim any leftover edges for both headers and containers
		// Wrapped up so clients don't have to manually handle both types
		internal void TrimEdgeSentinels()
		{
			TrimEdgeSentinels(ElementType.GroupHeader);
			TrimEdgeSentinels(ElementType.ItemContainer);
		}

		// Cleans up remaining edge sentinels for one type of element
		private void TrimEdgeSentinels(ElementType type)
		{
			// All we're doing here is searching the vectors and erasing elements
			// None of these operations allocate any memory, so we're pretty safe from exceptions

			var validElements = m_validElements[(int)type];

			// Trim the far end
			// Search backwards from the end of the range, looking for the first non-null ref
			int lastValid = validElements.Count - 1;
			while (lastValid >= 0 && validElements[lastValid] is null)
			{
				--lastValid;
			}
			// And erase everything from the element after it to the end
			validElements.RemoveRange(lastValid + 1, validElements.Count - (lastValid + 1));

			// Now, let's trim the front ends
			// Find the position of the first non-null element
			int firstValid = 0;
			while (firstValid < validElements.Count && validElements[firstValid] is null)
			{
				++firstValid;
			}
			// Calculate the distance so we can update our first valid index
			int removalCount = firstValid;
			// Erase the nulls
			validElements.RemoveRange(0, firstValid);
			// Increment the first valid index
			m_indicesOfFirstValidElement[(int)type] += removalCount;
		}

		// Helper to remove a realized element from the visual children
		private void RemoveFromVisualChildren(
			ElementType type,
			int index,
			UIElement? spElement,
			bool isForElementRemoval)
		{
			if (spElement is not null)
			{
				var currentMap = m_elementIndicesForChildren[(int)type];
				int childCollectionIterator;
				bool removed = false;

				// Optimize for the panning case (removing from ends) to avoid unneeded searches
				global::System.Diagnostics.Debug.Assert(currentMap.Count != 0);
				if (index == currentMap[0])
				{
					childCollectionIterator = 0;
				}
				else if (index == currentMap[currentMap.Count - 1])
				{
					childCollectionIterator = currentMap.Count - 1;
				}
				else
				{
					childCollectionIterator = LowerBound(currentMap, index);
				}

				// If we have a valid element, this index should exist in the map
				global::System.Diagnostics.Debug.Assert(childCollectionIterator != currentMap.Count &&
					currentMap[childCollectionIterator] == index);
				int childIndex = StartOfVisualSection(type) + childCollectionIterator;

				var spChildren = m_owner.m_cacheManager.GetChildren();

#if DEBUG
				{
					// If our maps get out of sync, we are in big trouble, so let's do some generous sanity-checking here against the actual visual children
					int foundIndex = spChildren.IndexOf(spElement);
					bool found = foundIndex >= 0;
					global::System.Diagnostics.Debug.Assert(found);
					global::System.Diagnostics.Debug.Assert(StartOfVisualSection(type) <= foundIndex &&
						foundIndex < EndOfVisualSection(type));
					global::System.Diagnostics.Debug.Assert(foundIndex == childIndex);
				}
#endif

				var isPinned = GetIsElementPinned(type, spElement, out _);

				// Handle non-generated containers differently.
				// If it is not pinned nor focused, remove it from the tree.
				// Otherwise put it in garbage section.  It will be removed from visual tree when it is unpinned/loses focus.

				// Containers have a special case for transitions and items that are their own container
				// Item containers are removed from the children collection to show their remove transition.
				bool removeItemContainerFromChildrenCollection =
					type == ElementType.ItemContainer &&
					(isForElementRemoval || (!ModernCollectionBasePanel.GetElementIsGenerated(spElement) && !isPinned));

				// As for group headers, to avoid infinite growth of the garbage section, we limit the number of headers in there to 16
				// or 4 times the CacheLength, whichever is larger (note that the default CacheLength is 4).
				// Applications that consistently provide brand new headers via the ChoosingGroupHeaderContainer event will thus not add
				// an ever growing number of visuals in the garbage section for instance.
				bool removeGroupHeaderFromChildrenCollection =
					type == ElementType.GroupHeader &&
					ModernCollectionBasePanel.GetElementIsGenerated(spElement) && !isPinned;

				if (removeGroupHeaderFromChildrenCollection)
				{
					uint maxHeadersInGarbageSection = (uint)Math.Max(16, (int)(m_owner.m_cacheLength * 4));
					uint headersInGarbageSection = 0;
					uint garbageStart = (uint)StartOfGarbageSection();
					uint childrenCount = (uint)spChildren.Count;

					for (uint garbageIndex = garbageStart; garbageIndex < childrenCount; ++garbageIndex)
					{
						UIElement currentElement = spChildren[(int)garbageIndex];

						if ((ModernCollectionBasePanel.GetElementIsHeader(currentElement) ? 1 : 0) == (int)ElementType.GroupHeader)
						{
							headersInGarbageSection++;

							if (headersInGarbageSection == maxHeadersInGarbageSection)
							{
								break;
							}
						}
					}

					removeGroupHeaderFromChildrenCollection = headersInGarbageSection == maxHeadersInGarbageSection;
				}

				if (removeItemContainerFromChildrenCollection || removeGroupHeaderFromChildrenCollection)
				{
					spChildren.RemoveAt(childIndex);
					removed = true;
				}
				else
				{
					// Move it to the junk section
					((UIElementCollection)spChildren).MoveInternal(childIndex, StartOfGarbageSection());
				}

				if (!isPinned ||
					removed)
				{
					// Keep pinned elements which are still in visual tree realized until they are unpinned.
					// This is to keep rendering Chrome when dragging an element.
					ModernCollectionBasePanel.SetElementIsRealized(spElement, false);
				}

				// Remove the entry from the child index map
				currentMap.RemoveAt(childCollectionIterator);
			}
		}

		// Perform cleanup once element is unpinned and outside of valid window.
		private void ElementUnpinned(
			ElementType type,
			int index,
			UIElement spElement,
			bool removeNonGeneratedContainer)
		{
			global::System.Diagnostics.Debug.Assert(spElement is not null);

			// Element can still be pinned since there are two reasons for pinning (focus + explicit pin).
			if (!GetIsElementPinned(type, spElement, out _) &&
				m_owner.m_windowState.validWindowCalculation &&
				!IsValidElementIndexWithinBounds(type, GetValidElementIndexFromDataIndex(type, index)))
			{
				if (removeNonGeneratedContainer &&
					!ModernCollectionBasePanel.GetElementIsGenerated(spElement))
				{
					// This element was moved to garbage area of visual tree to keep its focus.
					// Now it's time to remove it.
					var spChildren = m_owner.m_cacheManager.GetChildren();
					int foundIndex = spChildren.IndexOf(spElement);
					bool found = foundIndex >= 0;

					global::System.Diagnostics.Debug.Assert(found);
					spChildren.RemoveAt(foundIndex);

					// to avoid re-parenting we should mark NonGeneratedElement that it did't leave the tree in current layout cycle.
#if !HAS_UNO // TODO Uno: no CUIElement entering/leaving state.
					CoreImports.UIElement_SetIsLeaving(spElement, false);
#endif
				}

				// For all cases, disable rendering Chrome on once it's not within valid range.
				ModernCollectionBasePanel.SetElementIsRealized(spElement, false);
			}
		}

		private void AddToVisualChildren(
			ElementType type,
			int index,
			UIElement? spElement)
		{
			if (spElement is not null)
			{
				var currentMap = m_elementIndicesForChildren[(int)type];
				int childCollectionIterator;

				// Optimize for the panning case (adding to ends) to avoid unneeded searches
				if (currentMap.Count == 0 || index < currentMap[0])
				{
					childCollectionIterator = 0;
				}
				else if (index > currentMap[currentMap.Count - 1])
				{
					childCollectionIterator = currentMap.Count;
				}
				else
				{
					childCollectionIterator = LowerBound(currentMap, index);
				}

				// If we aren't processing a newly-inserted element, then we shouldn't already have an element for this index
				global::System.Diagnostics.Debug.Assert(childCollectionIterator == currentMap.Count || currentMap[childCollectionIterator] != index);

				int destinationChildIndex = StartOfVisualSection(type) + childCollectionIterator;

				var spChildren = m_owner.m_cacheManager.GetChildren();

				// TODO: This is a relatively slow linear search. This could be faster if we had knowledge
				// of the recycle queue's contents
				int foundIndex = spChildren.IndexOf(spElement);
				bool found = foundIndex >= 0;

				// Make sure our caller isn't trying to reinsert an existing container
				global::System.Diagnostics.Debug.Assert(!found || foundIndex >= StartOfGarbageSection());

				// mark as interesting
				ModernCollectionBasePanel.SetElementIsRealized(spElement, true);

				if (!found)
				{
					spChildren.Insert(destinationChildIndex, spElement);
				}
				else
				{
					((UIElementCollection)spChildren).MoveInternal(foundIndex, destinationChildIndex);
#if !HAS_UNO // TODO Uno: no CUIElement entering/leaving state.
					CoreImports.UIElement_SetIsEntering(spElement, true);
#endif
				}
				currentMap.Insert(childCollectionIterator, index);
			}
		}

		internal partial class PinnedElementInfo
		{
			internal PinnedElementInfo(WeakReference<UIElement>? wrElement, int index)
			{
				m_wrElement = wrElement;
				m_index = index;
			}
		}

		internal object? Interface_DataItemFromElement(ElementType type, DependencyObject element)
		{
			object? returnValue = null;
			UIElement? spElementAsUI = element as UIElement;

			// Convert to UIElement and inspect its virtualization information
			if (spElementAsUI is not null)
			{
				// Check the VirtualizationInfo. If somebody hands us a container we had nothing to do with
				// we should probably just return nullptr, rather than AV.
				VirtualizationInformation? pInfo = GetVirtualizationInformationFromElement(spElementAsUI);
				if (pInfo is not null)
				{
					returnValue = pInfo.Item;
				}
			}

			return returnValue;
		}

		internal int Interface_IndexFromElement(ElementType type, DependencyObject element, bool excludeHiddenEmptyGroups)
		{
			int returnValue = -1;

			var validElements = m_validElements[(int)type];

			// First, check the preparation holding area, as we expect this to match frequently, and it saves us from
			// doing a linear search on the realized element set
			if (m_preparationHoldingArea.Valid() &&
				type == m_preparationHoldingArea.m_type &&
				element == m_preparationHoldingArea.m_element)
			{
				returnValue = m_preparationHoldingArea.m_index;
			}
			else
			{
				// perf optimization: we usually call index from element on elements adjacent to eachother.
				// so do a 'fanning' out search.

				int lastFoundAtIndex = m_lastFoundIndexElements[(int)type];
				long foundAtIndex = -1;
				if (lastFoundAtIndex < 0 || lastFoundAtIndex >= validElements.Count)
				{
					// nothing of use here
					lastFoundAtIndex = 0;
				}

				int forward_iterator = lastFoundAtIndex;
				int reverse_iterator = lastFoundAtIndex - 1;

				while (forward_iterator != validElements.Count || reverse_iterator >= 0)
				{
					if (forward_iterator != validElements.Count)
					{
						if (ResolveWeakElement(validElements[forward_iterator]) == element)
						{
							foundAtIndex = forward_iterator;
							break;
						}
						++forward_iterator;
					}
					if (reverse_iterator >= 0)
					{
						if (ResolveWeakElement(validElements[reverse_iterator]) == element)
						{
							foundAtIndex = reverse_iterator;
							break;
						}
						--reverse_iterator;
					}
				}

				m_lastFoundIndexElements[(int)type] = (int)foundAtIndex;

				if (foundAtIndex > -1)
				{
					// in the regular valid elements section
					returnValue = GetDataIndexFromValidIndex(type, (int)foundAtIndex);
				}
				else
				{
					// could be in the pinned containers collection
					var pinnedElements = m_pinnedElements[(int)type];
					var foundPinnedElementInfo = pinnedElements.Find(elem => elem.GetElement() == element);

					if (foundPinnedElementInfo is not null)
					{
						returnValue = foundPinnedElementInfo.GetIndex();
					}

					// maybe through focused?
					if (type == ElementType.ItemContainer)
					{
						if (m_focusedContainer.GetIndex() > -1 && m_focusedContainer.GetElement() == element)
						{
							returnValue = m_focusedContainer.GetIndex();
						}
					}
				}
			}

			if (excludeHiddenEmptyGroups)
			{
				returnValue = (int)(uint)m_owner.m_cacheManager.DataIndexToLayoutIndex(type, returnValue);
			}

			return returnValue;
		}

		internal DependencyObject? Interface_ElementFromDataIndex(ElementType type, int index)
		{
			UIElement? spElement;

			int elementIndex = GetValidElementIndexFromDataIndex(type, index);
			if (IsValidElementIndexWithinBounds(type, elementIndex))
			{
				spElement = GetElementAtValidIndex(type, elementIndex);
			}
			else
			{
				if (m_preparationHoldingArea.Valid() &&
					type == m_preparationHoldingArea.m_type &&
					index == m_preparationHoldingArea.m_index)
				{
					spElement = m_preparationHoldingArea.m_element;
				}
				else
				{
					spElement = GetElementFromPinnedElements(type, index);
				}
			}

			return spElement;
		}

		internal DependencyObject? Interface_ElementFromDataItem(ElementType type, object? dataItem)
		{
			DependencyObject? returnValue = null;
			bool itemMatch = false;

			// We have an api on IGeneratorHost that allows a listview to give us a suggestion.
			// Let's use it! probably gives us null anyway.
			if (type == ElementType.ItemContainer)
			{
				IGeneratorHost? spIHost = m_owner.m_cacheManager.GetItemsHost();
				if (spIHost is not null)
				{
					DependencyObject? spSuggested = spIHost.SuggestContainerForContainerFromItemLookup();
					if (spSuggested is UIElement suggestedElement)
					{
						itemMatch = Equals(GetItemFromElement(suggestedElement), dataItem);

						if (itemMatch)
						{
							returnValue = spSuggested;
						}
					}
				}
			}

			// Look in the preparation holding area first, as this will save us a potentially expensive search
			if (!itemMatch && m_preparationHoldingArea.Valid() &&
				type == m_preparationHoldingArea.m_type)
			{
				itemMatch = Equals(GetItemFromElement(m_preparationHoldingArea.m_element!), dataItem);
				if (itemMatch)
				{
					returnValue = m_preparationHoldingArea.m_element;
				}
			}

			if (!itemMatch)
			{
				// fallback logic (slower)
				UIElement? foundElement = null;
				foreach (var weakRef in m_validElements[(int)type])
				{
					UIElement? spCandidateElement = ResolveWeakElement(weakRef);
					if (spCandidateElement is not null)
					{
						bool areEqual = Equals(GetItemFromElement(spCandidateElement), dataItem);
						if (areEqual)
						{
							foundElement = spCandidateElement;
							break;
						}
					}
				}

				if (foundElement is not null)
				{
					returnValue = foundElement;
				}
				else
				{
					// nothing found, let's not forget the pinned headers
					var foundPinnedElementInfo = m_pinnedElements[(int)type].Find(pinnedElement =>
					{
						UIElement? element = pinnedElement.GetElement();
						if (element is not null)
						{
							bool areEqual = Equals(GetItemFromElement(element), dataItem);
							return areEqual;
						}
						else
						{
							return false;
						}
					});

					if (foundPinnedElementInfo is not null)
					{
						returnValue = foundPinnedElementInfo.GetElement();
					}
					else if (type == ElementType.ItemContainer)
					{
						if (m_focusedContainer.GetIndex() > -1)
						{
							UIElement? element = m_focusedContainer.GetElement();
							if (element is not null)
							{
								bool areEqual = Equals(GetItemFromElement(element), dataItem);
								if (areEqual)
								{
									returnValue = element;
								}
							}
						}
					}
				}
			}

			return returnValue;
		}

		internal DependencyObject? Interface_GroupHeaderContainerFromItemContainerImpl(DependencyObject pItemContainer)
		{
			DependencyObject? returnValue = null;

			// make sure we are grouped
			if (m_owner.m_cacheManager.IsGrouping())
			{
				int index = -1;
				int indexOfGroup = -1;
				int indexInsideGroup = -1;
				int countInGroup = -1;

				// get index of container
				index = IICM_IndexFromContainer(pItemContainer);

				if (index >= 0)
				{
					// get container index to group index
					m_owner.m_cacheManager.GetGroupInformationFromItemIndex(index, out indexOfGroup, out indexInsideGroup, out countInGroup);

					// if container exists group must exist
					global::System.Diagnostics.Debug.Assert(indexOfGroup >= 0);

					// get group header from group index
					returnValue = IGHM_HeaderFromIndex(indexOfGroup);

					// if container exists group header must exist
					global::System.Diagnostics.Debug.Assert(returnValue is not null);
				}
			}

			return returnValue;
		}

		// returns an array of indices of all pinned elements by type
		internal List<uint> GetPinnedElementsIndexVector(ElementType type)
		{
			List<uint> returnValue = new();

#if !HAS_UNO // TODO Uno: WinAppSDK containment gate has no equivalent, the change is always on.
			if (WinAppSdk.Containment.IsChangeEnabled<WINAPPSDK_CHANGEID_62542953>())
#endif
			{
				returnValue.Capacity = m_pinnedElements[(int)type].Count;
			}

			foreach (var element in m_pinnedElements[(int)type])
			{
				returnValue.Add((uint)element.GetIndex());
			}

			return returnValue;
		}

		// Given an element index in the Items(Source), grab the actual pinned element (if it is actually pinned).
		internal UIElement? GetElementFromPinnedElements(ElementType type, int index)
		{
			UIElement? spElement = null;

			// focused containers are not necessarily in the pinned container collection, but do act as though they are pinned
			if (type == ElementType.ItemContainer && m_focusedContainer.GetIndex() > -1 && m_focusedContainer.GetIndex() == index)
			{
				spElement = m_focusedContainer.GetElement();
				global::System.Diagnostics.Debug.Assert(spElement is not null);
			}
			else if (type == ElementType.GroupHeader && m_focusedHeaderContainer.GetIndex() > -1 && m_focusedHeaderContainer.GetIndex() == index)
			{
				spElement = m_focusedHeaderContainer.GetElement();
				global::System.Diagnostics.Debug.Assert(spElement is not null);
			}
			else
			{
				// regular pinned
				var foundPinnedElementInfo = m_pinnedElements[(int)type].Find(pinnedElement => pinnedElement.GetIndex() == index);

				if (foundPinnedElementInfo is not null)
				{
					spElement = foundPinnedElementInfo.GetElement();
					global::System.Diagnostics.Debug.Assert(spElement is not null);
				}
			}

			return spElement;
		}

		// Given a data index in the Items(Source) and the element, add it to the pinned elements.
		internal void RegisterPinnedElement(ElementType type, int index, UIElement spElement)
		{
			// NOTE: We can't assert that the element is realized because we temporarily pin a partially realized
			// element during element generation, but we can assert that the element isn't a sentinel.
			global::System.Diagnostics.Debug.Assert(spElement is not null);

			// Let's also verify that we aren't re-pinning an existing index
#if DEBUG
			var it = m_pinnedElements[(int)type].Find(info => info.GetIndex() == index);
			global::System.Diagnostics.Debug.Assert(it is null);
#endif

			WeakReference<UIElement> wrElement = new(spElement);

			m_pinnedElements[(int)type].Add(
				new ModernCollectionBasePanel.ContainerManager.PinnedElementInfo(wrElement, index)
				);
		}

		// Given a data index in the Items(Source), remove the corresponding element from the pinned elements (if present).
		internal void UnregisterPinnedElement(ElementType type, int index)
		{
			// optimize for elements that have _just_ been added
			var pinnedElements = m_pinnedElements[(int)type];
			if (pinnedElements.Count != 0)
			{
				UIElement? spRemovedContainer = null;

				if (pinnedElements[pinnedElements.Count - 1].GetIndex() == index)
				{
					spRemovedContainer = pinnedElements[pinnedElements.Count - 1].GetElement();
					pinnedElements.RemoveAt(pinnedElements.Count - 1);
				}
				else
				{
					int foundPinnedElementInfo = pinnedElements.FindIndex(pinnedElement => pinnedElement.GetIndex() == index);

					if (foundPinnedElementInfo >= 0)
					{
						spRemovedContainer = pinnedElements[foundPinnedElementInfo].GetElement();
						pinnedElements.RemoveAt(foundPinnedElementInfo);
					}
				}

				if (spRemovedContainer is not null)
				{
					ElementUnpinned(
						type,
						index,
						spRemovedContainer,
						true /* removeNonGeneratedContainer */);
				}
			}
		}

		// Keep only the pinned containers that are approved by the given filter function.
		// The function takes the index and container as arguments, and should return TRUE for pinned containers to keep.
		internal void FilterPinnedContainers(Func<int, UIElement, bool> filterFunction)
		{
			var pinnedContainers = m_pinnedElements[(int)ElementType.ItemContainer];
			int iter = 0;

			while (iter != pinnedContainers.Count)
			{
				PinnedElementInfo info = pinnedContainers[iter];
				UIElement? spContainer = info.GetElement();
				int index = info.GetIndex();
				bool keep = false;

				keep = filterFunction(index, spContainer!);

				if (!keep)
				{
					pinnedContainers.RemoveAt(iter);

					ElementUnpinned(
						ElementType.ItemContainer,
						index,
						spContainer!,
						true /* removeNonGeneratedContainer */);
				}
				else
				{
					++iter;
				}
			}
		}

		// Is the given element pinned?
		internal bool GetIsElementPinned(
			ElementType type,
			UIElement? spElement,
			out int pinnedIndex)
		{
			// todo: if this starts becoming a perf issue, cache a boolean on pVirtualizationInformation on UIElement
			pinnedIndex = -1;

			bool isPinned = false;

			// focused containers are not necessarily in the pinned container collection, but do act as though they are pinned
			if (type == ElementType.ItemContainer && m_focusedContainer.GetElement() is not null && m_focusedContainer.GetElement() == spElement)
			{
				isPinned = true;
				pinnedIndex = m_focusedContainer.GetIndex();
			}
			else if (type == ElementType.GroupHeader && m_focusedHeaderContainer.GetElement() is not null && m_focusedHeaderContainer.GetElement() == spElement)
			{
				isPinned = true;
				pinnedIndex = m_focusedHeaderContainer.GetIndex();
			}
			else
			{
				var foundPinnedElementInfo = m_pinnedElements[(int)type].Find(pinnedElement => pinnedElement.GetElement() == spElement);

				if (foundPinnedElementInfo is not null)
				{
					isPinned = true;
					pinnedIndex = foundPinnedElementInfo.GetIndex();
				}

				// We shouldn't ever find a sentinel pinned element
				global::System.Diagnostics.Debug.Assert(foundPinnedElementInfo is null || foundPinnedElementInfo.GetElement() is not null);
			}

			return isPinned;
		}

		internal bool GetIsIndexPinned(ElementType type, int index)
		{
			var pinnedElements = m_pinnedElements[(int)type];
			var pinnedIterator = pinnedElements.Find(element => index == element.GetIndex());

			return pinnedIterator is not null;
		}

		// Called when an item is added to Items(Source). We need to know about this so we can update various internal indexes.
		internal void OnDataItemAdded(ElementType type, int index)
		{
			// First, if this new item is in front of our valid block, let's update the first index
			if (GetValidElementCount(type) > 0 && index <= m_indicesOfFirstValidElement[(int)type])
			{
				++m_indicesOfFirstValidElement[(int)type];
			}

			// If the item was added at the end, there's no existing indices to update.
			var indices = m_elementIndicesForChildren[(int)type];
			if (indices.Count != 0 && index <= indices[indices.Count - 1])
			{
				// Update the indices of any entry in the child collection that comes after the new insert
				for (int i = 0; i < indices.Count; ++i)
				{
					if (index <= indices[i])
					{
						++indices[i];
					}
				}
			}

			// focused containers are not necessarily in the pinned container collection, but do act as though they are pinned
			if (type == ElementType.ItemContainer && m_focusedContainer.GetIndex() >= index)
			{
				m_focusedContainer.UpdateIndex(m_focusedContainer.GetIndex() + 1);
			}

			if (type == ElementType.GroupHeader && m_focusedHeaderContainer.GetIndex() >= index)
			{
				m_focusedHeaderContainer.UpdateIndex(m_focusedHeaderContainer.GetIndex() + 1);
			}

			foreach (var pinnedElement in m_pinnedElements[(int)type])
			{
				if (pinnedElement.GetIndex() >= index)
				{
					pinnedElement.UpdateIndex(pinnedElement.GetIndex() + 1);
				}
			}
		}

		// Called when an item is removed from Items(Source). We need to know about this so we can update various internal indexes.
		internal void OnDataItemRemoved(ElementType type, int index)
		{
			UnregisterPinnedElement(type, index);

			// focused containers are not necessarily in the pinned container collection, but do act as though they are pinned
			if (type == ElementType.ItemContainer)
			{
				if (m_focusedContainer.GetIndex() > -1 && m_focusedContainer.GetIndex() > index)
				{
					m_focusedContainer.UpdateIndex(m_focusedContainer.GetIndex() - 1);
				}
				else if (m_focusedContainer.GetIndex() == index)
				{
					// reset the focusedContainer. Unregistering a pinnedcontainer should not do that for you
					// because there could be a different reason a focusedcontainer was also pinned
					ResetFocusedContainer(true);
				}
			}

			if (type == ElementType.GroupHeader)
			{
				if (m_focusedHeaderContainer.GetIndex() > -1 && m_focusedHeaderContainer.GetIndex() > index)
				{
					m_focusedHeaderContainer.UpdateIndex(m_focusedHeaderContainer.GetIndex() - 1);
				}
				else if (m_focusedHeaderContainer.GetIndex() == index)
				{
					// reset the focusedContainer. Unregistering a pinnedcontainer should not do that for you
					// because there could be a different reason a focusedcontainer was also pinned
					ResetFocusedHeaderContainer(true);
				}
			}

			foreach (var pinnedElement in m_pinnedElements[(int)type])
			{
				if (pinnedElement.GetIndex() > index)
				{
					pinnedElement.UpdateIndex(pinnedElement.GetIndex() - 1);
				}
			}
		}

		internal void OnDataItemReplaced(
			ElementType type,
			int index,
			UIElement pNewElement)
		{
			WeakReference<UIElement> newWeakRef = new(pNewElement);
			int childIndex = StartOfGarbageSection();

			var spChildren = m_owner.m_cacheManager.GetChildren();

			int validIndex = GetValidElementIndexFromDataIndex(type, index);
			if (0 <= validIndex && validIndex < m_validElements[(int)type].Count)
			{
				// Grab the old element
				UIElement spOldElement = ResolveWeakElement(m_validElements[(int)type][validIndex])!;

				// Find the location of this element in the visual children collection
				var currentIndexMap = m_elementIndicesForChildren[(int)type];
				int indexMapIterator = LowerBound(currentIndexMap, index);
				global::System.Diagnostics.Debug.Assert(indexMapIterator != currentIndexMap.Count);
				childIndex = StartOfVisualSection(type) + indexMapIterator;

				// Sanity check the index lookup
#if DEBUG
				{
					int foundIndex = spChildren.IndexOf(spOldElement);
					bool found = foundIndex >= 0;
					global::System.Diagnostics.Debug.Assert(found);
					global::System.Diagnostics.Debug.Assert(StartOfVisualSection(type) <= foundIndex &&
						foundIndex < EndOfVisualSection(type));
					global::System.Diagnostics.Debug.Assert(foundIndex == childIndex);
				}
#endif

				// Remove the old element
				if (type == ElementType.ItemContainer && (!ModernCollectionBasePanel.GetElementIsGenerated(spOldElement)))
				{
					// Containers have a special case for items that are their own container
					// Rip it right out of the children. For item containers, we do this to show the remove transition
					spChildren.RemoveAt(childIndex);
				}
				else
				{
					// Move it to the junk section
					((UIElementCollection)spChildren).MoveInternal(childIndex, StartOfGarbageSection());
				}
				ModernCollectionBasePanel.SetElementIsRealized(spOldElement, false);

				// Replace the valid element entry with the new element
				m_validElements[(int)type][validIndex] = newWeakRef;
			}

			// Place the new element
			int foundIndexOfNewElement = spChildren.IndexOf(pNewElement);
			bool foundNewElement = foundIndexOfNewElement >= 0;

			// Make sure we're not reinserting an existing container
			global::System.Diagnostics.Debug.Assert(!foundNewElement || foundIndexOfNewElement >= StartOfGarbageSection());
			if (!foundNewElement)
			{
				spChildren.Insert(childIndex, pNewElement);
			}
			else
			{
				((UIElementCollection)spChildren).MoveInternal(foundIndexOfNewElement, childIndex);
			}

			ModernCollectionBasePanel.SetElementIsRealized(pNewElement, true);
		}

		internal void SetFocusedContainer(int itemIndex, UIElement? spContainer)
		{
			global::System.Diagnostics.Debug.Assert((itemIndex >= 0 && spContainer is not null) || (itemIndex == -1 && spContainer is null)); // either we have a valid pair or an 'empty' pair (=reset)

			if (itemIndex != -1 && m_focusedContainer.GetIndex() > -1 && itemIndex != m_focusedContainer.GetIndex())  // if this is not a reset action (-1) then we should currently already be empty
			{
				// we should always have tested the current focused container and removed it if possible
				throw new InvalidOperationException();
			}

			ResetFocusedContainer(true);

			WeakReference<UIElement>? wrContainer = spContainer is null ? null : new WeakReference<UIElement>(spContainer);

			m_focusedContainer = new ModernCollectionBasePanel.ContainerManager.PinnedElementInfo(wrContainer, itemIndex);
		}

		internal void GetFocusedContainer(out int itemIndex, out UIElement? container)
		{
			UIElement? spContainer = null;
			int index = m_focusedContainer.GetIndex();

			if (index > -1)
			{
				spContainer = m_focusedContainer.GetElement();
			}

			container = spContainer;
			itemIndex = index;
		}

		internal void ResetFocusedContainer(bool removeNonGeneratedContainer)
		{
			UIElement? spPreviousFocusedElement = m_focusedContainer.GetElement();
			int previousFocusedIndex = m_focusedContainer.GetIndex();

			m_focusedContainer = new ModernCollectionBasePanel.ContainerManager.PinnedElementInfo();

			if (spPreviousFocusedElement is not null)
			{
				ElementUnpinned(
					ElementType.ItemContainer,
					previousFocusedIndex,
					spPreviousFocusedElement,
					removeNonGeneratedContainer);
			}
		}

		internal void SetFocusedHeaderContainer(int groupIndex, UIElement? spContainer)
		{
			global::System.Diagnostics.Debug.Assert((groupIndex >= 0 && spContainer is not null) || (groupIndex == -1 && spContainer is null)); // either we have a valid pair or an 'empty' pair (=reset)

			if (groupIndex != -1 && m_focusedHeaderContainer.GetIndex() > -1 && groupIndex != m_focusedHeaderContainer.GetIndex())  // if this is not a reset action (-1) then we should currently already be empty
			{
				// we should always have tested the current focused container and removed it if possible
				throw new InvalidOperationException();
			}

			ResetFocusedHeaderContainer(true);

			WeakReference<UIElement>? wrContainer = spContainer is null ? null : new WeakReference<UIElement>(spContainer);

			m_focusedHeaderContainer = new ModernCollectionBasePanel.ContainerManager.PinnedElementInfo(wrContainer, groupIndex);
		}

		internal void GetFocusedHeaderContainer(out int groupIndex, out UIElement? container)
		{
			UIElement? spContainer = null;
			int index = m_focusedHeaderContainer.GetIndex();

			if (index > -1)
			{
				spContainer = m_focusedHeaderContainer.GetElement();
			}

			container = spContainer;
			groupIndex = index;
		}

		internal void ResetFocusedHeaderContainer(bool removeNonGeneratedContainer)
		{
			UIElement? spPreviousFocusedElement = m_focusedHeaderContainer.GetElement();
			int previousFocusedIndex = m_focusedHeaderContainer.GetIndex();

			m_focusedHeaderContainer = new ModernCollectionBasePanel.ContainerManager.PinnedElementInfo();

			if (spPreviousFocusedElement is not null)
			{
				ElementUnpinned(
					ElementType.GroupHeader,
					previousFocusedIndex,
					spPreviousFocusedElement,
					removeNonGeneratedContainer);
			}
		}

		// Temporary holding for elements being prepared
		internal void HoldForPrepare(
			UIElement spElement,
			ElementType type,
			int index)
		{
			// Somebody is goofed up if this didn't happen
			global::System.Diagnostics.Debug.Assert(m_preparationHoldingArea.m_index == -1 && m_preparationHoldingArea.m_element is null);
			m_preparationHoldingArea.m_element = spElement;
			m_preparationHoldingArea.m_type = type;
			m_preparationHoldingArea.m_index = index;
		}

		internal void ResetAfterPrepare() => m_preparationHoldingArea.Reset();

		// std::lower_bound over a sorted deque
		private static int LowerBound(Deque<int> map, int value)
		{
			int first = 0;
			int count = map.Count;
			while (count > 0)
			{
				int step = count / 2;
				int it = first + step;
				if (map[it] < value)
				{
					first = it + 1;
					count -= step + 1;
				}
				else
				{
					count = step;
				}
			}
			return first;
		}
	}
}
