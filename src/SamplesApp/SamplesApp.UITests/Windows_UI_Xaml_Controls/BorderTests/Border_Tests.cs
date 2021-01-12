using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.BorderTests
{
	public class Border_Tests : SampleControlUITestBase
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
		public void Check_CornerRadius_Border()
		{
			// Verify that border is drawn with the same thickness with/without CornerRadius
			Run("UITests.Shared.Windows_UI_Xaml_Controls.BorderTests.Border_CornerRadius_Toggle");

			_app.WaitForElement("SubjectBorder");

			var verificationRect = _app.GetRect("SnapshotBorder");

			using var scrBefore = TakeScreenshot("No CornerRadius");

			_app.FastTap("ToggleCornerRadiusButton");

			_app.WaitForText("StatusTextBlock", "5");

			using var scrAfter = TakeScreenshot("CornerRadius=5");

			ImageAssert.AreAlmostEqual(scrBefore, scrAfter, verificationRect, permittedPixelError: 5);
		}

		[Test]
		[AutoRetry]
		public void Border_CornerRadius_BorderThickness()
		{
			const string red = "#FF0000";
			const string blue = "#0000FF";

			var expectedColors = new[]
			{
				new ExpectedColor { Thicknesses = new [] { 10, 10, 10, 10 }, Colors = new [] { red, red, red, red } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 10, 10 }, Colors = new [] { red, blue, red, red } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 0, 10 }, Colors = new [] { red, blue, blue, red } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 0, 0 }, Colors = new [] { red, blue, blue, blue } },
				new ExpectedColor { Thicknesses = new [] { 0, 0, 0, 0 }, Colors = new [] { blue, blue, blue, blue } },
			};

			Run("UITests.Windows_UI_Xaml_Controls.BorderTests.Border_CornerRadius_BorderThickness");

			_app.WaitForElement("MyBorder");

			var leftTarget = _app.GetPhysicalRect("LeftTarget");
			var topTarget = _app.GetPhysicalRect("TopTarget");
			var rightTarget = _app.GetPhysicalRect("RightTarget");
			var bottomTarget = _app.GetPhysicalRect("BottomTarget");

			SetBorderProperty("MyBorder", "CornerRadius", "10");

			foreach (var expected in expectedColors)
			{
				SetBorderProperty("MyBorder", "BorderThickness", expected.ToString());

				using var snapshot = TakeScreenshot($"Border-CornerRadius-10-BorderThickness-{expected}");

				ImageAssert.HasPixels(
					snapshot,
					ExpectedPixels
						.At($"left-{expected}-progress", leftTarget.CenterX, leftTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[0]),
					ExpectedPixels
						.At($"top-{expected}-progress", topTarget.CenterX, topTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[1]),
					ExpectedPixels
						.At($"right-{expected}-progress", rightTarget.CenterX, rightTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[2]),
					ExpectedPixels
						.At($"bottom-{expected}-progress", bottomTarget.CenterX, bottomTarget.CenterY)
						.WithPixelTolerance(1, 1)
						.Pixel(expected.Colors[3])
				);
			}
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
