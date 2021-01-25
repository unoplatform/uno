using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation.Collections;

namespace Windows.Foundation.Collections
{
	internal class ObservableVector<T> : IObservableVector<T>, IObservableVector
	{
		private readonly List<T> _list = new List<T>();

		object IObservableVector.this[int index] => this[index];
		public virtual T this[int index]
		{
			get { return _list[index]; }
			set
			{
				var originalValue = _list[index];

				if (!ReferenceEquals(originalValue, value))
				{
					_list[index] = value;

					RaiseVectorChanged(CollectionChange.ItemChanged, index);
				}
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
		public int IndexOf(T item) => _list.IndexOf(item);

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
	}
}
