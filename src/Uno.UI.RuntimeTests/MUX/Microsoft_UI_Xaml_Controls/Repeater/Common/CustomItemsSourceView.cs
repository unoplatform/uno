// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using IKeyIndexMapping = Microsoft/* UWP don't rename */.UI.Xaml.Controls.IKeyIndexMapping;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class CustomItemsSourceView : IList, INotifyCollectionChanged
	{
		#region IList

		public int Count
		{
			get
			{
				return GetSizeCore();
			}
		}

		public object this[int index]
		{
			get { return GetAtCore(index); }
			set { throw new NotImplementedException(); }
		}


		public bool IsFixedSize
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsReadOnly
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsSynchronized
		{
			get { throw new NotImplementedException(); }
		}

		public object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		public int Add(object value)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(object value)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public IEnumerator GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public int IndexOf(object value)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, object value)
		{
			throw new NotImplementedException();
		}

		public void Remove(object value)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region INotifyCollectionChanged

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion

		protected virtual int GetSizeCore()
		{
			throw new NotImplementedException();
		}

		protected virtual object GetAtCore(int index)
		{
			throw new NotImplementedException();
		}

		protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
		{
			CollectionChanged?.Invoke(this, args);
		}
	}

	public class CustomItemsSourceViewWithUniqueIdMapping : CustomItemsSourceView, IKeyIndexMapping
	{
		#region IKeyIndexMapping

		public string KeyFromIndex(int index)
		{
			return KeyFromIndexCore(index);
		}

		public int IndexFromKey(string id)
		{
			return IndexFromKey(id);
		}

		#endregion

		protected virtual string KeyFromIndexCore(int index)
		{
			throw new NotImplementedException();
		}

		protected virtual int IndexFromKeyCore(string id)
		{
			throw new NotImplementedException();
		}
	}
}
