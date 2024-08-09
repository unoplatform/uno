using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation.Metadata;
using Uno.UI;
using static Private.Infrastructure.TestServices;
using Windows.Media.Core;
using Uno.UI.RuntimeTests.Extensions;
using Windows.UI.Composition;
using System.IO;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;
using System.Numerics;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_Border
	{
		private partial class CustomControl : Control
		{
			public Border Child { get; set; }
			public Size AvailableSizePassedToMeasureOverride { get; private set; }
			public Size SizeReturnedFromMeasureOverride { get; private set; }
			public Size FinalSizePassedToArrangeOverride { get; private set; }
			public Size SizeReturnedFromArrangeOverride { get; private set; }

			protected override Size MeasureOverride(Size availableSize)
			{
				AvailableSizePassedToMeasureOverride = availableSize;
				Child.Measure(availableSize);
				return SizeReturnedFromMeasureOverride = Child.DesiredSize;
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				FinalSizePassedToArrangeOverride = finalSize;
				Child.Arrange(new(0, 0, finalSize.Width, finalSize.Height));
				return SizeReturnedFromArrangeOverride = new(Child.ActualSize.X, Child.ActualSize.Y);
			}
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
#if __ANDROID__ || __IOS__ || __MACOS__
		[Ignore("Layouter doesn't work properly")]
#endif
		public async Task Check_Border_Margin(bool useCustomControl)
		{
			double outerDimension = 300;
			double innerDimension = 50;
			double innerMargin = 30;
			double outerMargin = 2;

			var innerBorder = new Border
			{
				Width = innerDimension,
				Height = innerDimension,
				Margin = new Thickness(innerMargin),
				Background = new SolidColorBrush(Colors.Blue),
			};

			var customControl = new CustomControl
			{
				Child = innerBorder,
			};

			var outerBorder = new Border
			{
				Width = outerDimension,
				Height = outerDimension,
				Margin = new Thickness(outerMargin),
				Child = useCustomControl ? customControl : innerBorder,
				Background = new SolidColorBrush(Colors.Red),
			};

			WindowHelper.WindowContent = outerBorder;
			await WindowHelper.WaitForLoaded(outerBorder);

			var expectedDimensionInner = Math.Min(outerDimension, innerDimension + 2 * innerMargin);
			var expectedSizeInner = new Size(expectedDimensionInner, expectedDimensionInner);
			var expectedOffsetDimension = (outerDimension - innerDimension - 2 * innerMargin) / 2 + innerMargin;


			if (useCustomControl)
			{
				Assert.AreEqual(new Size(outerDimension, outerDimension), customControl.AvailableSizePassedToMeasureOverride);
				Assert.AreEqual(new Size(outerDimension, outerDimension), customControl.FinalSizePassedToArrangeOverride);
				Assert.AreEqual(expectedSizeInner, customControl.SizeReturnedFromMeasureOverride);
				Assert.AreEqual(new Size(innerDimension, innerDimension), customControl.SizeReturnedFromArrangeOverride);
				Assert.AreEqual(new Size(innerDimension, innerDimension), customControl.RenderSize);
				Assert.AreEqual(new Vector2((float)innerDimension, (float)innerDimension), customControl.ActualSize);

				// TODO: This assert currently fails.
				//Assert.AreEqual(new Vector3((float)expectedOffsetDimension, (float)expectedOffsetDimension, 0), customControl.ActualOffset);

				Assert.AreEqual(expectedSizeInner, customControl.DesiredSize);
				Assert.AreEqual(null, customControl.Clip);
			}

			// TODO: This assert currently fails.
			// var expectedInnerBorderOffset = useCustomControl ? innerMargin : expectedOffsetDimension;
			//Assert.AreEqual(new Vector3((float)expectedInnerBorderOffset, (float)expectedInnerBorderOffset, 0), innerBorder.ActualOffset);

			Assert.AreEqual(new Size(innerDimension, innerDimension), innerBorder.RenderSize);
			Assert.AreEqual(new Vector2((float)innerDimension, (float)innerDimension), innerBorder.ActualSize);
			Assert.AreEqual(expectedSizeInner, innerBorder.DesiredSize);
			Assert.AreEqual(null, innerBorder.Clip);
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("It doesn't yet work properly on Android")]
#endif
		public async Task When_Clip_And_CornerRadius()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border()
			{
				Width = 300,
				Height = 300,
				Background = new SolidColorBrush(Windows.UI.Colors.Red),
				CornerRadius = new CornerRadius(50),
				Clip = new RectangleGeometry()
				{
					Rect = new Rect(0, 0, 150, 150),
				},
			};

			await UITestHelper.Load(SUT);
			var screenshot = await UITestHelper.ScreenShot(SUT);
			var redBounds = ImageAssert.GetColorBounds(screenshot, Colors.Red, 10);
			Assert.AreEqual(new Size(149, 149), new Size(redBounds.Width, redBounds.Height));
			ImageAssert.DoesNotHaveColorAt(screenshot, 10, 10, Windows.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 20, 20, Windows.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 10, 148, Windows.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 148, 148, Windows.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 5, 148, Windows.UI.Colors.Red, tolerance: 10);
			ImageAssert.DoesNotHaveColorAt(screenshot, 155, 155, Windows.UI.Colors.Red, tolerance: 10);
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("Fails on Android")]
#endif
		public async Task When_Clipped_With_TransformMatrix()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Windows.UI.Colors.Red),
				Clip = new RectangleGeometry
				{
					Rect = new Rect(0, 0, 100, 100),
					Transform = new TranslateTransform
					{
						Y = -50
					}
				}
			};

			await UITestHelper.Load(SUT);

			var bitmap = await UITestHelper.ScreenShot(SUT);
			ImageAssert.DoesNotHaveColorInRectangle(bitmap, new System.Drawing.Rectangle(0, 50, 100, 50), Windows.UI.Colors.Red);
		}

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
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
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

			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
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

			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
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

			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border() { Background = new SolidColorBrush(Colors.Blue) };
			var inner = new Border() { CornerRadius = new CornerRadius(40), Width = 80, Height = 80 };
			inner.Child = new Windows.UI.Xaml.Shapes.Rectangle() { Fill = new SolidColorBrush(Colors.Red), Width = 80, Height = 80 };
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
#if __ANDROID__ || __IOS__ || __WASM__
		[Ignore("Not supported yet")]
