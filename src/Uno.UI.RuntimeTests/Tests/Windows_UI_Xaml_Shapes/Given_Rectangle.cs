#if __APPLE_UIKIT__ || __SKIA__ || WINAPPSDK
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Windows.Devices.Perception;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation.Metadata;
using FluentAssertions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes;

[TestClass]
[RunsOnUIThread]
public class Given_Rectangle
{
	[TestMethod]
	[DataRow(Stretch.Fill, double.NaN, double.NaN, 0d, 0d, 50d, 50d, 0d, 0d)]
	[DataRow(Stretch.Fill, double.NaN, double.NaN, 10d, 20d, 50d, 50d, 10d, 20d)]
	[DataRow(Stretch.Fill, 30d, double.NaN, 10d, 20d, 50d, 50d, 30d, 20d)]
	[DataRow(Stretch.Fill, double.NaN, 30d, 10d, 20d, 50d, 50d, 10d, 30d)]
	[DataRow(Stretch.UniformToFill, double.NaN, double.NaN, 0d, 0d, double.PositiveInfinity, double.PositiveInfinity, 0d, 0d)]
	[DataRow(Stretch.UniformToFill, double.NaN, double.NaN, 10d, 20d, double.PositiveInfinity, double.PositiveInfinity, 10d, 20d)]
	public async Task When_RectangleMeasure(
		Stretch stretch,
		double width,
		double height,
		double minWidth,
		double minHeight,
		double availableWidth,
		double availableHeight,
		double expectedWidth,
		double expectedHeight)
	{
		try
		{
			var SUT = new Rectangle();

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			SUT.Width = width;
			SUT.Height = height;
			SUT.MinWidth = minWidth;
			SUT.MinHeight = minHeight;
			SUT.Stretch = stretch;
			SUT.Measure(new Windows.Foundation.Size(availableWidth, availableHeight));

			Assert.AreEqual(new Size(expectedWidth, expectedHeight), SUT.DesiredSize);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(50.0d, 5.0d, 50.0d, 99.0d, 54.0d)]
	[DataRow(70.0d, 5.0d, 50.0d, 119.0d, 54.0d)]
	[DataRow(20.0d, 5.0d, 50.0d, 69.0d, 54.0d)]
	[DataRow(5.0d, 5.0d, 50.0d, 54.0d, 54.0d)]
	[DataRow(10.0d, 10.0d, 10.0d, 19.0d, 19.0d)]
	[DataRow(50.0d, 10.0d, 10.0d, 59.0d, 19.0d)]
	[DataRow(10.0d, 50.0d, 50.0d, 59.0d, 99.0d)]
	[DataRow(10.0d, 50.0d, 10.0d, 19.0d, 59.0d)]
	[DataRow(60.0d, 70.0d, 10.0d, 59.0d, 69.0d)]
	[DataRow(10.0d, 80.0d, 30.0d, 39d, 109.0d)]
	[RequiresFullWindow]
#if __APPLE_UIKIT__
	[Ignore("Does not work on iOS")]
#endif
	public async Task When_StrokeThickness_Is_GreaterThan_Or_Equals_Width(double width, double height, double strokeThickness, double expectedWidth, double expectedHeight)
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var rectangle = new Rectangle()
		{
			Width = width,
			Height = height,
			StrokeThickness = strokeThickness,
			Stroke = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};

		var root = new Grid
		{
			Children =
			{
				rectangle,
			},
		};

		await UITestHelper.Load(root);

		var screenshot = await UITestHelper.ScreenShot(root);
		var shapeBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 15);
		Assert.AreEqual(expectedWidth, shapeBounds.Width);
		Assert.AreEqual(expectedHeight, shapeBounds.Height);
	}

	[TestMethod]
	[DataRow(19.0d, 19.0d, 199.0d)]
	[DataRow(20.0d, 39.0d, 219.0d)]
	[RequiresFullWindow]
#if __APPLE_UIKIT__
	[Ignore("Does not work on iOS")]
#endif
	public async Task When_StrokeThickness_Should_Arrange_Correctly(double strokeThickness, double expectedGreenWidth, double expectedGreenHeight)
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var rectangle = new Rectangle()
		{
			Width = 20,
			Height = 200,
			StrokeThickness = strokeThickness,
			Stroke = new SolidColorBrush(Microsoft.UI.Colors.Green),
		};

		var grid = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Height = 300,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
			Children =
			{
				rectangle,
			},
		};

		var root = new Grid()
		{
			Children =
			{
				grid,
			},
		};

		await UITestHelper.Load(root);

		var screenshot = await UITestHelper.ScreenShot(root);
		var redBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 15);
		Assert.AreEqual(19, redBounds.Width);
		Assert.AreEqual(299, redBounds.Height);

		var greenBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Green, tolerance: 15);
		Assert.AreEqual(expectedGreenWidth, greenBounds.Width);
		Assert.AreEqual(expectedGreenHeight, greenBounds.Height);

		Assert.AreEqual(greenBounds.Top - redBounds.Top, redBounds.Bottom - greenBounds.Bottom);
	}

#if __APPLE_UIKIT__
	[TestMethod]
	public async Task When_Fill_Is_AcrylicBrush()
	{
		var unhandledExceptionFired = false;
		void OnUnhandled(object sender, global::Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
		{
			unhandledExceptionFired = true;
			args.Handled = true;
		}
		try
		{
			global::Microsoft.UI.Xaml.Application.Current.UnhandledException += OnUnhandled;
			var rectangle = new Rectangle()
			{
				Width = 100,
				Height = 100,
				Fill = new AcrylicBrush()
				{
					BackgroundSource = AcrylicBackgroundSource.Backdrop,
					TintColor = Microsoft.UI.Colors.Red,
					TintOpacity = 0.5,
					FallbackColor = Microsoft.UI.Colors.Green,
				},
			};
			var root = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Children =
				{
					rectangle,
				},
			};
			await UITestHelper.Load(root);
			var screenshot = await UITestHelper.ScreenShot(rectangle);
			ImageAssert.HasColorAt(screenshot, new(50, 50), Microsoft.UI.Colors.Green, tolerance: 0);
			unhandledExceptionFired.Should().BeFalse();
		}
		finally
		{
			Application.Current.UnhandledException -= OnUnhandled;
		}
	}
#endif
}
#endif
