#nullable disable

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

		public MaterializableList(List<T> collection)
		{
			_innerList = collection;
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
			ClearMaterialized();
		}

		public void Clear()
		{
			_innerList.Clear();
			ClearMaterialized();
		}

		public bool Contains(T item) => _innerList.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => _innerList.CopyTo(array, arrayIndex);

		public bool Remove(T item)
		{
			ClearMaterialized();
			return _innerList.Remove(item);
		}

		public int Count => _innerList.Count;

		public bool IsReadOnly => false;

		public int IndexOf(T item) => _innerList.IndexOf(item);

		public void Insert(int index, T item)
		{
			_innerList.Insert(index, item);
			ClearMaterialized();
		}

		public void RemoveAt(int index)
		{
			_innerList.RemoveAt(index);
			ClearMaterialized();
		}

		public T this[int index]
		{
			get => _innerList[index];
			set
			{
				_innerList[index] = value;
				ClearMaterialized();
			}
		}

		private List<T> _materialized;

		/// <summary>
		/// Get a materialized copy of the inner list. DON'T UPDATE IT!!
		/// </summary>
		/// <remarks>
		/// You should NEVER update directly this list or you can still produce a
		/// "Collection was modified" exception.
		/// </remarks>
		public List<T> Materialized => _materialized ?? (_materialized = _innerList.ToList());

		/// <summary>
		/// Get an exclusive copy of the inner list which can be freely updated
		/// without impacting any concurrent enumeration.
		/// </summary>
		public List<T> GetUpdatableCopy() => _innerList.ToList();

		/// <summary>
		/// Force this instance to regenerate a new `Materialized` instance.
		/// </summary>
		/// <remarks>
		/// Useful it you're planning to "own" the MaterializedList and update it.
		/// </remarks>
		public void ClearMaterialized()
		{
			_materialized = null;
		}
	}
}
