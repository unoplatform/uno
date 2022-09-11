using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Data;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using System.Linq;
using Uno.Disposables;
using System.Collections.Specialized;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ItemCollection : IList<object>, IEnumerable<object>, IObservableVector<object>, IObservableVector
	{
		private readonly IList<object> _inner = new List<object>();

		private IList<object> _itemsSource;
		private readonly SerialDisposable _itemsSourceCollectionChangeDisposable = new SerialDisposable();

		public event VectorChangedEventHandler<object> VectorChanged;

		private event VectorChangedEventHandler _untypedVectorChanged;
		event VectorChangedEventHandler IObservableVector.UntypedVectorChanged
		{
			add => _untypedVectorChanged += value;
			remove => _untypedVectorChanged -= value;
		}

		public IEnumerator<object> GetEnumerator()
		{
			if (_itemsSource == null)
			{
				return _inner.GetEnumerator();
			}
			else
			{
				return _itemsSource.GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			if (_itemsSource == null)
			{
				return _inner.GetEnumerator();
			}
			else
			{
				return _itemsSource.GetEnumerator();
			}
		}

		public void Add(object item)
		{
			ThrowIfItemsSourceSet();
			_inner.Add(item);
			(VectorChanged, _untypedVectorChanged).TryRaiseInserted(this, _inner.Count - 1);
		}

		public void Clear()
		{
			ThrowIfItemsSourceSet();
			_inner.Clear();
			(VectorChanged, _untypedVectorChanged).TryRaiseReseted(this);
		}

		public bool Contains(object item)
		{
			if (_itemsSource == null)
			{
				return _inner.Contains(item);
			}
			else
			{
				return _itemsSource.Contains(item);
			}
		}

		public void CopyTo(object[] array, int arrayIndex)
		{
			if (_itemsSource == null)
			{
				_inner.CopyTo(array, arrayIndex);
			}
			else
			{
				int targetIndex = arrayIndex;
				foreach (var item in _itemsSource)
				{
					array[targetIndex] = item;
					targetIndex++;
				}
			}
		}

		public bool Remove(object item)
		{
			ThrowIfItemsSourceSet();
			var vectorChanged = (VectorChanged, _untypedVectorChanged);
			if (vectorChanged == default)
			{
				return _inner.Remove(item);
			}
			else
			{
				var index = _inner.IndexOf(item);
				if (index >= 0
					&& _inner.Remove(item))
				{
					vectorChanged.TryRaiseRemoved(this, index);
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public int Count => _itemsSource == null ? _inner.Count : _itemsSource.Count;

		public uint Size => (uint)Count;

		public bool IsReadOnly => _inner.IsReadOnly; // This actually matches UWP - Items do not reflect read-only attribute of ItemsSource

		public int IndexOf(object item) => _itemsSource == null ? _inner.IndexOf(item) : _itemsSource.IndexOf(item);

		public void Insert(int index, object item)
		{
			ThrowIfItemsSourceSet();
			_inner.Insert(index, item);
			(VectorChanged, _untypedVectorChanged).TryRaiseInserted(this, index);
		}

		public void RemoveAt(int index)
		{
			ThrowIfItemsSourceSet();
			_inner.RemoveAt(index);
			(VectorChanged, _untypedVectorChanged).TryRaiseRemoved(this, index);
		}

		public object this[int index]
		{
			get => _itemsSource == null ? _inner[index] : _itemsSource.ElementAt(index);
			set
			{
				ThrowIfItemsSourceSet();
				_inner[index] = value;
			}
		}

		internal void SetItemsSource(IEnumerable itemsSource)
		{
			if (_itemsSource == itemsSource)
			{
				// Items source did not actually change.
				return;
			}

			if (itemsSource == null)
			{
				_itemsSource = null;
				ObserveCollectionChanged(null);
			}
			else
			{
				object listSource = null;
				if (itemsSource is IList<object> itemsSourceGenericList)
				{
					listSource = itemsSourceGenericList;
					_itemsSource = itemsSourceGenericList;
				}
				else if (itemsSource is IList itemsSourceList)
				{
					listSource = itemsSourceList;
					_itemsSource = new UntypedListWrapper(itemsSourceList);
				}
				else
				{
					_itemsSource = itemsSource.ToObjectArray();
				}

				ObserveCollectionChanged(listSource);
			}

			(VectorChanged, _untypedVectorChanged).TryRaiseReseted(this);
		}

		private void ObserveCollectionChanged(object itemsSource)
		{
			if (itemsSource is null)
			{
				// fast path for null
				_itemsSourceCollectionChangeDisposable.Disposable = null;
			}
			else if (itemsSource is INotifyCollectionChanged existingObservable)
			{
				ObserveCollectionChangedInner(existingObservable);
			}
			else if (itemsSource is IObservableVector<object> observableVector)
			{
				ObserveCollectionChangedInner(observableVector);
			}
			else if (itemsSource is IObservableVector genericObservableVector)
			{
				ObserveCollectionChangedInner(genericObservableVector);
			}
			else
			{
				_itemsSourceCollectionChangeDisposable.Disposable = null;
			}
		}

		private void ObserveCollectionChangedInner(INotifyCollectionChanged existingObservable)
		{
			var thatRef = new WeakReference(this);

			void handler(object s, NotifyCollectionChangedEventArgs e)
			{
				// Wrap the registered delegate to avoid creating a strong
				// reference to this ItemsCollection. The ItemsCollection is holding
				// a reference to the items source, so it won`t be collected
				// unless unset.Note that this block is not extracted to a separate
				// helper to avoid the cost of creating additional delegates.

				if (thatRef.Target is ItemCollection that)
				{
					that.OnItemsSourceCollectionChanged(s, e);
				}
				else
				{
					existingObservable.CollectionChanged -= handler;
				}
			}

			// This is a workaround for a bug with EventRegistrationTokenTable on Xamarin, where subscribing/unsubscribing to a class method directly won't 
			// remove the handler.
			_itemsSourceCollectionChangeDisposable.Disposable = Disposable.Create(() =>
				existingObservable.CollectionChanged -= handler
			);
			existingObservable.CollectionChanged += handler;
		}

		private void ObserveCollectionChangedInner(IObservableVector<object> observableVector)
		{
			var thatRef = new WeakReference(this);

			void handler(IObservableVector<object> s, IVectorChangedEventArgs e)
			{
				// Wrap the registered delegate to avoid creating a strong
				// reference to this ItemsCollection.The ItemsCollection is holding
				// a reference to the items source, so it won`t be collected
				// unless unset.Note that this block is not extracted to a separate
				// helper to avoid the cost of creating additional delegates.

				if (thatRef.Target is ItemCollection that)
				{
					that.OnItemsSourceVectorChanged(s, e);
				}
				else
				{
					observableVector.VectorChanged -= handler;
				}
			}

			// This is a workaround for a bug with EventRegistrationTokenTable on Xamarin, where subscribing/unsubscribing to a class method directly won't 
			// remove the handler.
			_itemsSourceCollectionChangeDisposable.Disposable = Disposable.Create(() =>
				observableVector.VectorChanged -= handler
			);
			observableVector.VectorChanged += handler;
		}

		private void ObserveCollectionChangedInner(IObservableVector genericObservableVector)
		{
			var thatRef = new WeakReference(this);

			void handler(object s, IVectorChangedEventArgs e)
			{
				// Wrap the registered delegate to avoid creating a strong
				// reference to this ItemsCollection.The ItemsCollection is holding
				// a reference to the items source, so it won`t be collected
				// unless unset.Note that this block is not extracted to a separate
				// helper to avoid the cost of creating additional delegates.

				if (thatRef.Target is ItemCollection that)
				{
					that.OnItemsSourceVectorChanged(s, e);
				}
				else
				{
					genericObservableVector.UntypedVectorChanged -= handler;
				}
			}

			_itemsSourceCollectionChangeDisposable.Disposable = Disposable.Create(() =>
				genericObservableVector.UntypedVectorChanged -= handler
			);
			genericObservableVector.UntypedVectorChanged += handler;
		}

		private void OnItemsSourceVectorChanged(object sender, IVectorChangedEventArgs args)
		{
			(VectorChanged, _untypedVectorChanged).TryRaise(this, args);
		}

		private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			(VectorChanged, _untypedVectorChanged).TryRaise(this, args.ToVectorChangedEventArgs());
		}

		private void ThrowIfItemsSourceSet()
		{
			if (_itemsSource != null)
			{
				throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
			}
		}

		private class UntypedListWrapper : IList<object>
		{
			private readonly IList _inner;

			public IList Original => _inner;

			public UntypedListWrapper(IList list)
			{
				_inner = list ?? throw new ArgumentNullException(nameof(list));
			}

			public object this[int index] { get => _inner[index]; set => _inner[index] = value; }

			public int Count => _inner.Count;

			public bool IsReadOnly => _inner.IsReadOnly;

			public void Add(object item) => _inner.Add(item);
			public void Clear() => _inner.Clear();
			public bool Contains(object item) => _inner.Contains(item);
			public void CopyTo(object[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
			public IEnumerator<object> GetEnumerator()
			{
				var enumerator = _inner.GetEnumerator();
				while (enumerator.MoveNext())
				{
					yield return enumerator.Current;
				}
			}

			public int IndexOf(object item) => _inner.IndexOf(item);
			public void Insert(int index, object item) => _inner.Insert(index, item);
			public bool Remove(object item)
			{
				var initialCount = _inner.Count;
				_inner.Remove(item);
				return _inner.Count < initialCount;
			}

			public void RemoveAt(int index) => _inner.RemoveAt(index);
			IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
		}
	}
}
