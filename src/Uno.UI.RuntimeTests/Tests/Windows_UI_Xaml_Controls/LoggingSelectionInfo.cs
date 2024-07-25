using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;
using System.Threading.Tasks;

public class LoggingSelectionInfo : ISelectionInfo, ICollectionView
{
	public List<string> MethodLog { get; } = new List<string>();

	public void SelectRange(ItemIndexRange itemIndexRange)
	{
		MethodLog.Add($"SelectRange({itemIndexRange.FirstIndex}, {itemIndexRange.Length})");
	}

	public void DeselectRange(ItemIndexRange itemIndexRange)
	{
		MethodLog.Add($"DeselectRange({itemIndexRange.FirstIndex}, {itemIndexRange.Length})");
	}

	public bool IsSelected(int index)
	{
		MethodLog.Add($"IsSelected({index})");
		return false;
	}

	public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
	{
		MethodLog.Add("GetSelectedRanges()");
		return new List<ItemIndexRange>();
	}

	public event CurrentChangingEventHandler CurrentChanging;
	public event CurrentChangedEventHandler CurrentChanged;

	private object _currentItem;
	public object CurrentItem
	{
		get
		{
			MethodLog.Add("get_CurrentItem()");
			return _currentItem;
		}
		set
		{
			if (_currentItem != value)
			{
				if (CurrentChanging != null)
				{
					var args = new CurrentChangingEventArgs();
					CurrentChanging(this, args);
					if (args.Cancel)
					{
						MethodLog.Add("set_CurrentItem() - Cancelled");
						return;
					}
				}

				_currentItem = value;
				MethodLog.Add($"set_CurrentItem({value})");

				CurrentChanged?.Invoke(this, null);
			}
		}
	}

	private int _currentPosition;
	public int CurrentPosition
	{
		get
		{
			MethodLog.Add("get_CurrentPosition()");
			return _currentPosition;
		}
		set
		{
			if (_currentPosition != value)
			{
				_currentPosition = value;
				MethodLog.Add($"set_CurrentPosition({value})");
			}
		}
	}

	public bool HasMoreItems
	{
		get
		{
			MethodLog.Add("get_HasMoreItems()");
			return false;
		}
	}

	public bool IsCurrentAfterLast
	{
		get
		{
			MethodLog.Add("get_IsCurrentAfterLast()");
			return false;
		}
	}

	public bool IsCurrentBeforeFirst
	{
		get
		{
			MethodLog.Add("get_IsCurrentBeforeFirst()");
			return false;
		}
	}

	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
	{
		MethodLog.Add($"LoadMoreItemsAsync({count})");
		return Task.FromResult(new LoadMoreItemsResult { Count = 0 }).AsAsyncOperation();
	}

	public void MoveCurrentToPosition(int index)
	{
		MethodLog.Add($"MoveCurrentToPosition({index})");
		CurrentPosition = index;
	}

	public bool MoveCurrentTo(object item)
	{
		MethodLog.Add($"MoveCurrentTo({item})");
		CurrentItem = item;
		return true;
	}

	public bool MoveCurrentToFirst()
	{
		MethodLog.Add("MoveCurrentToFirst()");
		return true;
	}

	public bool MoveCurrentToLast()
	{
		MethodLog.Add("MoveCurrentToLast()");
		return true;
	}

	public bool MoveCurrentToNext()
	{
		MethodLog.Add("MoveCurrentToNext()");
		return true;
	}

	public bool MoveCurrentToPrevious()
	{
		MethodLog.Add("MoveCurrentToPrevious()");
		return true;
	}
}
