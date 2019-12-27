using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Toolkit
{

	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1635
		public void Elevation_Validation()
		{
			Run("UITests.Shared.Toolkit.Elevation");

			_app.WaitForElement(_app.Marked("TurnElevation_ON_Button"));

			var turnElevation_ON_Button = _app.Marked("TurnElevation_ON_Button");
			
			// Take ScreenShot with no elevation
			var screenshot_NoElevation = TakeScreenshot("Elevation - No Elevation");

			turnElevation_ON_Button.Tap();

			// Take ScreenShot of with elevation
			var screenshot_WithElevation = TakeScreenshot("Elevation - With Elevation");

			Bitmap img1 = new Bitmap(screenshot_NoElevation.ToString());
			Bitmap img2 = new Bitmap(screenshot_WithElevation.ToString());

			float diffPercentage = 0;
			float diff = 0;

			if (img1.Size != img2.Size)
			{
				throw new Exception("Images are of different sizes");
			}
			else
			{
				for (int x = 0; x < img1.Width; x++)
				{
					for (int y = 0; y < img1.Height; y++)
					{
						Color img1P = img1.GetPixel(x, y);
						Color img2P = img2.GetPixel(x, y);

						diff += Math.Abs(img1P.R - img2P.R);
						diff += Math.Abs(img1P.G - img2P.G);
						diff += Math.Abs(img1P.B - img2P.B);
					}
				}

				diffPercentage = 100 * (diff / 255) / (img1.Width * img1.Height * 3);
				if (diffPercentage < 0.5)
				{
					Assert.Fail("Images are the same");
				}
			}
		}
	}
}
