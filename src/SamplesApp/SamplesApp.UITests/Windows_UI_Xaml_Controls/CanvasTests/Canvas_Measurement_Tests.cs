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
using Uno.UITests.Helpers;

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
		public void Verify_Canvas_ZIndex()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Canvas.Canvas_ZIndex");

			var screenshot = TakeScreenshot("Rendered");

			var redBorderRect1 = _app.GetRect("CanvasBorderRed1");
			ImageAssert.HasColorAt(screenshot, redBorderRect1.CenterX, redBorderRect1.CenterY, Color.Green /*psych*/);
			var redBorderRect2 = _app.GetRect("CanvasBorderRed2");
			ImageAssert.HasColorAt(screenshot, redBorderRect2.CenterX, redBorderRect2.CenterY, Color.Green /*psych*/);

			if (AppInitializer.GetLocalPlatform() != Platform.Android) // Android doesn't support Canvas.ZIndex on any panel
			{
				var redBorderRect3 = _app.GetRect("CanvasBorderRed3");
				ImageAssert.HasColorAt(screenshot, redBorderRect3.CenterX, redBorderRect3.CenterY, Color.Green /*psych*/);
			}

			var greenBorderRect1 = _app.GetRect("CanvasBorderGreen1");
			ImageAssert.HasColorAt(screenshot, greenBorderRect1.CenterX, greenBorderRect1.CenterY, Color.Brown);
			ImageAssert.HasColorAt(screenshot, greenBorderRect1.Right - 1, greenBorderRect1.CenterY, Color.Blue);
			var greenBorderRect2 = _app.GetRect("CanvasBorderGreen2");
			ImageAssert.HasColorAt(screenshot, greenBorderRect2.CenterX, greenBorderRect2.CenterY, Color.Brown);
			ImageAssert.HasColorAt(screenshot, greenBorderRect2.Right-1, greenBorderRect2.CenterY, Color.Blue);

			if (AppInitializer.GetLocalPlatform() != Platform.Android) // Android doesn't support Canvas.ZIndex on any panel
			{
				var CanvasBorderGreen3 = _app.GetRect("CanvasBorderGreen3");
				ImageAssert.HasColorAt(screenshot, CanvasBorderGreen3.CenterX, CanvasBorderGreen3.CenterY, Color.Brown);
				ImageAssert.HasColorAt(screenshot, CanvasBorderGreen3.Right - 1, CanvasBorderGreen3.CenterY, Color.Blue);
			}
		}
	}
}
