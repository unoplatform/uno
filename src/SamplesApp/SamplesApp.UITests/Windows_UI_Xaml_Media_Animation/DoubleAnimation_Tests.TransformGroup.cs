using System;
using System.Drawing;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Animation
{
	[TestFixture]
	public partial class DoubleAnimation_Tests : SampleControlUITestBase
	{
		private const string _transformGroupTestControl = "UITests.Windows_UI_Xaml_Media_Animation.DoubleAnimation_TransformGroup";

		private const string color1 = "#FF0000";
		private const string color2 = "#FF8000";
		private const string color3 = "#FFFF00";
		private const string color4 = "#008000";
		private const string color5 = "#0000FF";
		private const string color6 = "#A000C0";
		private const string bgColor = "#D3D3D3";
		private const string defaultColor = "#FFFFFF";

		private const int elementVirtualSize = 50;

		[Test]
		[AutoRetry]
		public void When_TransformGroup_Translate()
		{
			const string color = color1;
			const int trY = 50;

			var (host, scale, half, final) = BeginTransformGroupTest("TranslateHost");

			// "Half" is approximative, we only validate that the element has move bellow
			ImageAssert.HasPixels(
				half,

				ExpectedPixels
					.At("Top left", host.X - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, defaultColor},
						{ defaultColor, bgColor},
					}),

				ExpectedPixels
					.At("Top right", host.Right - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, defaultColor},
						{ bgColor, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ color, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ defaultColor, color},
					})
			);

			//// Complete animation
			var finalTrY = trY * scale;
			ImageAssert.HasPixels(
				final,

				ExpectedPixels
					.At("Top left", host.X - 1, host.Y + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, bgColor},
						{ defaultColor, color}
					}),

				ExpectedPixels
					.At("Top right", host.Right - 1, host.Y + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ bgColor, defaultColor},
						{ color, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ defaultColor, default},
					}),

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Bottom + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ default, defaultColor},
					})
			);
		}


		[Test]
		[AutoRetry]
		public void When_TransformGroup_Scale()
		{
			const string color = color2;
			const float scaleY = 2;

			var (host, scale, half, final) = BeginTransformGroupTest("ScaleHost");

			// "Half" is approximative, we only validate that the element is now flowing bellow
			ImageAssert.HasPixels(
				half,

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ defaultColor, color},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ color, defaultColor},
					})
			);

			// Assert the final state
			ImageAssert.HasPixels(
				final,

				ExpectedPixels
					.At("Top left", host.X - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ default, defaultColor},
						{ defaultColor, color}
					}),

				ExpectedPixels
					.At("Top right", host.Right - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, default},
						{ color, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Y + host.Height * scaleY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ defaultColor, default},
					}),

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Y + host.Height * scaleY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ default, defaultColor},
					})
			);
		}

		[Test]
		[AutoRetry]
		public void When_TransformGroup_Rotate()
		{
			const string color = color3;

			var (host, scale, half, final) = BeginTransformGroupTest("RotateHost");

			// "Half" is approximative, we only validate that the element is now flowing the bottom right
			ImageAssert.HasPixels(
				half,

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, color},
						{ color, color},
					})
			);

			// Assert the final state
			ImageAssert.HasPixels(
				final,

				ExpectedPixels
					.At("Top left", host.X - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, bgColor},
						{ defaultColor, color}
					}),

				ExpectedPixels
					.At("Top right", host.Right - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ bgColor, defaultColor},
						{ color, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom + host.Width - 1) // pi / 2, so add width
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ defaultColor, default},
					}),

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Bottom + host.Width - 1) // pi / 2, so add width
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ default, defaultColor},
					})
			);
		}

		[Test]
		[AutoRetry]
		public void When_TransformGroup_Skew()
		{
			const string color = color4;

			var (host, scale, half, final) = BeginTransformGroupTest("SkewHost");

			// "Half" is approximative, we only validate that the element is now flowing the bottom right
			ImageAssert.HasPixels(
				half,

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ color, defaultColor},
					})
			);

			// Assert the final state
			ImageAssert.HasPixels(
				final,

				ExpectedPixels
					.At("Top left", host.X - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, default, default, default, defaultColor},
						{ defaultColor, default, default, default, bgColor},
						{ defaultColor, default, default, default, default},
						{ defaultColor, default, default, default, default},
						{ defaultColor, color, default, default, default}
					}),

				ExpectedPixels
					.At("Top right", host.Right - 1, host.Y + host.Height - 3) // skew Y is pi / 2, so add height
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ bgColor, defaultColor},
						{ default, defaultColor},
						{ default, defaultColor},
						{ default, defaultColor},
						{ color, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom + host.Height - 1) // skew Y is pi / 2, so add height
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ defaultColor, default},
					})
					.WithColorTolerance(192), // known to be up to 128 ...

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Bottom - 3)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ defaultColor, default},
						{ defaultColor, default},
						{ defaultColor, default},
						{ defaultColor, defaultColor}
					})
			);
		}

		[Test]
		[AutoRetry]
		public void When_TransformGroup_Composite()
		{
			const string color = color5;
			const int trY = 50;

			var (host, scale, half, final) = BeginTransformGroupTest("CompositeHost");

			// "Half" is approximative, we only validate that the element has move bellow
			ImageAssert.HasPixels(
				half,

				ExpectedPixels
					.At("Top left", host.X - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, defaultColor},
						{ defaultColor, bgColor},
					}),

				ExpectedPixels
					.At("Top right", host.Right - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, defaultColor},
						{ bgColor, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ color, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Bottom - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ defaultColor, color},
					})
			);

			// Assert the final state
			var finalTrY = trY * scale;
			ImageAssert.HasPixels(
				final,

				ExpectedPixels
					.At("Top left", host.X - 1, host.Y + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, bgColor},
						{ defaultColor, color}
					}),

				ExpectedPixels
					.At("Top right", host.Right - 1, host.Y + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ bgColor, defaultColor},
						{ color, defaultColor},
					}),

				ExpectedPixels
					.At("Bottom right", host.Right - 1, host.Bottom + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ color, defaultColor},
						{ defaultColor, default},
					}),

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Bottom + finalTrY - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, color},
						{ default, defaultColor},
					})
			);
		}

		[Test]
		[AutoRetry]
		public void When_TransformGroup_Nested()
		{
			const string color = color6;

			var (host, scale, half, final) = BeginTransformGroupTest("CrazyHost");

			// "Half" is approximative, we only validate that the element is no longer in top left
			ImageAssert.HasPixels(
				half,

				ExpectedPixels
					.At("Bottom left", host.X - 1, host.Y - 1)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, defaultColor},
						{ defaultColor, bgColor},
					})
			);

			// Assert the final state
			ImageAssert.HasPixels(
				final,

				ExpectedPixels
					.At("Top left", host.X - 3, host.Bottom - 3)
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, default, default, default, default, bgColor},
						{ default, default, default, default, default, default},
						{ default, default, default, default, default, default},
						{ default, default, default, default, default, default},
						{ default, default, color, color, default, default}
					}),

				ExpectedPixels
					.At("Top right", (float)(host.X + host.Width * Math.Cos(Math.PI / 4) - 3), (float)(host.Y + host.Height * (1 + Math.Cos(Math.PI / 4)) - 3))
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ default, default, default, default, default, defaultColor},
						{ default, default, default, default, default, defaultColor},
						{ color, default, default, default, default, defaultColor},
						{ default, default, default, default, default, defaultColor},
						{ default, default, default, default, default, defaultColor}
					}),

				ExpectedPixels
					.At("Bottom right", host.X - 3, (float)(host.Bottom + host.Height * Math.Sqrt(2) - 3))
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ default, default, color, color, default, default},
						{ default, default, default, default, default, default},
						{ default, default, default, default, default, default},
						{ default, default, default, default, default, default},
						{ defaultColor, default, default, default, default, defaultColor}
					}),

				ExpectedPixels
					.At("Bottom left", (float)(host.X - host.Width * Math.Cos(Math.PI / 4) - 3), (float)(host.Y + host.Height * (1 + Math.Cos(Math.PI / 4)) - 3))
					.WithPixelTolerance(x: 1, y: 1)
					.Pixels(new[,]
					{
						{ defaultColor, default, default, default, default, default},
						{ defaultColor, default, default, default, default, default},
						{ defaultColor, default, default, default, default, color},
						{ defaultColor, default, default, default, default, default},
						{ defaultColor, default, default, default, default, default}
					})
			);
		}

		private (Rectangle host, float scale, ScreenshotInfo half, ScreenshotInfo final) BeginTransformGroupTest(string elementName)
		{
			Run(_transformGroupTestControl, skipInitialScreenshot: true);

			var status = _app.Marked("Status");
			var host = _app.GetRect(elementName);
			var scale = host.Height / elementVirtualSize;

			// Capture the original rect (updated on WASM when the element moves)
			var immutableHostRect = new Rectangle((int)host.X, (int)host.Y, (int)host.Width, (int)host.Height);

			// Run the first half of the animation
			_app.Marked("StartButton").Tap();
			_app.WaitForDependencyPropertyValue(status, "Text", "Paused");

			using var half = TakeScreenshot("half", ignoreInSnapshotCompare: true);

			// Complete the animation
			_app.Marked("ResumeButton").Tap();
			_app.WaitForDependencyPropertyValue(status, "Text", "Completed");

			using var final = TakeScreenshot("final", ignoreInSnapshotCompare: true);

			return (immutableHostRect, scale, half, final);
		}
	}
}
