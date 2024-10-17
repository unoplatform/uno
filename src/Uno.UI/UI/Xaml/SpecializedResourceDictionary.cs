// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Imported from https://github.com/dotnet/runtime/blob/c7804d5b3c8bd32e35cbb674e8f275fcaf754d93/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs

#define TARGET_64BIT // Use runtime detection of 64bits target

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Uno.Foundation;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Specialized Dictionary for ResourceDictionary values backing, using <see cref="ResourceKey"/> for the dictionary key.
	/// </summary>
	internal class SpecializedResourceDictionary
	{
		/// <summary>
		/// Represents a key for the source dictionary
		/// </summary>
		[DebuggerDisplay("Key={Key}")]
		public readonly struct ResourceKey
		{
			public readonly string Key;
			public readonly Type TypeKey;
			public readonly uint HashCode;

			public static ResourceKey Empty { get; } = new ResourceKey(false);

			public bool IsEmpty => Key == null;

			private ResourceKey(bool dummy)
			{
				Key = null;
				TypeKey = null;
				HashCode = 0;
			}

			/// <summary>
			/// Builds a ResourceKey based on an unknown object
			/// </summary>
			/// <param name="key">The original key to use</param>
			public ResourceKey(object key)
			{
				if (key is string s)
				{
					Key = s;
					TypeKey = null;
					HashCode = (uint)s.GetHashCode();
				}
				else if (key is Type t)
				{
					Key = t.FullName;
					TypeKey = t;
					HashCode = (uint)t.GetHashCode();
				}
				else if (key is ResourceKey)
				{
					// This should never happen. A ResourceKey should always be passed to a parameter of type ResourceKey via an appropriate method overload,
					// rather than being passed as an object, for performance.
					throw new InvalidOperationException($"Received {nameof(ResourceKey)} wrapped as object.");
				}
				else
				{
					Key = key.ToString();
					TypeKey = null;
					HashCode = (uint)key.GetHashCode();
				}
			}

			/// <summary>
			/// Builds a ResourceKey based on a string for faster creation
			/// </summary>
			/// <param name="key">A string typed key</param>
			public ResourceKey(string key)
			{
				Key = key;
				TypeKey = null;
				HashCode = (uint)key.GetHashCode();
			}

			/// <summary>
			/// Builds a ResourceKey based on a Type for faster creation
			/// </summary>
			/// <param name="key">A string typed key</param>
			public ResourceKey(Type key)
			{
				Key = key.FullName;
				TypeKey = key;
				HashCode = (uint)key.GetHashCode();
			}

			/// <summary>
			/// Compares this instance with another ResourceKey instance
			/// </summary>
			public bool Equals(in ResourceKey other)
				=> TypeKey == other.TypeKey && Key == other.Key;


			public static implicit operator ResourceKey(string key)
				=> new ResourceKey(key);

			public static implicit operator ResourceKey(Type key)
				=> new ResourceKey(key);
		}

		private int[] _buckets;
		private Entry[] _entries;
#if TARGET_64BIT
		private ulong _fastModMultiplier;
		private static bool Is64Bits = Marshal.SizeOf(typeof(IntPtr)) >= 8
#if __WASM__
			|| WebAssemblyRuntime.IsWebAssembly;
#else
			;
#endif
#endif
		private int _count;
		private int _freeList;
		private int _freeCount;
		private int _version;

		private KeyCollection _keys;
		private ValueCollection _values;
		private const int StartOfFreeList = -3;

		public SpecializedResourceDictionary() : this(0) { }

		public SpecializedResourceDictionary(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("ExceptionArgument.capacity");
			}

			if (capacity > 0)
			{
				Initialize(capacity);
			}
		}

		public void AddRange(SpecializedResourceDictionary source)
		{
			// Fallback path for IEnumerable that isn't a non-subclassed Dictionary<TKey,TValue>.
			foreach (KeyValuePair<ResourceKey, object> pair in source)
			{
				Add(pair.Key, pair.Value);
			}
		}

		public int Count => _count - _freeCount;

		public KeyCollection Keys => _keys ??= new KeyCollection(this);

		public ValueCollection Values => _values ??= new ValueCollection(this);

		public object this[in ResourceKey key]
		{
			get
			{
				ref object value = ref FindValue(key);
				if (!Unsafe.IsNullRef(ref value))
				{
					return value;
				}

				throw new KeyNotFoundException("key");
			}
			set
			{
				bool modified = TryInsert(key, value, InsertionBehavior.OverwriteExisting);
				Debug.Assert(modified);
			}
		}

		public void Add(in ResourceKey key, object value)
		{
			bool modified = TryInsert(key, value, InsertionBehavior.ThrowOnExisting);
			Debug.Assert(modified); // If there was an existing key and the Add failed, an exception will already have been thrown.
		}

		public void Clear()
		{
			int count = _count;
			if (count > 0)
			{
				Debug.Assert(_buckets != null, "_buckets should be non-null");
				Debug.Assert(_entries != null, "_entries should be non-null");

				Array.Clear(_buckets);

				_count = 0;
				_freeList = -1;
				_freeCount = 0;
				Array.Clear(_entries, 0, count);
			}
		}

		public bool ContainsKey(in ResourceKey key) => !Unsafe.IsNullRef(ref FindValue(key));

		public bool ContainsValue(object value)
		{
			Entry[] entries = _entries;
			if (value == null)
			{
				for (int i = 0; i < _count; i++)
				{
					if (entries![i].next >= -1 && entries[i].value == null)
					{
						return true;
					}
				}
			}
			else if (typeof(object).IsValueType)
			{
				// ValueType: Devirtualize with EqualityComparer<object>.Default intrinsic
				for (int i = 0; i < _count; i++)
				{
					if (entries![i].next >= -1 && EqualityComparer<object>.Default.Equals(entries[i].value, value))
					{
						return true;
					}
				}
			}
			else
			{
				// Object type: Shared Generic, EqualityComparer<object>.Default won't devirtualize
				// https://github.com/dotnet/runtime/issues/10050
				// So cache in a local rather than get EqualityComparer per loop iteration
				EqualityComparer<object> defaultComparer = EqualityComparer<object>.Default;
				for (int i = 0; i < _count; i++)
				{
					if (entries![i].next >= -1 && defaultComparer.Equals(entries[i].value, value))
					{
						return true;
					}
				}
			}

			return false;
		}

		public Enumerator GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

		private ref object FindValue(in ResourceKey key)
		{
			ref Entry entry = ref Unsafe.NullRef<Entry>();

			if (_buckets != null)
			{
				Debug.Assert(_entries != null, "expected entries to be != null");

				uint hashCode = key.HashCode;
				int i = GetBucket(hashCode);
				Entry[] entries = _entries;
				uint length = (uint)entries.Length;
				uint collisionCount = 0;
				i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
				do
				{
					// Should be a while loop https://github.com/dotnet/runtime/issues/9422
					// Test in if to drop range check for following array access
					if ((uint)i >= length)
					{
						goto ReturnNotFound;
					}

					entry = ref entries[i];
					if (entry.hashCode == hashCode && entry.key.Equals(key))
					{
						goto ReturnFound;
					}

					i = entry.next;

					collisionCount++;
				} while (collisionCount <= length);

				// The chain of entries forms a loop; which means a concurrent update has happened.
				// Break out of the loop and throw, rather than looping forever.
				goto ConcurrentOperation;
			}

			goto ReturnNotFound;

		ConcurrentOperation:
			throw new InvalidOperationException("ConcurrentOperationsNotSupported");
		ReturnFound:
			ref object value = ref entry.value;
		Return:
			return ref value;
		ReturnNotFound:
			value = ref Unsafe.NullRef<object>();
			goto Return;
		}

		private int Initialize(int capacity)
		{
			int size = HashHelpers.GetPrime(capacity);
			int[] buckets = new int[size];
			Entry[] entries = new Entry[size];

			// Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
			_freeList = -1;
#if TARGET_64BIT
			if (Is64Bits)
			{
				_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
			}
#endif
			_buckets = buckets;
			_entries = entries;

			return size;
		}

		private bool TryInsert(in ResourceKey key, object value, InsertionBehavior behavior)
		{
			if (_buckets == null)
			{
				Initialize(0);
			}

			Debug.Assert(_buckets != null);

			Entry[] entries = _entries;
			Debug.Assert(entries != null, "expected entries to be non-null");

			uint hashCode = key.HashCode;

			uint collisionCount = 0;
			ref int bucket = ref GetBucket(hashCode);
			int i = bucket - 1; // Value in _buckets is 1-based

			while (true)
			{
				// Should be a while loop https://github.com/dotnet/runtime/issues/9422
				// Test uint in if rather than loop condition to drop range check for following array access
				if ((uint)i >= (uint)entries.Length)
				{
					break;
				}

				if (entries[i].hashCode == hashCode && entries[i].key.Equals(key))
				{
					if (behavior == InsertionBehavior.OverwriteExisting)
					{
						entries[i].value = value;
						return true;
					}

					if (behavior == InsertionBehavior.ThrowOnExisting)
					{
						throw new InvalidOperationException("AddingDuplicateWithKeyArgumentException(key)");
					}

					return false;
				}

				i = entries[i].next;

				collisionCount++;
				if (collisionCount > (uint)entries.Length)
				{
					// The chain of entries forms a loop; which means a concurrent update has happened.
					// Break out of the loop and throw, rather than looping forever.
					throw new InvalidOperationException("ConcurrentOperationsNotSupported");
				}
			}

			int index;
			if (_freeCount > 0)
			{
				index = _freeList;
				Debug.Assert((StartOfFreeList - entries[_freeList].next) >= -1, "shouldn't overflow because `next` cannot underflow");
				_freeList = StartOfFreeList - entries[_freeList].next;
				_freeCount--;
			}
			else
			{
				int count = _count;
				if (count == entries.Length)
				{
					Resize();
					bucket = ref GetBucket(hashCode);
				}
				index = count;
				_count = count + 1;
				entries = _entries;
			}

			ref Entry entry = ref entries![index];
			entry.hashCode = hashCode;
			entry.next = bucket - 1; // Value in _buckets is 1-based
			entry.key = key;
			entry.value = value;
			bucket = index + 1; // Value in _buckets is 1-based
			_version++;

			return true;
		}

		private void Resize() => Resize(HashHelpers.ExpandPrime(_count), false);

		private void Resize(int newSize, bool forceNewHashCodes)
		{
			// Value types never rehash
			Debug.Assert(!forceNewHashCodes || !typeof(object).IsValueType);
			Debug.Assert(_entries != null, "_entries should be non-null");
			Debug.Assert(newSize >= _entries.Length);

			Entry[] entries = new Entry[newSize];

			int count = _count;
			Array.Copy(_entries, entries, count);

			// Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
			_buckets = new int[newSize];
#if TARGET_64BIT
			if (Is64Bits)
			{
				_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
			}
#endif
			for (int i = 0; i < count; i++)
			{
				if (entries[i].next >= -1)
				{
					ref int bucket = ref GetBucket(entries[i].hashCode);
					entries[i].next = bucket - 1; // Value in _buckets is 1-based
					bucket = i + 1;
				}
			}

			_entries = entries;
		}

		public bool Remove(in ResourceKey key)
		{
			// The overload Remove(object key, out object value) is a copy of this method with one additional
			// statement to copy the value for entry being removed into the output parameter.
			// Code has been intentionally duplicated for performance reasons.

			if (_buckets != null)
			{
				Debug.Assert(_entries != null, "entries should be non-null");
				uint collisionCount = 0;
				uint hashCode = key.HashCode;
				ref int bucket = ref GetBucket(hashCode);
				Entry[] entries = _entries;
				int last = -1;
				int i = bucket - 1; // Value in buckets is 1-based
				while (i >= 0)
				{
					ref Entry entry = ref entries[i];

					if (entry.hashCode == hashCode && entry.key.Equals(key))
					{
						if (last < 0)
						{
							bucket = entry.next + 1; // Value in buckets is 1-based
						}
						else
						{
							entries[last].next = entry.next;
						}

						Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
						entry.next = StartOfFreeList - _freeList;

						//if (RuntimeHelpers.IsReferenceOrContainsReferences<object>())
						{
							entry.key = default!;
						}

						//if (RuntimeHelpers.IsReferenceOrContainsReferences<object>())
						{
							entry.value = default!;
						}

						_freeList = i;
						_freeCount++;
						return true;
					}

					last = i;
					i = entry.next;

					collisionCount++;
					if (collisionCount > (uint)entries.Length)
					{
						// The chain of entries forms a loop; which means a concurrent update has happened.
						// Break out of the loop and throw, rather than looping forever.
						throw new InvalidOperationException("ConcurrentOperationsNotSupported");
					}
				}
			}
			return false;
		}

		public bool Remove(in ResourceKey key, out object value)
		{
			// This overload is a copy of the overload Remove(object key) with one additional
			// statement to copy the value for entry being removed into the output parameter.
			// Code has been intentionally duplicated for performance reasons.

			if (_buckets != null)
			{
				Debug.Assert(_entries != null, "entries should be non-null");
				uint collisionCount = 0;
				uint hashCode = key.HashCode;
				ref int bucket = ref GetBucket(hashCode);
				Entry[] entries = _entries;
				int last = -1;
				int i = bucket - 1; // Value in buckets is 1-based
				while (i >= 0)
				{
					ref Entry entry = ref entries[i];

					if (entry.hashCode == hashCode && entry.key.Equals(key))
					{
						if (last < 0)
						{
							bucket = entry.next + 1; // Value in buckets is 1-based
						}
						else
						{
							entries[last].next = entry.next;
						}

						value = entry.value;

						Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
						entry.next = StartOfFreeList - _freeList;

						//if (RuntimeHelpers.IsReferenceOrContainsReferences<object>())
						{
							entry.key = default!;
						}

						// if (RuntimeHelpers.IsReferenceOrContainsReferences<object>())
						{
							entry.value = default!;
						}

						_freeList = i;
						_freeCount++;
						return true;
					}

					last = i;
					i = entry.next;

					collisionCount++;
					if (collisionCount > (uint)entries.Length)
					{
						// The chain of entries forms a loop; which means a concurrent update has happened.
						// Break out of the loop and throw, rather than looping forever.
						throw new InvalidOperationException("ConcurrentOperationsNotSupported()");
					}
				}
			}

			value = default;
			return false;
		}

		public bool TryGetValue(in ResourceKey key, out object value)
		{
			ref object valRef = ref FindValue(key);
			if (!Unsafe.IsNullRef(ref valRef))
			{
				value = valRef;
				return true;
			}

			value = default;
			return false;
		}

		public bool TryAdd(in ResourceKey key, object value) =>
			TryInsert(key, value, InsertionBehavior.None);

		/// <summary>
		/// Ensures that the dictionary can hold up to 'capacity' entries without any further expansion of its backing storage
		/// </summary>
		public int EnsureCapacity(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("ExceptionArgument.capacity");
			}

			int currentCapacity = _entries == null ? 0 : _entries.Length;
			if (currentCapacity >= capacity)
			{
				return currentCapacity;
			}

			_version++;

			if (_buckets == null)
			{
				return Initialize(capacity);
			}

			int newSize = HashHelpers.GetPrime(capacity);
			Resize(newSize, forceNewHashCodes: false);
			return newSize;
		}

		/// <summary>
		/// Sets the capacity of this dictionary to what it would be if it had been originally initialized with all its entries
		/// </summary>
		/// <remarks>
		/// This method can be used to minimize the memory overhead
		/// once it is known that no new elements will be added.
		///
		/// To allocate minimum size storage array, execute the following statements:
		///
		/// dictionary.Clear();
		/// dictionary.TrimExcess();
		/// </remarks>
		public void TrimExcess() => TrimExcess(Count);

		/// <summary>
		/// Sets the capacity of this dictionary to hold up 'capacity' entries without any further expansion of its backing storage
		/// </summary>
		/// <remarks>
		/// This method can be used to minimize the memory overhead
		/// once it is known that no new elements will be added.
		/// </remarks>
		public void TrimExcess(int capacity)
		{
			if (capacity < Count)
			{
				throw new ArgumentOutOfRangeException("ExceptionArgument.capacity");
			}

			int newSize = HashHelpers.GetPrime(capacity);
			Entry[] oldEntries = _entries;
			int currentCapacity = oldEntries == null ? 0 : oldEntries.Length;
			if (newSize >= currentCapacity)
			{
				return;
			}

			int oldCount = _count;
			_version++;
			Initialize(newSize);

			Debug.Assert(!(oldEntries is null));

			CopyEntries(oldEntries, oldCount);
		}

		private void CopyEntries(Entry[] entries, int count)
		{
			Debug.Assert(!(_entries is null));

			Entry[] newEntries = _entries;
			int newCount = 0;
			for (int i = 0; i < count; i++)
			{
				uint hashCode = entries[i].hashCode;
				if (entries[i].next >= -1)
				{
					ref Entry entry = ref newEntries[newCount];
					entry = entries[i];
					ref int bucket = ref GetBucket(hashCode);
					entry.next = bucket - 1; // Value in _buckets is 1-based
					bucket = newCount + 1;
					newCount++;
				}
			}

			_count = newCount;
			_freeCount = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ref int GetBucket(uint hashCode)
		{
			int[] buckets = _buckets!;
#if TARGET_64BIT
			if (Is64Bits)
			{
				return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
			}
			else
			{
				return ref buckets[hashCode % (uint)buckets.Length];
			}
#else
            return ref buckets[hashCode % (uint)buckets.Length];
#endif
		}

		private struct Entry
		{
			public uint hashCode;
			/// <summary>
			/// 0-based index of next entry in chain: -1 means end of chain
			/// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
			/// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
			/// </summary>
			public int next;
			public ResourceKey key;     // Key of entry
			public object value; // Value of entry
		}

		public struct Enumerator : IEnumerator<KeyValuePair<ResourceKey, object>>, IEnumerator, IDictionaryEnumerator
		{
			private readonly SpecializedResourceDictionary _dictionary;
			private readonly int _version;
			private int _index;
			private KeyValuePair<ResourceKey, object> _current;
			private readonly int _getEnumeratorRetType;  // What should Enumerator.Current return?

			internal const int DictEntry = 1;
			internal const int KeyValuePair = 2;

			internal Enumerator(SpecializedResourceDictionary dictionary, int getEnumeratorRetType)
			{
				_dictionary = dictionary;
				_version = dictionary._version;
				_index = 0;
				_getEnumeratorRetType = getEnumeratorRetType;
				_current = default;
			}

			public bool MoveNext()
			{
				if (_version != _dictionary._version)
				{
					throw new InvalidOperationException("InvalidOperation_EnumFailedVersion()");
				}

				// Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
				// dictionary.count+1 could be negative if dictionary.count is int.MaxValue
				while ((uint)_index < (uint)_dictionary._count)
				{
					ref Entry entry = ref _dictionary._entries![_index++];

					if (entry.next >= -1)
					{
						_current = new KeyValuePair<ResourceKey, object>(entry.key, entry.value);
						return true;
					}
				}

				_index = _dictionary._count + 1;
				_current = default;
				return false;
			}

			public KeyValuePair<ResourceKey, object> Current => _current;

			public void Dispose() { }

			object IEnumerator.Current
			{
				get
				{
					if (_index == 0 || (_index == _dictionary._count + 1))
					{
						throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen()");
					}

					if (_getEnumeratorRetType == DictEntry)
					{
						return new DictionaryEntry(_current.Key, _current.Value);
					}

					return new KeyValuePair<object, object>(_current.Key, _current.Value);
				}
			}

			void IEnumerator.Reset()
			{
				if (_version != _dictionary._version)
				{
					throw new InvalidOperationException("InvalidOperation_EnumFailedVersion()");
				}

				_index = 0;
				_current = default;
			}

			DictionaryEntry IDictionaryEnumerator.Entry
			{
				get
				{
					if (_index == 0 || (_index == _dictionary._count + 1))
					{
						throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen()");
					}

					return new DictionaryEntry(_current.Key, _current.Value);
				}
			}

			object IDictionaryEnumerator.Key
			{
				get
				{
					if (_index == 0 || (_index == _dictionary._count + 1))
					{
						throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen()");
					}

					return _current.Key;
				}
			}

			object IDictionaryEnumerator.Value
			{
				get
				{
					if (_index == 0 || (_index == _dictionary._count + 1))
					{
						throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen()");
					}

					return _current.Value;
				}
			}
		}

		public sealed class KeyCollection : ICollection<ResourceKey>, ICollection, IReadOnlyCollection<ResourceKey>
		{
			private readonly SpecializedResourceDictionary _dictionary;

			public KeyCollection(SpecializedResourceDictionary dictionary)
			{
				if (dictionary == null)
				{
					throw new ArgumentNullException("ExceptionArgument.dictionary");
				}

				_dictionary = dictionary;
			}

			public Enumerator GetEnumerator() => new Enumerator(_dictionary);

			public void CopyTo(ResourceKey[] array, int index)
			{
				if (array == null)
				{
					throw new ArgumentNullException("ExceptionArgument.array");
				}

				if (index < 0 || index > array.Length)
				{
					throw new ArgumentOutOfRangeException("NeedNonNegNumException");
				}

				if (array.Length - index < _dictionary.Count)
				{
					throw new ArgumentException("ExceptionResource.Arg_ArrayPlusOffTooSmall");
				}

				int count = _dictionary._count;
				Entry[] entries = _dictionary._entries;
				for (int i = 0; i < count; i++)
				{
					if (entries![i].next >= -1) array[index++] = entries[i].key;
				}
			}

			public int Count => _dictionary.Count;

			bool ICollection<ResourceKey>.IsReadOnly => true;

			void ICollection<ResourceKey>.Add(ResourceKey item) =>
				throw new NotSupportedException("ExceptionResource.NotSupported_KeyCollectionSet");

			void ICollection<ResourceKey>.Clear() =>
				throw new NotSupportedException("ExceptionResource.NotSupported_KeyCollectionSet");

			bool ICollection<ResourceKey>.Contains(ResourceKey item) =>
				_dictionary.ContainsKey(item);

			bool ICollection<ResourceKey>.Remove(ResourceKey item)
			{
				throw new NotSupportedException("ExceptionResource.NotSupported_KeyCollectionSet");
			}

			IEnumerator<ResourceKey> IEnumerable<ResourceKey>.GetEnumerator() => new Enumerator(_dictionary);

			IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dictionary);

			void ICollection.CopyTo(Array array, int index)
			{
				if (array == null)
				{
					throw new ArgumentNullException("ExceptionArgument.array");
				}

				if (array.Rank != 1)
				{
					throw new ArgumentException("ExceptionResource.Arg_RankMultiDimNotSupported");
				}

				if (array.GetLowerBound(0) != 0)
				{
					throw new ArgumentException("ExceptionResource.Arg_NonZeroLowerBound");
				}

				if ((uint)index > (uint)array.Length)
				{
					throw new ArgumentOutOfRangeException("NeedNonNegNumException()");
				}

				if (array.Length - index < _dictionary.Count)
				{
					throw new ArgumentException("ExceptionResource.Arg_ArrayPlusOffTooSmall");
				}

				if (array is ResourceKey[] keys)
				{
					CopyTo(keys, index);
				}
				else
				{
					object[] objects = array as object[];
					if (objects == null)
					{
						throw new ArgumentException("Argument_InvalidArrayType()");
					}

					int count = _dictionary._count;
					Entry[] entries = _dictionary._entries;
					index = MoveKeys(index, objects, count, entries);
				}
			}

			/// <remarks>
			/// This method contains or is called by a try/catch containing method and
			/// can be significantly slower than other methods as a result on WebAssembly.
			/// See https://github.com/dotnet/runtime/issues/56309
			/// </remarks>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static int MoveKeys(int index, object[] objects, int count, Entry[] entries)
			{
				try
				{
					for (int i = 0; i < count; i++)
					{
						if (entries![i].next >= -1) objects[index++] = entries[i].key;
					}
				}
				catch (ArrayTypeMismatchException)
				{
					throw new ArgumentException("Argument_InvalidArrayType()");
				}

				return index;
			}

			bool ICollection.IsSynchronized => false;

			object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

			public struct Enumerator : IEnumerator<ResourceKey>, IEnumerator
			{
				private readonly SpecializedResourceDictionary _dictionary;
				private int _index;
				private readonly int _version;
				private ResourceKey _currenobject;

				internal Enumerator(SpecializedResourceDictionary dictionary)
				{
					_dictionary = dictionary;
					_version = dictionary._version;
					_index = 0;
					_currenobject = default;
				}

				public void Dispose() { }

				public bool MoveNext()
				{
					if (_version != _dictionary._version)
					{
						throw new InvalidOperationException("InvalidOperation_EnumFailedVersion()");
					}

					while ((uint)_index < (uint)_dictionary._count)
					{
						ref Entry entry = ref _dictionary._entries![_index++];

						if (entry.next >= -1)
						{
							_currenobject = entry.key;
							return true;
						}
					}

					_index = _dictionary._count + 1;
					_currenobject = default;
					return false;
				}

				public ResourceKey Current => _currenobject!;

				object IEnumerator.Current
				{
					get
					{
						if (_index == 0 || (_index == _dictionary._count + 1))
						{
							throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen()");
						}

						return _currenobject;
					}
				}

				void IEnumerator.Reset()
				{
					if (_version != _dictionary._version)
					{
						throw new InvalidOperationException("InvalidOperation_EnumFailedVersion()");
					}

					_index = 0;
					_currenobject = default;
				}
			}
		}

		public sealed class ValueCollection : ICollection<object>, ICollection, IReadOnlyCollection<object>
		{
			private readonly SpecializedResourceDictionary _dictionary;

			public ValueCollection(SpecializedResourceDictionary dictionary)
			{
				if (dictionary == null)
				{
					throw new ArgumentNullException("ExceptionArgument.dictionary");
				}

				_dictionary = dictionary;
			}

			public Enumerator GetEnumerator() => new Enumerator(_dictionary);

			public void CopyTo(object[] array, int index)
			{
				if (array == null)
				{
					throw new ArgumentNullException("ExceptionArgument.array");
				}

				if ((uint)index > array.Length)
				{
					throw new ArgumentOutOfRangeException("NeedNonNegNumException()");
				}

				if (array.Length - index < _dictionary.Count)
				{
					throw new ArgumentException("ExceptionResource.Arg_ArrayPlusOffTooSmall");
				}

				int count = _dictionary._count;
				Entry[] entries = _dictionary._entries;
				for (int i = 0; i < count; i++)
				{
					if (entries![i].next >= -1) array[index++] = entries[i].value;
				}
			}

			public int Count => _dictionary.Count;

			bool ICollection<object>.IsReadOnly => true;

			void ICollection<object>.Add(object item) =>
				throw new NotSupportedException("ExceptionResource.NotSupported_ValueCollectionSet");

			bool ICollection<object>.Remove(object item)
			{
				throw new NotSupportedException("ExceptionResource.NotSupported_ValueCollectionSet");
			}

			void ICollection<object>.Clear() =>
				throw new NotSupportedException("ExceptionResource.NotSupported_ValueCollectionSet");

			bool ICollection<object>.Contains(object item) => _dictionary.ContainsValue(item);

			IEnumerator<object> IEnumerable<object>.GetEnumerator() => new Enumerator(_dictionary);

			IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dictionary);

			void ICollection.CopyTo(Array array, int index)
			{
				if (array == null)
				{
					throw new ArgumentNullException("ExceptionArgument.array");
				}

				if (array.Rank != 1)
				{
					throw new ArgumentException("ExceptionResource.Arg_RankMultiDimNotSupported");
				}

				if (array.GetLowerBound(0) != 0)
				{
					throw new ArgumentException("ExceptionResource.Arg_NonZeroLowerBound");
				}

				if ((uint)index > (uint)array.Length)
				{
					throw new ArgumentOutOfRangeException("eedNonNegNumException()");
				}

				if (array.Length - index < _dictionary.Count)
				{
					throw new ArgumentException("ExceptionResource.Arg_ArrayPlusOffTooSmall");
				}

				if (array is object[] values)
				{
					CopyTo(values, index);
				}
				else
				{
					object[] objects = array as object[];
					if (objects == null)
					{
						throw new ArgumentException("Argument_InvalidArrayType()");
					}

					int count = _dictionary._count;
					Entry[] entries = _dictionary._entries;
					index = MoveValues(index, objects, count, entries);
				}
			}

			/// <remarks>
			/// This method contains or is called by a try/catch containing method and can be significantly slower than other methods as a result on WebAssembly.
			/// See https://github.com/dotnet/runtime/issues/56309
			/// </remarks>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static int MoveValues(int index, object[] objects, int count, Entry[] entries)
			{
				try
				{
					for (int i = 0; i < count; i++)
					{
						if (entries![i].next >= -1) objects[index++] = entries[i].value!;
					}
				}
				catch (ArrayTypeMismatchException)
				{
					throw new ArgumentException("Argument_InvalidArrayType()");
				}

				return index;
			}

			bool ICollection.IsSynchronized => false;

			object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

			public struct Enumerator : IEnumerator<object>, IEnumerator
			{
				private readonly SpecializedResourceDictionary _dictionary;
				private int _index;
				private readonly int _version;
				private object _currenobject;

				internal Enumerator(SpecializedResourceDictionary dictionary)
				{
					_dictionary = dictionary;
					_version = dictionary._version;
					_index = 0;
					_currenobject = default;
				}

				public void Dispose() { }

				public bool MoveNext()
				{
					if (_version != _dictionary._version)
					{
						throw new InvalidOperationException("InvalidOperation_EnumFailedVersion()");
					}

					while ((uint)_index < (uint)_dictionary._count)
					{
						ref Entry entry = ref _dictionary._entries![_index++];

						if (entry.next >= -1)
						{
							_currenobject = entry.value;
							return true;
						}
					}
					_index = _dictionary._count + 1;
					_currenobject = default;
					return false;
				}

				public object Current => _currenobject!;

				object IEnumerator.Current
				{
					get
					{
						if (_index == 0 || (_index == _dictionary._count + 1))
						{
							throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen()");
						}

						return _currenobject;
					}
				}

				void IEnumerator.Reset()
				{
					if (_version != _dictionary._version)
					{
						throw new InvalidOperationException("InvalidOperation_EnumFailedVersion()");
					}

					_index = 0;
					_currenobject = default;
				}
			}
		}
	}

	/// <summary>
	/// Used internally to control behavior of insertion into a <see cref="Dictionary{TKey, TValue}"/> or <see cref="HashSet{T}"/>.
	/// </summary>
	internal enum InsertionBehavior : byte
	{
		/// <summary>
		/// The default insertion behavior.
		/// </summary>
		None = 0,

		/// <summary>
		/// Specifies that an existing entry with the same key should be overwritten if encountered.
		/// </summary>
		OverwriteExisting = 1,

		/// <summary>
		/// Specifies that if an existing entry with the same key is encountered, an exception should be thrown.
		/// </summary>
		ThrowOnExisting = 2
	}

	internal static partial class HashHelpers
	{
		public const uint HashCollisionThreshold = 100;

		// This is the maximum prime smaller than Array.MaxArrayLength
		public const int MaxPrimeArrayLength = 0x7FEFFFFD;

		public const int HashPrime = 101;

		// Table of prime numbers to use as hash table sizes.
		// A typical resize algorithm would pick the smallest prime number in this array
		// that is larger than twice the previous capacity.
		// Suppose our Hashtable currently has capacity x and enough elements are added
		// such that a resize needs to occur. Resizing first computes 2x then finds the
		// first prime in the table greater than 2x, i.e. if primes are ordered
		// p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
		// Doubling is important for preserving the asymptotic complexity of the
		// hashtable operations such as add.  Having a prime guarantees that double
		// hashing does not lead to infinite loops.  IE, your hash function will be
		// h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
		// We prefer the low computation costs of higher prime numbers over the increased
		// memory allocation of a fixed prime number i.e. when right sizing a HashSet.
		private static readonly int[] s_primes =
		{
			3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
			1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
			17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
			187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
			1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
		};

		public static bool IsPrime(int candidate)
		{
			if ((candidate & 1) != 0)
			{
				int limit = (int)Math.Sqrt(candidate);
				for (int divisor = 3; divisor <= limit; divisor += 2)
				{
					if ((candidate % divisor) == 0)
						return false;
				}
				return true;
			}
			return candidate == 2;
		}

		public static int GetPrime(int min)
		{
			if (min < 0)
				throw new ArgumentException("SR.Arg_HTCapacityOverflow");

			foreach (int prime in s_primes)
			{
				if (prime >= min)
					return prime;
			}

			// Outside of our predefined table. Compute the hard way.
			for (int i = (min | 1); i < int.MaxValue; i += 2)
			{
				if (IsPrime(i) && ((i - 1) % HashPrime != 0))
					return i;
			}
			return min;
		}

		// Returns size of hashtable to grow to.
		public static int ExpandPrime(int oldSize)
		{
			int newSize = 2 * oldSize;

			// Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
			// Note that this check works even when _items.Length overflowed thanks to the (uint) cast
			if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
			{
				Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
				return MaxPrimeArrayLength;
			}

			return GetPrime(newSize);
		}

		/// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
		/// <remarks>This should only be used on 64-bit.</remarks>
		public static ulong GetFastModMultiplier(uint divisor) =>
			ulong.MaxValue / divisor + 1;

		/// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
		/// <remarks>This should only be used on 64-bit.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint FastMod(uint value, uint divisor, ulong multiplier)
		{
			// We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
			// which allows to avoid the long multiplication if the divisor is less than 2**31.
			Debug.Assert(divisor <= int.MaxValue);

			// This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
			// is faster than BigMul currently because we only need the high bits.
			uint highbits = (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

			Debug.Assert(highbits == value % divisor);
			return highbits;
		}
	}
}
