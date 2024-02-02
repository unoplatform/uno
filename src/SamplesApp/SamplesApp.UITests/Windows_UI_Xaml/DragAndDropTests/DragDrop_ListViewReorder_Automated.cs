using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.DragAndDropTests
{
	public partial class DragDrop_ListViewReorder_Automated : SampleControlUITestBase
	{
		private static readonly string[] _items = new[] { "#FF0018", "#FFA52C", "#FFFF41", "#008018", "#0000F9", "#86007D" };
		private const int _itemHeight = 90;

		private static float Item(IAppRect sut, int index) => sut.Y + (index * _itemHeight) + 25;

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public async Task When_Disabled()
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_ListView", skipInitialScreenshot: true);

			var sut = _app.Marked("SUT");
			var mode = _app.Marked("DragMode");

			_app.WaitForElement(sut);
			await Task.Delay(250); // wait out the EntranceThemeTransition (duration=100)

			// Disable re-ordering
			mode.SetDependencyPropertyValue("IsChecked", "False");

			// Tap outside the ListView to get rid of PointerOver visual from the step above
			var sutBounds = _app.Query(sut).Single().Rect;
			_app.TapCoordinates(sutBounds.CenterX, sutBounds.Bottom + 10);

			var before = TakeScreenshot("Before", ignoreInSnapshotCompare: true);

			// Attempt to re-order by dragging from item1 (orange) to item3 (green)
			var x = sutBounds.X + 50;
			var srcY = Item(sutBounds, 1);
			var dstY = Item(sutBounds, 3);
			_app.DragCoordinates(x, srcY, x, dstY);

			// Tap outside the ListView to get rid of PointerOver visual from the step above
			_app.TapCoordinates(sutBounds.CenterX, sutBounds.Bottom + 10);

			var after = TakeScreenshot("After", ignoreInSnapshotCompare: true);

			// note: we only test the left-most 100 pixels width to avoid failure due to scrollbar being visible in "after" screenshot
			var testBounds = new Rectangle((int)sutBounds.X, (int)sutBounds.Y, 100, (int)sutBounds.Height);
			ImageAssert.AreEqual(before, after, testBounds);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_Down() => Test_Reorder(1, 3);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_Up() => Test_Reorder(3, 1);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_First() => Test_Reorder(0, 2);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_Last() => Test_Reorder(5, 2);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_To_First() => Test_Reorder(1, 0);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_To_Last() => Test_Reorder(3, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_To_Last_2() => Test_Reorder(3, 6 /* out of range */, expectedTo: 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_First_To_Last() => Test_Reorder(0, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_Last_To_First() => Test_Reorder(0, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_Down() => Test_ReorderMulti(1, 3);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_Up() => Test_ReorderMulti(3, 1);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_First() => Test_ReorderMulti(0, 2);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_Last() => Test_ReorderMulti(5, 2);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_To_First() => Test_ReorderMulti(1, 0);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_To_Last() => Test_ReorderMulti(3, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_First_To_Last() => Test_ReorderMulti(0, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromAnUnselectedItem_Last_To_First() => Test_ReorderMulti(0, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromASelectedItem_Down() => Test_ReorderMulti(2, 4, expectedTo: 3);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromASelectedItem_Up() => Test_ReorderMulti(2, 1);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromASelectedItem_To_First() => Test_ReorderMulti(2, 0);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_ReorderWithMultiSelectStartingFromASelectedItem_To_Last() => Test_ReorderMulti(2, 5, expectedTo: 4);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		public void When_Reorder_OnTopPadding() => Test_ReorderWithPadding(null, 25, 0);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		[Ignore("https://github.com/unoplatform/Uno.UITest/pull/35")]
		public void When_Reorder_OnBottomPadding() => Test_ReorderWithPadding(null, -25, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		[Ignore("https://github.com/unoplatform/Uno.UITest/pull/35")]
		public void When_Reorder_OnLeftPadding() => Test_ReorderWithPadding(25, null, 1);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
		[Ignore("https://github.com/unoplatform/Uno.UITest/pull/35")]
		public void When_Reorder_OnRightPadding() => Test_ReorderWithPadding(-25, null, 4);

		private void Test_Reorder(int from, int to, int? expectedTo = null)
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_ListView", skipInitialScreenshot: true);

			var sut = _app.Marked("SUT");
			var op = _app.Marked("Operation");

			_app.WaitForElement(sut);

			var sutBounds = _app.Query(sut).Single().Rect;
			var x = sutBounds.X + 50;
			var srcY = Item(sutBounds, from);
			var dstY = Item(sutBounds, to);
			var expectedY = expectedTo is null ? dstY : Item(sutBounds, expectedTo.Value);

			_app.DragCoordinates(x, srcY, x, dstY);

			var result = TakeScreenshot("Result", ignoreInSnapshotCompare: true);

			ImageAssert.HasColorAt(result, x * GetDisplayScreenScaling(), expectedY * GetDisplayScreenScaling(), _items[from], tolerance: 10);
			Assert.IsTrue(op.GetDependencyPropertyValue<string>("Text").Contains("Move"));
		}

		private void Test_ReorderMulti(int from, int to, int? expectedTo = null)
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_ListView", skipInitialScreenshot: true);

			var sut = _app.Marked("SUT");
			var op = _app.Marked("Operation");

			_app.WaitForElement(sut);

			var sutBounds = _app.Query(sut).Single().Rect;
			var x = sutBounds.X + 50;
			var srcY = Item(sutBounds, from);
			var dstY = Item(sutBounds, to);
			var expectedY = expectedTo is null ? dstY : Item(sutBounds, expectedTo.Value);

			_app.TapCoordinates(x, Item(sutBounds, 4)); // We select the 4th item first, as it has to remain after the 2nd
			_app.TapCoordinates(x, Item(sutBounds, 2));
			_app.DragCoordinates(x, srcY, x, dstY);

			var result = TakeScreenshot("Result", ignoreInSnapshotCompare: true);
			if (from is 2 or 4)
			{
				ImageAssert.HasColorAt(result, x * GetDisplayScreenScaling(), expectedY * GetDisplayScreenScaling(), _items[2], tolerance: 10);
				ImageAssert.HasColorAt(result, x * GetDisplayScreenScaling(), (expectedY + _itemHeight) * GetDisplayScreenScaling(), _items[4], tolerance: 10);
			}
			else
			{
				ImageAssert.HasColorAt(result, x, dstY, _items[from], tolerance: 10);
			}
			Assert.IsTrue(op.GetDependencyPropertyValue<string>("Text").Contains("Move"));
		}

		private void Test_ReorderWithPadding(float? relativeX, float? relativeY, int expectedTo)
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_ListView_WithPadding", skipInitialScreenshot: true);

			var sut = _app.Marked("SUT");
			var result = _app.Marked("Result");

			_app.WaitForElement(sut);

			const int itemSize = 50;
			const int movedItem = 4; // 4th item

			var sutBounds = _app.Query(sut).Single().Rect;
			var srcX = sutBounds.X + 50 /* Left padding */ + 100 /* Over element */;
			var srcY = sutBounds.Y + 50 /* Top padding */ + itemSize * movedItem;
			var dstX = relativeX.HasValue
				? (relativeX.Value >= 0 ? sutBounds.X + relativeX.Value : sutBounds.Right + relativeX.Value)
				: srcX;
			var dstY = relativeY.HasValue
				? (relativeY.Value >= 0 ? sutBounds.Y + relativeY.Value : sutBounds.Bottom + relativeY.Value)
				: sutBounds.Y + 50 + itemSize * (expectedTo + 1) + 25;

			_app.DragCoordinates(srcX, srcY, dstX, dstY);

			var expectedOrder = GetExpected();
			var actualOrder = result.GetDependencyPropertyValue<string>("Text");

			Assert.AreEqual(expectedOrder, actualOrder);

			string GetExpected()
			{
				var items = _items.ToList();
				items.RemoveAt(movedItem - 1);
				items.Insert(expectedTo, _items[movedItem - 1]);

				return string.Join(';', items);
			}
		}
	}
}
