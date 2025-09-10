using System;
using System.Collections.Generic;
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
using Windows.Media.Core;
using Uno.UI.RuntimeTests.Extensions;
using Microsoft.UI.Composition;
using System.IO;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;
using System.Numerics;
using Combinatorial.MSTest;

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
		[CombinatorialData]
#if __ANDROID__ || __APPLE_UIKIT__
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
				Assert.IsNull(customControl.Clip);
			}

			// TODO: This assert currently fails.
			// var expectedInnerBorderOffset = useCustomControl ? innerMargin : expectedOffsetDimension;
			//Assert.AreEqual(new Vector3((float)expectedInnerBorderOffset, (float)expectedInnerBorderOffset, 0), innerBorder.ActualOffset);

			Assert.AreEqual(new Size(innerDimension, innerDimension), innerBorder.RenderSize);
			Assert.AreEqual(new Vector2((float)innerDimension, (float)innerDimension), innerBorder.ActualSize);
			Assert.AreEqual(expectedSizeInner, innerBorder.DesiredSize);
			Assert.IsNull(innerBorder.Clip);
		}

		[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_Non_Empty_Null_Border()
		{
			static Border CreateBorder(Color color)
			{
				return new Border()
				{
					Width = 40,
					Height = 40,
					Background = new SolidColorBrush(color),
				};
			}
			var container = new StackPanel() { Spacing = 10 };
			var transparentParent = CreateBorder(Colors.Green);
			var transparentBorder = CreateBorder(Colors.Red);
			transparentBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
			transparentBorder.BorderThickness = new Thickness(4);
			transparentParent.Child = transparentBorder;

			var nullParent = CreateBorder(Colors.Green);
			var nullBorder = CreateBorder(Colors.Red);
			nullBorder.BorderBrush = null;
			nullBorder.BorderThickness = new Thickness(4);
			nullParent.Child = nullBorder;

			var noParent = CreateBorder(Colors.Green);
			var noBorder = CreateBorder(Colors.Red);
			noBorder.BorderThickness = new Thickness(0);
			noParent.Child = noBorder;

			container.Children.Add(transparentParent);
			container.Children.Add(nullParent);
			container.Children.Add(noParent);

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);

			// screenshot of nullBorder should be the same of transparentBorder
			var transparentBorderScreenshot = await UITestHelper.ScreenShot(transparentParent);
			var nullBorderScreenshot = await UITestHelper.ScreenShot(nullParent);
			await ImageAssert.AreEqualAsync(transparentBorderScreenshot, nullBorderScreenshot);

			// screenshot of noBorder should be different
			var noBorderScreenshot = await UITestHelper.ScreenShot(noParent);
			await ImageAssert.AreNotEqualAsync(nullBorderScreenshot, noBorderScreenshot);
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("It doesn't yet work properly on Android")]
#endif
		public async Task When_Clip_And_CornerRadius()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border()
			{
				Width = 300,
				Height = 300,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
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
			ImageAssert.DoesNotHaveColorAt(screenshot, 10, 10, Microsoft.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 20, 20, Microsoft.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 10, 148, Microsoft.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 148, 148, Microsoft.UI.Colors.Red, tolerance: 10);
			ImageAssert.HasColorAt(screenshot, 5, 148, Microsoft.UI.Colors.Red, tolerance: 10);
			ImageAssert.DoesNotHaveColorAt(screenshot, 155, 155, Microsoft.UI.Colors.Red, tolerance: 10);
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("Fails on Android")]
#endif
		public async Task When_Clipped_With_TransformMatrix()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
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
			ImageAssert.DoesNotHaveColorInRectangle(bitmap, new System.Drawing.Rectangle(0, 50, 100, 50), Microsoft.UI.Colors.Red);
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

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
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

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
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

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
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

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Border() { Background = new SolidColorBrush(Colors.Blue) };
			var inner = new Border() { CornerRadius = new CornerRadius(40), Width = 80, Height = 80 };
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
#if __ANDROID__ || __APPLE_UIKIT__ || __WASM__
		[Ignore("Not supported yet")]
