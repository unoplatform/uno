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
	public sealed partial class ItemCollection : IList<object>, IEnumerable<object>, IObservableVector<object>
	{
		private readonly IList<object> _inner = new List<object>();

		private IList _itemsSource = null;
		private readonly SerialDisposable _itemsSourceCollectionChangeDisposable = new SerialDisposable();

		public event VectorChangedEventHandler<object> VectorChanged;

		public IEnumerator<object> GetEnumerator()
		{
			if (_itemsSource == null)
			{
				return _inner.GetEnumerator();
			}
			else
			{
				return _itemsSource.OfType<object>().GetEnumerator();
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
			VectorChanged.TryRaiseInserted(this, _inner.Count - 1);
		}

		public void Clear()
		{
			ThrowIfItemsSourceSet();
			_inner.Clear();
			VectorChanged.TryRaiseReseted(this);
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
			var vectorChanged = VectorChanged;
			if (vectorChanged == null)
			{
				return _inner.Remove(item);
			}
			else
			{
				var index = _inner.IndexOf(item);
				if (index >= 0
					&& _inner.Remove(item))
				{
					VectorChanged.TryRaiseRemoved(this, index);
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public int Count => _itemsSource == null ? _inner.Count : _itemsSource.Count();

		public uint Size => (uint)Count;

		public bool IsReadOnly => _inner.IsReadOnly; // This actually matches UWP - Items do not reflect read-only attribute of ItemsSource

		public int IndexOf(object item) => _itemsSource == null ? _inner.IndexOf(item) : _itemsSource.IndexOf(item);

		public void Insert(int index, object item)
		{
			ThrowIfItemsSourceSet();
			_inner.Insert(index, item);
			VectorChanged.TryRaiseInserted(this, index);
		}

		public void RemoveAt(int index)
		{
			ThrowIfItemsSourceSet();
			_inner.RemoveAt(index);
			VectorChanged.TryRaiseRemoved(this, index);
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

		internal void SetItemsSource(object itemsSource)
		{
			if (_itemsSource == itemsSource)
			{
				// Items source did not actually change.
				return;
			}

			if (itemsSource == null)
			{
				_itemsSource = null;
			}
			else
			{
				var unwrappedSource = UnwrapItemsSource(itemsSource);

				if (unwrappedSource is IList itemsSourceList)
				{
					_itemsSource = itemsSourceList;
				}
				else if (unwrappedSource is IEnumerable itemsSourceEnumerable)
				{
					_itemsSource = itemsSourceEnumerable.ToObjectArray();
				}
				else
				{
					throw new InvalidOperationException("Only IList- or IEnumerable-based ItemsSource is supported.");
				}

				ObserveCollectionChanged();
			}

			VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.Reset, 0));
		}

		private void ObserveCollectionChanged()
		{
			if (_itemsSource is INotifyCollectionChanged existingObservable)
			{
				// This is a workaround for a bug with EventRegistrationTokenTable on Xamarin, where subscribing/unsubscribing to a class method directly won't 
				// remove the handler.
				NotifyCollectionChangedEventHandler handler = OnItemsSourceCollectionChanged;
				_itemsSourceCollectionChangeDisposable.Disposable = Disposable.Create(() =>
					existingObservable.CollectionChanged -= handler
				);
				existingObservable.CollectionChanged += handler;
			}
			else if (_itemsSource is IObservableVector<object> observableVector)
			{
				// This is a workaround for a bug with EventRegistrationTokenTable on Xamarin, where subscribing/unsubscribing to a class method directly won't 
				// remove the handler.
				VectorChangedEventHandler<object> handler = OnItemsSourceVectorChanged;
				_itemsSourceCollectionChangeDisposable.Disposable = Disposable.Create(() =>
					observableVector.VectorChanged -= handler
				);
				observableVector.VectorChanged += handler;
			}
			else if (_itemsSource is IObservableVector genericObservableVector)
			{
				VectorChangedEventHandler handler = OnItemsSourceVectorChanged;
				_itemsSourceCollectionChangeDisposable.Disposable = Disposable.Create(() =>
					genericObservableVector.UntypedVectorChanged -= handler
				);
				genericObservableVector.UntypedVectorChanged += handler;
			}
			else
			{
				_itemsSourceCollectionChangeDisposable.Disposable = null;
			}
		}

		private void OnItemsSourceVectorChanged(object sender, IVectorChangedEventArgs args)
		{
			VectorChanged?.Invoke(this, args);
		}

		private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			VectorChanged?.Invoke(this, args.ToVectorChangedEventArgs());
		}

		private void ThrowIfItemsSourceSet()
		{
			if (_itemsSource != null)
			{
				throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
			}
		}

		private object UnwrapItemsSource(object itemsSource)
			=> itemsSource is CollectionViewSource cvs ? (object)cvs.View : itemsSource;
	}
}
