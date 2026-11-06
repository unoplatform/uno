// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_ICTCP_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal ThemeTransitionContext GetChildTransitionContext(UIElement element, int layoutTickId)
	{
		bool isWrapping = false;

		bool isHeader = element.GetVirtualizationInformationOrNull()!.IsHeader;
		if (!isHeader)
		{
			isWrapping = m_spLayoutStrategy.GetIsWrappingStrategy();
		}

		return m_transitionContextManager.GetTransitionContext(layoutTickId, isWrapping);
	}

	// determines if mutations are going fast
	internal bool IsCollectionMutatingFast() => m_transitionContextManager.IsCollectionMutatingFast();

	internal Rect GetChildTransitionBounds(UIElement element)
	{
		Rect returnValue;
		int itemIndex = -1;
		global::System.Diagnostics.Debug.Assert(!GetVirtualizationInformationFromElement(element)!.IsHeader);
		itemIndex = m_containerManager.IICM_IndexFromContainer(element);

		if (m_cacheManager.IsGrouping())
		{
			int groupIndex = -1;

			m_cacheManager.GetGroupInformationFromItemIndex(itemIndex, out groupIndex, out _ /* pIndexInGroup */, out _ /* pItemCountInGroup */);

			returnValue = m_spLayoutStrategy.GetElementTransitionsBounds(
				ElementType.GroupHeader,
				groupIndex,
				m_windowState.GetRealizationWindow());
		}
		else
		{
			returnValue = m_spLayoutStrategy.GetElementTransitionsBounds(
				ElementType.ItemContainer,
				itemIndex,
				m_windowState.GetRealizationWindow());
		}

		return returnValue;
	}

	internal void NotifyOfItemsReordered(uint nCount) => m_transitionContextManager.NotifyOfItemsReordered(nCount);

	internal partial class TransitionContextManager
	{
		internal TransitionContextManager(ModernCollectionBasePanel owner)
		{
			m_owner = owner;    // no ref needed, TransitionContextManager is not at all exposed to the outside world
			m_elementCountAddedThisLayoutTick = 0;
			m_elementCountRemovedThisLayoutTick = 0;
			m_elementCountReorderedThisLayoutTick = 0;
			m_resetItemsThisLayoutTick = false;
			m_shouldIncrementallyLoad = false;
			m_isLoadingStarted = false;
			m_isLoading = false;
			m_currentTickCounterId = 0;
			m_startIncrementallyLoadingTime = 0;
			m_currentMutationTime = 0;
			m_previousMutationTime = 0;
		}

		internal void NotifyOfItemsChanging(CollectionChange action)
		{
			UpdateContextCounters(true /* isUpdateDueToMutation */);

			switch (action)
			{
				case CollectionChange.Reset:
					{
						m_resetItemsThisLayoutTick = true;
						break;
					}
				case CollectionChange.ItemInserted:
					{
						m_elementCountAddedThisLayoutTick += 1;
						// incremental loading should show Entrance transitions.
						m_isLoading = m_shouldIncrementallyLoad;
						break;
					}
				case CollectionChange.ItemRemoved:
					{
						m_elementCountRemovedThisLayoutTick += 1;
						break;
					}
			}
		}

		internal void NotifyOfItemsChanged(CollectionChange action)
		{
		}

		internal void NotifyOfGroupsChanged(CollectionChange action)
		{
			UpdateContextCounters(true /* isUpdateDueToMutation */);
		}

		internal void NotifyOfItemsReordered(uint nCount)
		{
			UpdateContextCounters(true /* isUpdateDueToMutation */);
			m_elementCountReorderedThisLayoutTick += (int)nCount;
		}

		internal bool IsCollectionMutatingFast()
		{
			uint currentTime = unchecked((uint)Environment.TickCount);

			return m_previousMutationTime != 0 && currentTime - m_previousMutationTime < fastmutationThreshold;
		}

		private void UpdateContextCounters(bool isUpdateDueToMutation)
		{
			short currentTickCounterId = 0;
			uint currentTime = unchecked((uint)Environment.TickCount);

			// load should no longer come into play
			m_isLoading = false;

			if (m_shouldIncrementallyLoad)
			{
				uint timeDelta = 0;

				if (currentTime < m_startIncrementallyLoadingTime)
				{
					// we pass 49.7 days and started all over again
					timeDelta = uint.MaxValue - m_startIncrementallyLoadingTime + currentTime;
				}
				else
				{
					timeDelta = currentTime - m_startIncrementallyLoadingTime;
				}

				if (timeDelta > incrementalLoadingDuration)
				{
					m_shouldIncrementallyLoad = false;
				}
			}

			currentTickCounterId = CoreImports.LayoutManager_GetLayoutTickForTransition(m_owner);

			// update timers
			if (isUpdateDueToMutation && currentTickCounterId != m_currentTickCounterId)
			{
				// The reason we only do it when it is on separate ticks is because changes get
				// coalesced into multi-add or multi-delete contexts, which we do have animations for.
				// It really is only a problem if they happen in separate ticks.

				// different tick, store time and move current to previous
				m_previousMutationTime = m_currentMutationTime;
				m_currentMutationTime = currentTime;
			}

			if (!m_shouldIncrementallyLoad)
			{
				if (currentTickCounterId != m_currentTickCounterId)
				{
					m_elementCountAddedThisLayoutTick = 0;
					m_elementCountRemovedThisLayoutTick = 0;
					m_elementCountReorderedThisLayoutTick = 0;
					m_resetItemsThisLayoutTick = false;
				}

				m_currentTickCounterId = currentTickCounterId;
			}
		}

		internal ThemeTransitionContext GetTransitionContext(int layoutTickId, bool isWrapping)
		{
			ThemeTransitionContext themeTransitionContext = ThemeTransitionContext.None;

			// loaded is an async event, so the tick might not come in until later
			if (!m_isLoadingStarted)
			{
				m_currentTickCounterId = layoutTickId;
				m_isLoadingStarted = true;
				m_isLoading = true;
				if (m_shouldIncrementallyLoad)
				{
					m_startIncrementallyLoadingTime = unchecked((uint)Environment.TickCount);
				}
			}
			else
			{
				if (m_currentTickCounterId != layoutTickId)
				{
					UpdateContextCounters(false /* isUpdateDueToMutation */);
					m_isLoading = m_shouldIncrementallyLoad;
				}
			}

			// If layoutTick is same as the one during Load, call EntraceTransition
			if (m_isLoading)
			{
				// Entrance Transition should happen only on First time, this animation runs only first time
				themeTransitionContext = ThemeTransitionContext.Entrance;
			}
			else
			{
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
					themeTransitionContext = isWrapping ? ThemeTransitionContext.SingleReorderGrid : ThemeTransitionContext.SingleReorderList;
				}
				else if (m_elementCountReorderedThisLayoutTick > 1)
				{
					// if multiple reorder happens
					themeTransitionContext = isWrapping ? ThemeTransitionContext.MultipleReorderGrid : ThemeTransitionContext.MultipleReorderList;
				}
				else if (m_elementCountAddedThisLayoutTick == 1 && m_elementCountRemovedThisLayoutTick == 0)
				{
					// if only 1 item gets added or removed
					themeTransitionContext = isWrapping ? ThemeTransitionContext.SingleAddGrid : ThemeTransitionContext.SingleAddList;
				}
				else if (m_elementCountAddedThisLayoutTick == 0 && m_elementCountRemovedThisLayoutTick == 1)
				{
					themeTransitionContext = isWrapping ? ThemeTransitionContext.SingleDeleteGrid : ThemeTransitionContext.SingleDeleteList;
				}
				else if (m_elementCountAddedThisLayoutTick >= 1 && m_elementCountRemovedThisLayoutTick >= 1)
				{
					// If there is a mix
					themeTransitionContext = isWrapping ? ThemeTransitionContext.MixedOperationsGrid : ThemeTransitionContext.MixedOperationsList;
				}
				else if (m_elementCountAddedThisLayoutTick > 0)
				{
					// if multiple items gets added/removed
					themeTransitionContext = isWrapping ? ThemeTransitionContext.MultipleAddGrid : ThemeTransitionContext.MultipleAddList;
				}
				else if (m_elementCountAddedThisLayoutTick == 0 && m_elementCountRemovedThisLayoutTick > 0)
				{
					// if multiple items gets added/removed
					themeTransitionContext = isWrapping ? ThemeTransitionContext.MultipleDeleteGrid : ThemeTransitionContext.MultipleDeleteList;
				}
			}

			return themeTransitionContext;
		}
	}
}
