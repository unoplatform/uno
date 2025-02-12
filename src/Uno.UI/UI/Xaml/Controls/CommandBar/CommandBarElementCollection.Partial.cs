// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	partial class CommandBarElementCollection
	{
		private readonly List<ICommandBarElement> _list = new List<ICommandBarElement>();

		public void Init(CommandBar parent, bool notifyCollectionChanging)
		{
			m_parent = parent;
			m_notifyCollectionChanging = notifyCollectionChanging;
		}

		public ICommandBarElement this[int index]
		{
			get => _list[index];
			set => SetAt(index, value);
		}

		public void SetAt(int index, ICommandBarElement item)
		{
			RaiseVectorChanging(CollectionChange.ItemChanged, index);
			_list[index] = item;
			RaiseVectorChanged(CollectionChange.ItemChanged, index);
		}

		public int Count => _list.Count;

		public bool IsReadOnly => ((ICollection<ICommandBarElement>)_list).IsReadOnly;

		public event VectorChangedEventHandler<ICommandBarElement>? VectorChanged;

		public void Add(ICommandBarElement item) => Append(item);
		public void Append(ICommandBarElement item)
		{
			Insert(Count, item);
		}

		public void Clear()
		{
			RaiseVectorChanging(CollectionChange.Reset, 0);
			_list.Clear();
			RaiseVectorChanged(CollectionChange.Reset, 0);
		}
		public bool Contains(ICommandBarElement item) => _list.Contains(item);

		public void CopyTo(ICommandBarElement[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

		public IEnumerator<ICommandBarElement> GetEnumerator() => _list.GetEnumerator();

		public int IndexOf(ICommandBarElement item) => _list.IndexOf(item);

		public void Insert(int index, ICommandBarElement item)
		{
			RaiseVectorChanging(CollectionChange.ItemInserted, index);
			_list.Insert(index, item);
			RaiseVectorChanged(CollectionChange.ItemInserted, index);
		}

		public bool Remove(ICommandBarElement item)
		{
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

		public void RemoveAt(int index)
		{
			RaiseVectorChanging(CollectionChange.ItemRemoved, index);
			_list.RemoveAt(index);
			RaiseVectorChanged(CollectionChange.ItemRemoved, index);
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		private void RaiseVectorChanged(CollectionChange change, int changeIndex)
		{
			VectorChangedEventArgs spArgs;

			spArgs = new VectorChangedEventArgs(change, (uint)changeIndex);
			VectorChanged?.Invoke(this, spArgs);

		}

		private void RaiseVectorChanging(CollectionChange change, int changeIndex)
		{
			if (m_notifyCollectionChanging)
			{
				if (m_parent is { })
				{
					m_parent.NotifyElementVectorChanging(this, change, changeIndex);
				}
			}
		}
	}
}
