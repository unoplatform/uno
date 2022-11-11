using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Specialized;
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
	internal class ObservableVectorEnumerableWrapper : ObservableVectorWrapper, IObservableVector<object>
	{
		private readonly IEnumerable _source;

		public ObservableVectorEnumerableWrapper(IEnumerable source) : base(source)
		{
			_source = source;
		}

		public object this[int index] { get => _source.ElementAt(index); set => throw new NotSupportedException(); }

		public int Count => _source.Count();

		public bool IsReadOnly => false;

		public void Add(object item)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(object item)
		{
			return _source.Contains(item);
		}

		public void CopyTo(object[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}

		IEnumerator<object> IEnumerable<object>.GetEnumerator()
		{
			var enumerator = (this as IEnumerable).GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
		}


		public int IndexOf(object item)
		{
			return _source.IndexOf(item);
		}

		public void Insert(int index, object item)
		{
			throw new NotSupportedException();
		}

		public bool Remove(object item)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _source.GetEnumerator();
		}
	}
}
