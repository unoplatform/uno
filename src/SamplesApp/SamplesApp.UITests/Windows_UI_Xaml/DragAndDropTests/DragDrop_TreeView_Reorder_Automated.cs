using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace SamplesApp.UITests.Windows_UI_Xaml.DragAndDropTests
{
	public partial class DragDrop_TreeView_Reorder_Automated : SampleControlUITestBase
	{
		const int _itemHeight = 70;
		const int _offset = 30;
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Dragging_TreeView_Item()
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_TreeView", skipInitialScreenshot: true);

			var tv = _app.Marked("tv");
			var bt0 = _app.Marked("bt0");
			var bt1 = _app.Marked("bt1");
			var bt2 = _app.Marked("bt2");
			var bt3 = _app.Marked("bt3");
			var focusbt = _app.Marked("focusbt");
			var radio_disable = _app.Marked("radio_disable");

			_app.WaitForElement(radio_disable);
			_app.Tap(radio_disable);

			_app.WaitForElement(tv);
			_app.Tap(bt1);
			_app.Tap(focusbt);
			var case1 = _app.Screenshot("case1");

			_app.Tap(bt2);
			_app.Tap(focusbt);
			var case2 = _app.Screenshot("case2");

			_app.Tap(bt3);
			_app.Tap(focusbt);
			var case3 = _app.Screenshot("case3");

			_app.Tap(bt0);
			var tvBounds = _app.Query("tv").Single().Rect;
			float fromX = tvBounds.X + 100;
			float fromY = tvBounds.Y + _itemHeight * 3 + _offset;
			float toX = tvBounds.X + 100;
			float toY = tvBounds.Y + _offset;
			_app.DragCoordinates(fromX, fromY, toX, toY);
			_app.Tap(focusbt);
			var result = _app.Screenshot("result1");
			ImageAssert.AreEqual(case1, result);

			fromY = tvBounds.Y + _itemHeight * 1 + _offset;
			toY = tvBounds.Y + _itemHeight * 4 + _offset;
			_app.DragCoordinates(fromX, fromY, toX, toY);
			_app.Tap(focusbt);
			result = _app.Screenshot("result2");
			ImageAssert.AreEqual(case2, result);

			fromY = tvBounds.Y + _itemHeight * 3 + _offset;
			toY = tvBounds.Y + _itemHeight - 5;
			_app.DragCoordinates(fromX, fromY, toX, toY);
			_app.Tap(focusbt);
			result = _app.Screenshot("result3");
			ImageAssert.AreEqual(case3, result);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_OnDragItemsCompleted()
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_TreeView", skipInitialScreenshot: true);
			var tv = _app.Marked("tv");
			var bt0 = _app.Marked("bt0");
			var radio_enable = _app.Marked("radio_enable");
			var tb = _app.Marked("tb");

			_app.WaitForElement(tv);

			_app.WaitForElement(radio_enable);
			_app.Tap(radio_enable);
			_app.WaitForElement(bt0);
			_app.Tap(bt0);

			var tvBounds = _app.Query("tv").Single().Rect;
			float fromX = tvBounds.X + 100;
			float fromY = tvBounds.Y + _itemHeight * 3 + _offset;
			float toX = tvBounds.X + 100;
			float toY = tvBounds.Y + _offset;
			_app.DragCoordinates(fromX, fromY, toX, toY);

			string text = _app.GetText("tb").Trim();
			Assert.AreEqual("DragItemsCompleted is triggered", text);
		}
	}
}
