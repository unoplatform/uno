// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TopNavigationViewDataProvider.cpp, commit de78834

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

internal partial class TopNavigationViewDataProvider : SplitDataSourceBase<object, NavigationViewSplitVectorID, double>
{
	internal TopNavigationViewDataProvider(object m_owner) : base(5)
	{
		m_rawDataSource = m_owner;
		m_dataSource = m_owner as ItemsSourceView;

		Func<object, int> lambda = (object value) => IndexOf(value);

		// TODO: Does it need m_owner?
		var primaryVector = new SplitVector<object, NavigationViewSplitVectorID>(NavigationViewSplitVectorID.PrimaryList, lambda);
		var overflowVector = new SplitVector<object, NavigationViewSplitVectorID>(NavigationViewSplitVectorID.OverflowList, lambda);

		InitializeSplitVectors(primaryVector, overflowVector);
	}

	internal IList<object> GetPrimaryItems()
	{
		return GetVector(NavigationViewSplitVectorID.PrimaryList).GetVector();
	}

	internal IList<object> GetOverflowItems()
	{
		return GetVector(NavigationViewSplitVectorID.OverflowList).GetVector();
	}

	// The raw data is from MenuItems or MenuItemsSource
	internal void SetDataSource(object rawData)
	{
		if (ShouldChangeDataSource(rawData)) // avoid to create multiple of datasource for the same raw data
		{
			ItemsSourceView dataSource = null;
			if (rawData != null)
			{
				dataSource = new InspectingDataSource(rawData);
			}
			ChangeDataSource(dataSource);
			m_rawDataSource = rawData;
			if (dataSource != null)
			{
				MoveAllItemsToPrimaryList();
			}
		}
	}

	private bool ShouldChangeDataSource(object rawData)
	{
		return rawData != m_rawDataSource;
	}

	internal void OnRawDataChanged(Action<NotifyCollectionChangedEventArgs> dataChangeCallback)
	{
		m_dataChangeCallback = dataChangeCallback;
	}

	public override int IndexOf(object value)
	{
		var dataSource = m_dataSource;
		if (dataSource != null)
		{
			return dataSource.IndexOf(value);
		}
		return -1;
	}

	public override object GetAt(int index)
	{
		var dataSource = m_dataSource;
		if (dataSource != null)
		{
			return dataSource.GetAt(index);
		}
		return null;
	}

	public override int Size()
	{
		var dataSource = m_dataSource;
		if (dataSource != null)
		{
			return (int)(dataSource.Count);
		}
		return 0;
	}

	protected override NavigationViewSplitVectorID DefaultVectorIDOnInsert()
	{
		return NavigationViewSplitVectorID.NotInitialized;
	}

	protected override double DefaultAttachedData()
	{
		return double.MinValue;
	}

	internal void MoveAllItemsToPrimaryList()
	{
		for (int i = 0; i < Size(); i++)
		{
			MoveItemToVector(i, NavigationViewSplitVectorID.PrimaryList);
		}
	}

	internal IList<int> ConvertPrimaryIndexToIndex(IList<int> indexesInPrimary)
	{
		var indexes = new List<int>();
		if (indexesInPrimary.Count > 0)
		{
			var vector = GetVector(NavigationViewSplitVectorID.PrimaryList);
			if (vector != null)
			{
				// transform PrimaryList index to OrignalVector index
				foreach (var index in indexesInPrimary)
				{
					var transformed = vector.IndexToIndexInOriginalVector(index);
					indexes.Add(transformed);
				}
			}
		}
		return indexes;
	}

	internal int ConvertOriginalIndexToIndex(int originalIndex)
	{
		var vector = GetVector(IsItemInPrimaryList(originalIndex) ? NavigationViewSplitVectorID.PrimaryList : NavigationViewSplitVectorID.OverflowList);
		return vector.IndexFromIndexInOriginalVector(originalIndex);
	}

	internal void MoveItemsOutOfPrimaryList(IList<int> indexes)
	{
		MoveItemsToList(indexes, NavigationViewSplitVectorID.OverflowList);
	}

	internal void MoveItemsToPrimaryList(IList<int> indexes)
	{
		MoveItemsToList(indexes, NavigationViewSplitVectorID.PrimaryList);
	}

	private void MoveItemsToList(IList<int> indexes, NavigationViewSplitVectorID vectorID)
	{
		foreach (var index in indexes)
		{
			MoveItemToVector(index, vectorID);
		}
	}

	internal int GetPrimaryListSize()
	{
		return GetPrimaryItems().Count;
	}

	internal int GetNavigationViewItemCountInPrimaryList()
	{
		int count = 0;
		for (int i = 0; i < Size(); i++)
		{
			if (IsItemInPrimaryList(i) && IsContainerNavigationViewItem(i))
			{
				count++;
			}
		}
		return count;
	}

