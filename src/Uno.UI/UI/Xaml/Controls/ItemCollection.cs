using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation.Collections;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ItemCollection : IList<object>, IEnumerable<object>, IObservableVector<object>
	{
		private readonly IList<object> _inner = new List<object>();

		private object _syncedItemsSource = null;

		public event VectorChangedEventHandler<object> VectorChanged;

		public IEnumerator<object> GetEnumerator()
		{
			if (_syncedItemsSource == null)
			{
				return _inner.GetEnumerator();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			if (_syncedItemsSource == null)
			{
				return _inner.GetEnumerator();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public void Add(object item)
		{
			if (_syncedItemsSource != null)
			{
				throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
			}
			_inner.Add(item);
			VectorChanged.TryRaiseInserted(this, _inner.Count - 1);
		}

		public void Clear()
		{
			if (_syncedItemsSource != null)
			{
				throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
			}
			_inner.Clear();
			VectorChanged.TryRaiseReseted(this);
		}

		public bool Contains(object item)
		{
			if (_syncedItemsSource != null)
			{
				return _inner.Contains(item);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public void CopyTo(object[] array, int arrayIndex)
		{
			if (_syncedItemsSource != null)
			{
				_inner.CopyTo(array, arrayIndex);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public bool Remove(object item)
		{
			if (_syncedItemsSource != null)
			{
				throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
			}
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

		public int Count => _syncedItemsSource == null ? _inner.Count : throw new NotImplementedException();

		public uint Size => (uint)Count;

		public bool IsReadOnly => _inner.IsReadOnly; // This actually matches UWP - Items do not reflect read-only attribute of ItemsSource

		public int IndexOf(object item) => _syncedItemsSource == null ? _inner.IndexOf(item) : throw new NotImplementedException();

		public void Insert(int index, object item)
		{
			if (_syncedItemsSource != null)
			{
				throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
			}
			_inner.Insert(index, item);
			VectorChanged.TryRaiseInserted(this, index);
		}

		public void RemoveAt(int index)
		{
			if (_syncedItemsSource != null)
			{
				throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
			}
			_inner.RemoveAt(index);
			VectorChanged.TryRaiseRemoved(this, index);
		}

		public object this[int index]
		{
			get => _syncedItemsSource == null ? _inner[index] : throw new NotImplementedException();
			set
			{
				if (_syncedItemsSource != null)
				{
					throw new InvalidOperationException("Items cannot be modified when ItemsSource is set.");
				}
				_inner[index] = value;
			}
		}

		internal void SetItemsSource(object itemsSource)
		{
			_syncedItemsSource = itemsSource;
		}
	}
}
