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

		// #pragma endregion

		private readonly List<KeyValuePair<int /* index */, UIElement>> m_realizedChildren;
		// C# IEnumerator starts before the first element (-1), unlike WinRT IIterator which starts at 0.
		private int m_index = -1;
	}
}
