// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ChildrenInTabFocusOrderIterable.cpp, commit ffa9bdad1

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

internal partial class ChildrenInTabFocusOrderIterable : IEnumerable<DependencyObject>
{
	private readonly ItemsRepeater m_repeater;

	public ChildrenInTabFocusOrderIterable(ItemsRepeater repeater)
	{
		m_repeater = repeater;
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public IEnumerator<DependencyObject> GetEnumerator() => new ChildrenInTabFocusOrderIterator(m_repeater);

	private class ChildrenInTabFocusOrderIterator : IEnumerator<DependencyObject>
	{
		private readonly List<KeyValuePair<int /* index */, UIElement>> m_realizedChildren;
		private int m_index = -1;

		public ChildrenInTabFocusOrderIterator(ItemsRepeater repeater)
		{
			var children = repeater.Children;
			m_realizedChildren = new List<KeyValuePair<int, UIElement>>(children.Count);

			// Filter out unrealized children.
			for (var i = 0; i < children.Count; ++i)
			{
				var element = children[i];
				var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
				if (virtInfo.IsRealized)
				{
					m_realizedChildren.Add(new KeyValuePair<int, UIElement>(virtInfo.Index, element));
				}
			}

			// Sort children by index.
			m_realizedChildren.Sort((lhs, rhs) => lhs.Key - rhs.Key);
		}

		object IEnumerator.Current => Current;

		public DependencyObject Current
		{
			get
			{
				if (m_index < m_realizedChildren.Count)
				{
					return m_realizedChildren[m_index].Value;
				}
				else
				{
					throw new IndexOutOfRangeException();
				}
			}
		}

		public bool MoveNext()
		{
			if (m_index < m_realizedChildren.Count)
			{
				++m_index;
				return m_index < m_realizedChildren.Count;
			}
			else
			{
				throw new IndexOutOfRangeException();
			}
		}

		public void Reset() => m_index = -1;

		public void Dispose() { }
	}
}
