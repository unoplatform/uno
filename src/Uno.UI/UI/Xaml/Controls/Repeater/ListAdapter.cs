using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class ListAdapter
	{
		/// <summary>
		/// Creates a typed list decorator that changes the type of the source list
		/// </summary>
		/// <remarks>This is the equivalent of the Linq Cast operator.</remarks>
		public static IList<TTarget> ChangeType<TSource, TTarget>(IList<TSource> list) => new CastList<TSource, TTarget>(list);

		/// <summary>
		/// Creates a typed list decorator over an IList
		/// </summary>
		/// <remarks>This is the equivalent of the Linq Cast operator.</remarks>
		public static IList<object> ToGeneric(IList list) => new GenericList<object>(list);

		/// <summary>
		/// Creates a typed list decorator over an IList
		/// </summary>
		/// <remarks>This is the equivalent of the Linq Cast operator.</remarks>
		public static IList<T> ToGeneric<T>(IList list) => new GenericList<T>(list);

		/// <summary>
		/// Creates an untyped list decorator over a generic list
		/// </summary>
		public static IList ToUntyped<T>(IList<T> list) => new UntypedList<T>(list);

		private class CastList<TSource, TTarget> : IList<TTarget>
		{
			private readonly IList<TSource> _inner;

			public CastList(IList<TSource> inner)
			{
				_inner = inner;
			}

			public TTarget this[int index]
			{
				get => To(_inner[index]);
				set => _inner[index] = From(value);
			}
			public int Count => _inner.Count;
			public bool IsReadOnly => _inner.IsReadOnly;

			public void Add(TTarget item) => _inner.Add(From(item));
			public void Insert(int index, TTarget item) => _inner.Insert(index, From(item));
			public bool Contains(TTarget item) => _inner.Contains(From(item));
			public int IndexOf(TTarget item) => _inner.IndexOf(From(item));
			public void RemoveAt(int index) => _inner.RemoveAt(index);
			public bool Remove(TTarget item)
			{
				if (_inner.Contains(From(item)))
				{
					_inner.Remove(From(item));
					return true;
				}
				else
				{
					return false;
				}
			}
			public void Clear() => _inner.Clear();
			public void CopyTo(TTarget[] array, int arrayIndex)
			{
				var length = array.Length - arrayIndex;
				var tmp = new TSource[length];
				_inner.CopyTo(tmp, 0);
				Array.Copy(tmp, 0, array, arrayIndex, length);
			}
			public IEnumerator<TTarget> GetEnumerator() => new GenericEnumerator<TTarget>(_inner.GetEnumerator());
			IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

			private static TSource From(object value) => (TSource)value;
			private static TTarget To(object value) => (TTarget)value;
		}

		private class GenericList<T> : IList<T>
		{
			private readonly IList _inner;

			public GenericList(IList inner)
			{
				_inner = inner;
			}

			public T this[int index]
			{
				get => (T)_inner[index];
				set => _inner[index] = value;
			}
			public int Count => _inner.Count;
			public bool IsReadOnly => _inner.IsReadOnly;

			public void Add(T item) => _inner.Add(item);
			public void Insert(int index, T item) => _inner.Insert(index, item);
			public bool Contains(T item) => _inner.Contains(item);
			public int IndexOf(T item) => _inner.IndexOf(item);
			public void RemoveAt(int index) => _inner.RemoveAt(index);
			public bool Remove(T item)
			{
				if (_inner.Contains(item))
				{
					_inner.Remove(item);
					return true;
				}
				else
				{
					return false;
				}
			}
			public void Clear() => _inner.Clear();
			public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
			public IEnumerator<T> GetEnumerator() => new GenericEnumerator<T>(_inner.GetEnumerator());
			IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
		}

		private class GenericEnumerator<T> : IEnumerator<T>
		{
			private readonly IEnumerator _inner;

			public GenericEnumerator(IEnumerator inner)
			{
				_inner = inner;
			}

			public T Current => (T)_inner.Current;
			object IEnumerator.Current => _inner.Current;

			public void Dispose() { }
			public bool MoveNext() => _inner.MoveNext();
			public void Reset() => _inner.Reset();
		}

		private class UntypedList<T> : IList
		{
			private readonly IList<T> _inner;

			public UntypedList(IList<T> inner)
			{
				_inner = inner;
				SyncRoot = _inner is ICollection col ? col.SyncRoot : new object();
			}

			public object this[int index]
			{
				get => _inner[index];
				set => _inner[index] = (T)value;
			}
			public int Count => _inner.Count;
			public bool IsReadOnly => _inner.IsReadOnly;
			public bool IsFixedSize => false;
			public object SyncRoot { get; }
			public bool IsSynchronized => _inner is ICollection col && col.IsSynchronized;

			public int Add(object value)
			{
				var index = _inner.Count;
				_inner.Add((T)value);
				return index;
			}
			public void Insert(int index, object value) => _inner.Insert(index, (T)value);
			public bool Contains(object value) => _inner.Contains((T)value);
			public int IndexOf(object value) => _inner.IndexOf((T)value);
			public void RemoveAt(int index) => _inner.RemoveAt(index);
			public void Remove(object value) => _inner.Remove((T)value);
			public void Clear() => _inner.Clear();
			public IEnumerator GetEnumerator() => _inner.GetEnumerator();

			public void CopyTo(Array array, int index)
			{
				var length = array.Length - index;
				var tmp = new T[length];
				_inner.CopyTo(tmp, 0);
				Array.Copy(tmp, 0, array, index, length);
			}
		}
	}
}
