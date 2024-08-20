#if HAS_UNO
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;
using System.Threading.Tasks;
using System;
using System.Collections;
using Microsoft.UI.Xaml;

internal class LoggingSelectionInfo : CollectionView, ISelectionInfo
{
	public List<string> MethodLog { get; } = new List<string>();

	public LoggingSelectionInfo(IEnumerable collection, bool isGrouped, PropertyPath itemsPath)
		: base(collection, isGrouped, itemsPath)
	{
	}

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

	public void LogAndMoveCurrentToPosition(int index)
	{
		MethodLog.Add($"MoveCurrentToPosition({index})");
		base.MoveCurrentToPosition(index);
	}

	public void LogAndMoveCurrentTo(object item)
	{
		MethodLog.Add($"MoveCurrentTo({item})");
		base.MoveCurrentTo(item);
	}

	public void LogAndMoveCurrentToFirst()
	{
		MethodLog.Add("MoveCurrentToFirst()");
		base.MoveCurrentToFirst();
	}

	public void LogAndMoveCurrentToLast()
	{
		MethodLog.Add("MoveCurrentToLast()");
		base.MoveCurrentToLast();
	}

	public void LogAndMoveCurrentToNext()
	{
		MethodLog.Add("MoveCurrentToNext()");
		base.MoveCurrentToNext();
	}

	public void LogAndMoveCurrentToPrevious()
	{
		MethodLog.Add("MoveCurrentToPrevious()");
		base.MoveCurrentToPrevious();
	}

	private bool IsUsingISelectionInfo => true;

	public void SafeMoveCurrentTo(object item)
	{
		if (IsUsingISelectionInfo)
		{
			MethodLog.Add("MoveCurrentTo() - Skipped due to ISelectionInfo usage");
		}
		else
		{
			LogAndMoveCurrentTo(item);
		}
	}

	public void SafeMoveCurrentToPosition(int index)
	{
		if (IsUsingISelectionInfo)
		{
			MethodLog.Add("MoveCurrentToPosition() - Skipped due to ISelectionInfo usage");
		}
		else
		{
			LogAndMoveCurrentToPosition(index);
		}
	}

	public void SafeMoveCurrentToFirst()
	{
		if (IsUsingISelectionInfo)
		{
			MethodLog.Add("MoveCurrentToFirst() - Skipped due to ISelectionInfo usage");
		}
		else
		{
			LogAndMoveCurrentToFirst();
		}
	}

	public void SafeMoveCurrentToLast()
	{
		if (IsUsingISelectionInfo)
		{
			MethodLog.Add("MoveCurrentToLast() - Skipped due to ISelectionInfo usage");
		}
		else
		{
			LogAndMoveCurrentToLast();
		}
	}

	public void SafeMoveCurrentToNext()
	{
		if (IsUsingISelectionInfo)
		{
			MethodLog.Add("MoveCurrentToNext() - Skipped due to ISelectionInfo usage");
		}
		else
		{
			LogAndMoveCurrentToNext();
		}
	}

	public void SafeMoveCurrentToPrevious()
	{
		if (IsUsingISelectionInfo)
		{
			MethodLog.Add("MoveCurrentToPrevious() - Skipped due to ISelectionInfo usage");
		}
		else
		{
			LogAndMoveCurrentToPrevious();
		}
	}
}
#endif
