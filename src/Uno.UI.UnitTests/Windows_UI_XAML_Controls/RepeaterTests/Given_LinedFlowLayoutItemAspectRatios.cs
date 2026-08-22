#if !WINAPPSDK
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using ItemAspectRatio = Microsoft.UI.Xaml.Controls.LinedFlowLayoutItemAspectRatios.ItemAspectRatio;

namespace Uno.UI.Tests.RepeaterTests
{
	[TestClass]
	public class Given_LinedFlowLayoutItemAspectRatios
	{
		[TestMethod]
		public void When_New_Then_Empty()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();

			Assert.IsTrue(sut.IsEmpty());

			var empty = sut.GetAt(5);
			Assert.AreEqual(0f, empty.AspectRatio);
			Assert.AreEqual(0, empty.Weight);
		}

		[TestMethod]
		public void When_SetAt_Then_GetAt_RoundTrips()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();
			sut.Resize(64, 0);

			sut.SetAt(10, new ItemAspectRatio(1.5f, 2));

			Assert.IsFalse(sut.IsEmpty());

			var stored = sut.GetAt(10);
			Assert.AreEqual(1.5f, stored.AspectRatio);
			Assert.AreEqual(2, stored.Weight);

			// An index that was never stored returns the empty ItemAspectRatio.
			Assert.AreEqual(0, sut.GetAt(11).Weight);
		}

		[TestMethod]
		public void When_ItemsInSeparateBlocks_Then_BothTracked()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();
			sut.Resize(128, 0); // 2 blocks: [0,63] and [64,127]

			sut.SetAt(10, new ItemAspectRatio(1f, 1));
			sut.SetAt(70, new ItemAspectRatio(3f, 1));

			Assert.AreEqual(1f, sut.GetAt(10).AspectRatio);
			Assert.AreEqual(3f, sut.GetAt(70).AspectRatio);
		}

		[TestMethod]
		public void When_GetAverageAspectRatio_Then_WeightedByRange()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();
			sut.Resize(64, 0);

			sut.SetAt(0, new ItemAspectRatio(2f, 1));
			sut.SetAt(1, new ItemAspectRatio(4f, 1));

			// Both items fall in [0,1] so both are included: (2*1 + 4*1) / (1 + 1) = 3.
			Assert.AreEqual(3f, sut.GetAverageAspectRatio(0, 1, 5), 0.0001f);
		}

		[TestMethod]
		public void When_HasLowerWeight_Then_DetectsBelowThresholdInRange()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();
			sut.Resize(64, 0);

			sut.SetAt(5, new ItemAspectRatio(1f, 3));

			// weight 3 is strictly positive and below 5, and index 5 is in [0,63].
			Assert.IsTrue(sut.HasLowerWeight(0, 63, 5));
			// 3 is not strictly below 3.
			Assert.IsFalse(sut.HasLowerWeight(0, 63, 3));
			// index 5 is outside [0,4].
			Assert.IsFalse(sut.HasLowerWeight(0, 4, 5));
		}

		[TestMethod]
		public void When_Cleared_Then_Empty()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();
			sut.Resize(64, 0);
			sut.SetAt(5, new ItemAspectRatio(1f, 1));

			Assert.IsFalse(sut.IsEmpty());

			sut.Clear();

			Assert.IsTrue(sut.IsEmpty());
			Assert.AreEqual(0, sut.GetAt(5).Weight);
		}

		[TestMethod]
		public void When_MoreBlocksNeededThanAllocated_Then_FurthestRecycled()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();
			sut.Resize(64, 0); // a single block

			sut.SetAt(10, new ItemAspectRatio(1f, 1)); // block [0,63]
			Assert.AreEqual(1, sut.GetAt(10).Weight);

			// Index 70 needs block [64,127]; with only one block allocated the [0,63] block
			// (furthest from 70) is recycled to track the new area.
			sut.SetAt(70, new ItemAspectRatio(2f, 1));

			Assert.AreEqual(1, sut.GetAt(70).Weight);
			Assert.AreEqual(0, sut.GetAt(10).Weight, "the [0,63] block should have been recycled");
		}

		[TestMethod]
		public void When_Shrunk_Then_FurthestFromReferenceRemoved()
		{
			var sut = new LinedFlowLayoutItemAspectRatios();
			sut.Resize(128, 0); // 2 blocks

			sut.SetAt(10, new ItemAspectRatio(1f, 1)); // block [0,63]
			sut.SetAt(70, new ItemAspectRatio(2f, 1)); // block [64,127]

			// Shrink to a single block, keeping the one closest to reference index 70.
			sut.Resize(64, 70);

			Assert.AreEqual(1, sut.GetAt(70).Weight, "the block nearest the reference index is kept");
			Assert.AreEqual(0, sut.GetAt(10).Weight, "the block furthest from the reference index is removed");
		}
	}
}
#endif
