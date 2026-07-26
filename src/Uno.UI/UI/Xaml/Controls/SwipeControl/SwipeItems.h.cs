// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeItem.h

using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SwipeItems
	{
		//public:
		//SwipeItems();

		#region IVector
		//winrt.SwipeItem GetAt(uint index);
		//uint Size();
		//winrt.IVectorView<winrt.SwipeItem> GetView();
		//bool IndexOf(winrt.SwipeItem & value, uint32_t& index);
		//void SetAt(uint index, winrt.SwipeItem & value);
		//void InsertAt(uint index, winrt.SwipeItem & value);
		//void RemoveAt(uint index);
		//void Append(winrt.SwipeItem & value);
		//void RemoveAtEnd();
		//void Clear();

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This method is not part of WinUI's projected API. Use GetEnumerator instead.")]
		public SwipeItem First() => GetAt(0);

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This method is not part of WinUI's projected API. Use CopyTo instead.")]
		public uint GetMany(uint startIndex, SwipeItem[] items)
		{
			ArgumentNullException.ThrowIfNull(items);

			if (startIndex > m_items.Count)
			{
				throw new IndexOutOfRangeException();
			}

			var count = Math.Min(items.Length, m_items.Count - (int)startIndex);
			for (var i = 0; i < count; i++)
			{
				items[i] = m_items[(int)startIndex + i];
			}

			return (uint)count;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This method is not part of WinUI's projected API. Use IList members instead.")]
		public void ReplaceAll(SwipeItem[] items)
		{
			ArgumentNullException.ThrowIfNull(items);
			Items = new ObservableCollection<SwipeItem>(items);
		}
		#endregion

		#region IObservableVector
		//winrt.event_token VectorChanged(winrt.VectorChangedEventHandler<winrt.SwipeItem> & handler);
		//void VectorChanged(winrt.event_token  token);
		#endregion

		//void OnPropertyChanged( winrt.DependencyPropertyChangedEventArgs& args);

		//private:
		//void put_Items( winrt.Collections.IVector<winrt.SwipeItem>& value);
		private ObservableCollection<SwipeItem> m_items;

		private event VectorChangedEventHandler<SwipeItem> m_vectorChangedEventSource;
	}
}
