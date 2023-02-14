using System;
using System.Collections.Generic;
using System.Text;
using DirectUI;
using DirectUI.Components;

namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Provides info about a range of items in the data source.
/// </summary>
public partial class ItemIndexRange
{
	/// <summary>
	/// Initializes an instance of the ItemIndexRange class.
	/// </summary>
	/// <param name="firstIndex">The index of the first item in the instance of the ItemIndexRange class.</param>
	/// <param name="length">The number of items in the instance of the ItemIndexRange class.</param>
	public ItemIndexRange(int firstIndex, uint length)
	{
		this.FirstIndex = firstIndex;
		this.Length = length;
	}

	/// <summary>
	/// Gets the index of the first item in the instance of the ItemIndexRange class.
	/// </summary>
	public int FirstIndex { get; }

	/// <summary>
	/// Gets the index of the last item in the instance of the ItemIndexRange class.
	/// </summary>
	public int LastIndex => FirstIndex + (int)Length - 1;

	/// <summary>
	/// Gets the number of items in the instance of the ItemIndexRange class.
	/// </summary>
	public uint Length { get; }

	public override bool Equals(object obj)
	{
		return obj is ItemIndexRange other &&
			other.FirstIndex == FirstIndex &&
			other.Length == Length;
	}

	public override int GetHashCode() => (FirstIndex, Length).GetHashCode();

	/// <summary>
	/// goes through the vector and creates ranges from continuous indices
	/// </summary>
	/// <param name="indices"></param>
	/// <returns></returns>
	internal static TrackerCollection<ItemIndexRange> AppendItemIndexRangesFromSortedVectorToItemIndexRangeCollection(IReadOnlyList<int> indices)
	{
		var pCollection = new TrackerCollection<ItemIndexRange>();
		var size = indices.Count;
		var length = 1;

		for (var i = 0; i < size; i += length)
		{
			length = ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, i);

			pCollection.Add(new(indices[i], (uint)length));
		}

		return pCollection;
	}
}
