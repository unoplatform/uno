using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	internal class SelectedItems<T> : IReadOnlyList<T>
	{
		public SelectedItems(
			IList<SelectedItemInfo> infos,
			Func<IList<SelectedItemInfo>, int, T> getAtImpl)
		{
			m_infos = infos;
			m_getAtImpl = getAtImpl;
			foreach (var info in infos)
			{
				if (info.Node.TryGetTarget(out var node))
				{
					m_totalCount += node.SelectedCount;
				}
				else
				{
					throw new InvalidOperationException("Selection changed after the SelectedIndices/Items property was read.");
				}
			}
		}

		~SelectedItems()
		{
			m_infos.Clear();
		}

		#region IVectorView<T>

		public int Count => m_totalCount;

		public T this[int index] => m_getAtImpl(m_infos, index);

		#endregion

		#region winrt::IIterable<T>

		//TODO: Verify IEnumerator implementation

		public IEnumerator<T> GetEnumerator() => new SelectedItemsEnumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private class SelectedItemsEnumerator : IEnumerator<T>
		{
			private readonly IReadOnlyList<T> m_selectedItems = null;
			private int m_currentIndex = -1;

			public SelectedItemsEnumerator(IReadOnlyList<T> selectedItems)
			{
				m_selectedItems = selectedItems;
			}

			public T Current
			{
				get
				{
					var items = m_selectedItems;
					if (m_currentIndex < items.Count)
					{
						return items[m_currentIndex];
					}
					else
					{
						throw new IndexOutOfRangeException();
					}
				}
			}

			object IEnumerator.Current => Current;

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (m_currentIndex < m_selectedItems.Count)
				{
					++m_currentIndex;
					return (m_currentIndex < m_selectedItems.Count);
				}
				else
				{
					throw new IndexOutOfRangeException();
				}
			}

			public void Reset() => m_currentIndex = -1;
		}

		#endregion

		private IList<SelectedItemInfo> m_infos;
		private Func<IList<SelectedItemInfo>, int, T> m_getAtImpl;
		private int m_totalCount = 0;
	}
}
