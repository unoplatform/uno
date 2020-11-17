using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Data;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ItemCollection : IList<object>, IEnumerable<object>, IObservableVector<object>
	{
		private readonly IList<object> _inner = new List<object>();

		private IEnumerable _itemsSource = null;

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
			if (_itemsSource != null)
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
			if (_itemsSource != null)
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

			//TODO: Observe items source changes to raise VectorChanged
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
