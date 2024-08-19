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

	private object _currentItem;

	private int _currentPosition;

	public LoggingSelectionInfo(IEnumerable collection, bool isGrouped, PropertyPath itemsPath) : base(collection, isGrouped, itemsPath)
	{
	}

}
#endif
