// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Specialized;

namespace Microsoft.UI.Xaml.Controls
{
	public class ItemsSourceView : INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private int m_cachedSize  =  -1;

		#region IDataSource

		public int Count
		{
			get
			{
				if (m_cachedSize == -1)
				{
					// Call the override the very first time. After this,
					// we can just update the size when there is a data source change.
					m_cachedSize = GetSizeCore();
				}

				return m_cachedSize;
			}
		}

		public bool HasKeyIndexMapping => HasKeyIndexMappingCore();

		public object GetAt(int index)
			=> GetAtCore(index);

		public int IndexFromKey(string id)
			=> IndexFromKeyCore(id);

		public string KeyFromIndex(int index)
			=> KeyFromIndexCore(index);

		private int IndexOf(object value)
			=> IndexOfCore(value);
		#endregion

		#region IDataSourceProtected

		private protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
		{
			m_cachedSize = GetSizeCore();
			CollectionChanged?.Invoke(this, args);
		}
		#endregion

		#region IDataSourceOverrides

		private protected virtual int GetSizeCore()
			=> throw new NotImplementedException();

		private protected virtual object GetAtCore(int index)
			=> throw new NotImplementedException();

		private protected virtual bool HasKeyIndexMappingCore()
			=> throw new NotImplementedException();

		private protected virtual string KeyFromIndexCore(int index)
			=> throw new NotImplementedException();

		private protected virtual int IndexFromKeyCore(string id)
			=> throw new NotImplementedException();
		#endregion

		private protected virtual int IndexOfCore(object value)
			=> throw new NotImplementedException();
	}
}
