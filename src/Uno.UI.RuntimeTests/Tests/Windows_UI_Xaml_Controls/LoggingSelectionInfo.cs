#if HAS_UNO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation.Collections;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

internal class LoggingSelectionInfo : ICollectionView, ISelectionInfo, IList<object>, INotifyCollectionChanged, INotifyPropertyChanged, IObservableVector<object>
{
	private readonly List<object> _collection = new List<object>();
	private readonly HashSet<int> _selectedIndices = new HashSet<int>();
	private object _currentItem;
	private int _currentPosition = -1;

	public LoggingSelectionInfo(IEnumerable<object> items)
	{
		_collection.AddRange(items);
	}

	#region ICollectionView Implementation

	public event CurrentChangingEventHandler CurrentChanging;
	public event EventHandler<object> CurrentChanged;

	public object CurrentItem => _currentItem;

	public int CurrentPosition => _currentPosition;

	public bool IsCurrentAfterLast => _currentPosition >= _collection.Count;

	public bool IsCurrentBeforeFirst => _currentPosition < 0;

	public bool MoveCurrentTo(object item)
	{
		int index = _collection.IndexOf(item);
		return MoveCurrentToPosition(index);
	}

	public bool MoveCurrentToPosition(int index)
	{
		if (index >= 0 && index < _collection.Count)
		{
			if (_currentPosition != index)
			{
				OnCurrentChanging();
				_currentItem = _collection[index];
				_currentPosition = index;
				OnCurrentChanged();
			}
			return true;
		}
		return false;
	}

	public bool MoveCurrentToFirst() => MoveCurrentToPosition(0);

	public bool MoveCurrentToLast() => MoveCurrentToPosition(_collection.Count - 1);

	public bool MoveCurrentToNext() => MoveCurrentToPosition(_currentPosition + 1);

	public bool MoveCurrentToPrevious() => MoveCurrentToPosition(_currentPosition - 1);

	public Predicate<object> Filter { get; set; }

	public bool CanFilter => Filter != null;

	public bool Contains(object item) => _collection.Contains(item);

	public IComparer<object> SortDescriptions { get; set; }

	public bool CanSort => SortDescriptions != null;

	public IObservableVector<object> CollectionGroups { get; } = null;

	public bool CanGroup => false;

	public object GetItemAt(int index) => _collection[index];

	public int IndexOf(object item) => _collection.IndexOf(item);

	public bool HasMoreItems => false;

	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
	{
		throw new NotImplementedException("LoadMoreItemsAsync is not implemented.");
	}

	protected virtual void OnCurrentChanged()
	{
		CurrentChanged?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnCurrentChanging()
	{
		CurrentChanging?.Invoke(this, new CurrentChangingEventArgs(false));
	}

	#endregion

	#region ISelectionInfo Implementation

	public void SelectRange(ItemIndexRange itemIndexRange)
	{
		for (int i = itemIndexRange.FirstIndex; i < itemIndexRange.FirstIndex + itemIndexRange.Length; i++)
		{
			if (i >= 0 && i < _collection.Count)
			{
				_selectedIndices.Add(i);
			}
		}
		OnSelectionChanged();
	}

	public void DeselectRange(ItemIndexRange itemIndexRange)
	{
		for (int i = itemIndexRange.FirstIndex; i < itemIndexRange.FirstIndex + itemIndexRange.Length; i++)
		{
			_selectedIndices.Remove(i);
		}
		OnSelectionChanged();
	}

	public bool IsSelected(int index)
	{
		return _selectedIndices.Contains(index);
	}

	public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
	{
		if (_selectedIndices.Count == 0)
		{
			return new List<ItemIndexRange>();
		}

		List<ItemIndexRange> ranges = new List<ItemIndexRange>();
		List<int> sortedIndices = new List<int>(_selectedIndices);
		sortedIndices.Sort();

		int rangeStart = sortedIndices[0];
		int rangeLength = 1;

		for (int i = 1; i < sortedIndices.Count; i++)
		{
			if (sortedIndices[i] == sortedIndices[i - 1] + 1)
			{
				rangeLength++;
			}
			else
			{
				ranges.Add(new ItemIndexRange(rangeStart, (uint)rangeLength));
				rangeStart = sortedIndices[i];
				rangeLength = 1;
			}
		}

		ranges.Add(new ItemIndexRange(rangeStart, (uint)rangeLength));
		return ranges;
	}

	protected virtual void OnSelectionChanged()
	{
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	#endregion

	#region IList<object> and ICollection<object> Implementation

	public int Count => _collection.Count;

	public bool IsReadOnly => false;

	public object this[int index]
	{
		get => _collection[index];
		set
		{
			if (_collection[index] != value)
			{
				_collection[index] = value;
				OnPropertyChanged($"Item[{index}]");
			}
		}
	}

	public void Add(object item)
	{
		_collection.Add(item);
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
	}

	public void Insert(int index, object item)
	{
		_collection.Insert(index, item);
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
	}

	public void RemoveAt(int index)
	{
		var removedItem = _collection[index];
		_collection.RemoveAt(index);
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
	}

	public void Clear()
	{
		_collection.Clear();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	public void CopyTo(object[] array, int arrayIndex)
	{
		_collection.CopyTo(array, arrayIndex);
	}

	public bool Remove(object item)
	{
		var index = _collection.IndexOf(item);
		if (index >= 0)
		{
			_collection.RemoveAt(index);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
			return true;
		}
		return false;
	}

	#endregion

	#region IEnumerable Implementation

	public IEnumerator<object> GetEnumerator() => _collection.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

	#endregion

	#region INotifyCollectionChanged Implementation

	public event NotifyCollectionChangedEventHandler CollectionChanged;

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
	}

	#endregion

	#region INotifyPropertyChanged Implementation

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region IObservableVector<object> Implementation

	public event VectorChangedEventHandler<object> VectorChanged;

	protected virtual void OnVectorChanged(CollectionChange change, int index)
	{
		VectorChanged?.Invoke(this, new VectorChangedEventArgs(change, (uint)index));
	}

	#endregion
}
#endif
