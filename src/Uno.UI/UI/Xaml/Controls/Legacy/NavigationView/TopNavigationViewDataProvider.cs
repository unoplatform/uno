using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.UI.Helpers.WinUI;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls
{
	enum NavigationViewSplitVectorID
	{
		NotInitialized = 0,
		PrimaryList = 1,
		OverflowList = 2,
		SkippedList = 3,
		Size = 4
	};


	internal class TopNavigationViewDataProvider : SplitDataSourceBase<object, NavigationViewSplitVectorID, double>
	{
		private SerialDisposable m_dataSourceChanged = new SerialDisposable();
		private Action<NotifyCollectionChangedEventArgs> m_dataChangeCallback;

		private IEnumerable m_dataSource;

		// If the raw datasource is the same, we don't need to create new winrt::ItemsSourceView object.
		private object m_rawDataSource;

		// Event tokens
		// winrt::event_token m_dataSourceChanged { };
		// std::function<void(const winrt::NotifyCollectionChangedEventArgs& args)> m_dataChangeCallback;
		private double m_overflowButtonCachedWidth;

		public TopNavigationViewDataProvider(NavigationView owner)
			: base(5)
		{
			Func<object, int> lambda = (object value) =>
			{
				return IndexOf(value);
			};

			var primaryVector = new SplitVector<object, NavigationViewSplitVectorID>(NavigationViewSplitVectorID.PrimaryList, lambda);
			var overflowVector = new SplitVector<object, NavigationViewSplitVectorID>(NavigationViewSplitVectorID.OverflowList, lambda);

			InitializeSplitVectors(primaryVector, overflowVector);
		}

		public IList<object> GetPrimaryItems()
		{
			return GetVector(NavigationViewSplitVectorID.PrimaryList).GetVector();
		}

		public IList<object> GetOverflowItems()
		{
			return GetVector(NavigationViewSplitVectorID.OverflowList).GetVector();
		}

		// The raw data is from MenuItems or MenuItemsSource
		public void SetDataSource(IEnumerable rawData)
		{
			if (ShouldChangeDataSource(rawData)) // avoid to create multiple of datasource for the same raw data
			{
				IEnumerable dataSource = null;
				if (rawData != null)
				{
					dataSource = rawData;
				}

				ChangeDataSource(dataSource);
				m_rawDataSource = rawData;
				if (dataSource != null)
				{
					MoveAllItemsToPrimaryList();
				}
			}
		}

		public bool ShouldChangeDataSource(IEnumerable rawData)
		{
			return rawData != m_rawDataSource;
		}

		public void OnRawDataChanged(Action<NotifyCollectionChangedEventArgs> dataChangeCallback)
		{
			m_dataChangeCallback = dataChangeCallback;
		}

		public override int IndexOf(object value)
		{
			var dataSource = m_dataSource;
			if (dataSource != null)
			{
				var inspectingDataSource = dataSource;

				return inspectingDataSource.IndexOf(value);
			}
			return -1;
		}

		public override object GetAt(int index)
		{
			var dataSource = m_dataSource;
			if (dataSource != null)
			{
				return dataSource.ElementAt(index);
			}
			return null;
		}

		public override int Size()
		{
			var dataSource = m_dataSource;
			if (dataSource != null)
			{
				return dataSource.Count();
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

		public void MoveAllItemsToPrimaryList()
		{
			for (int i = 0; i < Size(); i++)
			{
				MoveItemToVector(i, NavigationViewSplitVectorID.PrimaryList);
			}
		}

		public List<int> ConvertPrimaryIndexToIndex(List<int> indexesInPrimary)
		{
			List<int> indexes = new List<int>();
			if (!indexesInPrimary.Empty())
			{
				var vector = GetVector(NavigationViewSplitVectorID.PrimaryList);
				if (vector != null)
				{
					// transform NavigationViewSplitVectorID.PrimaryList index to OrignalVector index
					indexes.AddRange(
						indexesInPrimary.Select(
							index => vector.IndexToIndexInOriginalVector(index)
						)
					);
				}
			}
			return indexes;
		}

		public void MoveItemsOutOfPrimaryList(List<int> indexes)
		{
			MoveItemsToList(indexes, NavigationViewSplitVectorID.OverflowList);
		}

		public void MoveItemsToPrimaryList(List<int> indexes)
		{
			MoveItemsToList(indexes, NavigationViewSplitVectorID.PrimaryList);
		}

		public void MoveItemsToList(List<int> indexes, NavigationViewSplitVectorID vectorID)
		{
			foreach (var index in indexes)
			{
				MoveItemToVector(index, vectorID);
			}
		}

		public int GetPrimaryListSize()
		{
			return GetPrimaryItems().Count;
		}

		public int GetNavigationViewItemCountInPrimaryList()
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

		public int GetNavigationViewItemCountInTopNav()
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

		public void UpdateWidthForPrimaryItem(int indexInPrimary, double width)
		{
			var vector = GetVector(NavigationViewSplitVectorID.PrimaryList);
			if (vector != null)
			{
				var index = vector.IndexToIndexInOriginalVector(indexInPrimary);
				SetWidthForItem(index, width);
			}
		}

		public double WidthRequiredToRecoveryAllItemsToPrimary()
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

		public bool HasInvalidWidth(List<int> items)
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

		public double GetWidthForItem(int index)
		{
			var width = AttachedData(index);
			if (!IsValidWidth(width))
			{
				width = 0;
			}
			return width;
		}

		public double CalculateWidthForItems(List<int> items)
		{
			double width = 0.0;
			foreach (var index in items)
			{
				width += GetWidthForItem(index);
			}
			return width;
		}

		void InvalidWidthCache()
		{
			ResetAttachedData(-1.0f);
		}

		public double OverflowButtonWidth()
		{
			return m_overflowButtonCachedWidth;
		}

		public void OverflowButtonWidth(double width)
		{
			m_overflowButtonCachedWidth = width;
		}

#if false
		bool IsItemSelectableInPrimaryList(object value)
		{
			int index = IndexOf(value);
			return (index != -1);
		}
#endif

		public int IndexOf(object value, NavigationViewSplitVectorID vectorID)
		{
			return IndexOfImpl(value, vectorID);
		}

		void OnDataSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
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
			if (m_dataChangeCallback != null)
			{
				m_dataChangeCallback(args);
			}
		}

		bool IsValidWidth(double width)
		{
			return (width >= 0) && (width < double.MaxValue);
		}

		public bool IsValidWidthForItem(int index)
		{
			var width = AttachedData(index);
			return IsValidWidth(width);
		}

		public void InvalidWidthCacheIfOverflowItemContentChanged()
		{
			bool shouldRefreshCache = false;
			for (int i = 0; i < Size(); i++)
			{
				if (!IsItemInPrimaryList(i))
				{
					if (GetAt(i) is NavigationViewItem navItem)
					{
						var itemPointer = navItem;
						if (itemPointer.IsContentChangeHandlingDelayedForTopNav())
						{
							itemPointer.ClearIsContentChangeHandlingDelayedForTopNavFlag();
							shouldRefreshCache = true;
						}
					}
				}
			}

			if (shouldRefreshCache)
			{
				InvalidWidthCache();
			}
		}

		void SetWidthForItem(int index, double width)
		{
			if (IsValidWidth(width))
			{
				AttachedData(index, width);
			}
		}

		void ChangeDataSource(IEnumerable newValue)
		{
			var oldValue = m_dataSource;
			if (oldValue != newValue)
			{
				// update to the new datasource.

				if (oldValue is INotifyCollectionChanged oldIncc)
				{
					m_dataSourceChanged.Disposable = null;
				}

				Clear();

				m_dataSource = newValue;
				SyncAndInitVectorFlagsWithID(NavigationViewSplitVectorID.NotInitialized, DefaultAttachedData());

				if (newValue is INotifyCollectionChanged newIncc)
				{
					newIncc.CollectionChanged += OnDataSourceChanged;
					m_dataSourceChanged.Disposable = Disposable.Create(() => newIncc.CollectionChanged -= OnDataSourceChanged);
				}
			}

			// Move all to primary list
			MoveItemsToVector(NavigationViewSplitVectorID.NotInitialized);
		}

		public bool IsItemInPrimaryList(int index)
		{
			return GetVectorIDForItem(index) == NavigationViewSplitVectorID.PrimaryList;
		}

		bool IsContainerNavigationViewItem(int index)
		{
			bool isContainerNavigationViewItem = true;

			var item = GetAt(index);
			if (item != null && (item is NavigationViewItemHeader || item is NavigationViewItemSeparator))
			{
				isContainerNavigationViewItem = false;
			}
			return isContainerNavigationViewItem;
		}

#if false
		bool IsContainerNavigationViewHeader(int index)
		{
			bool isContainerNavigationViewHeader = false;

			var item = GetAt(index);
			if (item != null && item is NavigationViewItemHeader)
			{
				isContainerNavigationViewHeader = true;
			}
			return isContainerNavigationViewHeader;
		}
#endif
	}
}
