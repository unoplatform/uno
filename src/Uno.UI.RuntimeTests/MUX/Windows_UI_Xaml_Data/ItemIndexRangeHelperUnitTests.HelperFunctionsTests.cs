using System.Collections.Generic;
using DirectUI.Components;

namespace Uno.UI.RuntimeTests.MUX.Windows_UI_Xaml_Data;

public partial class ItemIndexRangeHelperUnitTests
{
	[TestMethod]
	public void ValidateIndexInRange()
	{
		VERIFY_IS_TRUE(ItemIndexRangeHelper.IndexInRange(1, 5, 1));
		VERIFY_IS_TRUE(ItemIndexRangeHelper.IndexInRange(1, 5, 2));
		VERIFY_IS_TRUE(ItemIndexRangeHelper.IndexInRange(1, 5, 5));

		VERIFY_IS_FALSE(ItemIndexRangeHelper.IndexInRange(1, 5, 6));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IndexInRange(0, 5, 6));
	}

	[TestMethod]
	public void ValidateAreRangesEqual()
	{
		VERIFY_IS_TRUE(ItemIndexRangeHelper.AreRangesEqual(1, 5, 1, 5));

		VERIFY_IS_FALSE(ItemIndexRangeHelper.AreRangesEqual(1, 5, 1, 6));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.AreRangesEqual(2, 5, 1, 5));
	}

	[TestMethod]
	public void ValidateGetContinousIndicesLengthStartingAtIndex()
	{
		uint[] tempIndices = new uint[] { 1, 2, 3, 4, 5, 7, 9, 10 };
		List<uint> indices = new();
		for (int i = 0; i < 8; ++i)
		{
			indices.Add(tempIndices[i]);
		}

		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 0), (uint)5);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 1), (uint)4);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 2), (uint)3);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 3), (uint)2);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 4), (uint)1);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 5), (uint)1);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 6), (uint)2);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.GetContinousIndicesLengthStartingAtIndex(indices, 7), (uint)1);
	}

	[TestMethod]
	public void ValidateIndexWithRespectToRange()
	{
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.IndexWithRespectToRange(1, 5, 1), ItemIndexRangeHelper.IndexLocation.Inside);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.IndexWithRespectToRange(1, 5, 2), ItemIndexRangeHelper.IndexLocation.Inside);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.IndexWithRespectToRange(1, 5, 5), ItemIndexRangeHelper.IndexLocation.Inside);

		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.IndexWithRespectToRange(1, 5, 0), ItemIndexRangeHelper.IndexLocation.Before);
		VERIFY_ARE_EQUAL(ItemIndexRangeHelper.IndexWithRespectToRange(1, 5, 6), ItemIndexRangeHelper.IndexLocation.After);
	}

	[TestMethod]
	public void ValidateIsFirstRangeInsideSecondRange()
	{
		VERIFY_IS_TRUE(ItemIndexRangeHelper.IsFirstRangeInsideSecondRange(1, 5, 1, 5));
		VERIFY_IS_TRUE(ItemIndexRangeHelper.IsFirstRangeInsideSecondRange(1, 5, 0, 6));

		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeInsideSecondRange(1, 5, 2, 6));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeInsideSecondRange(1, 5, 7, 6));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeInsideSecondRange(2, 6, 1, 5));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeInsideSecondRange(7, 6, 1, 5));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeInsideSecondRange(1, 5, 2, 1));
	}

	[TestMethod]
	public void ValidateIsFirstRangeAdjacentToSecondRange()
	{
		VERIFY_IS_TRUE(ItemIndexRangeHelper.IsFirstRangeAdjacentToSecondRange(1, 5, 6));

		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeAdjacentToSecondRange(1, 5, 0));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeAdjacentToSecondRange(1, 5, 1));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeAdjacentToSecondRange(1, 5, 2));
		VERIFY_IS_FALSE(ItemIndexRangeHelper.IsFirstRangeAdjacentToSecondRange(1, 5, 7));
	}
}
