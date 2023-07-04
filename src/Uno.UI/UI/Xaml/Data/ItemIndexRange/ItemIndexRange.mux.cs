using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI.Components;

namespace Windows.UI.Xaml.Data;

/// <summary>
/// Provides info about a range of items in the data source.
/// </summary>
public partial class ItemIndexRange
{
	private int GetLastIndexImpl()
	{
		int firstIndex = FirstIndex;
		uint length = Length;
		return (int)(firstIndex + length - 1);
	}

	// returns true if the index is inside the given range
	private static bool IndexInItemIndexRange(
		ItemIndexRange range,
		int index)
	{
		int firstIndex = range.FirstIndex;
		uint length = range.Length;

		return ItemIndexRangeHelper.IndexInRange(firstIndex, (int)(firstIndex + length - 1), index);
	}

	// returns true if the index is inside a range inside the given collection
	internal bool IndexInItemIndexRangeCollection(
		IList<ItemIndexRange> pCollection,
		int index)
	{
		var size = pCollection.Count;

		for (int i = 0; i < size; ++i)
		{
			var spRange = pCollection[i];

			var pbReturnValue = ItemIndexRange.IndexInItemIndexRange(spRange, index);

			if (pbReturnValue)
			{
				return true;
			}
		}

		return false;
	}

	// returns true if the two ranges are identical
	private static bool AreItemIndexRangesEqual(
		ItemIndexRange range1,
		ItemIndexRange range2)
	{
		var firstIndex1 = range1.FirstIndex;
		var length1 = range1.Length;

		var firstIndex2 = range2.FirstIndex;
		var length2 = range2.Length;

		return ItemIndexRangeHelper.AreRangesEqual(firstIndex1, length1, firstIndex2, length2);
	}

	// return true if the two collections are identical (even in order of items)
	// for example, {{5,1},{7,3}} is NOT equal to {{7,3},{5,1}}
	internal static bool AreItemIndexRangeCollectionsEqual(
		IList<ItemIndexRange> pCollection1,
		IList<ItemIndexRange> pCollection2)
	{
		var pbReturnValue = false;

		var size1 = pCollection1.Count;
		var size2 = pCollection2.Count;

		// if they are not of the same size, then they're definitely not equal
		if (size1 == size2)
		{
			int i = 0;

			for (; i < size1; ++i)
			{
				var spRange1 = pCollection1[i];
				var spRange2 = pCollection2[i];

				var areEqual = ItemIndexRange.AreItemIndexRangesEqual(spRange1, spRange2);

				// if the ranges are not equal, that means the collections are not identical
				// we break, ensuring that the return value will be false
				if (!areEqual)
				{
					break;
				}
			}

			// if we reached this point and i is equal to the size, that means that all ranges are identical
			if (i == size1)
			{
				pbReturnValue = true;
			}
		}

		return pbReturnValue;
	}

	// goes through the vector and creates ranges from continuous indices
	internal IList<ItemIndexRange> AppendItemIndexRangesFromSortedVectorToItemIndexRangeCollection(
		IList<uint> indices)
	{
		int size = indices.Count;
		uint length = 1;
		IList<ItemIndexRange> pCollection = new List<ItemIndexRange>();
		// grouping continuous ranges together
		for (int i = 0; i < size; i += (int)length)
		{
			length = ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, i);

			ItemIndexRange.AppendItemIndexRangeToItemIndexRangeCollection((int)indices[i], length, ref pCollection);
		}

		return pCollection;
	}

	// Used to create an ItemIndexRange and append it to the provided collection
	private static void AppendItemIndexRangeToItemIndexRangeCollection(
		int startIndex,
		uint length,
		ref IList<ItemIndexRange> pCollection)
	{
		var spRange = new ItemIndexRange(startIndex, length);

		// add the range to the tracked ranges list
		pCollection.Add(spRange);
	}

	// creates an ItemIndexRange collection from a vector of ranges
	internal static IList<ItemIndexRange> GetItemIndexRangeCollectionFromRangesVector(
		IList<ItemIndexRangeHelper.Range> source,
		int beginIndex,
		int endIndex)
	{
		var results = new List<ItemIndexRange>();
		for (int itr = beginIndex; itr < endIndex; ++itr)
		{
			var range = source[itr];

			results.Add(new ItemIndexRange(range.FirstIndex, range.Length));
		}

		return results;
	}
}
