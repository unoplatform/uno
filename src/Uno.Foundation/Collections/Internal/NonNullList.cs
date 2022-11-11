using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.Foundation.Collections;

internal class NonNullList<T> : IList<T>
{
	private readonly List<T> _innerList = new List<T>();

	public T this[int index]
	{
		get => _innerList[index];
		set => _innerList[index] = value;
	}

	public int Count => _innerList.Count;

	public bool IsReadOnly => false;

	public void Add(T item)
	{
		if (item == null) throw new ArgumentNullException(nameof(item));
		_innerList.Add(item);
	}

	public void Clear() => _innerList.Clear();

	public bool Contains(T item) => _innerList.Contains(item);

	public void CopyTo(T[] array, int arrayIndex) => _innerList.CopyTo(array, arrayIndex);

	public IEnumerator<T> GetEnumerator() => _innerList.GetEnumerator();

	public int IndexOf(T item) => _innerList.IndexOf(item);

	public void Insert(int index, T item)
	{
		if (item == null) throw new ArgumentNullException(nameof(item));
		_innerList.Insert(index, item);
	}

	public bool Remove(T item) => _innerList.Remove(item);

	public void RemoveAt(int index) => _innerList.RemoveAt(index);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
