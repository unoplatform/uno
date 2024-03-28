// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeItems.cpp

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class SwipeItems : DependencyObject, IEnumerable<SwipeItem>, IList<SwipeItem>, IObservableVector<SwipeItem>
	{
		public SwipeItems()
		{
			// create the Collection
			var collection = new ObservableCollection<SwipeItem>();

			Items = collection;
		}

		void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == ModeProperty)
			{
				if (((SwipeMode)args.NewValue) == SwipeMode.Execute && m_items.Count > 1)
				{
					throw new ArgumentException("Execute items should only have one item.");
				}
			}
		}

		private ObservableCollection<SwipeItem> Items
		{
			set
			{
				if (Mode == SwipeMode.Execute && value.Count > 1)
				{
					throw new ArgumentException("Execute items should only have one item.");
				}

				m_items = value;
				m_vectorChangedEventSource?.Invoke(this, null);
			}
		}

		public SwipeItem GetAt(uint index)
		{
			if (index >= m_items.Count)
			{
				throw new IndexOutOfRangeException();
			}

			return m_items[(int)index];
		}

		public uint Size => (uint)m_items.Count;

		public bool IndexOf(SwipeItem value, out uint index)
		{
			var i = m_items.IndexOf(value);
			if (i < 0)
			{
				index = 0;
				return false;
			}
			else
			{
				index = (uint)i;
				return true;
			}
		}

		public void SetAt(uint index, SwipeItem value)
		{
			if (index >= m_items.Count)
			{
				throw new IndexOutOfRangeException();
			}

			m_items[(int)index] = value;
			m_vectorChangedEventSource?.Invoke(this, null);
		}

		public void InsertAt(uint index, SwipeItem value)
		{
			if (Mode == SwipeMode.Execute && m_items.Count > 0)
			{
				throw new ArgumentException("Execute items should only have one item.");
			}

			if (index > m_items.Count)
			{
				throw new IndexOutOfRangeException();
			}

			m_items.Insert((int)index, value);
			m_vectorChangedEventSource?.Invoke(this, null);
		}

		public void RemoveAt(uint index)
		{
			if (index >= m_items.Count)
			{
				throw new IndexOutOfRangeException();
			}

			m_items.RemoveAt((int)index);
			m_vectorChangedEventSource?.Invoke(this, null);
		}

		public void Append(SwipeItem value)
		{
			if (Mode == SwipeMode.Execute && m_items.Count > 0)
			{
				throw new ArgumentException("Execute items should only have one item.");
			}

			m_items.Add(value);
			m_vectorChangedEventSource?.Invoke(this, null);
		}

		public void RemoveAtEnd()
		{
			m_items.RemoveAt(m_items.Count - 1);
			m_vectorChangedEventSource?.Invoke(this, null);
		}

		public void Clear()
		{
			m_items.Clear();
			m_vectorChangedEventSource?.Invoke(this, null);
		}

		// TODO Uno
		//public IVectorView<SwipeItem> GetView()
		//{
		//	return m_items.GetView();
		//}

		public event VectorChangedEventHandler<SwipeItem> VectorChanged
		{
			add => m_vectorChangedEventSource += value;
			remove => m_vectorChangedEventSource -= value;
		}
	}
}
