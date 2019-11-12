using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[TestFixture]
	public partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void BitmapSource_PixelSize()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.Image_ImageSource_PixelSize");

			// Request to render
			var button = _app.Marked("image01Button");
			var bitmapWidthElement = _app.Marked("bitmapWidth");
			var bitmapHeightElement = _app.Marked("bitmapHeight");
			var loadStateElement = _app.Marked("loadState");

			_app.WaitForDependencyPropertyValue(bitmapWidthElement, "Text", "");
			_app.WaitForDependencyPropertyValue(bitmapHeightElement, "Text", "");
			_app.WaitForDependencyPropertyValue(loadStateElement, "Text", "none");

			_app.WaitForElement(button);
			button.Tap();

			_app.WaitForDependencyPropertyValue(bitmapWidthElement, "Text", "1252");
			_app.WaitForDependencyPropertyValue(bitmapHeightElement, "Text", "836");
			_app.WaitForDependencyPropertyValue(loadStateElement, "Text", "ImageOpened");

			// Take screenshot
			TakeScreenshot("ImageSource_PixelSize - Result");
		}
	}
}
