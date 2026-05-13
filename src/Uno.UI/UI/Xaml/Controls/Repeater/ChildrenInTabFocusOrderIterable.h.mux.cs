// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ChildrenInTabFocusOrderIterable.h, commit 4b206bce3

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

partial class ChildrenInTabFocusOrderIterable
{
	private readonly ItemsRepeater m_repeater;

	private partial class ChildrenInTabFocusOrderIterator : IEnumerator<DependencyObject>
	{
		// #pragma region IIterable implementation

		public bool MoveNext()
		{
			if (m_index < m_realizedChildren.Count)
			{
				++m_index;
				return (m_index < m_realizedChildren.Count);
			}
			else
			{
				throw new IndexOutOfRangeException();
			}
		}

		// TODO Uno: WinRT IIterator::GetMany overload omitted; C# IEnumerator does not expose a
		// batched read API. Callers that need the batched form should iterate with MoveNext / Current.

		// #pragma endregion

		private readonly List<KeyValuePair<int /* index */, UIElement>> m_realizedChildren;
		// Uno specific: C# IEnumerator semantics require starting at -1 so that the first MoveNext()
		// advances to index 0. The WinRT iterator starts already positioned at index 0.
		private int m_index = -1;
	}
}
