using System.Collections;

namespace Windows.Foundation.Collections;

internal class ObservableVector<T> : IObservableVector<T>, IObservableVector, IList
{
	private readonly List<T> _list = new List<T>();

	object IObservableVector.this[int index] => this[index];
	public virtual T this[int index]
	{
		get { return _list[index]; }
		set
		{
			// Like WinUI's Vector<T>::SetAt, ItemChanged is raised even when the same instance is set again.
			_list[index] = value;

			RaiseVectorChanged(CollectionChange.ItemChanged, index);
		}
	}

	public int Count => _list.Count;

	public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

	public event VectorChangedEventHandler<T> VectorChanged;
	public event VectorChangedEventHandler UntypedVectorChanged;

	void IObservableVector.Add(object item) => Add((T)item);
	public virtual void Add(T item)
	{
		_list.Add(item);
		RaiseVectorChanged(CollectionChange.ItemInserted, _list.Count - 1);
	}

	public virtual void Clear()
	{
		_list.Clear();

		RaiseVectorChanged(CollectionChange.Reset, 0);
	}

	public bool Contains(T item) => _list.Contains(item);

	public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

	public virtual IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

	int IObservableVector.IndexOf(object item) => item is T t ? IndexOf(t) : -1;
	public virtual int IndexOf(T item) => _list.IndexOf(item);

	// MUX Reference Vector.h (VectorInnerImpl::IndexOf_OptimizedForRemove)
	// Uno specific: the caller holds the search position, so vectors that never use this don't carry the field.
	private protected bool IndexOf_OptimizedForRemove(T value, ref int startNextSearchAt, out int index)
	{
		index = 0;

		if (startNextSearchAt >= _list.Count)
		{
			if (_list.Count == 0)
			{
				startNextSearchAt = 0;
			}
			else
			{
				startNextSearchAt = _list.Count - 1;
			}
		}

		var it = _list.IndexOf(value, startNextSearchAt);
		bool found = it != -1;
		if (!found && startNextSearchAt > 0)
		{
			it = _list.IndexOf(value, 0, startNextSearchAt);
			found = it != -1;
		}

		if (found)
		{
			index = it;
			if (index > 0)
			{
				// Next time we search start near this index. We assume that the remove is being processed from the end
				// of the vector towards index 0, so the next item being removed is probably at "index - 1". Start the
				// next search there.
				startNextSearchAt = index - 1;
			}
			else
			{
				startNextSearchAt = index;
			}
			return true;
		}

		return false;
	}

	void IObservableVector.Insert(int index, object item) => Insert(index, (T)item);
	public virtual void Insert(int index, T item)
	{
		_list.Insert(index, item);

		RaiseVectorChanged(CollectionChange.ItemInserted, index);
	}

	public bool Remove(T item)
	{
		var index = _list.IndexOf(item);

		if (index != -1)
		{
			RemoveAt(index);

			return true;
		}
		else
		{
			return false;
		}
	}

	public virtual void RemoveAt(int index)
	{
		_list.RemoveAt(index);

		RaiseVectorChanged(CollectionChange.ItemRemoved, index);
	}

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

	private void RaiseVectorChanged(CollectionChange change, int index)
	{
		VectorChanged?.Invoke(this, new VectorChangedEventArgs(change, (uint)index));
		UntypedVectorChanged?.Invoke(this, new VectorChangedEventArgs(change, (uint)index));
	}

	bool IList.IsFixedSize => false;

	object ICollection.SyncRoot => ((IList)_list).SyncRoot;

	bool ICollection.IsSynchronized => false;

	int IList.Add(object value)
	{
		if (value is T typedValue)
		{
			Add(typedValue);
			return _list.Count - 1;
		}
		else
		{
			throw new ArgumentException($"Cannot add an instance of type {value?.GetType()}");
		}
	}

	bool IList.Contains(object value)
		=> GenericIndexOf(value) != -1;

	int IList.IndexOf(object value)
		=> GenericIndexOf(value);

	private int GenericIndexOf(object value)
	{
		for (int i = 0; i < Count; i++)
		{
			if (Equals(this[i], value))
			{
				return i;
			}
		}

		return -1;
	}

	void IList.Insert(int index, object value)
	{
		if (value is T typedValue)
		{
			Insert(index, typedValue);
		}
		else
		{
			throw new ArgumentException($"Cannot use an instance of type {value?.GetType()}");
		}
	}

	void IList.Remove(object value)
	{
		if (value is T typedValue)
		{
			Remove(typedValue);
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((IList)_list).CopyTo(array, index);
	}

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			if (value is T typedValue)
			{
				this[index] = typedValue;
			}
			else
			{
				throw new ArgumentException($"Cannot use an instance of type {value?.GetType()}");
			}
		}
	}
}