#endif
		[CombinatorialData]
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
#if __APPLE_UIKIT__
			Assert.Inconclusive(); // iOS not working currently. https://github.com/unoplatform/uno/issues/6749
#endif
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
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
		[RunsOnUIThread]
		public async Task When_Child_Set_Same_Reference()
		{
			var SUT = new Border() { Width = 100, Height = 100 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var child = new MeasureArrangeCounterButton();
			SUT.Child = child;

			await WindowHelper.WaitForLoaded(child);

#if !HAS_UNO // Uno specific: The initial layout count here is incorrect - should be 1 as in WinUI
			Assert.AreEqual(1, child.MeasureCount);
			Assert.AreEqual(1, child.ArrangeCount);
#endif

			var initialMeasureCount = child.MeasureCount;
			var initialArrangeCount = child.ArrangeCount;

			SUT.Child = child;

			// Both measure and arrange count must increase
			await WindowHelper.WaitFor(() => child.MeasureCount > initialMeasureCount);
			await WindowHelper.WaitFor(() => child.ArrangeCount > initialArrangeCount);

#if !HAS_UNO // Uno specific: The initial layout count here is incorrect - should be 1 as in WinUI
			Assert.AreEqual(2, child.MeasureCount);
			Assert.AreEqual(2, child.ArrangeCount);
#endif
		}

		internal partial class MeasureArrangeCounterButton : Button
		{
			internal int MeasureCount { get; private set; }

			internal int ArrangeCount { get; private set; }

			protected override Size MeasureOverride(Size availableSize)
			{
				MeasureCount++;
				return base.MeasureOverride(availableSize);
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				ArrangeCount++;
				return base.ArrangeOverride(finalSize);
			}
		}

#if HAS_UNO
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		[RunsOnUIThread]
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
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		[RunsOnUIThread]
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
		public async Task Border_CornerRadius_GradientBrush()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
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

#if HAS_UNO
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, (~RuntimeTestPlatforms.SkiaDesktop) | RuntimeTestPlatforms.SkiaMacOS)]
		[RequiresScaling(1.0f)]
		public async Task When_CornerRadius_AntiAliasing()
		{
			var background = new Border
			{
				Width = 60,
				Height = 60,
				Background = new SolidColorBrush(Colors.Green),
			};

			var roundedCorner = new Border
			{
				Width = 60,
				Height = 60,
				CornerRadius = new CornerRadius(30),
				Background = new SolidColorBrush(Colors.Red),
			};

			var stackPanel = new Grid()
			{
				Children =
				{
					background,
					roundedCorner
				}
			};

			await UITestHelper.Load(stackPanel);
			// await (await UITestHelper.ScreenShot(stackPanel)).Save("When_CornerRadius_AntiAliasing.png");
			var screenShot = await UITestHelper.ScreenShot(stackPanel);

			var image = new Image()
			{
				Height = 60,
				Width = 60,
				Source = "ms-appx:///Assets/When_CornerRadius_AntiAliasing.png"
			};
			await UITestHelper.Load(image);
			await ImageAssert.AreEqualAsync(await UITestHelper.ScreenShot(image), screenShot);
		}
#endif

#if HAS_UNO
		[TestMethod]
#if !__SKIA__
		[Ignore("Only skia accurately hittests CorderRadius")]
