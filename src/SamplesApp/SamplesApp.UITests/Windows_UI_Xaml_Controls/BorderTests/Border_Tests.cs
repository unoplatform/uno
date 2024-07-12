using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SamplesApp.UITests._Utils;
using SamplesApp.UITests.TestFramework;
using SamplesApp.UITests.Windows_UI_Xaml_Controls.FrameworkElementTests;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.BorderTests
{
	public partial class Border_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Change_Manipulation_Property()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.BorderTests.Border_Clipped_Change_Property");

			_app.WaitForElement("ClippedBorder");

			using var before = TakeScreenshot("Before property change");

			_app.FastTap("ClippedBorder");

			_app.WaitForDependencyPropertyValue(_app.Marked("ClippedBorder"), "ManipulationMode", "None");

			using var after = TakeScreenshot("After property change");

			ImageAssert.AreEqual(before, after);
		}

		[Test]
		[AutoRetry]
		//For other platform the test have been moved to runtime
		//It will be moves to when an equivalent of TakesScreenshot exist for that target
		[ActivePlatforms(Platform.Browser)]
		public void Check_CornerRadius_Border_Basic()
		{
			const string white = "#FFFFFF";

			// Verify that border is drawn with CornerRadius
			Run("Uno.UI.Samples.UITests.BorderTestsControl.Border_CornerRadius", skipInitialScreenshot: true);

			using var result = TakeScreenshot("sample");
			var sample = _app.GetPhysicalRect("Sample1");
			var eighth = sample.Width / 8;


			ImageAssert.HasPixels(
				result,
				ExpectedPixels.At(sample.X + eighth, sample.Y + eighth).Named("top left corner").Pixel(white),
				ExpectedPixels.At(sample.Right - eighth, sample.Y + eighth).Named("top right corner").Pixel(white),
				ExpectedPixels.At(sample.Right - eighth, sample.Bottom - eighth).Named("bottom right corner").WithPixelTolerance(1, 1).Pixel(white),
				ExpectedPixels.At(sample.X + eighth, sample.Bottom - eighth).Named("bottom left corner").Pixel(white)
			);

#if __WASM__ // See https://github.com/unoplatform/uno/issues/5440 for the scenario being tested.
			var sample2 = _app.GetPhysicalRect("Sample2");

			var top = sample2.Y + 1;
			ImageAssert.HasColorAt(result, sample2.CenterX, top, Color.Red, tolerance: 20);
			ImageAssert.HasColorAt(result, sample2.CenterX + 25, top, Color.White);
			ImageAssert.HasColorAt(result, sample2.CenterX - 25, top, Color.White);

			var bottom = sample2.Bottom - 1;
			ImageAssert.HasColorAt(result, sample2.CenterX, bottom, Color.Red, tolerance: 20);
			ImageAssert.HasColorAt(result, sample2.CenterX + 20, bottom, Color.White);
			ImageAssert.HasColorAt(result, sample2.CenterX - 20, bottom, Color.White);

			var right = sample2.Right - 1;
			ImageAssert.HasColorAt(result, right, sample2.CenterY, Color.Red, tolerance: 20);
			ImageAssert.HasColorAt(result, right, sample2.CenterY - 10, Color.White);
			ImageAssert.HasColorAt(result, right, sample2.CenterY + 10, Color.White);

			var left = sample2.X + 2;
			ImageAssert.HasColorAt(result, left, sample2.CenterY, Color.Red, tolerance: 20);
			ImageAssert.HasColorAt(result, left, sample2.CenterY - 10, Color.White);
			ImageAssert.HasColorAt(result, left, sample2.CenterY + 10, Color.White);
#endif
		}

		[Test]
		[AutoRetry]
		public void Check_CornerRadius_Border()
		{
			// Verify that border is drawn with the same thickness with/without CornerRadius
			Run("UITests.Shared.Windows_UI_Xaml_Controls.BorderTests.Border_CornerRadius_Toggle");

			_app.WaitForElement("SubjectBorder");

			var verificationRect = _app.GetPhysicalRect("SnapshotBorder");

			using var scrBefore = TakeScreenshot("No CornerRadius");

			_app.FastTap("ToggleCornerRadiusButton");

			_app.WaitForText("StatusTextBlock", "5");

			using var scrAfter = TakeScreenshot("CornerRadius=5");

			ImageAssert.AreAlmostEqual(scrBefore, scrAfter, verificationRect, permittedPixelError: 5);
		}

		[Test]
		[AutoRetry]
		//For other platform the test have been moved to runtime
		//It will be moves to when an equivalent of TakesScreenshot exist for that target
		[ActivePlatforms(Platform.Browser)]
		public void Border_CornerRadius_BorderThickness()
		{
			// White Background color underneath
			const string white = "#FFFFFF";

			//Colors with 50% Opacity
			const string red50 = "#80FF0000";
			const string blue50 = "#800000FF";

			//Same colors but with the addition of a White background color underneath
			const string lightPink = "#FF7F7F";
			const string lightBlue = "#7F7FFF";

			var expectedColors = new[]
			{
				new ExpectedColor { Thicknesses = new [] { 10, 10, 10, 10 }, Colors = new [] { lightPink, lightPink, lightPink, lightPink } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 10, 10 }, Colors = new [] { lightPink, lightBlue, lightPink, lightPink } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 0, 10 }, Colors = new [] { lightPink, lightBlue, lightBlue, lightPink } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 0, 0 }, Colors = new [] { lightPink, lightBlue, lightBlue, lightBlue } },
				new ExpectedColor { Thicknesses = new [] { 0, 0, 0, 0 }, Colors = new [] { lightBlue, lightBlue, lightBlue, lightBlue } },
			};

			Run("UITests.Windows_UI_Xaml_Controls.BorderTests.Border_CornerRadius_BorderThickness");

			_app.WaitForElement("MyBackgroundUnderneath");

			SetBorderProperty("MyBackgroundUnderneath", "Background", white);

			_app.WaitForElement("MyBorder");

			var leftTarget = _app.GetPhysicalRect("LeftTarget");
			var topTarget = _app.GetPhysicalRect("TopTarget");
			var rightTarget = _app.GetPhysicalRect("RightTarget");
			var bottomTarget = _app.GetPhysicalRect("BottomTarget");
			var centerTarget = _app.GetPhysicalRect("CenterTarget");

			SetBorderProperty("MyBorder", "CornerRadius", "10");
			SetBorderProperty("MyBorder", "BorderBrush", red50);
			SetBorderProperty("MyBorder", "Background", blue50);

			foreach (var expected in expectedColors)
			{
				SetBorderProperty("MyBorder", "BorderThickness", expected.ToString());

				using var snapshot = TakeScreenshot($"Border-CornerRadius-10-BorderThickness-{expected}");

				ImageAssert.HasPixels(
					snapshot,
					ExpectedPixels
						.At($"left-{expected}", leftTarget.CenterX, leftTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[0]),
					ExpectedPixels
						.At($"top-{expected}", topTarget.CenterX, topTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[1]),
					ExpectedPixels
						.At($"right-{expected}", rightTarget.CenterX, rightTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[2]),
					ExpectedPixels
						.At($"bottom-{expected}", bottomTarget.CenterX, bottomTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[3]),
					ExpectedPixels
						.At($"center-{expected}", centerTarget.CenterX, centerTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(lightBlue)
				);
			}
		}

		[Test]
		[AutoRetry]
		//For other platform the test have been moved to runtime
		//It will be moves to when an equivalent of TakesScreenshot exist for that target
		[ActivePlatforms(Platform.Browser)]
		public void Border_CornerRadius_Clipping()
		{
			const string red = "#FF0000";

			Run("UITests.Windows_UI_Xaml_Controls.BorderTests.Border_CornerRadius_Clipping");

			_app.WaitForElement("MainBorder");

			var topLeftTarget = _app.GetPhysicalRect("TopLeftTarget");
			var topRightTarget = _app.GetPhysicalRect("TopRightTarget");
			var bottomLeftTarget = _app.GetPhysicalRect("BottomLeftTarget");
			var bottomRightTarget = _app.GetPhysicalRect("BottomRightTarget");

			using var snapshot = TakeScreenshot("Screenshot");

			ImageAssert.HasPixels(
				snapshot,
				ExpectedPixels
					.At($"top-left", topLeftTarget.CenterX, topLeftTarget.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At($"top-right", topRightTarget.CenterX, topRightTarget.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At($"bottom-left", bottomLeftTarget.CenterX, bottomLeftTarget.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At($"bottom-right", bottomRightTarget.CenterX, bottomRightTarget.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(red)
			);
		}

		[Test]
		[AutoRetry]
		//For other platform the test have been moved to runtime
		//It will be moves to when an equivalent of TakesScreenshot exist for that target
		[ActivePlatforms(Platform.Browser)]
		[Ignore("LinearGradientBrush doesn't work well when there is a CornerRadius")]
		public void Border_LinearGradient()
		{
			Run("UITests.Windows_UI_Xaml_Controls.BorderTests.Border_LinearGradientBrush");

			var textBoxRect = _app.GetPhysicalRect("MyTextBox");

			using var screenshot = TakeScreenshot("Screenshot");

			// The color near the end is blueish.
			ImageAssert.HasColorAt(screenshot, (float)(textBoxRect.CenterX + 0.45 * textBoxRect.Width), textBoxRect.Y, Color.FromArgb(31, 0, 224), tolerance: 20);

			// The color near the start is reddish.
			ImageAssert.HasColorAt(screenshot, (float)(textBoxRect.CenterX - 0.45 * textBoxRect.Width), textBoxRect.Y, Color.Red, tolerance: 20);
		}

		[Test]
		[AutoRetry]
		//For other platform the test have been moved to runtime
		//It will be moves to when an equivalent of TakesScreenshot exist for that target
		[ActivePlatforms(Platform.Browser)]
		public void Border_CornerRadius_GradientBrush()
		{
			Run("UITests.Windows_UI_Xaml_Controls.BorderTests.Border_CornerRadius_Gradient");

			var textBoxRect = _app.GetPhysicalRect("RedBorder");

			using var screenshot = TakeScreenshot("Screenshot");

			ImageAssert.HasColorAt(screenshot, textBoxRect.CenterX, textBoxRect.CenterY, Color.FromArgb(0, 255, 0));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void Border_AntiAlias()
		{
			const string firstRectBlueish = "#ff6262ff";
			const string secondRectBlueish = "#ff9e9eff";

			Run("UITests.Windows_UI_Xaml_Controls.BorderTests.BorderAntiAlias");

			var firstBorderRect = _app.GetPhysicalRect("firstBorder");
			var secondBorderRect = _app.GetPhysicalRect("secondBorder");

			using var screenshot = TakeScreenshot(nameof(Border_AntiAlias));

			ImageAssert.HasColorInRectangle(
					screenshot,
					firstBorderRect.ToRectangle(),
					ColorCodeParser.Parse(firstRectBlueish)
				);

			ImageAssert.HasPixels(
					screenshot,
					ExpectedPixels
						.At($"top-left", secondBorderRect.X, secondBorderRect.Y)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish),
					ExpectedPixels
						.At($"top-right", secondBorderRect.Right, secondBorderRect.Y)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish),
					ExpectedPixels
						.At($"bottom-right", secondBorderRect.Right, secondBorderRect.Bottom)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish),
					ExpectedPixels
						.At($"bottom-left", secondBorderRect.X, secondBorderRect.Bottom)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish)
				);
		}

		private void SetBorderProperty(string borderName, string propertyName, string value)
		{
			Console.WriteLine($"Setting {borderName}'s {propertyName} to {value}");
			var slider = _app.Marked(borderName);
			var _ = slider.SetDependencyPropertyValue(propertyName, value);
		}

		private struct ExpectedColor
		{
			public int[] Thicknesses { get; set; }

			public string[] Colors { get; set; }

			public override string ToString() => $"{Thicknesses[0]} {Thicknesses[1]} {Thicknesses[2]} {Thicknesses[3]}";
		}
	}
}
