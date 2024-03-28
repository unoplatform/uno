// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class WinRTCollection : IObservableVector<object>
	{
		private IList<object> m_data;

		public WinRTCollection(IEnumerable<object> collection)
		{
			m_data = new List<object>(collection);
		}

		public object this[int index]
		{
			get { return m_data[index]; }
			set
			{
				m_data[index] = value;
				if (VectorChanged != null) { VectorChanged(this, new VectorChangedEventArgs(CollectionChange.ItemChanged, (uint)index)); }
			}
		}

		public int Count { get { return m_data.Count; } }
		public bool IsReadOnly { get { return false; } }

		public event VectorChangedEventHandler<object> VectorChanged;

		public void Add(object item)
		{
			m_data.Add(item);
			if (VectorChanged != null) { VectorChanged(this, new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)m_data.Count - 1u)); }
		}

		public void Clear()
		{
			m_data.Clear();
			if (VectorChanged != null) { VectorChanged(this, new VectorChangedEventArgs(CollectionChange.Reset, 0u)); }
		}

		public bool Contains(object item)
		{
			return m_data.Contains(item);
		}

		public void CopyTo(object[] array, int arrayIndex)
		{
			m_data.CopyTo(array, arrayIndex);
		}

		public IEnumerator<object> GetEnumerator()
		{
			return m_data.GetEnumerator();
		}

		public int IndexOf(object item)
		{
			return m_data.IndexOf(item);
		}

		public void Insert(int index, object item)
		{
			m_data.Insert(index, item);
			if (VectorChanged != null) { VectorChanged(this, new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)index)); }
		}

		public bool Remove(object item)
		{
			var index = (uint)m_data.IndexOf(item);
			if (m_data.Remove(item))
			{
				if (VectorChanged != null) { VectorChanged(this, new VectorChangedEventArgs(CollectionChange.ItemRemoved, index)); }
				return true;
			}

			return false;
		}

		public void RemoveAt(int index)
		{
			m_data.RemoveAt(index);
			if (VectorChanged != null) { VectorChanged(this, new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)index)); }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_data.GetEnumerator();
		}

		public void Reset()
		{
			if (VectorChanged != null) { VectorChanged(this, new VectorChangedEventArgs(CollectionChange.Reset, 0)); }
		}

		public void ResetWith(IEnumerable<object> enumerable)
		{
			m_data = new List<object>(enumerable);
			Reset();
		}

		private class VectorChangedEventArgs : IVectorChangedEventArgs
		{
			public CollectionChange CollectionChange { get; private set; }
			public uint Index { get; private set; }

			public VectorChangedEventArgs(CollectionChange change, uint index)
			{
				CollectionChange = change;
				Index = index;
			}
		}
	}
}
