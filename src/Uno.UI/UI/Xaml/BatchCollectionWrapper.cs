using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Collections;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A collection of items, 
	///  Implements the INotifyCollectionChanged
	/// Call back when thread is liberated
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract partial class BatchCollection<T> : ICollection<T>, IEnumerable<T>, IList<T>, INotifyCollectionChanged
	{
		private readonly DependencyObject _owner;

		/// <summary>
		/// The owner of this collection.
		/// This is intended to be used to redispatch to a specific instance in ** static ** CollectionChanged handlers.
		/// </summary>
		internal DependencyObject Owner => _owner;

		public BatchCollection(DependencyObject owner)
		{
			_owner = owner;
		}

		#region IList implementation

		public int IndexOf(T item)
		{
			return IndexOfCore(item);
		}

		protected abstract int IndexOfCore(T item);

		public void Insert(int index, T item)
		{
			item.SetParent(_owner);

			InsertCore(index, item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
		}

		protected abstract void InsertCore(int index, T item);

		public void RemoveAt(int index)
		{
			var item = RemoveAtCore(index);
			item.SetParent(null);

			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
		}

		protected abstract T RemoveAtCore(int index);

		public T this[int index]
		{
			get { return GetAtIndexCore(index); }
			set
			{
				value.SetParent(_owner);

				var previousItem = SetAtIndexCore(index, value);
				previousItem.SetParent(null);

				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, previousItem, index));
			}
		}

		protected abstract T GetAtIndexCore(int index);
		protected abstract T SetAtIndexCore(int index, T value);

		#endregion

		#region ICollection implementation

		public void Add(T item)
		{
			if (item is IDependencyObjectStoreProvider provider)
			{
				item.SetParent(_owner);
			}

			AddCore(item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		protected abstract void AddCore(T item);

		public void Clear()
		{
			var items = ClearCore();

			foreach(var item in items)
			{
				if (item is IDependencyObjectStoreProvider provider)
				{
					item.SetParent(null);
				}
			}

			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items.ToList()));
		}

		protected abstract IEnumerable<T> ClearCore();

		public bool Contains(T item)
		{
			return ContainsCore(item);
		}

		protected abstract bool ContainsCore(T item);

		public void CopyTo(T[] array, int arrayIndex)
		{
			CopyToCore(array, arrayIndex);
		}

		protected abstract void CopyToCore(T[] array, int arrayIndex);

		public bool Remove(T item)
		{
			if (item != null)
			{
				item.SetParent(null);

				var removed = RemoveCore(item);
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
				return removed;
			}
			else
			{
				return false;
			}
		}

		protected abstract bool RemoveCore(T item);

		public int Count
		{
			get
			{
				return CountCore();
			}
		}

		protected abstract int CountCore();

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<T> GetEnumerator() => GetEnumeratorCore();

		protected abstract List<T>.Enumerator GetEnumeratorCore();

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorCore();

		#endregion

		/// <summary>
		/// Moves the item at the specified index to a new location in the collection.
		/// </summary>
		/// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
		/// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
		public void Move(uint oldIndex, uint newIndex)
		{
			if (oldIndex == newIndex)
			{
				return;
			}

			MoveCore(oldIndex, newIndex);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, this[(int)newIndex], (int)newIndex, (int)oldIndex));
		}

		protected abstract void MoveCore(uint oldIndex, uint newIndex);


		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);
		}
	}
}

