using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.DragAndDropTests;

public partial class DragDrop_ListView_Selection_Automated : SampleControlUITestBase
{
	private float Item(IAppRect sut, int index, int itemLogicalHeight)
		=> sut.Y + (index + .5f) * LogicalToPhysical(itemLogicalHeight);

	[Test]
	[AutoRetry]
	[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
	public void When_Reorder_SelectedItem_Up()
	{
		const int itemHeight = 75;

		Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_ListView_Selection", skipInitialScreenshot: true);

		var mode = App.Marked("SelectionModeConfig");
		var sut = App.Marked("SUT");

		mode.SetDependencyPropertyValue("SelectedValue", "Single");

		var sutBounds = _app.Query(sut).Single().Rect;
		var x = sutBounds.X + 50;
		float Item(int index) => this.Item(sutBounds, index, itemHeight);

		// Select "Item #6"
		App.TapCoordinates(x, Item(5));
		var afterSelect = TakeScreenshot("Item_5_Selected");

		// Reorder "Item #6" to position 3
		App.DragCoordinates(x, Item(5), x, Item(2));
		var afterReorder = TakeScreenshot("Item_5_selected_at_position_2");

		ImageAssert.HasColorAt(afterSelect, x, Item(2), Color.White);
		ImageAssert.HasColorAt(afterSelect, x, Item(4), Color.White);
		ImageAssert.HasPixels(afterSelect, ExpectedPixels.At(x, Item(5)).Pixel(Color.Red).OrPixel("#FFFF00").OrPixel("#FFA000C0")); // Any selected stateColor.Red);
		ImageAssert.HasColorAt(afterSelect, x, Item(6), Color.White);

		ImageAssert.HasColorAt(afterReorder, x, Item(1), Color.White);
		ImageAssert.HasPixels(afterReorder, ExpectedPixels.At(x, Item(2)).Pixel(Color.Red).OrPixel("#FFFF00").OrPixel("#FFA000C0")); // Any selected state
		ImageAssert.HasColorAt(afterReorder, x, Item(3), Color.White);
		ImageAssert.HasColorAt(afterReorder, x, Item(5), Color.White);
	}

	[Test]
	[AutoRetry]
	[ActivePlatforms(Platform.Browser)] // TODO: support drag-and-drop testing on mobile https://github.com/unoplatform/Uno.UITest/issues/31
	public void When_Reorder_SelectedItem_Down()
	{
		const int itemHeight = 75;

		Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_ListView_Selection", skipInitialScreenshot: true);

		var mode = App.Marked("SelectionModeConfig");
		var sut = App.Marked("SUT");

		mode.SetDependencyPropertyValue("SelectedValue", "Single");

		var sutBounds = _app.Query(sut).Single().Rect;
		var x = sutBounds.X + 50;
		float Item(int index) => this.Item(sutBounds, index, itemHeight);

		// Select "Item #3"
		App.TapCoordinates(x, Item(2));
		var afterSelect = TakeScreenshot("Item_2_Selected");

		// Reorder "Item #3" to position 6
		App.DragCoordinates(x, Item(2), x, Item(5));
		var afterReorder = TakeScreenshot("Item_2_selected_at_position_5");

		ImageAssert.HasColorAt(afterSelect, x, Item(1), Color.White);
		ImageAssert.HasPixels(afterSelect, ExpectedPixels.At(x, Item(2)).Pixel(Color.Red).OrPixel("#FFFF00").OrPixel("#FFA000C0")); // Any selected state
		ImageAssert.HasColorAt(afterSelect, x, Item(3), Color.White);
		ImageAssert.HasColorAt(afterSelect, x, Item(5), Color.White);

		ImageAssert.HasColorAt(afterReorder, x, Item(2), Color.White);
		ImageAssert.HasColorAt(afterReorder, x, Item(4), Color.White);
		ImageAssert.HasPixels(afterReorder, ExpectedPixels.At(x, Item(5)).Pixel(Color.Red).OrPixel("#FFFF00").OrPixel("#FFA000C0")); // Any selected state
		ImageAssert.HasColorAt(afterReorder, x, Item(6), Color.White);
	}
}
