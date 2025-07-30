// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbIterator.cpp, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

internal partial class BreadcrumbIterator : IEnumerator<object?>
{
	private int m_currentIndex;
	private ItemsSourceView? m_breadcrumbItemsSourceView;
	private int m_size;

	internal BreadcrumbIterator(object? itemsSource)
	{
#if !HAS_UNO
		m_currentIndex = 0;
#else // Uno specific: IEnumerator starts on "-1" index as MoveNext is called first!
		m_currentIndex = -1;
#endif

		if (itemsSource != null)
		{
			m_breadcrumbItemsSourceView = new InspectingDataSource(itemsSource);

			// Add 1 to account for the leading null/ellipsis element
			m_size = m_breadcrumbItemsSourceView.Count + 1;
		}
		else
		{
			m_size = 1;
		}
	}

	public object? Current
	{
		get
		{
			if (m_currentIndex == 0)
			{
				return null;
			}
			else if (HasCurrent())
			{
				return m_breadcrumbItemsSourceView!.GetAt(m_currentIndex - 1);
			}
			else
			{
				throw new InvalidOperationException("Out of bounds");
			}
		}
	}

	object? IEnumerator.Current => Current;

	private bool HasCurrent()
	{
		return m_currentIndex < m_size;
	}

	//uint GetMany(array_view<object> items)
	//{
	//	uint howMany{ };
	//	if (HasCurrent())
	//	{
	//		do
	//		{
	//			if (howMany >= items.size()) break;

	//			items[howMany] = Current();
	//			howMany++;
	//		} while (MoveNext());
	//	}

	//	return howMany;
	//}

	public bool MoveNext()
	{
		if (HasCurrent())
		{
			++m_currentIndex;
			return HasCurrent();
		}
		else
		{
			throw new InvalidOperationException("Out of bounds");
		}
	}

	bool IEnumerator.MoveNext() => MoveNext();

	public void Reset() { }

	public void Dispose() { }
}
