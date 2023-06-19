// MUX Reference ItemIndexRangeHelper.h

using System.Collections.Generic;

namespace DirectUI.Components;

internal static partial class ItemIndexRangeHelper
{
	internal enum IndexLocation
	{
		Before,
		After,
		Inside
	}

	internal struct Range //TODO:MZ:WinUI uses struct here
	{
		public Range(int firstIndex, uint length)
		{
			FirstIndex = firstIndex;
			Length = length;
		}

		public int FirstIndex { get; }

		public uint Length { get; }

		public int LastIndex => (int)(FirstIndex + Length - 1);
	}

	internal partial class RangeSelection
	{
		private List<Range> m_selectedRanges = new();

		internal int Count => m_selectedRanges.Count;

		internal Range this[int index] => m_selectedRanges[index];
	}
}
