using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;

public class LoggingSelectionInfo : ISelectionInfo
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
}