	internal int GetNavigationViewItemCountInTopNav()
	{
		int count = 0;
		for (int i = 0; i < Size(); i++)
		{
			if (IsContainerNavigationViewItem(i))
			{
				count++;
			}
		}
		return count;
	}

	internal void UpdateWidthForPrimaryItem(int indexInPrimary, double width)
	{
		var vector = GetVector(NavigationViewSplitVectorID.PrimaryList);
		if (vector != null)
		{
			var index = vector.IndexToIndexInOriginalVector(indexInPrimary);
			SetWidthForItem(index, width);
		}
	}

	internal double WidthRequiredToRecoveryAllItemsToPrimary()
	{
		var width = 0.0;
		for (int i = 0; i < Size(); i++)
		{
			if (!IsItemInPrimaryList(i))
			{
				width += GetWidthForItem(i);
			}
		}
		width -= m_overflowButtonCachedWidth;
		return Math.Max(0.0, width);
	}

	internal bool HasInvalidWidth(IList<int> items)
	{
		bool hasInvalidWidth = false;
		foreach (var index in items)
		{
			if (!IsValidWidthForItem(index))
			{
				hasInvalidWidth = true;
				break;
			}
		}
		return hasInvalidWidth;
	}

	internal double GetWidthForItem(int index)
	{
		var width = AttachedData(index);
		if (!IsValidWidth(width))
		{
			width = 0;
		}
		return width;
	}

	internal double CalculateWidthForItems(IList<int> items)
	{
		double width = 0.0;
		foreach (var index in items)
		{
			width += GetWidthForItem(index);
		}
		return width;
	}

	internal void InvalidWidthCache()
	{
		ResetAttachedData(-1.0f);
	}

	internal double OverflowButtonWidth
	{
		get => m_overflowButtonCachedWidth;
		set => m_overflowButtonCachedWidth = value;
	}

#if false
	private bool IsItemSelectableInPrimaryList(object value)
	{
		int index = IndexOf(value);
		return (index != -1);
	}
#endif

	internal int IndexOf(object value, NavigationViewSplitVectorID vectorID)
	{
		return IndexOfImpl(value, vectorID);
	}

	private void OnDataSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				{
					OnInsertAt(args.NewStartingIndex, args.NewItems.Count);
					break;
				}

			case NotifyCollectionChangedAction.Remove:
				{
					OnRemoveAt(args.OldStartingIndex, args.OldItems.Count);
					break;
				}

			case NotifyCollectionChangedAction.Reset:
				{
					OnClear();
					break;
				}

			case NotifyCollectionChangedAction.Replace:
				{
					OnRemoveAt(args.OldStartingIndex, args.OldItems.Count);
					OnInsertAt(args.NewStartingIndex, args.NewItems.Count);
					break;
				}
		}

		m_dataChangeCallback?.Invoke(args);
	}

	private bool IsValidWidth(double width)
	{
		return (width >= 0) && (width < double.MaxValue);
	}

	internal bool IsValidWidthForItem(int index)
	{
		var width = AttachedData(index);
		return IsValidWidth(width);
	}

	private void SetWidthForItem(int index, double width)
	{
		if (IsValidWidth(width))
		{
			AttachedData(index, width);
		}
	}

	private void ChangeDataSource(ItemsSourceView newValue)
	{
		var oldValue = m_dataSource;
		if (oldValue != newValue)
		{
			// update to the new datasource.

			if (oldValue != null)
			{
				oldValue.CollectionChanged -= OnDataSourceChanged;
			}

			Clear();

			m_dataSource = newValue;
			SyncAndInitVectorFlagsWithID(NavigationViewSplitVectorID.NotInitialized, DefaultAttachedData());

			if (newValue != null)
			{
				newValue.CollectionChanged += OnDataSourceChanged;
			}
		}

		// Move all to primary list
		MoveItemsToVector(NavigationViewSplitVectorID.NotInitialized);
	}

	internal bool IsItemInPrimaryList(int index)
	{
		return GetVectorIDForItem(index) == NavigationViewSplitVectorID.PrimaryList;
	}

	private bool IsContainerNavigationViewItem(int index)
	{
		bool isContainerNavigationViewItem = true;

		var item = GetAt(index);
		if (item is NavigationViewItemHeader || item is NavigationViewItemSeparator)
		{
			isContainerNavigationViewItem = false;
		}
		return isContainerNavigationViewItem;
	}

#if false
	private bool IsContainerNavigationViewHeader(int index)
	{
		bool isContainerNavigationViewHeader = false;

		var item = GetAt(index);
		if (item is NavigationViewItemHeader)
		{
			isContainerNavigationViewHeader = true;
		}
		return isContainerNavigationViewHeader;
	}
#endif
}
