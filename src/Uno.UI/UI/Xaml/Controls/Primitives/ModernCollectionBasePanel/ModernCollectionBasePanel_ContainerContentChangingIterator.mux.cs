// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_ContainerContentChangingIterator.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	internal partial class ContainerContentChangingIterator
	{
		public ContainerContentChangingIterator()
		{
			m_pMCBP = null!;
			m_direction = PanelScrollingDirection.None;
			m_visibleStartInVector = -1;
			m_visibleEndInVector = -1;
			m_cacheEndInVector = -1;
			m_currentIndex = -1;
		}

		internal void Initialize(ModernCollectionBasePanel pPanel)
		{
			m_pMCBP = pPanel;

			// the cache/visible indicies will be fetched from the panel
			// notice how they are not guaranteed not to be stale: one scenario in which they are
			// plain and simply wrong is when you collapse/remove a panel from the tree and start
			// mutating the collection. Arrange will not get a chance to run after the mutation and
			// if we are still registered to do work, we will not be able to fetch the container.

			// translate to array indices
			if (m_pMCBP.m_lastCacheIndexBase > -1)  // -1 means there is nothing.. no visible or cached containers
			{
				m_direction = m_pMCBP.PanningDirectionBase;

				int firstVisibleIndexBase = m_pMCBP.m_firstVisibleIndexBase;
				int lastVisibleIndexBase = m_pMCBP.m_lastVisibleIndexBase;
				int firstCacheIndexBase = m_pMCBP.m_firstCacheIndexBase;
				int lastCacheIndexBase = m_pMCBP.m_lastCacheIndexBase;

				m_visibleStartInVector = firstVisibleIndexBase > -1 ? firstVisibleIndexBase - firstCacheIndexBase : 0;
				m_visibleEndInVector = lastVisibleIndexBase > -1 ? lastVisibleIndexBase - firstCacheIndexBase : m_visibleStartInVector;
				m_cacheEndInVector = lastCacheIndexBase - firstCacheIndexBase;
			}
			else
			{
				m_direction = PanelScrollingDirection.None;
				m_visibleStartInVector = -1;
				m_visibleEndInVector = -1;
				m_cacheEndInVector = -1;
			}

			Reset();
		}

		public uint Size
		{
			get
			{
				global::System.Diagnostics.Debug.Assert(m_pMCBP is not null);
				return (uint)(m_cacheEndInVector + 1);
			}
		}

		public DependencyObject? Current
		{
			get
			{
				int size = m_cacheEndInVector + 1;
				bool forward = m_direction != PanelScrollingDirection.Backward;
				int currentPositionInVector;

				global::System.Diagnostics.Debug.Assert(m_pMCBP is not null);
				global::System.Diagnostics.Debug.Assert(0 <= m_currentIndex && m_currentIndex < size);

				if (forward)
				{
					currentPositionInVector = m_currentIndex + m_visibleStartInVector;

					// We go forward until you reach the end of the forward buffer, and start in the opposite buffer
					// going in the opposite direction
					if (currentPositionInVector > m_cacheEndInVector)
					{
						// processing to the left
						// m_visibleStartInVector - (currentPositionInVector - m_cacheEndInVector) == m_cacheEndInVector - m_currentIndex
						currentPositionInVector = m_cacheEndInVector - m_currentIndex;
					}
				}
				else
				{
					currentPositionInVector = m_visibleEndInVector - m_currentIndex;

					// We go backward until you reach the start of the backward buffer, and start in the opposite buffer
					// going in the opposite direction
					if (currentPositionInVector < 0)
					{
						// processing to the right
						// m_visibleEndInVector + (-currentPositionInVector) == m_currentIndex
						currentPositionInVector = m_currentIndex;
					}
				}

				global::System.Diagnostics.Debug.Assert(0 <= currentPositionInVector && currentPositionInVector < size);
				return m_pMCBP.ContainerFromIndex(m_pMCBP.m_firstCacheIndexBase + currentPositionInVector);
			}
		}

		public bool MoveNext()
		{
			global::System.Diagnostics.Debug.Assert(m_pMCBP is not null);

			int size = m_cacheEndInVector + 1;

			++m_currentIndex;
			m_currentIndex = Math.Max(Math.Min(m_currentIndex, size), -1);
			return 0 <= m_currentIndex && m_currentIndex < size;
		}

		public void Reset()
		{
			global::System.Diagnostics.Debug.Assert(m_pMCBP is not null);
			m_currentIndex = -1;
		}
	}

	internal IContainerContentChangingIterator GetContainersForIncrementalVisualization()
	{
		if (m_lastCacheIndexBase > -1)  // -1 means there is nothing.. no visible or cached containers
		{
			if (m_spContainerContentChangingIterator is not null)
			{
				m_spContainerContentChangingIterator.Initialize(this);
			}
			else
			{
				m_spContainerContentChangingIterator = new ContainerContentChangingIterator();
				m_spContainerContentChangingIterator.Initialize(this);
			}

			return m_spContainerContentChangingIterator;
		}
		else
		{
			// Early out. We will return nullptr to signal there isn't any work for CCC to do.
			return null!;
		}
	}
}
