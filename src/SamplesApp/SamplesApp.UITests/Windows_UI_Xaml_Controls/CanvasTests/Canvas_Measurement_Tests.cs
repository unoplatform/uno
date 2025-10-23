using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.CanvasTests
{
	public partial class Canvas_Measurement_Tests : SampleControlUITestBase
	{
		// The test Measur_CanvasChildren have been moved to Runtime Test
		// src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Control/Given_Canvas_Meqsurement.cs

		// For other platforms than Browser all the tests are also tested in RuntimeTests
		// These UI Test can be deleted once we can test Wasm in RuntimeTests.
		// This is currently blocked due to lack of support for RenderTargetBitmap on Wasm.
		[ActivePlatforms(Platform.Browser)]
		[Test]
		[AutoRetry]
		public void Measur_CanvasChildren()
		{
			Run("SamplesApp.Windows_UI_Xaml_Controls.Canvas.Measure_Children_In_Canvas");

			_app.WaitForElement("BBorderInCanvas");

			var inRect = _app.GetLogicalRect("BBorderInCanvas");
			var outRect = _app.GetLogicalRect("BBorderOutCanvas");

			using var _ = new AssertionScope();

			inRect.Width.Should().Be(outRect.Width, "Border in canvas measurement failed");
			inRect.Height.Should().Be(outRect.Height, "Border in canvas measurement failed");
		}

		[ActivePlatforms(Platform.Browser)]
		[Test]
		[AutoRetry]
		public void Verify_Canvas_With_Outer_Clip()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Canvas.Canvas_With_Outer_Clip");

			using var screenshot = TakeScreenshot("Rendered");

			var clippedLocation = _app.GetPhysicalRect("LocatorBorder1");

			ImageAssert.HasColorAt(screenshot, clippedLocation.CenterX, clippedLocation.CenterY, Color.Red);

			var unclippedLocation = _app.GetPhysicalRect("LocatorBorder2");

			ImageAssert.HasColorAt(screenshot, unclippedLocation.CenterX, unclippedLocation.CenterY, Color.Blue);
		}
		[ActivePlatforms(Platform.Browser)]
		[Test]
		[AutoRetry]
		public void Verify_Canvas_ZIndex()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Canvas.Canvas_ZIndex");

			using var screenshot = TakeScreenshot("Rendered");

			var redBorderRect1 = _app.GetPhysicalRect("CanvasBorderRed1");
			ImageAssert.HasColorAt(screenshot, redBorderRect1.CenterX, redBorderRect1.CenterY, Color.Green /*psych*/);
			var redBorderRect2 = _app.GetPhysicalRect("CanvasBorderRed2");
			ImageAssert.HasColorAt(screenshot, redBorderRect2.CenterX, redBorderRect2.CenterY, Color.Green /*psych*/);

			if (AppInitializer.GetLocalPlatform() != Platform.Android) // Android doesn't support Canvas.ZIndex on any panel
			{
				var redBorderRect3 = _app.GetPhysicalRect("CanvasBorderRed3");
				ImageAssert.HasColorAt(screenshot, redBorderRect3.CenterX, redBorderRect3.CenterY, Color.Green /*psych*/);
			}

			var greenBorderRect1 = _app.GetPhysicalRect("CanvasBorderGreen1");
			ImageAssert.HasColorAt(screenshot, greenBorderRect1.CenterX, greenBorderRect1.CenterY, Color.Brown);
			ImageAssert.HasColorAt(screenshot, greenBorderRect1.Right - 1, greenBorderRect1.CenterY, Color.Blue);
			var greenBorderRect2 = _app.GetPhysicalRect("CanvasBorderGreen2");
			ImageAssert.HasColorAt(screenshot, greenBorderRect2.CenterX, greenBorderRect2.CenterY, Color.Brown);
			ImageAssert.HasColorAt(screenshot, greenBorderRect2.Right - 1, greenBorderRect2.CenterY, Color.Blue);

			if (AppInitializer.GetLocalPlatform() != Platform.Android) // Android doesn't support Canvas.ZIndex on any panel
			{
				var CanvasBorderGreen3 = _app.GetPhysicalRect("CanvasBorderGreen3");
				ImageAssert.HasColorAt(screenshot, CanvasBorderGreen3.CenterX, CanvasBorderGreen3.CenterY, Color.Brown);
				ImageAssert.HasColorAt(screenshot, CanvasBorderGreen3.Right - 1, CanvasBorderGreen3.CenterY, Color.Blue);
			}
		}

		[ActivePlatforms(Platform.Browser)]
		[Test]
		[AutoRetry]
		public void Verify_Canvas_In_Canvas()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Canvas.Canvas_In_Canvas");

			using var screenshot = TakeScreenshot("Rendered");

			// All is required for Android to find zero-sized elements.
			var clippedLocation = _app.GetPhysicalRect(q => q.All().Marked("CanvasBorderBlue1"));

			ImageAssert.HasColorAt(screenshot, clippedLocation.CenterX, clippedLocation.CenterY, Color.Blue);
		}

	}
}
