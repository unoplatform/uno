using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ItemsControl
{
	[TestFixture]
	public partial class ItemsControlTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void ItemsControl_ItemContainerStyle()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ItemsControl.ItemsControl_ItemContainerStyle");

			_app.WaitForElement(_app.Marked("theItemsControl"));
			var theItemsControl = _app.Marked("theItemsControl");

			const string deepPink = "#FF1493";
			const string lime = "#00FF00";

			var firstItem = _app.GetPhysicalRect("FirstItem");
			var secondItem = _app.GetPhysicalRect("SecondItem");

			using var snapshot = TakeScreenshot("ItemsControl_ItemContainerStyle");

			ImageAssert.HasPixels(
					snapshot,
					ExpectedPixels
						.At("first item center", firstItem.CenterX, firstItem.CenterY)
						.Pixel(deepPink),
					ExpectedPixels
						.At("second item center", secondItem.CenterX, secondItem.CenterY)
						.Pixel(lime));
		}

		[Test]
		[AutoRetry]
		public void ItemsControl_ReplaceItem()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ItemsControl.ItemsControl_ReplaceItem");

			var theItemsControl = _app.Marked("theItemsControl");
			_app.WaitForElement(theItemsControl);

			_app.WaitForElement("UpdateItem01");

			_app.Tap("UpdateContent01");

			_app.WaitForElement("UpdateItem02");
		}

		[Test]
		[AutoRetry]
		public void ItemsControl_AppendItem()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ItemsControl.ItemsControl_AppendItem");

			var theItemsControl = _app.Marked("theItemsControl");
			_app.WaitForElement(theItemsControl);

			var item01 = _app.WaitForElement("AppendItem01").FirstOrDefault();

			_app.Tap("AppendContent01");

			var item02 = _app.WaitForElement("AppendItem02").FirstOrDefault();


			// This check validates that the native collection is properly manipulated
			Assert.Less(item02.Rect.CenterY, item01.Rect.CenterY);
		}
	}
}
