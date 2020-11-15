using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Toolkit
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void ElevatedView_Dimensions_Validation()
		{
			Run("UITests.Toolkit.ElevatedView_Dimensions");

			_app.WaitForElement("elevatedViewText");

			var elevatedView = _app.Marked("elevatedView");

			using (new AssertionScope())
			{
				elevatedView.FirstResult().Rect.X.Should().Be(42f);
				elevatedView.FirstResult().Rect.Y.Should().Be(98f);
				elevatedView.FirstResult().Rect.Width.Should().Be(200f);
				elevatedView.FirstResult().Rect.Height.Should().Be(160f);
			}
		}

		[Test]
		[AutoRetry]
		public void ElevatedView_Corners_Validation()
		{
			Run("UITests.Toolkit.ElevatedView_Corners");

			_app.WaitForElement("Elevation");

			var elevationRect = _app.GetPhysicalRect("Elevation");

			using var snapshot = this.TakeScreenshot("check", ignoreInSnapshotCompare: true);

			const string white = "#FFFFFF";
			const string gray = "#A9A9A9"; // DarkGray
			const string pink = "#FFE0E0"; // Red shadow mixed with white background

			ImageAssert.HasPixels(
				snapshot,
				ExpectedPixels
					.At("top-middle", elevationRect.CenterX, elevationRect.Y)
					.WithPixelTolerance(1, 1)
					.Pixel(gray),
				ExpectedPixels
					.At("bottom-middle", elevationRect.CenterX, elevationRect.Bottom)
					.WithPixelTolerance(1, 1)
					.Pixel(gray),
				ExpectedPixels
					.At("left-middle", elevationRect.X, elevationRect.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(gray),
				ExpectedPixels
					.At("right-middle", elevationRect.Right, elevationRect.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(gray),
				ExpectedPixels
					.At("right-middle-shadow", elevationRect.Right + 1, elevationRect.CenterY)
					.WithPixelTolerance(5, 1)
					.WithColorTolerance(22)
					.Pixel(pink),
				ExpectedPixels
					.At("bottom-middle-shadow", elevationRect.CenterX, elevationRect.Bottom + 1)
					.WithPixelTolerance(1, 5)
					.WithColorTolerance(22)
					.Pixel(pink),
				ExpectedPixels
					.At("top-left", elevationRect.X, elevationRect.Y)
					.WithPixelTolerance(1, 1)
					.Pixel(white),
				ExpectedPixels
					.At("top-right", elevationRect.Right, elevationRect.Y)
					.WithPixelTolerance(1, 1)
					.Pixel(white),
				ExpectedPixels
					.At("bottom-left", elevationRect.X, elevationRect.Bottom)
					.WithPixelTolerance(1, 1)
					.Pixel(white),
				ExpectedPixels
					.At("bottom-right", elevationRect.Right, elevationRect.Bottom)
					.WithPixelTolerance(1, 1)
					.Pixel(white)
			);
		}
	}
}
