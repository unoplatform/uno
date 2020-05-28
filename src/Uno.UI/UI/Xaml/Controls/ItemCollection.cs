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
		public event VectorChangedEventHandler<object> VectorChanged;

		private readonly IList<object> _inner = new List<object>();

		public IEnumerator<object> GetEnumerator() => _inner.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

		public void Add(object item)
		{
			_inner.Add(item);
			VectorChanged.TryRaiseInserted(this, _inner.Count - 1);
		}

		public void Clear()
		{
			_inner.Clear();
			VectorChanged.TryRaiseReseted(this);
		}

		public bool Contains(object item) => _inner.Contains(item);

		public void CopyTo(object[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

		public bool Remove(object item)
		{
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

		public int Count => _inner.Count;

		public uint Size => (uint)Count;

		public bool IsReadOnly => _inner.IsReadOnly;

		public int IndexOf(object item) =>  _inner.IndexOf(item);

		public void Insert(int index, object item)
		{
			_inner.Insert(index, item);
			VectorChanged.TryRaiseInserted(this, index);
		}

		public void RemoveAt(int index)
		{
			_inner.RemoveAt(index);
			VectorChanged.TryRaiseRemoved(this, index);
		}

		public object this[int index]
		{
			get { return _inner[index]; }
			set { _inner[index] = value; }
		}
	}
}
