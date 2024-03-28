// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal partial class VirtualLayoutContextAdapter
	{
		private class ChildrenCollection : IReadOnlyList<UIElement>, IEnumerable<UIElement>
		{
			private VirtualizingLayoutContext m_context;

			public ChildrenCollection(VirtualizingLayoutContext context)
			{
				m_context = context;
			}

			public int Count => m_context.ItemCount;

			public UIElement this[int index] => m_context.GetOrCreateElementAt(index, ElementRealizationOptions.None);

			public IEnumerator<UIElement> GetEnumerator()
				=> new Iterator(this);

			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();
		}

		private class Iterator : IEnumerator<UIElement>
		{
			private readonly IReadOnlyList<UIElement> m_childCollection;
			private int m_currentIndex = -1; // UNO:: This is 0 on WinUI

			public Iterator(IReadOnlyList<UIElement> childCollection)
			{
				m_childCollection = childCollection;
			}

			object IEnumerator.Current => Current;
			public UIElement Current
			{
				get
				{
					if (m_currentIndex < m_childCollection.Count)
					{
						return m_childCollection[m_currentIndex];
					}
					else
					{
						throw new IndexOutOfRangeException();
					}
				}
			}

			public bool MoveNext()
			{
				if (m_currentIndex < m_childCollection.Count)
				{
					++m_currentIndex;
					return m_currentIndex < m_childCollection.Count;
				}
				else
				{
					throw new IndexOutOfRangeException();
				}
			}

			public void Reset() { }

			public void Dispose() { }
		}
	}
}
