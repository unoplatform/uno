// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_CollectionIterator_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal partial struct CollectionIterator
	{
		// Initializes the iterator at the beginning.
		internal CollectionIterator(CacheManager cache)
		{
			m_cache = cache;
			m_current = default;
			Init(0, cache.IsGrouping() ? ElementType.GroupHeader : ElementType.ItemContainer);
		}

		// Initializes the iterator at a location in the collection.
		internal CollectionIterator(CacheManager cache, int index, ElementType type)
		{
			m_cache = cache;
			m_current = default;
			Init(index, type);
		}

		// Used to initializes or re-initializes the iterator.
		internal void Init(int index, ElementType type)
		{
			m_current.type = type;

			if (m_current.isHeader())
			{
				// Don't use this to iterate over an empty collection.
				global::System.Diagnostics.Debug.Assert(index >= 0 && index < m_cache.GetTotalGroupCount());

				m_current.groupIndex = index;
				UpdateCurrentGroupInformation();
			}
			// isHeader is false if we are starting at a container or if we are
			// iterating over a non grouped collection.
			else
			{
				// Don't use this to iterate over an empty collection.
				global::System.Diagnostics.Debug.Assert(index >= 0 && index < m_cache.GetTotalItemCount());

				m_current.itemIndex = index;

				if (m_cache.IsGrouping())
				{
					int indexInsideGroup = 0;

					m_cache.GetGroupInformationFromItemIndex(
						m_current.itemIndex,
						out m_current.groupIndex,
						out indexInsideGroup,
						out m_current.itemCountInGroup);

					m_current.startItemIndex = m_current.itemIndex - indexInsideGroup;
				}
				// Non grouped collections are treated as a big group, except we stop at the first container
				// when iterating backward.
				else
				{
					m_current.groupIndex = 0;
					m_current.startItemIndex = 0;
					m_current.itemCountInGroup = m_cache.GetTotalItemCount();
				}
			}
		}

		// Try moving to the next element. Retruns true if it succeeds.
		internal bool MoveNext()
		{
			bool canMove = true;

			if (m_current.isHeader())
			{
				// If the current element is a header, the next element is a
				// container if the current group is not empty
				if (m_current.itemCountInGroup > 0)
				{
					// m_current.itemIndex is already set.
					m_current.type = ElementType.ItemContainer;
				}
				// if the current group is empty, then the next element is a
				// header assuming there is one.
				else if (m_current.groupIndex < m_cache.GetTotalGroupCount() - 1)
				{
					// m_current.itemIndex won't need to change for an empty group.
					++m_current.groupIndex;
					UpdateCurrentGroupInformation();
				}
				// Otherwise, we are at the end.
				else
				{
					canMove = false;
				}
			}
			else
			{
				// If the current element is a container, the next element is also
				// a container if there are more items in the current group.
				if (m_current.IndexInGroup() < m_current.itemCountInGroup - 1)
				{
					++m_current.itemIndex;
				}
				// If we are the last container of the current group, the next element
				// is a header assuming we are not at the end already.
				else if (m_current.groupIndex < m_cache.GetTotalGroupCount() - 1)
				{
					m_current.type = ElementType.GroupHeader;
					++m_current.groupIndex;
					UpdateCurrentGroupInformation();
				}
				// Otherwise, we are at the end.
				else
				{
					canMove = false;
				}
			}

			return canMove;
		}

		// Try moving to the previous element. Retruns true if it succeeds.
		internal bool MovePrevious()
		{
			bool canMove = true;

			if (m_current.isHeader())
			{
				// If we are already at the beginning, there is nowhere to move.
				if (m_current.groupIndex == 0)
				{
					canMove = false;
				}
				// Otherwise, retrieve information about the previous group.
				else
				{
					--m_current.groupIndex;
					UpdateCurrentGroupInformation();

					// If the previous (now current!) group is empty, we are done.
					// Otherwise, if it's not empty, we will move to the last container.
					if (m_current.itemCountInGroup > 0)
					{
						m_current.type = ElementType.ItemContainer;
						m_current.itemIndex = m_current.startItemIndex + m_current.itemCountInGroup - 1;
					}
				}
			}
			else
			{
				// If we are a container, the previous element is also a container if
				// we are not the first container of the group.
				if (m_current.IndexInGroup() > 0)
				{
					--m_current.itemIndex;
				}
				// Otherwise, it's the header of the current group if we are iterating over a
				// grouped collection.
				else if (m_cache.IsGrouping())
				{
					m_current.type = ElementType.GroupHeader;
				}
				else
				{
					canMove = false;
				}

			}

			return canMove;
		}
	}
}
