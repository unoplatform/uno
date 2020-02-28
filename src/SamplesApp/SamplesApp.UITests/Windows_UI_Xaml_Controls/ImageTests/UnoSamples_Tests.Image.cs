using System;
using System.Collections.Generic;
using System.Drawing;
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
		public void WriteableBitmap_Invalidate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.ImageSourceWriteableBitmapInvalidate");

			// Request to render
			var button = _app.Marked("_update");
			_app.WaitForElement(button);
			button.FastTap();

			// Take screenshot
			TakeScreenshot("WriteableBitmap_Invalidate - Result");
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

				base.TakeScreenshot("Mode-" + i);
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

			var bmp = TakeScreenshot("After");

			var expectedRect = _app.GetRect("simpleImage");
			var firstControlRect = _app.GetRect("firstControl");
			var secondControlRect = _app.GetRect("secondControl");

			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, firstControlRect, permittedPixelError: 20);
			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, secondControlRect, permittedPixelError: 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Images sometimes fail to load on iOS https://github.com/unoplatform/uno/issues/2295
		public void Late_With_Fixed_Dimensions()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.ImageWithLateSourceFixedDimensions");

			_app.Tap("setSource");

			_app.WaitForElement("lateImage");

			var bmp = TakeScreenshot("Source set");

			var expectedRect = _app.GetRect("refImage");

			var lateRect = _app.GetRect("lateImage");

			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, lateRect, permittedPixelError: 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Images sometimes fail to load on iOS https://github.com/unoplatform/uno/issues/2295
		public void Late_With_UniformToFill()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.ImageWithLateSourceUniformToFill");

			_app.Tap("setSource");

			_app.WaitForElement("lateImage");

			var bmp = TakeScreenshot("Source set");

			var expectedRect = _app.GetRect("refImage");

			var lateRect = _app.GetRect("lateImage");

			ImageAssert.AreAlmostEqual(bmp, expectedRect, bmp, lateRect, permittedPixelError: 20);
		}

		[Test]
		[AutoRetry]
		public void Large_Image_With_Margin()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ImageTests.Image_Margin_Large");

			var bmp = TakeScreenshot("Ready");

			var rect = _app.GetRect("outerBorder");

			ImageAssert.DoesNotHaveColorAt(bmp, rect.CenterX, rect.CenterY, Color.Black);
		}
	}
}
