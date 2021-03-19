using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class SwipeItems
	{
		/// <inheritdoc />
		public IEnumerator<SwipeItem> GetEnumerator()
			=> m_items.GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> m_items.GetEnumerator();

		/// <inheritdoc />
		public void Add(SwipeItem item)
			=> m_items.Add(item);

		/// <inheritdoc />
		public bool Contains(SwipeItem item)
			=> m_items.Contains(item);

		/// <inheritdoc />
		public void CopyTo(SwipeItem[] array, int arrayIndex)
			=> m_items.CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public bool Remove(SwipeItem item)
			=> m_items.Remove(item);

		/// <inheritdoc />
		public int Count => m_items.Count;

		/// <inheritdoc />
		public bool IsReadOnly => ((ICollection<SwipeItem>)m_items).IsReadOnly;

		/// <inheritdoc />
		public int IndexOf(SwipeItem item)
			=> m_items.IndexOf(item);

		/// <inheritdoc />
		public void Insert(int index, SwipeItem item)
			=> m_items.Insert(index, item);

		/// <inheritdoc />
		public void RemoveAt(int index)
			=> m_items.RemoveAt(index);

		/// <inheritdoc />
		public SwipeItem this[int index]
		{
			get => m_items[index];
			set => m_items[index] = value;
		}
	}
}
