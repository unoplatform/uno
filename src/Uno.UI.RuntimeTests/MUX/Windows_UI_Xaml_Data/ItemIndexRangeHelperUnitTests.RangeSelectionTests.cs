using System.Collections.Generic;
using DirectUI.Components;

namespace Uno.UI.RuntimeTests.MUX.Windows_UI_Xaml_Data;

public partial class ItemIndexRangeHelperUnitTests
{
	//
	// Test Cases
	//
	[TestMethod]
	public void ValidateCreate()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new ItemIndexRangeHelper.RangeSelection();

		VERIFY_ARE_EQUAL(rangeSelection.Count, 0);
	}

	[TestMethod]
	public void ValidateItemInsertedAt()
	{
		ItemInsertedAtHelperBefore();
		ItemInsertedAtHelperAfter();
		ItemInsertedAtHelperInside();
	}

	[TestMethod]
	public void ValidateItemRemovedAt()
	{
		ItemRemovedAtHelperBefore();
		ItemRemovedAtHelperAfter();
		ItemRemovedAtHelperInside();
		ItemRemovedAtHelperInBetween();
	}

	[TestMethod]
	public void ValidateItemChangedAt()
	{
		ItemChangedAtHelperBefore();
		ItemChangedAtHelperAfter();
		ItemChangedAtHelperInside();
	}

	[TestMethod]
	public void ValidateSelectAll()
	{
		SelectAllHelperNoRanges();
		SelectAllHelperWithRanges();
	}

	[TestMethod]
	public void ValidateSelectRange()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();

		SelectRangeInitialRange(rangeSelection);
		SelectRangeBeforeRange(rangeSelection);
		SelectRangeAfterRange(rangeSelection);
		SelectRangeFrontAdjacentRange(rangeSelection);
		SelectRangeEndAdjacentRange(rangeSelection);
		SelectRangeInsideRange(rangeSelection);
		SelectRangeOverlapRange(rangeSelection);
		SelectRangeFrontIntersectionRange(rangeSelection);
		SelectRangeEndIntersectionRange(rangeSelection);
		SelectRangeMultipleIntersectionRange(rangeSelection);
		SelectRangeMultipleIntersectionOverlapRange(rangeSelection);
	}

	[TestMethod]
	public void ValidateDeselectRange()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		rangeSelection.SelectRange(5, 20, ref addedRanges);

		DeselectRangeBeforeRange(rangeSelection);
		DeselectRangeAfterRange(rangeSelection);
		DeselectRangeFrontAdjacentRange(rangeSelection);
		DeselectRangeEndAdjacentRange(rangeSelection);
		DeselectRangeInsideRange(rangeSelection);
		DeselectRangeOverlapRange(rangeSelection);
		DeselectRangeFrontIntersectionRange(rangeSelection);
		DeselectRangeEndIntersectionRange(rangeSelection);
		DeselectRangeMultipleIntersectionRange(rangeSelection);
		DeselectRangeMultipleIntersectionOverlapRange(rangeSelection);
	}

	[TestMethod]
	public void ValidateDeselectAll()
	{
		DeselectAllHelperNoRanges();
		DeselectAllHelperWithRanges();
	}

	//
	// ItemInsertedAt Helpers
	//
	private void ItemInsertedAtHelperBefore()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemInsertedAt Before***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemInsertedAt(0);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 3, 5);
		VerifyRange(rangeSelection[1], 11, 3);
	}

	private void ItemInsertedAtHelperAfter()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemInsertedAt After***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemInsertedAt(7);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 5);
		VerifyRange(rangeSelection[1], 11, 3);
	}

	private void ItemInsertedAtHelperInside()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemInsertedAt Inside***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemInsertedAt(4);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 2);
		VerifyRange(rangeSelection[1], 5, 3);
		VerifyRange(rangeSelection[2], 11, 3);
	}

	//
	// ItemRemovedAt Helpers
	//
	private void ItemRemovedAtHelperBefore()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemRemovedAt Before***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemRemovedAt(0);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 1, 5);
		VerifyRange(rangeSelection[1], 9, 3);
	}

	private void ItemRemovedAtHelperAfter()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemRemovedAt After***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemRemovedAt(7);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 5);
		VerifyRange(rangeSelection[1], 9, 3);
	}

	private void ItemRemovedAtHelperInside()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemRemovedAt Inside***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemRemovedAt(4);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 4);
		VerifyRange(rangeSelection[1], 9, 3);
	}

	private void ItemRemovedAtHelperInBetween()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemRemovedAt InBetween***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(8, 3, ref addedRanges);

		rangeSelection.ItemRemovedAt(7);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 2, 8);
	}

	//
	// ItemChangedAt Helpers
	//
	private void ItemChangedAtHelperBefore()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemChangedAt Before***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemChangedAt(0);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 5);
		VerifyRange(rangeSelection[1], 10, 3);
	}

	private void ItemChangedAtHelperAfter()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemChangedAt After***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemChangedAt(7);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 5);
		VerifyRange(rangeSelection[1], 10, 3);
	}

	private void ItemChangedAtHelperInside()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***ItemChangedAt Inside***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.ItemChangedAt(4);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 2);
		VerifyRange(rangeSelection[1], 5, 2);
		VerifyRange(rangeSelection[2], 10, 3);
	}

	//
	// SelectAll Helpers
	//
	private void SelectAllHelperNoRanges()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectAll NoRanges***");

		rangeSelection.SelectAll((20), ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 0, 20);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 0, 20);
	}

	private void SelectAllHelperWithRanges()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectAll WithRanges***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.SelectAll((20), ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 0, 20);

		VERIFY_ARE_EQUAL(addedRanges.Count, (3));

		VerifyRange(addedRanges[0], 0, 2);
		VerifyRange(addedRanges[1], 7, 3);
		VerifyRange(addedRanges[2], 13, 7);
	}

	//
	// SelectRange Helpers
	//
	private void SelectRangeInitialRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange InitialRange***");

		rangeSelection.SelectRange(8, 2, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 8, 2);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 8, 2);
	}

	private void SelectRangeBeforeRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange BeforeRange***");

		rangeSelection.SelectRange(2, 3, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 8, 2);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 2, 3);
	}

	private void SelectRangeAfterRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange AfterRange***");

		rangeSelection.SelectRange(16, 4, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 8, 2);
		VerifyRange(rangeSelection[2], 16, 4);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 16, 4);
	}

	private void SelectRangeFrontAdjacentRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange FrontAdjacentRange***");

		rangeSelection.SelectRange(6, 2, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 6, 4);
		VerifyRange(rangeSelection[2], 16, 4);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 6, 2);
	}

	private void SelectRangeEndAdjacentRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange EndAdjacentRange***");

		rangeSelection.SelectRange(10, 1, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 6, 5);
		VerifyRange(rangeSelection[2], 16, 4);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 10, 1);
	}

	private void SelectRangeInsideRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange InsideRange***");

		rangeSelection.SelectRange(7, 2, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 6, 5);
		VerifyRange(rangeSelection[2], 16, 4);

		VERIFY_ARE_EQUAL(addedRanges.Count, (0));
	}

	private void SelectRangeOverlapRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange OverlapRange***");

		rangeSelection.SelectRange(14, 7, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 6, 5);
		VerifyRange(rangeSelection[2], 14, 7);

		VERIFY_ARE_EQUAL(addedRanges.Count, (2));

		VerifyRange(addedRanges[0], 14, 2);
		VerifyRange(addedRanges[1], 20, 1);
	}

	private void SelectRangeFrontIntersectionRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange FrontIntersectionRange***");

		rangeSelection.SelectRange(13, 5, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 6, 5);
		VerifyRange(rangeSelection[2], 13, 8);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 13, 1);
	}

	private void SelectRangeEndIntersectionRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange EndIntersectionRange***");

		rangeSelection.SelectRange(18, 5, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (3));

		VerifyRange(rangeSelection[0], 2, 3);
		VerifyRange(rangeSelection[1], 6, 5);
		VerifyRange(rangeSelection[2], 13, 10);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 21, 2);
	}

	private void SelectRangeMultipleIntersectionRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange MultipleIntersectionRange***");

		rangeSelection.SelectRange(3, 7, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 9);
		VerifyRange(rangeSelection[1], 13, 10);

		VERIFY_ARE_EQUAL(addedRanges.Count, (1));

		VerifyRange(addedRanges[0], 5, 1);
	}

	private void SelectRangeMultipleIntersectionOverlapRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();

		LOG_OUTPUT("***SelectRange MultipleIntersectionOverlapRange***");

		rangeSelection.SelectRange(1, 25, ref addedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 1, 25);

		VERIFY_ARE_EQUAL(addedRanges.Count, (3));

		VerifyRange(addedRanges[0], 1, 1);
		VerifyRange(addedRanges[1], 11, 2);
		VerifyRange(addedRanges[2], 23, 3);
	}

	//
	// DeselectRange Helpers
	//
	private void DeselectRangeBeforeRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange BeforeRange***");

		rangeSelection.DeselectRange(1, 2, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 5, 20);

		VERIFY_ARE_EQUAL(removedRanges.Count, (0));
	}

	private void DeselectRangeAfterRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange AfterRange***");

		rangeSelection.DeselectRange(26, 2, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 5, 20);

		VERIFY_ARE_EQUAL(removedRanges.Count, (0));
	}

	private void DeselectRangeFrontAdjacentRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange FrontAdjacentRange***");

		rangeSelection.DeselectRange(3, 2, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 5, 20);

		VERIFY_ARE_EQUAL(removedRanges.Count, (0));
	}

	private void DeselectRangeEndAdjacentRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange EndAdjacentRange***");

		rangeSelection.DeselectRange(25, 1, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 5, 20);

		VERIFY_ARE_EQUAL(removedRanges.Count, (0));
	}

	private void DeselectRangeInsideRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange InsideRange***");

		rangeSelection.DeselectRange(11, 3, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 5, 6);
		VerifyRange(rangeSelection[1], 14, 11);

		VERIFY_ARE_EQUAL(removedRanges.Count, (1));

		VerifyRange(removedRanges[0], 11, 3);
	}

	private void DeselectRangeOverlapRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange OverlapRange***");

		rangeSelection.DeselectRange(3, 9, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 14, 11);

		VERIFY_ARE_EQUAL(removedRanges.Count, (1));

		VerifyRange(removedRanges[0], 5, 6);
	}

	private void DeselectRangeFrontIntersectionRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange FrontIntersectionRange***");

		rangeSelection.DeselectRange(12, 5, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 17, 8);

		VERIFY_ARE_EQUAL(removedRanges.Count, (1));

		VerifyRange(removedRanges[0], 14, 3);
	}

	private void DeselectRangeEndIntersectionRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange EndIntersectionRange***");

		rangeSelection.DeselectRange(22, 5, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (1));

		VerifyRange(rangeSelection[0], 17, 5);

		VERIFY_ARE_EQUAL(removedRanges.Count, (1));

		VerifyRange(removedRanges[0], 22, 3);
	}

	private void DeselectRangeMultipleIntersectionRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange MultiIntersectionRange***");

		rangeSelection.SelectRange(2, 3, ref addedRanges);
		rangeSelection.SelectRange(8, 4, ref addedRanges);

		rangeSelection.DeselectRange(3, 16, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (2));

		VerifyRange(rangeSelection[0], 2, 1);
		VerifyRange(rangeSelection[1], 19, 3);

		VERIFY_ARE_EQUAL(removedRanges.Count, (3));

		VerifyRange(removedRanges[0], 3, 2);
		VerifyRange(removedRanges[1], 8, 4);
		VerifyRange(removedRanges[2], 17, 2);
	}

	private void DeselectRangeMultipleIntersectionOverlapRange(ItemIndexRangeHelper.RangeSelection rangeSelection)
	{
		List<ItemIndexRangeHelper.Range> addedRanges = new();
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectRange MultipleIntersectionOverlapRange***");

		rangeSelection.SelectRange(8, 4, ref addedRanges);

		rangeSelection.DeselectRange(2, 20, ref removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (0));

		VERIFY_ARE_EQUAL(removedRanges.Count, (3));

		VerifyRange(removedRanges[0], 2, 1);
		VerifyRange(removedRanges[1], 8, 4);
		VerifyRange(removedRanges[2], 19, 3);
	}

	//
	// DeselectAll Helpers
	//
	private void DeselectAllHelperNoRanges()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectAll NoRanges***");

		rangeSelection.DeselectAll(out removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (0));

		VERIFY_ARE_EQUAL(removedRanges.Count, (0));
	}

	private void DeselectAllHelperWithRanges()
	{
		ItemIndexRangeHelper.RangeSelection rangeSelection = new();
		List<ItemIndexRangeHelper.Range> addedRanges = new();
		List<ItemIndexRangeHelper.Range> removedRanges = new();

		LOG_OUTPUT("***DeselectAll WithRanges***");

		rangeSelection.SelectRange(2, 5, ref addedRanges);
		rangeSelection.SelectRange(10, 3, ref addedRanges);

		rangeSelection.DeselectAll(out removedRanges);

		VERIFY_ARE_EQUAL(rangeSelection.Count, (0));

		VERIFY_ARE_EQUAL(removedRanges.Count, (2));

		VerifyRange(removedRanges[0], 2, 5);
		VerifyRange(removedRanges[1], 10, 3);
	}
}
