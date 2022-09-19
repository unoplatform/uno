// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Uno.Collections
{
	/// <summary>
	/// An immutable list implementation, designed for safe concurrent access.
	/// </summary>
	/// <remarks>
	/// It is possible to mutate the content of this immutable list. Not all
	/// protections are in place to ensure a perfect immutability. Don't use it
	/// if you are exposing it outside your code. For true immutability
	/// protection, you should prefer those from Micrososft's Immutable Collections.
	/// </remarks>
	internal class ImmutableList<T> : IImmutableList<T>
	{
		private readonly T[] _data;

		/// <summary>
		/// Provides an empty list
		/// </summary>
		public static ImmutableList<T> Empty { get; } = new ImmutableList<T>(Array.Empty<T>(), false);

		/// <summary>
		/// Creates an empty list
		/// </summary>
		public ImmutableList()
		{
			_data = Empty.Data;
		}

		/// <summary>
		/// Initializes the list with the provided array.
		/// </summary>
		/// <param name="data">An array as source</param>
		/// <param name="copyData">If the array should be copied</param>
		public ImmutableList(T[] data, bool copyData = false)
		{
			if (copyData)
			{
				_data = new T[data.Length];
				Array.Copy(data, _data, data.Length);
			}
			else
			{
				_data = data;
			}
		}

		/// <summary>
		/// Initializes the list with the provided array.
		/// </summary>
		public ImmutableList(IEnumerable<T> source) : this(source.ToArray(), copyData: false)
		{
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> Clear()
		{
			return Empty;
		}

		/// <inheritdoc />
		[Pure]
		public int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer)
		{
			var comparer = equalityComparer ?? EqualityComparer<T>.Default;
			for (var i = index; i < index + count; i++)
			{
				if (comparer.Equals(_data[i], item))
				{
					return i;
				}
			}
			return -1;
		}

		/// <inheritdoc />
		[Pure]
		public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer)
		{
			var comparer = equalityComparer ?? EqualityComparer<T>.Default;
			for (var i = index + count - 1; i >= index; i--)
			{
				if (comparer.Equals(_data[i], item))
				{
					return i;
				}
			}
			return -1;
		}

		/// <inheritdoc />
		[Pure]
		IImmutableList<T> IImmutableList<T>.Add(T value)
		{
			return Add(value);
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> AddRange(IEnumerable<T> items)
		{
			var itemsToAdd = items.ToArray();
			var newData = new T[_data.Length + itemsToAdd.Length];
			Array.Copy(_data, newData, _data.Length);
			Array.Copy(itemsToAdd, 0, newData, _data.Length, itemsToAdd.Length);
			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> Insert(int index, T element)
		{
			var newData = new T[_data.Length + 1];
			newData[index] = element; // will throw out-of-range exception if index is not valid

			if (index > 0)
			{
				Array.Copy(_data, newData, index);
			}
			if (index + 1 < _data.Length)
			{
				Array.Copy(_data, index, newData, index + 1, _data.Length - index);
			}
			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> InsertRange(int index, IEnumerable<T> items)
		{
			var insertedItems = items.ToArray();
			if (insertedItems.Length == 0)
			{
				return this; // nothing to insert
			}

			var newData = new T[_data.Length + insertedItems.Length];

			if (index > 0)
			{
				Array.Copy(_data, newData, index);
			}

			Array.Copy(insertedItems, 0, newData, index, insertedItems.Length);

			if (index + 1 < _data.Length)
			{
				Array.Copy(_data, index, newData, index + insertedItems.Length, _data.Length - index);
			}
			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> Remove(T value, IEqualityComparer<T> equalityComparer)
		{
			var comparer = equalityComparer ?? EqualityComparer<T>.Default;
			return RemoveAll(x => comparer.Equals(x, value));

		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> RemoveAll(Predicate<T> match)
		{
			var newData = _data.Where(x => !match(x)).ToArray();
			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
		{
			var itemsToRemove = items.ToArray();
			if (itemsToRemove.Length == 0)
			{
				return this; // nothing to remove
			}
			var comparer = equalityComparer ?? EqualityComparer<T>.Default;
			return RemoveAll(x => itemsToRemove.Contains(x, comparer));
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> RemoveRange(int index, int count)
		{
			if (index < 0 || index >= _data.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}
			if (count < 0 || count + index > _data.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			if (count == 0)
			{
				return this;
			}
			if (count == _data.Length)
			{
				return Empty;
			}

			var newData = new T[_data.Length - count];
			if (index > 0)
			{
				Array.Copy(_data, 0, newData, 0, index);
			}

			if (index + count < _data.Length)
			{
				Array.Copy(_data, 0, newData, 0, index);
			}

			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <inheritdoc />
		[Pure]
		IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
		{
			return RemoveAt(index);
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> SetItem(int index, T value)
		{
			if (Equals(_data[index], value))
			{
				return this;
			}
			var newData = new T[_data.Length];
			Array.Copy(_data, newData, _data.Length);
			newData[index] = value;
			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <inheritdoc />
		[Pure]
		public IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
		{
			var comparer = equalityComparer ?? EqualityComparer<T>.Default;

			if (comparer.Equals(oldValue, newValue))
			{
				return this; // nothing to change
			}

			var changed = false;

			var newData = new T[_data.Length];
			Array.Copy(_data, newData, _data.Length);

			for (var i = 0; i < _data.Length; i++)
			{
				if (comparer.Equals(_data[i], oldValue))
				{
					newData[i] = newValue;
					changed = true;
				}
			}

			return changed ? new ImmutableList<T>(newData, copyData: false) : this;
		}

		/// <summary>
		/// Returns a new list with the specifed value appended at the end.
		/// </summary>
		/// <param name="value"></param>
		[Pure]
		public ImmutableList<T> Add(T value)
		{
			var newData = new T[_data.Length + 1];
			Array.Copy(_data, newData, _data.Length);
			newData[_data.Length] = value;
			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <summary>
		/// Returns a new list with specified value removed.
		/// </summary>
		/// <param name="value">The value to remove</param>
		/// <returns>A new list</returns>
		[Pure]
		public ImmutableList<T> Remove(T value)
		{
			var i = IndexOf(value);
			return i < 0 ? this : RemoveAt(i);
		}


		/// <summary>
		/// Determines whether the list contains a specified element
		/// </summary>
		/// <param name="value">The value to locate.</param>
		[Pure]
		public bool Contains(T value)
		{
			return _data.Contains(value);
		}

		/// <summary>
		/// Removes the item at the specified index.
		/// </summary>
		/// <param name="index">The index to remove</param>
		/// <returns>A new list with the item removed</returns>
		[Pure]
		public ImmutableList<T> RemoveAt(int index)
		{
			if(_data.Length == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			var newData = new T[_data.Length - 1];
			if (index > 0)
			{
				Array.Copy(_data, 0, newData, 0, index);
			}
			Array.Copy(_data, index + 1, newData, index, _data.Length - index - 1);
			return new ImmutableList<T>(newData, copyData: false);
		}

		/// <summary>
		/// Returns the index of the specified value
		/// </summary>
		/// <param name="value"></param>
		[Pure]
		public int IndexOf(T value)
		{
			for (var i = 0; i < _data.Length; ++i)
			{
				if (Equals(_data[i], value))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// The underlying data available for thread-safe access
		/// </summary>
		/// <remarks>
		/// Please, don't mutate it! There's not protection against this.
		/// </remarks>
		public T[] Data => _data;

		/// <inheritdoc />
		public IEnumerator<T> GetEnumerator()
		{
			return (_data as ICollection<T>).GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <inheritdoc />
		public int Count => _data.Length;

		/// <inheritdoc />
		public T this[int index] => _data[index];
	}
}
