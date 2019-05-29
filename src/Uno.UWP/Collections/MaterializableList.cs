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
	public class MaterializableList<T> : IList<T>
	{
		private readonly List<T> _listImplementation = new List<T>();

		public IEnumerator<T> GetEnumerator() => Materialized.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Materialized.GetEnumerator();

		public void Add(T item)
		{
			_listImplementation.Add(item);
			_materialized = null;
		}

		public void Clear()
		{
			_listImplementation.Clear();
			_materialized = null;
		}

		public bool Contains(T item) => _listImplementation.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => _listImplementation.CopyTo(array, arrayIndex);

		public bool Remove(T item)
		{
			_materialized = null;
			return _listImplementation.Remove(item);
		}

		public int Count => _listImplementation.Count;

		public bool IsReadOnly => false;

		public int IndexOf(T item) => _listImplementation.IndexOf(item);

		public void Insert(int index, T item)
		{
			_listImplementation.Insert(index, item);
			_materialized = null;
		}

		public void RemoveAt(int index)
		{
			_listImplementation.RemoveAt(index);
			_materialized = null;
		}

		public T this[int index]
		{
			get => _listImplementation[index];
			set
			{
				_listImplementation[index] = value;
				_materialized = null;
			}
		}

		private List<T> _materialized;

		public List<T> Materialized => _materialized ?? (_materialized = _listImplementation.ToList());
	}
}
