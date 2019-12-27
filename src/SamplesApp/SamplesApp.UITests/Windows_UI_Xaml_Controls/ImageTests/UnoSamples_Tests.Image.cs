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
		[ActivePlatforms(Platform.iOS, Platform.Android, Platform.Browser)]
		public void WriteableBitmap_Invalidate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.ImageSourceWriteableBitmapInvalidate");

			// Request to render
			var button = _app.Marked("_update");
			_app.WaitForElement(button);
			button.Tap();

			// Take screenshot
			TakeScreenshot("WriteableBitmap_Invalidate - Result");
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
	}
}
