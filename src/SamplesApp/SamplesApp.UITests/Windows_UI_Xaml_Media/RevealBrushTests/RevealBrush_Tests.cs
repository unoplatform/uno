using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.RevealBrushTests
{
	[TestFixture]
	class RevealBrush_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_FallbackColor_Set()
		{
			Run("UITests.Windows_UI_Xaml_Media.BrushesTests.RevealBrush_Fallback");

			_app.WaitForElement("StatusTextBlock");

			var views = new[]
			{
				"RevealGrid",
				"RevealGridCR",
				"RevealBorder",
				"RevealBorderCR",
			};

			using var initial = TakeScreenshot("Initial");
			foreach (var view in views)
			{
				AssertHasColor(view, initial, Color.Orange);
			}

			_app.FastTap("ChangeColorButton");
			_app.WaitForText("StatusTextBlock", "Color changed");

			using var after = TakeScreenshot("After");
			foreach (var view in views)
			{
				AssertHasColor(view, after, Color.ForestGreen);
			}

			void AssertHasColor(string view, ScreenshotInfo screenshot, Color expectedColor)
			{
				var rect = _app.GetPhysicalRect(view);
				ImageAssert.HasColorAt(screenshot, rect.CenterX, rect.CenterY, expectedColor);
			}
		}
	}
}
