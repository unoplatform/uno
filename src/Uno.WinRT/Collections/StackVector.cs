using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Buffers;

namespace Uno.Collections
{
	internal class StackVector<T> : IEnumerable<T> where T : struct
	{
		internal delegate bool RefPredicateDelegate(ref T item);

		private Memory<T> _inner;
		private T[] _originalArray;

		public StackVector(int capacity, int initialLength = 0)
		{
			AllocateInner(capacity);
			Count = initialLength;
		}

		public IEnumerator<T> GetEnumerator() => new StackVectorEnumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count { get; private set; }

		public Memory<T> Memory => _inner.Slice(0, Count);

		public void Resize(int newCount)
		{
			if (_inner.Length < newCount)
			{
				// This is a specialized list: we know
				// there's no need to recover items from previous
				// list.
				// The resize takes place only to reuse the working
				// memory, not to store it.
				AllocateInner(newCount);

				IncrementResizeCount();
			}

			Count = newCount;
		}

		private void AllocateInner(int newSize)
		{
			var newArray = ArrayPool<T>.Shared.Rent(newSize);
			var newMemory = new Memory<T>(newArray);
			_inner.CopyTo(newMemory);

			if (_originalArray != null)
			{
				ArrayPool<T>.Shared.Return(_originalArray, clearArray: true);
			}

			_originalArray = newArray;
			_inner = new Memory<T>(_originalArray);
		}

		public void Dispose()
		{
			if (_originalArray != null)
			{
				ArrayPool<T>.Shared.Return(_originalArray, clearArray: true);
			}
		}

#if DEBUG
		internal int _numberOfResizes;
		internal static int _totalNumberOfResizes;
#endif

		[Conditional("DEBUG")]
		private void IncrementResizeCount()
		{
#if DEBUG
			_numberOfResizes++;
			_totalNumberOfResizes++;
#endif
		}

		public ref T PushBack()
		{
			var newLength = Count + 1;
			Resize(newLength);
			return ref _inner.Span[newLength - 1];

		}

		public bool FirstOrDefault(RefPredicateDelegate predicate, ref T item)
		{
			for (var i = 0; i < _inner.Length; i++)
			{
				ref var itm = ref _inner.Span[i];
				if (predicate(ref itm))
				{
					item = itm;
					return true;
				}
			}

			return false;
		}

		public bool LastOrDefault(ref T last)
		{
			var length = Count;

			if (length == 0)
			{
				return false;
			}

			last = ref _inner.Span[length - 1];
			return true;
		}

		public ref T this[int index]
		{
			get
			{
				if (index < 0 || index >= Count)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}
				return ref _inner.Span[index];
			}
		}

		private class StackVectorEnumerator : IEnumerator<T>
		{
			private readonly StackVector<T> _owner;
			private int _index = -1;

			public StackVectorEnumerator(StackVector<T> owner)
			{
				_owner = owner;
			}

			bool IEnumerator.MoveNext()
			{
				_index++;
				return _index < _owner.Count;
			}

			void IEnumerator.Reset() => _index = -1;

			T IEnumerator<T>.Current => _owner._inner.Span[_index];

			object IEnumerator.Current => _owner._inner.Span[_index];

			void IDisposable.Dispose() { }
		}
	}
}
