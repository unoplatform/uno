using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml
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
		private object _vectorChangedHandlersLock = new();

		// Explicit handlers list to avoid the cost of generic multicast
		// delegates handling on mono's AOT.
		private List<VectorChangedEventHandler<T>> _vectorChangedHandlers;

		public event VectorChangedEventHandler<T> VectorChanged
		{
			add
			{
				lock (_vectorChangedHandlersLock)
				{
					(_vectorChangedHandlers ??= new()).Add(value);
				}
			}

			remove
			{
				lock (_vectorChangedHandlersLock)
				{
					var list = _vectorChangedHandlers ??= new();

					var lastIndex = list.LastIndexOf(value);

					if (lastIndex != -1)
					{
						list.RemoveAt(lastIndex);
					}
				}
			}
		}

		private readonly List<T> _list = new List<T>();

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

			VectorChanged += (s, e) => OnCollectionChanged();
		}

		/// <summary>
		/// An internal direct access to the internal list to be able to do allocation-free enumeration
		/// </summary>
		internal List<T> Items => _list;

		internal void UpdateParent(object parent)
		{
			var actualParent = parent ?? this;

			for (var i = 0; i < _list.Count; i++)
			{
				var item = _list[i];

				// Because parent propagation doesn't currently support all cases, 
				// we can't assume that the DependencyObjectCollection will have a parent.
				// To preserve DataContext propagation, we fallback to self if no parent is set.
				item.SetParent(actualParent);
			}
		}

		public uint Size => (uint)_list.Count;

		public int Count => _list.Count;

		public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

		private protected virtual void ValidateItem(T item) { }

		public T this[int index]
		{
			get => _list[index];
			set
			{
				ValidateItem(value);

				var originalValue = _list[index];

				if (!ReferenceEquals(originalValue, value))
				{
					EnsureNotLocked();

					OnRemoved(originalValue);

					_list[index] = value;

					OnAdded(value);

					RaiseVectorChanged(CollectionChange.ItemChanged, index);
				}
			}
		}

		public int IndexOf(T item) => _list.IndexOf(item);

		public void Insert(int index, T item)
		{
			ValidateItem(item);

			EnsureNotLocked();

			_list.Insert(index, item);

			OnAdded(item);

			RaiseVectorChanged(CollectionChange.ItemInserted, index);
		}

		public void RemoveAt(int index)
		{
			EnsureNotLocked();

			OnRemoved(_list[index]);

			_list.RemoveAt(index);

			RaiseVectorChanged(CollectionChange.ItemRemoved, index);
		}

		public void Add(T item)
		{
			EnsureNotLocked();

			ValidateItem(item);

			_list.Add(item);

			OnAdded(item);

			RaiseVectorChanged(CollectionChange.ItemInserted, _list.Count - 1);
		}

		public void Clear()
		{
			EnsureNotLocked();

			for (int index = 0; index < _list.Count; index++)
			{
				OnRemoved(_list[index]);
			}

			_list.Clear();

			RaiseVectorChanged(CollectionChange.Reset, 0);
		}

		public bool Contains(T item)
			=> _list.Contains(item);

		public void CopyTo(T[] array, int arrayIndex)
			=> _list.CopyTo(array, arrayIndex);

		public bool Remove(T item)
		{
			EnsureNotLocked();

			var index = _list.IndexOf(item);

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
			=> _list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> _list.GetEnumerator();

		internal List<T>.Enumerator GetEnumeratorFast()
			=> _list.GetEnumerator();

		private void RaiseVectorChanged(CollectionChange change, int index)
		{
			// Gets an executable list that does not need to be locked
			int GetInvocationList(out VectorChangedEventHandler<T> single, out VectorChangedEventHandler<T>[] array)
			{
				lock (_vectorChangedHandlersLock)
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
