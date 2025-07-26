// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InspectingDataSource.cpp, commit 37ade09; ItemsSourceView.cpp, commit dc8d573

using System;
using System.Collections.Specialized;

namespace Microsoft.UI.Xaml.Controls;

// In the source C++ code, this class is implemented in InspectingDataSource
// and its constructor is then set to return InspectingDataSource instead.
// To achieve similar behavior in C#, we need to implement this class here
// and make InspectingDataSource just a thin wrapper around this class.
// The actual implementation of most of the logic is in ItemsSourceView.Impl.cs

/// <summary>
/// Represents a standardized view of the supported interactions between a given ItemsSource object and an ItemsRepeater control.
/// </summary>
public partial class ItemsSourceView : INotifyCollectionChanged
{
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	private int m_cachedSize = -1;

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

	public int IndexFromKey(string key)
		=> IndexFromKeyCore(key);

	public string KeyFromIndex(int index)
		=> KeyFromIndexCore(index);

	internal int IndexOf(object item)
		=> IndexOfCore(item);
	#endregion

	#region IDataSourceProtected

	private protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
	{
		m_cachedSize = GetSizeCore();
		CollectionChanged?.Invoke(this, args);
	}
	#endregion
}
