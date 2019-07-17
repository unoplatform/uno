using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Collections
{
	/// <summary>
	/// List using a materialized version to enumerate itself to prevent
	/// "Collection was modified" error.
	/// </summary>
	/// <remarks>
	/// THIS IS NOT THREAD-SAFE. It is designed to be used on
	/// the UI thread.
	/// </remarks>
	public class MaterializableList<T> : IList<T>, IReadOnlyList<T>
	{
		private readonly List<T> _innerList;

		public MaterializableList()
		{
			_innerList = new List<T>();
		}

		public MaterializableList(int capacity)
		{
			_innerList = new List<T>(capacity);
		}

		public MaterializableList(IEnumerable<T> collection)
		{
			_innerList = new List<T>(collection);
		}

		public IEnumerator<T> GetEnumerator() => Materialized.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Materialized.GetEnumerator();

		public void Add(T item)
		{
			_innerList.Add(item);
			_materialized = null;
		}

		public void Clear()
		{
			_innerList.Clear();
			_materialized = null;
		}

		public bool Contains(T item) => _innerList.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => _innerList.CopyTo(array, arrayIndex);

		public bool Remove(T item)
		{
			_materialized = null;
			return _innerList.Remove(item);
		}

		public int Count => _innerList.Count;

		public bool IsReadOnly => false;

		public int IndexOf(T item) => _innerList.IndexOf(item);

		public void Insert(int index, T item)
		{
			_innerList.Insert(index, item);
			_materialized = null;
		}

		public void RemoveAt(int index)
		{
			_innerList.RemoveAt(index);
			_materialized = null;
		}

		public T this[int index]
		{
			get => _innerList[index];
			set
			{
				_innerList[index] = value;
				_materialized = null;
			}
		}

		private List<T> _materialized;

		public List<T> Materialized => _materialized ?? (_materialized = _innerList.ToList());
	}
}