#endif
		[DataRow(true, true)]
		[DataRow(false, true)]
		[DataRow(false, false)]
		public async Task When_Border_CornerRadius_HitTesting(bool addBorderChild, bool addGridBackground)
		{
			var borderPressedCount = 0;
			var rectanglePressedCount = 0;
			var gridPressedCount = 0;
			var root = new Grid
			{
				Background = addGridBackground ? new SolidColorBrush(Colors.Transparent) : null,
				Children =
				{
					new Border
					{
						Height = 150,
						Width = 150,
						CornerRadius = 50,
						BorderBrush = Colors.Red,
						BorderThickness = new Thickness(20),
						Child = !addBorderChild ? null : new Rectangle
						{
							Width = 150,
							Height = 150,
							Fill = Colors.Green
						}.Apply(r => r.PointerPressed += (_, args) =>
						{
							rectanglePressedCount++;
							args.Handled = true;
						})
					}.Apply(b => b.PointerPressed += (_, args) =>
					{
						borderPressedCount++;
						args.Handled = true;
					})
				}
			}.Apply(g => g.PointerPressed += (_, _) => gridPressedCount++);

			await UITestHelper.Load(root);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// points are relative to root
			var pointToTarget = new Dictionary<Point, string>
			{
				{ new Point(12, 11), addGridBackground ? "grid" : "" },
				{ new Point(7, 21), addGridBackground ? "grid" : "" },
				{ new Point(7, 127), addGridBackground ? "grid" : "" },
				{ new Point(19, 138), addGridBackground ? "grid" : "" },
				{ new Point(9, 143), addGridBackground ? "grid" : "" },
				{ new Point(125, 144), addGridBackground ? "grid" : "" },
				{ new Point(140, 134), addGridBackground ? "grid" : "" },
				{ new Point(145, 27), addGridBackground ? "grid" : "" },
				{ new Point(134, 13), addGridBackground ? "grid" : "" },

				{ new Point(140, 77), "border" },
				{ new Point(122, 135), "border" },
				{ new Point(74, 143), "border" },
				{ new Point(19, 125), "border" },
				{ new Point(10, 80), "border" },
				{ new Point(24, 18), "border" },

				// This is actually what WinUI reports.The inner radius of the corners doesn't clip in hit-testing, but is itself hit-testable.
				// So if there's a child in the border, you can click it "through" the thickness of the border, but if there is no child,
				// the border will be clicked.
				{ new Point(24, 25), addBorderChild ? "rectangle" : "border" },
				{ new Point(21, 125), addBorderChild ? "rectangle" : "border" }, // (20, 125) passes on 100 % scaling, but not on 150 % scaling #19246
				{ new Point(122, 126), addBorderChild ? "rectangle" : "border" },
				{ new Point(121, 22), addBorderChild ? "rectangle" : "border" },
				{ new Point(29, 123), addBorderChild ? "rectangle" : "border" },
				{ new Point(118, 123), addBorderChild ? "rectangle" : "border" },
				{ new Point(117, 29), addBorderChild ? "rectangle" : "border" },
				{ new Point(27, 33), addBorderChild ? "rectangle" : "border" },

				{ new Point(35, 39), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
				{ new Point(33, 112), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
				{ new Point(73, 126), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
				{ new Point(113, 119), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
				{ new Point(127, 73), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
				{ new Point(116, 37), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
				{ new Point(74, 26), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
				{ new Point(33, 35), addBorderChild ? "rectangle" : addGridBackground ? "grid" : "" },
			};

			var expectedBorderPressedCount = 0;
			var expectedRectanglePressedCount = 0;
			var expectedGridPressedCount = 0;

			Assert.AreEqual(expectedBorderPressedCount, borderPressedCount);
			Assert.AreEqual(expectedRectanglePressedCount, rectanglePressedCount);
			Assert.AreEqual(expectedGridPressedCount, gridPressedCount);

			await Task.Delay(100); // wait for the renderer
			foreach (var (point, actualTarget) in pointToTarget)
			{
				mouse.MoveTo(root.TransformToVisual(null).TransformPoint(point), 1);
				mouse.Press();
				mouse.Release();

				switch (actualTarget)
				{
					case "grid":
						expectedGridPressedCount++;
						break;
					case "border":
						expectedBorderPressedCount++;
						break;
					case "rectangle":
						expectedRectanglePressedCount++;
						break;
				}

				Assert.AreEqual(expectedBorderPressedCount, borderPressedCount);
				Assert.AreEqual(expectedRectanglePressedCount, rectanglePressedCount);
				Assert.AreEqual(expectedGridPressedCount, gridPressedCount);
			}
		}
#endif

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
