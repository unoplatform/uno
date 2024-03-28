// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewGeneratorHost : IVector<object>
	{

		// DataVirtualization - to avoid creating large number of calendar items,
		// we only implement GetAt and get_Size.

		// IVector<object> implementation
		public object GetAt(uint index)
		{
			DateTime date;

			date = GetDateAt(index);

			var item = PropertyValue.CreateDateTime(date);

			return item;
		}

		public uint Size()
		{
			var value = m_size;
			return value;
		}

		public IVectorView<object> GetView()
		{
			IVectorView<object> spResult;

			CoreDispatcher.CheckThreadAccess(); // CheckThread();

			spResult = new TrackerView<object>();
			(spResult as TrackerView<object>).SetCollection(this);

			return spResult;
		}

		public void IndexOf(object value, out uint index, out bool found)
		{
			throw new NotImplementedException();
		}

		public void SetAt(uint index, object item)
		{
			throw new NotImplementedException();
		}

		public void InsertAt(uint index, object item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(uint index)
		{
			throw new NotImplementedException();
		}

		public void Append(object item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAtEnd()
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		// BEGIN UNO Specific

		public bool IsReadOnly { get; }
		public int Count => (int)Size();
		public object this[int index]
		{
			get => (object)GetAt((uint)index);
			set => SetAt((uint)index, value);
		}

		public void Add(object item) => Append(item);
		public void Insert(int index, object item) => InsertAt((uint)index, item);
		public bool Contains(object item) => IndexOf(item, out _);
		public int IndexOf(object item) => IndexOf(item, out uint index) ? (int)index : -1;
		public void CopyTo(object[] array, int arrayIndex) => throw new NotSupportedException();
		public void RemoveAt(int index) => RemoveAt((uint)index);
		public bool Remove(object item)
		{
			if (IndexOf(item, out var index))
			{
				RemoveAt(index); // Make sure to use the RemoveAt which will raise the change event
				return true;
			}
			else
			{
				return false;
			}
		}

		public IEnumerator<object> GetEnumerator() => throw new NotSupportedException();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private bool IndexOf(object item, out uint index)
		{
			IndexOf(item, out index, out var found);
			return found;
		}

		// END UNO Specific
	}
}
