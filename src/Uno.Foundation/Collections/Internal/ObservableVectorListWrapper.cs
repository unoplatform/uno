using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation.Collections;

namespace Windows.Foundation.Collections
{
#if !NET6_0_OR_GREATER // moved to linker file
#if __IOS__
	[global::Foundation.Preserve(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.Preserve(AllMembers = true)]
#endif
#endif
	internal class ObservableVectorListWrapper : ObservableVectorWrapper, IObservableVector<object>
	{
		private readonly IList _source;

		public ObservableVectorListWrapper(IList source) : base(source)
		{
			_source = source;
		}

		public object this[int index] { get => _source[index]; set => _source[index] = value; }

		public bool IsReadOnly => _source.IsReadOnly;

		public bool IsFixedSize => _source.IsFixedSize;

		public int Count => _source.Count;

		public object SyncRoot => _source.SyncRoot;

		public bool IsSynchronized => _source.IsSynchronized;

		public void Add(object value)
		{
			_source.Add(value);
		}

		public void Clear()
		{
			_source.Clear();
		}

		public bool Contains(object value)
		{
			return _source.Contains(value);
		}

		public void CopyTo(Array array, int index)
		{
			_source.CopyTo(array, index);
		}

		public void CopyTo(object[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _source.GetEnumerator();
		}

		public int IndexOf(object value)
		{
			return _source.IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			_source.Insert(index, value);
		}

		public bool Remove(object value)
		{
			_source.Remove(value);

			return true;
		}

		public void RemoveAt(int index)
		{
			_source.RemoveAt(index);
		}

		IEnumerator<object> IEnumerable<object>.GetEnumerator()
		{
			return (_source as IEnumerable<object> ?? _source.Cast<object>()).GetEnumerator();
		}
	}
}
