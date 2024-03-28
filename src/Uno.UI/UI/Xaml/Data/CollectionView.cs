using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Uno;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Data
{
	internal partial class CollectionView : ICollectionView
	{
		private IEnumerable _collection;
		private readonly bool _isGrouped;
		private readonly PropertyPath _itemsPath;

		public CollectionView(IEnumerable collection, bool isGrouped, PropertyPath itemsPath)
		{
			_collection = collection;
			_isGrouped = isGrouped;
			_itemsPath = itemsPath;

			if (isGrouped)
			{
				var collectionGroups = new ObservableVector<object>();
				foreach (var group in collection)
				{
					collectionGroups.Add(new CollectionViewGroup(group, _itemsPath));
				}

				CollectionGroups = collectionGroups;

				if (_collection is INotifyCollectionChanged observableCollection)
				{
					observableCollection.CollectionChanged += OnCollectionChangedUpdateGroups;
				}
			}
		}

		public IEnumerable InnerCollection => _collection;

		private void OnCollectionChangedUpdateGroups(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
					{
						CollectionGroups.Insert(i, new CollectionViewGroup(_collection.ElementAt(i), _itemsPath));
					}
					break;
				case NotifyCollectionChangedAction.Move:

					for (int i = e.OldStartingIndex + e.OldItems.Count - 1; i >= e.OldStartingIndex; i--)
					{
						//TODO: Untested. This may be incorrect if OldItems.Count > 1.
						var group = CollectionGroups[i];
						CollectionGroups.RemoveAt(i);
						CollectionGroups.Insert(i, group);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					for (int i = e.OldStartingIndex + e.OldItems.Count - 1; i >= e.OldStartingIndex; i--)
					{
						CollectionGroups.RemoveAt(i);
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
					{
						CollectionGroups[i] = new CollectionViewGroup(_collection.ElementAt(i), _itemsPath);
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					CollectionGroups.Clear();
					break;
			}
		}

		object IList<object>.this[int index]
		{
			get
			{
				if (!_isGrouped)
				{
					return _collection.ElementAt(index);
				}
				else
				{
					return AsEnumerable.ElementAt(index); //TODO: should use logic from ItemsControl for caching and utilizing group counts
				}
			}

			set
			{
				(_collection as IList ?? throw new NotSupportedException())[index] = value;
			}
		}

		/// <summary>
		/// Hack to prevent extension methods from detecting IList properties and optimizing in a way that breaks with grouping. Can be removed once grouping is handled more efficiently.
		/// </summary>
		private IEnumerable AsEnumerable
		{
			get
			{
				foreach (var item in this)
				{
					yield return item;
				}
			}
		}

		public object CurrentItem
		{
			get
			{
				if (!_isGrouped)
				{
					return CurrentPosition >= 0 && CurrentPosition < Count ? _collection.ElementAt(CurrentPosition) : null;
				}
				else
				{
					return AsEnumerable.ElementAt(CurrentPosition); //TODO: should use logic from ItemsControl for caching and utilizing group counts
				}
			}
		}

		public int CurrentPosition { get; private set; }

		public bool HasMoreItems => false;

		public bool IsCurrentAfterLast => CurrentPosition >= Count;

		public bool IsCurrentBeforeFirst => CurrentPosition < 0;

		public int Count
		{
			get
			{
				if (!_isGrouped)
				{
					return _collection.Count();
				}
				else
				{
					var count = 0;

					foreach (ICollectionViewGroup group in CollectionGroups)
					{
						count += group.GroupItems.Count;
					}

					return count;
				}
			}
		}

		bool ICollection<object>.IsReadOnly => false;

		public IObservableVector<object> CollectionGroups { get; }

		public event EventHandler<object> CurrentChanged;
		public event CurrentChangingEventHandler CurrentChanging;

#pragma warning disable 67 // Unused member
		[NotImplemented]
		public event VectorChangedEventHandler<object> VectorChanged; //TODO: this should be raised if underlying source implements INotifyCollectionChanged
#pragma warning restore 67 // Unused member

		public IEnumerator<object> GetEnumerator()
		{
			// In Windows if CollectionView is from a CollectionViewSource marked grouped, it enumerates the flattened list of objects
			if (_isGrouped)
			{
				return (_collection as IEnumerable<IEnumerable<object>> ?? Enumerable.Empty<IEnumerable<object>>()).SelectMany(g => g).GetEnumerator();
			}
			return (_collection as IEnumerable<object>)?.GetEnumerator();
		}

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			throw new NotSupportedException();
		}

		public bool MoveCurrentTo(object item)
		{
			var index = IndexOf(item);
			return MoveCurrentToPosition(index);
		}

		public bool MoveCurrentToFirst()
		{
			var target = Count > 0 ? 0 : -1;
			return MoveCurrentToPosition(target);
		}

		public bool MoveCurrentToLast()
		{
			var target = Count - 1;
			return MoveCurrentToPosition(target);
		}

		public bool MoveCurrentToNext()
		{
			return MoveCurrentToPosition(CurrentPosition + 1);
		}

		public bool MoveCurrentToPosition(int index)
		{
			if (index != CurrentPosition)
			{
				if (index < -1 || index >= Count)
				{
					return false;
				}

				var e = new CurrentChangingEventArgs();
				CurrentChanging?.Invoke(this, e);
				if (e.Cancel)
				{
					return false;
				}
				CurrentPosition = index;
				CurrentChanged?.Invoke(this, null); // null matches Windows here
				return true;
			}

			return true;
		}

		public bool MoveCurrentToPrevious()
		{
			return MoveCurrentToPosition(CurrentPosition - 1);
		}

		void ICollection<object>.Add(object item) => (_collection as IList ?? throw new NotSupportedException()).Add(item);

		void ICollection<object>.Clear() => (_collection as IList ?? throw new NotSupportedException()).Clear();

		public bool Contains(object item) => _collection?.Contains(item) ?? false;

		void ICollection<object>.CopyTo(object[] array, int arrayIndex)
		{
			//TODO: this is used by eg Linq.ToArray(), it should take grouping into account

			if (_collection is ICollection<object> list)
			{
				list.CopyTo(array, arrayIndex);
			}
			else if (_collection is ICollection collection)
			{
				collection.CopyTo(array, arrayIndex);
			}

			_collection?.ToObjectArray().CopyTo(array, arrayIndex);
		}

		IEnumerator<object> IEnumerable<object>.GetEnumerator()
		{
			var enumerator = (this as IEnumerable).GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			// In Windows if CollectionView is from a CollectionViewSource marked grouped, it enumerates the flattened list of objects
			if (_isGrouped)
			{
				return (_collection as IEnumerable<IEnumerable> ?? Enumerable.Empty<IEnumerable>()).SelectManyUntyped(g => g).GetEnumerator();
			}
			return (_collection as IEnumerable).GetEnumerator();
		}

		public int IndexOf(object item) => _collection.IndexOf(item);

		void IList<object>.Insert(int index, object item) => (_collection as IList ?? throw new NotSupportedException()).Insert(index, item);

		bool ICollection<object>.Remove(object item)
		{
			var contains = Contains(item);
			(_collection as IList ?? throw new NotSupportedException()).Remove(item);
			return contains;
		}

		void IList<object>.RemoveAt(int index) => (_collection as IList ?? throw new NotSupportedException()).RemoveAt(index);
	}
}
