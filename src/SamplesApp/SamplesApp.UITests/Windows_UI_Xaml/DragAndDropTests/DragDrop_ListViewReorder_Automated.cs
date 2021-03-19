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
		private static readonly string[] _items = new[] {"#FF0018", "#FFA52C", "#FFFF41", "#008018", "#0000F9", "#86007D"};
		private const int _itemHeight = 100;

		private static float Item(IAppRect sut, int index) => sut.Y + (index * _itemHeight) + 25;

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_Down() => Test_Reorder(1, 3);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_Up() => Test_Reorder(3, 1);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_First() => Test_Reorder(0, 2);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_Last() => Test_Reorder(5, 2);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_To_First() => Test_Reorder(1, 0);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_To_Last() => Test_Reorder(3, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_First_To_Last() => Test_Reorder(0, 5);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void When_Reorder_Last_To_First() => Test_Reorder(0, 5);

		public void Test_Reorder(int from, int to)
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_ListView", skipInitialScreenshot: true);

			var sut = _app.Marked("SUT");

			_app.WaitForElement(sut);

			var sutBounds = _app.Query(sut).Single().Rect;
			var x = sutBounds.X + 50;
			var srcY = Item(sutBounds, from);
			var dstY = Item(sutBounds, to);

			_app.DragCoordinates(x, srcY, x, dstY);

			var result = TakeScreenshot("Result", ignoreInSnapshotCompare: true);

			ImageAssert.HasColorAt(result, x, dstY, _items[from], tolerance: 10);
		}
	}
}
