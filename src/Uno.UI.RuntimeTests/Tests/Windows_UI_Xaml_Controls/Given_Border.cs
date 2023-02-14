using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation.Metadata;
using Uno.UI;
using static Private.Infrastructure.TestServices;
using Rectangle = System.Drawing.Rectangle;
using Windows.Media.Core;
using Uno.UI.RuntimeTests.Extensions;
using Microsoft.UI.Composition;
using System.IO;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Border
	{

		[TestMethod]
		public async Task Check_DataContext_Propagation()
		{
			var tb = new TextBlock();
			tb.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("TestText") });
			var SUT = new Border
			{
				Child = tb
			};

			var root = new Grid
			{
				DataContext = new MyContext()
			};

			root.Children.Add(SUT);

			WindowHelper.WindowContent = root;

			await WindowHelper.WaitFor(() => tb.Text == "Vampire squid");
		}

		private class MyContext
		{
			public string TestText => "Vampire squid";
		}

		[TestMethod]
		public async Task When_Border_Centered_With_Margin_Inside_Tall_Rectangle()
		{
			const int ContentHeight = 300;
			const int ContentMargin = 10;
			var content = new Border
			{
				Width = 300,
				Height = ContentHeight,
				Margin = new Thickness(ContentMargin),
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var SUT = new Border
			{
				Background = new SolidColorBrush(Colors.Pink),
				Width = 50,
				Height = double.NaN,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = content
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(content);

			const double ScrollViewerHeight = ContentHeight + 2 * ContentMargin;
			await WindowHelper.WaitForEqual(ScrollViewerHeight, () => SUT.ActualHeight);
		}

		[TestMethod]
		public async Task When_Border_Centered_With_Margin_Inside_Wide_Rectangle()
		{
			const int ContentWidth = 300;
			const int ContentMargin = 10;
			var content = new Border
			{
				Height = 300,
				Width = ContentWidth,
				Margin = new Thickness(ContentMargin),
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var SUT = new Border
			{
				Background = new SolidColorBrush(Colors.Pink),
				Height = 50,
				Width = double.NaN,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = content
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(content);

			const double ScrollViewerWidth = ContentWidth + 2 * ContentMargin;
			await WindowHelper.WaitForEqual(ScrollViewerWidth, () => SUT.ActualWidth);
		}

		[TestMethod]
		public async Task Check_CornerRadius_Border_Basic()
		{
			// Verify that border is drawn with the same thickness with/without CornerRadius
			const string white = "#FFFFFFFF";

#if __MACOS__
			Assert.Inconclusive(); // MACOS interpret colors differently
#endif
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border_CornerRadius();
			var result = await TakeScreenshot(SUT);
			var sample = SUT.GetRelativeCoords(SUT.Sample1);
			var eighth = sample.Width / 8;

			ImageAssert.HasPixels(
				result,
				ExpectedPixels.At(sample.X + eighth, sample.Y + eighth).Named("top left corner").WithPixelTolerance(2, 2).Pixel(white),
				ExpectedPixels.At(sample.Right - eighth, sample.Y + eighth).Named("top right corner").WithPixelTolerance(2, 2).Pixel(white),
				ExpectedPixels.At(sample.Right - eighth, sample.Bottom - eighth).Named("bottom right corner").WithPixelTolerance(2, 2).Pixel(white),
				ExpectedPixels.At(sample.X + eighth, sample.Bottom - eighth).Named("bottom left corner").WithPixelTolerance(2, 2).Pixel(white)
			);

#if __WASM__ && false // See https://github.com/unoplatform/uno/issues/5440 for the scenario being tested.
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

		[TestMethod]
		public async Task Border_CornerRadius_BorderThickness()
		{
			//Same colors but with the addition of a White background color underneath
			const string lightPink = "#FF7F7F";
			const string lightBlue = "#7F7FFF";

#if __MACOS__
			Assert.Inconclusive(); // MACOS interpret colors differently
#endif

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var expectedColors = new[]
			{
				new ExpectedColor { Thicknesses = new [] { 10, 10, 10, 10 }, Colors = new [] { lightPink, lightPink, lightPink, lightPink } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 10, 10 }, Colors = new [] { lightPink, lightBlue, lightPink, lightPink } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 0, 10 }, Colors = new [] { lightPink, lightBlue, lightBlue, lightPink } },
				new ExpectedColor { Thicknesses = new [] { 10, 0, 0, 0 }, Colors = new [] { lightPink, lightBlue, lightBlue, lightBlue } },
				new ExpectedColor { Thicknesses = new [] { 0, 0, 0, 0 }, Colors = new [] { lightBlue, lightBlue, lightBlue, lightBlue } },
			};

			var SUT = new BorderCornerRadiusBorderThickness();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			await WindowHelper.WaitForIdle();

			foreach (var expected in expectedColors)
			{
				Thickness t = new Thickness(expected.Thicknesses[0], expected.Thicknesses[1], expected.Thicknesses[2], expected.Thicknesses[3]);
				SUT.MyBorder.BorderThickness = t;
				await WindowHelper.WaitForIdle();
				var snapshot = await TakeScreenshot(SUT);

				var leftTarget = SUT.GetRelativeCoords(SUT.LeftTarget);
				var topTarget = SUT.GetRelativeCoords(SUT.TopTarget);
				var rightTarget = SUT.GetRelativeCoords(SUT.RightTarget);
				var bottomTarget = SUT.GetRelativeCoords(SUT.BottomTarget);
				var centerTarget = SUT.GetRelativeCoords(SUT.CenterTarget);

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

		[TestMethod]
		public async Task Border_CornerRadius_Clipping()
		{
			const string red = "#FFFF0000";

#if __MACOS__
			Assert.Inconclusive(); //MACOS interprets colors differently
#endif

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border_CornerRadius_Clipping();
			var snapshot = await TakeScreenshot(SUT);

			var topLeftTarget = SUT.GetRelativeCoords(SUT.TopLeftTarget);
			var topRightTarget = SUT.GetRelativeCoords(SUT.TopRightTarget);
			var bottomLeftTarget = SUT.GetRelativeCoords(SUT.BottomLeftTarget);
			var bottomRightTarget = SUT.GetRelativeCoords(SUT.BottomRightTarget);

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

		[TestMethod]
		public async Task Border_CornerRadius_Content_Clipping()
		{
			const string blue = "#FF0000FF";

#if __MACOS__
			Assert.Inconclusive(); //MACOS interprets colors differently
#endif

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border() { Background = new SolidColorBrush(Colors.Blue) };
			var inner = new Border() { CornerRadius = CornerRadiusHelper.FromUniformRadius(40), Width = 80, Height = 80 };
			inner.Child = new Microsoft.UI.Xaml.Shapes.Rectangle() { Fill = new SolidColorBrush(Colors.Red), Width = 80, Height = 80 };
			SUT.Child = inner;

			var snapshot = await TakeScreenshot(SUT);

			ImageAssert.HasPixels(
				snapshot,
				ExpectedPixels
					.At($"top-left", 4, 4)
					.WithPixelTolerance(1, 1)
					.Pixel(blue),
				ExpectedPixels
					.At($"top-right", 76, 4)
					.WithPixelTolerance(1, 1)
					.Pixel(blue),
				ExpectedPixels
					.At($"bottom-left", 4, 76)
					.WithPixelTolerance(1, 1)
					.Pixel(blue),
				ExpectedPixels
					.At($"bottom-right", 76, 76)
					.WithPixelTolerance(1, 1)
					.Pixel(blue)
			);
		}

		[TestMethod]
		public async Task Border_LinearGradient()
		{
#if __MACOS__
			Assert.Inconclusive(); // MACOS interpret colors differently
#endif
#if __IOS__
			Assert.Inconclusive(); // iOS not working currently. https://github.com/unoplatform/uno/issues/6749
#endif
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border_LinearGradientBrush();
			var screenshot = await TakeScreenshot(SUT);
			var textBoxRect = SUT.GetRelativeCoords(SUT.MyTextBox);

			ImageAssert.HasColorAt(screenshot, textBoxRect.CenterX + (float)(0.45 * textBoxRect.Width), textBoxRect.Y, "#1F00E0", tolerance: 20);

			// The color near the start is reddish.
			ImageAssert.HasColorAt(screenshot, textBoxRect.CenterX - (float)(0.45 * textBoxRect.Width), textBoxRect.Y, "#FF0000", tolerance: 20);
		}

		[TestMethod]
		public async Task Border_CornerRadius_GradientBrush()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}
			var SUT = new Border_CornerRadius_Gradient();
			var result = await TakeScreenshot(SUT);
			var textBoxRect = SUT.GetRelativeCoords(SUT.RedBorder);
			ImageAssert.HasColorAt(result, textBoxRect.CenterX, textBoxRect.CenterY, "#FF00FF00", tolerance: 10);
		}

		[TestMethod]
		public async Task Border_AntiAlias()
		{

#if !__ANDROID__
			Assert.Inconclusive();
#endif
			const string secondRectBlueish = "#ff9e9eff";

			var SUT = new Border_AntiAlias();
			WindowHelper.WindowContent = SUT;
			var screenshot = await TakeScreenshot(SUT);

			var firstBorderRect = SUT.GetRelativeCoords(SUT.FirstBorder);
			var secondBorderRect = SUT.GetRelativeCoords(SUT.SecondBorder);
			var rect = new Rectangle((int)firstBorderRect.X, (int)firstBorderRect.Y,
				(int)firstBorderRect.Width + 1, (int)firstBorderRect.Height + 1);

			await WindowHelper.WaitForIdle();

			ImageAssert.HasColorInRectangle(
					screenshot,
					rect,
					 Windows.UI.Color.FromArgb(255, 216, 216, 255),
					 tolerance: 20
					);

			ImageAssert.HasPixels(
					screenshot,
					ExpectedPixels
						.At($"top-left", secondBorderRect.X, secondBorderRect.Y)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish)
						.WithColorTolerance(15),
					ExpectedPixels
						.At($"top-right", secondBorderRect.Right, secondBorderRect.Y)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish)
						.WithColorTolerance(15),
					ExpectedPixels
						.At($"bottom-right", secondBorderRect.Right, secondBorderRect.Bottom)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish)
						.WithColorTolerance(15),
					ExpectedPixels
						.At($"bottom-left", secondBorderRect.X, secondBorderRect.Bottom)
						.WithPixelTolerance(1, 1)
						.Pixel(secondRectBlueish)
						.WithColorTolerance(15)
				);
		}
		private struct ExpectedColor
		{
			public int[] Thicknesses { get; set; }

			public string[] Colors { get; set; }

			public override string ToString() => $"{Thicknesses[0]} {Thicknesses[1]} {Thicknesses[2]} {Thicknesses[3]}";
		}

		private async Task<RawBitmap> TakeScreenshot(FrameworkElement SUT)
		{
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			var renderer = new RenderTargetBitmap();
			await WindowHelper.WaitForIdle();
			await renderer.RenderAsync(SUT);
			var result = await RawBitmap.From(renderer, SUT);
			await WindowHelper.WaitForIdle();
			return result;
		}
	}
}