#endif
		[DataRow(true)]
		[DataRow(false)]
		public async Task Border_CornerRadiusAndClip_Clipping(bool useNullBackground)
		{
			var sut = new Border
			{
				Name = "sut",
				Height = 150,
				Width = 150,
				CornerRadius = new CornerRadius(50),
				BorderBrush = new SolidColorBrush(Colors.Red),
				BorderThickness = new Thickness(20),
				Background = useNullBackground ? null : new SolidColorBrush(Colors.Blue),
				Clip = new RectangleGeometry
				{
					Rect = new Rect(10, 10, 130, 130)
				},
				Child = new Rectangle
				{
					Name = "the_nested_rectangle",
					Fill = new SolidColorBrush(Colors.Green),
					Width = 150,
					Height = 150
				}
			};

			var root = new Border { Child = sut };
			await UITestHelper.Load(root);
			var snapshot = await UITestHelper.ScreenShot(root);

			ImageAssert.HasPixels(
				snapshot,
				ExpectedPixels
					.At($"top-left-content-radius", 30, 30)
					.WithColorTolerance(1)
					.Pixel(Colors.Red),
				ExpectedPixels
					.At($"top-right-content-radius", 30, 120)
					.WithColorTolerance(1)
					.Pixel(Colors.Red),
				ExpectedPixels
					.At($"bottom-left-content-radius", 120, 30)
					.WithColorTolerance(1)
					.Pixel(Colors.Red),
				ExpectedPixels
					.At($"bottom-right-content-radius", 120, 120)
					.WithColorTolerance(1)
					.Pixel(Colors.Red),
				ExpectedPixels
					.At($"left-clip", 5, 75)
					.Pixel(Colors.Transparent),
				ExpectedPixels
					.At($"top-clip", 75, 5)
					.Pixel(Colors.Transparent),
				ExpectedPixels
					.At($"right-clip", 145, 75)
					.Pixel(Colors.Transparent),
				ExpectedPixels
					.At($"bottom-clip", 145, 145)
					.Pixel(Colors.Transparent)

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
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
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

#if HAS_UNO
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#else
		[TestMethod]
		[RunsOnUIThread]
#endif
		public async Task Nested_Element_Tapped()
		{
			var SUT = new Border()
			{
				Child = new Button()
			};

			var outerBorderTaps = 0;
			var outerBorderRightTaps = 0;

			SUT.Tapped += (_, _) => outerBorderTaps++;
			SUT.RightTapped += (_, _) => outerBorderRightTaps++;
			SUT.Child.RightTapped += (_, _) => { };

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			mouse.Press(SUT.GetAbsoluteBounds().GetCenter());
			mouse.Release();

			Assert.AreEqual(1, outerBorderTaps);
			Assert.AreEqual(0, outerBorderRightTaps);

			mouse.PressRight(SUT.GetAbsoluteBounds().GetCenter());
			mouse.ReleaseRight();

			Assert.AreEqual(1, outerBorderTaps);
			Assert.AreEqual(1, outerBorderRightTaps);
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#else
		[TestMethod]
		[RunsOnUIThread]
#endif
		public async Task Parent_DoubleTapped_When_Child_Has_Tapped()
		{
			var SUT = new Border()
			{
				Child = new Border()
				{
					Width = 100,
					Height = 100,
					Background = new SolidColorBrush(Colors.Red),
				},
			};

			var outerBorderDoubleTaps = 0;
			var childBorderTaps = 0;

			SUT.DoubleTapped += (_, _) => outerBorderDoubleTaps++;
			SUT.Child.Tapped += (_, _) => childBorderTaps++;


			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var borderCenter = SUT.GetAbsoluteBounds().GetCenter();

			mouse.Press(borderCenter);
			mouse.Release();

			mouse.Press(borderCenter);
			mouse.Release();

			Assert.AreEqual(1, outerBorderDoubleTaps);
			Assert.AreEqual(2, childBorderTaps); // This doesn't look right. It should be 1.
		}
#endif

		[TestMethod]
#if __MACOS__
		[Ignore("Currently flaky on macOS, part of #9282 epic")]
#endif
		public async Task Border_CornerRadius_GradientBrush()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}
			var SUT = new Border_CornerRadius_Gradient();
			var result = await TakeScreenshot(SUT);
			var textBoxRect = SUT.GetRelativeCoords(SUT.RedBorder);
			ImageAssert.HasColorAt(result, textBoxRect.CenterX, textBoxRect.CenterY, "#FF00FF00", tolerance: 10);
		}

		[TestMethod]
		public async Task When_CornerRadius()
		{
			var case1A = new Border()
			{
				Width = 200,
				Height = 100,
				CornerRadius = new(200),
				Background = new SolidColorBrush(Colors.Red),
			};

			var case1B = new Border()
			{
				Width = 200,
				Height = 100,
				CornerRadius = new(100),
				Background = new SolidColorBrush(Colors.Red),
			};

			var case1Expected = new Ellipse()
			{
				Width = 200,
				Height = 100,
				Fill = new SolidColorBrush(Colors.Red),
			};

			var case2 = new Border()
			{
				Width = 200,
				Height = 100,
				CornerRadius = new(200, 0, 0, 0),
				Background = new SolidColorBrush(Colors.Blue),
			};

			var case2Expected = new Grid()
			{
				Width = 200,
				Height = 100,
				Children =
				{
					new Ellipse()
					{
						Width = 400,
						Height = 200,
						Fill = new SolidColorBrush(Colors.Blue),
					}
				}
			};

			var stackPanel = new StackPanel()
			{
				Children =
				{
					case1A,
					case1B,
					case1Expected,
					case2,
					case2Expected,
				}
			};

			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(stackPanel);

			var renderer1A = new RenderTargetBitmap();
			await renderer1A.RenderAsync(case1A);
			var bitmap1A = await RawBitmap.From(renderer1A, case1A);

			var renderer1B = new RenderTargetBitmap();
			await renderer1B.RenderAsync(case1B);
			var bitmap1B = await RawBitmap.From(renderer1B, case1B);

			var renderer1Expected = new RenderTargetBitmap();
			await renderer1Expected.RenderAsync(case1Expected);
			var bitmap1Expected = await RawBitmap.From(renderer1Expected, case1Expected);

			await ImageAssert.AreSimilarAsync(bitmap1A, bitmap1Expected, imperceptibilityThreshold: 0.7);
			await ImageAssert.AreSimilarAsync(bitmap1B, bitmap1Expected, imperceptibilityThreshold: 0.7);

			var renderer2 = new RenderTargetBitmap();
			await renderer2.RenderAsync(case2);
			var bitmap2 = await RawBitmap.From(renderer2, case2);

			var renderer2Expected = new RenderTargetBitmap();
			await renderer2Expected.RenderAsync(case2Expected);
			var bitmap2Expected = await RawBitmap.From(renderer2Expected, case2Expected);

			await ImageAssert.AreSimilarAsync(bitmap2, bitmap2Expected, imperceptibilityThreshold: 0.7);
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
			var rect = new System.Drawing.Rectangle((int)firstBorderRect.X, (int)firstBorderRect.Y,
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
			await UITestHelper.Load(SUT);
			return await UITestHelper.ScreenShot(SUT);
		}
	}
}
