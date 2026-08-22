using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Non-generic base class used to generate the DependencyObject implementation.
	/// </summary>
	public partial class DependencyObjectCollectionBase : DependencyObject
	{
	}

	/// <summary>
	/// Generic base class from which other collections (DependencyObjectCollection, InlineCollection, GeometryCollection, PathFigureCollection, ArcFigureCollection, etc.) derive.
	/// </summary>
	public partial class DependencyObjectCollection<T> : DependencyObjectCollectionBase, IList<T>, IEnumerable<T>, IEnumerable, IObservableVector<T>
		where T : DependencyObject
	{
		private const int InlineCapacity = 2;

		/// <summary>
		/// Inline storage for the first <see cref="InlineCapacity"/> items, which is enough for the
		/// vast majority of the collections found in a XAML tree.
		/// </summary>
		[InlineArray(InlineCapacity)]
		private struct InlineItems
		{
			private T _item0;
		}

		private InlineItems _inlineItems;

		/// <summary>
		/// Spilled storage, allocated when the collection grows past <see cref="InlineCapacity"/> and
		/// used as the sole storage from that point on.
		/// </summary>
		private List<T> _spilledItems;

		private int _count;

		/// <summary>
		/// Incremented on every mutation, so that enumerators can detect concurrent
		/// modifications the same way <see cref="List{T}"/> does.
		/// </summary>
		private int _version;

		private object _vectorChangedHandlersLock;

		// Explicit handlers list to avoid the cost of generic multicast
		// delegates handling on mono's AOT.
		private List<VectorChangedEventHandler<T>> _vectorChangedHandlers;

		public event VectorChangedEventHandler<T> VectorChanged
		{
			add
			{
				lock (GetOrCreateHandlersLock())
				{
					(_vectorChangedHandlers ??= new()).Add(value);
				}
			}

			remove
			{
				lock (GetOrCreateHandlersLock())
				{
					if (_vectorChangedHandlers is { } list)
					{
						var lastIndex = list.LastIndexOf(value);

						if (lastIndex != -1)
						{
							list.RemoveAt(lastIndex);
						}
					}
				}
			}
		}

		private object GetOrCreateHandlersLock()
		{
			if (_vectorChangedHandlersLock is { } handlersLock)
			{
				return handlersLock;
			}

			var created = new object();
			return Interlocked.CompareExchange(ref _vectorChangedHandlersLock, created, null) ?? created;
		}

		private int _isLocked;

		internal void Lock() => _isLocked++;

		internal void Unlock() => _isLocked--;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EnsureNotLocked()
		{
			if (_isLocked > 0)
			{
				throw new InvalidOperationException("Collection is locked.");
			}
		}

		public DependencyObjectCollection()
		{
			Initialize();
		}

		internal DependencyObjectCollection(DependencyObject parent, bool isAutoPropertyInheritanceEnabled = true)
		{
			IsAutoPropertyInheritanceEnabled = isAutoPropertyInheritanceEnabled;

			Initialize();
			this.SetParent(parent);
		}

		private void Initialize()
		{
			((IDependencyObjectStoreProvider)this).Store.RegisterSelfParentChangedCallback(
				(instance, k, handler) => UpdateParent(handler.NewParent)
			);
		}

		internal void UpdateParent(object parent)
		{
			var actualParent = parent ?? this;

			for (var i = 0; i < _count; i++)
			{
				var item = GetItemUnchecked(i);

				// Because parent propagation doesn't currently support all cases, 
				// we can't assume that the DependencyObjectCollection will have a parent.
				// To preserve DataContext propagation, we fallback to self if no parent is set.
				item.SetParent(actualParent);
			}
		}

		public uint Size => (uint)_count;

		public int Count => _count;

		public bool IsReadOnly => false;

		private protected virtual void ValidateItem(T item) { }

		public T this[int index]
		{
			get => index < _count ? GetItemChecked(index) : default;
			set
			{
				ValidateItem(value);

				var originalValue = GetItemChecked(index);

				if (!ReferenceEquals(originalValue, value))
				{
					EnsureNotLocked();

					OnRemoved(originalValue);

					SetItem(index, value);

					OnAdded(value);

					RaiseVectorChanged(CollectionChange.ItemChanged, index);
				}
			}
		}

		public int IndexOf(T item)
		{
			if (_spilledItems is { } spilled)
			{
				return spilled.IndexOf(item);
			}

			var comparer = EqualityComparer<T>.Default;

			for (var i = 0; i < _count; i++)
			{
				if (comparer.Equals(_inlineItems[i], item))
				{
					return i;
				}
			}

			return -1;
		}

		public void Insert(int index, T item)
		{
			ValidateItem(item);

			EnsureNotLocked();

			InsertItem(index, item);

			OnAdded(item);

			RaiseVectorChanged(CollectionChange.ItemInserted, index);
		}

		public void RemoveAt(int index)
		{
			EnsureNotLocked();

			OnRemoved(GetItemChecked(index));

			RemoveItemAt(index);

			RaiseVectorChanged(CollectionChange.ItemRemoved, index);
		}

		public void Add(T item)
		{
			EnsureNotLocked();

			ValidateItem(item);

			InsertItem(_count, item);

			OnAdded(item);

			RaiseVectorChanged(CollectionChange.ItemInserted, _count - 1);
		}

		public void Clear()
		{
			EnsureNotLocked();

			for (int index = 0; index < _count; index++)
			{
				OnRemoved(GetItemUnchecked(index));
			}

			ClearItems();

			RaiseVectorChanged(CollectionChange.Reset, 0);
		}

		public bool Contains(T item)
			=> IndexOf(item) != -1;

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (_spilledItems is { } spilled)
			{
				spilled.CopyTo(array, arrayIndex);
				return;
			}

			if (array is null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			}

			if (array.Length - arrayIndex < _count)
			{
				throw new ArgumentException("Destination array is not long enough to copy all the items in the collection.", nameof(array));
			}

			for (var i = 0; i < _count; i++)
			{
				array[arrayIndex + i] = _inlineItems[i];
			}
		}

		public bool Remove(T item)
		{
			EnsureNotLocked();

			var index = IndexOf(item);

			if (index != -1)
			{
				RemoveAt(index);

				return true;
			}
			else
			{
				return false;
			}
		}

		public IEnumerator<T> GetEnumerator()
			=> new Enumerator(this);

		IEnumerator IEnumerable.GetEnumerator()
			=> new Enumerator(this);

		/// <summary>
		/// An internal struct-based enumerator to be able to do allocation-free enumeration
		/// </summary>
		internal Enumerator GetEnumeratorFast()
			=> new Enumerator(this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T GetItemUnchecked(int index)
			=> _spilledItems is { } spilled ? spilled[index] : _inlineItems[index];

		private T GetItemChecked(int index)
		{
			if ((uint)index >= (uint)_count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			return GetItemUnchecked(index);
		}

		private void SetItem(int index, T item)
		{
			if ((uint)index >= (uint)_count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if (_spilledItems is { } spilled)
			{
				spilled[index] = item;
			}
			else
			{
				_inlineItems[index] = item;
			}

			_version++;
		}

		private void InsertItem(int index, T item)
		{
			if ((uint)index > (uint)_count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if (_spilledItems is { } spilled)
			{
				spilled.Insert(index, item);
			}
			else if (_count < InlineCapacity)
			{
				for (var i = _count; i > index; i--)
				{
					_inlineItems[i] = _inlineItems[i - 1];
				}

				_inlineItems[index] = item;
			}
			else
			{
				Spill().Insert(index, item);
			}

			_count++;
			_version++;
		}

		private void RemoveItemAt(int index)
		{
			if ((uint)index >= (uint)_count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if (_spilledItems is { } spilled)
			{
				spilled.RemoveAt(index);
			}
			else
			{
				for (var i = index; i < _count - 1; i++)
				{
					_inlineItems[i] = _inlineItems[i + 1];
				}

				_inlineItems[_count - 1] = default;
			}

			_count--;
			_version++;
		}

		private void ClearItems()
		{
			if (_spilledItems is { } spilled)
			{
				spilled.Clear();
			}
			else
			{
				for (var i = 0; i < _count; i++)
				{
					_inlineItems[i] = default;
				}
			}

			_count = 0;
			_version++;
		}

		/// <summary>
		/// Moves the inline items to a <see cref="List{T}"/>, which becomes the sole storage of this collection.
		/// </summary>
		private List<T> Spill()
		{
			var spilled = new List<T>(InlineCapacity * 2);

			for (var i = 0; i < _count; i++)
			{
				spilled.Add(_inlineItems[i]);

				// Release the inline references, the spilled list is now the only storage.
				_inlineItems[i] = default;
			}

			return _spilledItems = spilled;
		}

		private void RaiseVectorChanged(CollectionChange change, int index)
		{
			// Invoked before the external handlers, as the internal hook used to be registered first.
			OnCollectionChanged();

			if (_vectorChangedHandlers is null)
			{
				return;
			}

			// Gets an executable list that does not need to be locked
			int GetInvocationList(out VectorChangedEventHandler<T> single, out VectorChangedEventHandler<T>[] array)
			{
				lock (GetOrCreateHandlersLock())
				{
					if (_vectorChangedHandlers is { Count: > 0 })
					{
						if (_vectorChangedHandlers.Count == 1)
						{
							single = _vectorChangedHandlers[0];
							array = null;
							return 1;
						}
						else
						{
							single = null;

							array = ArrayPool<VectorChangedEventHandler<T>>.Shared.Rent(_vectorChangedHandlers.Count);
							_vectorChangedHandlers.CopyTo(array, 0);

							return _vectorChangedHandlers.Count;
						}
					}
				}

				single = null;
				array = null;
				return 0;
			}

			var count = GetInvocationList(out var single, out var array);

			if (count > 0)
			{
				var args = new VectorChangedEventArgs(change, (uint)index);

				if (count == 1)
				{
					single.Invoke(this, args);
				}
				else
				{
					for (int i = 0; i < count; i++)
					{
						ref var handler = ref array[i];
						handler.Invoke(this, args);

						// Clear the handle immediately, so we don't
						// call ArrayPool.Return with clear.
						handler = null;
					}

					ArrayPool<VectorChangedEventHandler<T>>.Shared.Return(array);
				}
			}
		}

		private protected virtual void OnAdded(T d)
		{
			// Because parent propagation doesn't currently support all cases, 
			// we can't assume that the DependencyObjectCollection will have a parent.
			// To preserve DataContext propagation, we fallback to self if no parent is set.
			d.SetParent(this.GetParent() ?? this);
		}

		private protected virtual void OnRemoved(T d)
		{
			d.SetParent(null);
		}

		private protected virtual void OnCollectionChanged()
		{
		}

		/// <summary>
		/// An allocation-free enumerator over the inline or spilled storage of the collection.
		/// </summary>
		internal struct Enumerator : IEnumerator<T>
		{
			private readonly DependencyObjectCollection<T> _collection;
			private readonly int _version;
			private int _index;
			private T _current;

			internal Enumerator(DependencyObjectCollection<T> collection)
			{
				_collection = collection;
				_version = collection._version;
				_index = 0;
				_current = default;
			}

			public T Current => _current;

			object IEnumerator.Current => _current;

			public bool MoveNext()
			{
				var collection = _collection;

				if (_version != collection._version)
				{
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
				}

				if (_index < collection._count)
				{
					_current = collection.GetItemUnchecked(_index);
					_index++;
					return true;
				}

				_current = default;
				return false;
			}

			void IEnumerator.Reset()
			{
				if (_version != _collection._version)
				{
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
				}

				_index = 0;
				_current = default;
			}

			public void Dispose()
			{
			}
		}
	}

	/// <summary>
	/// Implements a practical collection class that can contain DependencyObject items.
	/// </summary>
	public partial class DependencyObjectCollection : DependencyObjectCollection<DependencyObject>
	{
		public DependencyObjectCollection()
		{
		}

		internal DependencyObjectCollection(DependencyObject parent, bool isAutoPropertyInheritanceEnabled = true)
			: base(parent, isAutoPropertyInheritanceEnabled)
		{
		}
	}
}
