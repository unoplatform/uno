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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.CanvasTests
{
	public class Canvas_Measurement_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Measur_CanvasChildren()
		{
			Run("SamplesApp.Windows_UI_Xaml_Controls.Canvas.Measure_Children_In_Canvas");

			_app.WaitForElement("BBorderInCanvas");
			_app.WaitForElement("BBorderOutCanvas");

			var BorderInWidth = _app.Query(_app.Marked("BBorderInCanvas")).First().Rect.Width;
			var BorderInHeight = _app.Query(_app.Marked("BBorderInCanvas")).First().Rect.Height;

			var BorderOutWidth = _app.Query(_app.Marked("BBorderOutCanvas")).First().Rect.Width;
			var BorderOutHeight = _app.Query(_app.Marked("BBorderOutCanvas")).First().Rect.Height;

			if (BorderInWidth != BorderOutWidth || BorderInHeight != BorderOutHeight)
				Assert.Fail("Border in canvas measurement failed");

			TakeScreenshot($"Measure_Children_In_Canvas - Measure Border");
		}

		[Test]
		[AutoRetry]
		public void Verify_Canvas_With_Outer_Clip()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Canvas.Canvas_With_Outer_Clip");

			var screenshot = TakeScreenshot("Rendered");

			var clippedLocation = _app.GetRect("LocatorBorder1");

			ImageAssert.HasColorAt(screenshot, clippedLocation.CenterX, clippedLocation.CenterY, Color.Red);

			var unclippedLocation = _app.GetRect("LocatorBorder2");

			ImageAssert.HasColorAt(screenshot, unclippedLocation.CenterX, unclippedLocation.CenterY, Color.Blue);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // Canvas.ZIndex isn't implemented for WASM yet
		public void Verify_Canvas_ZIndex()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Canvas.Canvas_ZIndex");

			var screenshot = TakeScreenshot("Rendered");

			var redBorderRect = _app.GetRect("CanvasBorderRed");

			ImageAssert.HasColorAt(screenshot, redBorderRect.CenterX, redBorderRect.CenterY, Color.Green /*psych*/);

			var greenBorderRect = _app.GetRect("CanvasBorderGreen");

			ImageAssert.HasColorAt(screenshot, greenBorderRect.CenterX, greenBorderRect.CenterY, Color.Blue);
		}
	}
}
