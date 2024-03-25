using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
		public void WriteableBitmap_Invalidate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.ImageSourceWriteableBitmapInvalidate");

			// Request to render
			var button = _app.Marked("_update");
			_app.WaitForElement(button);

			var screenshotBefore = TakeScreenshot("WriteableBitmap_Invalidate - Before");

			button.FastTap();

			// Take screenshot
			var screenshotAfter = TakeScreenshot("WriteableBitmap_Invalidate - Result");

			ImageAssert.AreNotEqual(screenshotBefore, screenshotAfter);
		}

		[Test]
		[AutoRetry]
		public void WriteableBitmap_MultiInvalidate()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.WriteableBitmap_MultiInvalidate");

			// Request to render
			var button = _app.Marked("_randomize");
			_app.WaitForElement(button);
			button.FastTap();

			var screenshotBefore = TakeScreenshot("WriteableBitmap_MultiInvalidate - Before");

			button.FastTap();

			// Take screenshot
			var screenshotAfter = TakeScreenshot("WriteableBitmap_MultiInvalidate - After");

			ImageAssert.AreNotEqual(screenshotBefore, screenshotAfter);
		}

		[Test]
		[AutoRetry]
		public void ImageStretch_None()
		{
			Run("Uno.UI.Samples.UITests.ImageTestsControl.Image_Stretch_None");

			void HasValidSize(string name)
			{
				var element = _app.Marked(name);
				_app.WaitForElement(element);
				var rect = _app.Query(element).First().Rect;
				Assert.That(rect.Width != 0);
				Assert.That(rect.Height != 0);
			}

			HasValidSize("image01");
			HasValidSize("image02");
			HasValidSize("image03");
			HasValidSize("image04");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Algmnt_Inf_Horizontal()
		{
			RunScreenShots_Image_Alignment_page("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.Image_Stretch_Algmnt_Inf_Horizontal");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Algmnt_Inf_Vertical()
		{
			RunScreenShots_Image_Alignment_page("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.Image_Stretch_Algmnt_Inf_Vertical");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Alignment_Bigger()
		{
			RunScreenShots_Image_Alignment_page("Uno.UI.Samples.UITests.Image.Image_Stretch_Alignment_Bigger");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Alignment_Equal()
		{
			RunScreenShots_Image_Alignment_page("Uno.UI.Samples.UITests.Image.Image_Stretch_Alignment_Equal");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Alignment_SizeOnControl()
		{
			RunScreenShots_Image_Alignment_page("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.Image_Stretch_Alignment_SizeOnControl");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Alignment_Smaller()
		{
			RunScreenShots_Image_Alignment_page("Uno.UI.Samples.UITests.Image.Image_Stretch_Alignment_Smaller");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Alignment_Taller()
		{
			RunScreenShots_Image_Alignment_page("Uno.UI.Samples.UITests.Image.Image_Stretch_Alignment_Taller");
		}

		[Test]
		[AutoRetry]
		public void Screenshots_Image_Stretch_Alignment_Wider()
		{
			RunScreenShots_Image_Alignment_page("Uno.UI.Samples.UITests.Image.Image_Stretch_Alignment_Wider");
		}

		private void RunScreenShots_Image_Alignment_page(string testName)
		{
			Run(testName, skipInitialScreenshot: true);

			var picker = _app.Marked("modesPicker");

			var currentModeButton = _app.Marked("currentMode");
			_app.WaitForElement(currentModeButton);
			_app.WaitForDependencyPropertyValue(currentModeButton, "Content", "00");

			for (var i = 0; i < 4; i++)
			{
				// Use SetDependencyPropertyValue instead of Tap for performance on iOS/Android (Tap takes multiple seconds to complete)
				picker.SetDependencyPropertyValue("Mode", "S" + i);
				_app.WaitForDependencyPropertyValue(currentModeButton, "Content", (i * 16).ToString("00"));

				TakeScreenshot("Mode-" + i);
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Images sometimes fail to load on iOS https://github.com/unoplatform/uno/issues/2295
		public void UniformToFill_Second_Load()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.Image_UseTargetSizeLate", skipInitialScreenshot: true);

			_app.WaitForElement("loadImageButton");

			_app.Tap("loadImageButton");

			_app.WaitForElement("secondControl");

			using var bmp = TakeScreenshot("After");

			var expectedRect = _app.GetPhysicalRect("simpleImage");
			var firstControlRect = _app.GetPhysicalRect("firstControl");
			var secondControlRect = _app.GetPhysicalRect("secondControl");

			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, firstControlRect, permittedPixelError: 20);
			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, secondControlRect, permittedPixelError: 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Images sometimes fail to load on iOS https://github.com/unoplatform/uno/issues/2295
		public void Late_With_Fixed_Dimensions()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.ImageWithLateSourceFixedDimensions");

			_app.Tap("setSource");

			_app.WaitForElement("lateImage");

			using var bmp = TakeScreenshot("Source set");

			var expectedRect = _app.GetPhysicalRect("refImage");

			var lateRect = _app.GetPhysicalRect("lateImage");

			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, lateRect, permittedPixelError: 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Images sometimes fail to load on iOS https://github.com/unoplatform/uno/issues/2295
		public void Late_With_UniformToFill()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.ImageWithLateSourceUniformToFill");

			_app.Tap("setSource");

			_app.WaitForElement("lateImage");

			using var bmp = TakeScreenshot("Source set");

			var borderThickness = LogicalToPhysical(3);

			var expectedRect = _app.GetPhysicalRect("refImage").DeflateBy(borderThickness);
			var lateRect = _app.GetPhysicalRect("lateImage").DeflateBy(borderThickness);

			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, lateRect, permittedPixelError: 20);
		}

		[Test]
		[AutoRetry]
		public void Large_Image_With_Margin()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.Image_Margin_Large");

			using var bmp = TakeScreenshot("Ready");

			var rect = _app.GetPhysicalRect("outerBorder");

			ImageAssert.DoesNotHaveColorAt(bmp, rect.CenterX, rect.CenterY, Color.Black);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void BitmapImage_vs_SvgImageSource_BitmapRemote()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.BitmapImage_vs_SvgImageSource");

			var url = _app.Marked("url");
			var btnBitmap = _app.Marked("btnBitmap");
			var streamMode = _app.Marked("streamMode");
			var showLow = _app.Marked("showLow");
			var img = _app.Marked("img");

			showLow.SetDependencyPropertyValue("IsCheked", "False");

			// Load image from url
			url.SetDependencyPropertyValue("Text", "https://uno-assets.platform.uno/tests/images/uno-overalls.png");
			streamMode.SetDependencyPropertyValue("IsChecked", "False");

			btnBitmap.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotDirect = TakeScreenshot("url_direct");

			streamMode.SetDependencyPropertyValue("IsChecked", "True");

			btnBitmap.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotStream = TakeScreenshot("url_stream");

			var rect = _app.GetPhysicalRect(img);

			// Both images should be the same
			ImageAssert.AreEqual(screenshotDirect, screenshotStream, rect, tolerance: PixelTolerance.Cummulative(2));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void BitmapImage_vs_SvgImageSource_SvgRemote()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.BitmapImage_vs_SvgImageSource");

			var url = _app.Marked("url");
			var btnSvg = _app.Marked("btnSvg");
			var streamMode = _app.Marked("streamMode");
			var showLow = _app.Marked("showLow");
			var img = _app.Marked("img");

			showLow.SetDependencyPropertyValue("IsCheked", "False");

			// Load image from url
			url.SetDependencyPropertyValue("Text", "https://uno-assets.platform.uno/tests/images/uno-overalls.svg");
			streamMode.SetDependencyPropertyValue("IsChecked", "False");

			btnSvg.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotDirect = TakeScreenshot("url_direct");

			streamMode.SetDependencyPropertyValue("IsChecked", "True");

			btnSvg.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotStream = TakeScreenshot("url_stream");

			var rect = _app.GetPhysicalRect(img);

			// Both images should be the same
			ImageAssert.AreEqual(screenshotDirect, screenshotStream, rect, tolerance: PixelTolerance.Cummulative(2));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void BitmapImage_vs_SvgImageSource_BitmapLocal()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.BitmapImage_vs_SvgImageSource");

			var url = _app.Marked("url");
			var btnBitmap = _app.Marked("btnBitmap");
			var streamMode = _app.Marked("streamMode");
			var showLow = _app.Marked("showLow");
			var img = _app.Marked("img");

			showLow.SetDependencyPropertyValue("IsCheked", "False");

			// Load image from url
			url.SetDependencyPropertyValue("Text", "ms-appx:///Assets/Formats/uno-overalls.png");
			streamMode.SetDependencyPropertyValue("IsChecked", "False");

			btnBitmap.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotDirect = TakeScreenshot("url_local");

			url.SetDependencyPropertyValue("Text", "https://uno-assets.platform.uno/tests/images/uno-overalls.png");

			btnBitmap.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotStream = TakeScreenshot("url_remote");

			var rect = _app.GetPhysicalRect(img);

			// Both images should be the same
			ImageAssert.AreEqual(screenshotDirect, screenshotStream, rect, tolerance: PixelTolerance.Exclusive(12));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void BitmapImage_vs_SvgImageSource_SvgLocal()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.BitmapImage_vs_SvgImageSource");

			var url = _app.Marked("url");
			var btnSvg = _app.Marked("btnSvg");
			var streamMode = _app.Marked("streamMode");
			var showLow = _app.Marked("showLow");
			var img = _app.Marked("img");

			showLow.SetDependencyPropertyValue("IsCheked", "False");

			// Load image from url
			url.SetDependencyPropertyValue("Text", "ms-appx:///Assets/Formats/uno-overalls.svg");
			streamMode.SetDependencyPropertyValue("IsChecked", "False");

			btnSvg.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotDirect = TakeScreenshot("url_local");

			url.SetDependencyPropertyValue("Text", "https://uno-assets.platform.uno/tests/images/uno-overalls.svg");

			btnSvg.FastTap();

			WaitForBitmapOrSvgLoaded();

			using var screenshotStream = TakeScreenshot("url_remote");

			var rect = _app.GetPhysicalRect(img);

			// Both images should be the same
			ImageAssert.AreEqual(screenshotDirect, screenshotStream, rect, tolerance: PixelTolerance.Exclusive(12));
		}

		[Test]
		[AutoRetry]
		public void Image_Invalid()
		{
			Run("Uno.UI.Samples.UITests.ImageTests.Image_Invalid");

			var panel = _app.Marked("ComparePanel");
			var button = _app.Marked("HideButton");
			var physicalRect = _app.GetPhysicalRect(panel);

			// Copy of the rect, as the panel will be hidden, so the X & Y coords would become negative
			var originalRect = new AppRect(physicalRect.X, physicalRect.Y, physicalRect.Width, physicalRect.Height);

			using var beforeHide = TakeScreenshot("image_invalid_before_hide");

			button.FastTap();

			using var afterHide = TakeScreenshot("image_invalid_after_hide");

			ImageAssert.AreEqual(afterHide, beforeHide, originalRect, tolerance: PixelTolerance.Exclusive(1));
		}

		// Can be removed when #10340 is done (in favor of When_Image_Source_Nullify runtime test).
		[Test]
		[AutoRetry]
		public void Image_Source_Nullify()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.Image_Source_Nullify");

			var panel = _app.Marked("CompareGrid");
			var loadButton = _app.Marked("LoadButton");
			var clearButton = _app.Marked("ClearButton");

			var physicalRect = _app.GetPhysicalRect(panel);

			using var beforeLoad = TakeScreenshot("image_source_nullify_empty");

			loadButton.FastTap();

			using var afterLoad = TakeScreenshot("image_source_nullify_loaded");

			ImageAssert.AreNotEqual(beforeLoad, afterLoad, physicalRect);

			clearButton.FastTap();

			using var afterClear = TakeScreenshot("image_source_nullify_cleared");

			ImageAssert.AreEqual(beforeLoad, afterClear, physicalRect, tolerance: PixelTolerance.Exclusive(1));
		}

		private void WaitForBitmapOrSvgLoaded()
		{
			var isLoaded = _app.Marked("isLoaded");
			var imgIsLoaded = _app.Marked("imgIsLoaded");
			var loadTimeout = TimeSpan.FromSeconds(10);

			bool Predicate()
			{
				return isLoaded.GetDependencyPropertyValue<bool>("IsChecked")
					   || imgIsLoaded.GetDependencyPropertyValue<bool>("IsChecked");
			}

			_app.WaitFor(Predicate, timeout: loadTimeout);

			Thread.Sleep(350);
		}
	}
}
