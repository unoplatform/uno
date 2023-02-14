using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using Uno.UITest;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UITest.Helpers.Queries;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.UITests;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using MUXControlsTestApp;
using Windows.Foundation.Metadata;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Canvas_Measurement_Tests
	{
		private const string Red = "#FFFF0000";
		private const string Blue = "#FF0000FF";
		private const string Green = "#FF008000";
		private const string Brown = "#FFA52A2A";

		[TestMethod]
		public async Task When_Measure_CanvasChildren()
		{
			var canvas = new Measure_Children_In_Canvas();
			WindowHelper.WindowContent = canvas;
			await WindowHelper.WaitForLoaded(canvas);
			var inBorder = canvas.Get_InBorder();
			var outBorder = canvas.Get_OutBorder();
			using var _ = new AssertionScope();
			inBorder.Width.Should().Be(outBorder.Width, "Border in canvas measurement failed");
			inBorder.Height.Should().Be(outBorder.Height, "Border in canvas measurement failed");
		}

		[TestMethod]

		public async Task When_Verify_Canvas_With_Outer_Clip()
		{
#if __MACOS__ //Color are not interpreted the same way in Mac
			Assert.Inconclusive();
#endif
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
			}

			var canvas = new Canvas_With_Outer_Clip();
			WindowHelper.WindowContent = canvas;
			await WindowHelper.WaitForLoaded(canvas);

			var renderer = new RenderTargetBitmap();
			await WindowHelper.WaitForIdle();
			await renderer.RenderAsync(canvas);
			var bitmap = await RawBitmap.From(renderer, canvas);

			var clippedLocation = canvas.Get_LocatorBorder1();
			await WindowHelper.WaitForIdle();
			ImageAssert.HasColorAtChild(bitmap, clippedLocation, (float)clippedLocation.Width / 2, (float)clippedLocation.Height / 2, Red);

			var unclippedLocation = canvas.Get_LocatorBorder2();
			await WindowHelper.WaitForIdle();
			ImageAssert.HasColorAtChild(bitmap, unclippedLocation, (float)unclippedLocation.Width / 2, (float)unclippedLocation.Height / 2, Blue);
		}

		[TestMethod]
		public async Task When_Verify_Canvas_ZIndex()
		{
#if __MACOS__
			Assert.Inconclusive(); //Color are not interpreted the same way in Mac
#endif
#if __ANDROID__
			Assert.Inconclusive(); // Android doesn't support Canvas.ZIndex on any panel
#endif
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var canvas = new CanvasZIndex();
			WindowHelper.WindowContent = canvas;
			await WindowHelper.WaitForLoaded(canvas);

			var renderer = new RenderTargetBitmap();
			await WindowHelper.WaitForIdle();
			await renderer.RenderAsync(canvas);
			var bitmap = await RawBitmap.From(renderer, canvas);

			var redBorderRect1 = canvas.Get_CanvasBorderRed1();
			ImageAssert.HasColorAtChild(bitmap, redBorderRect1, (float)redBorderRect1.Width / 2, (float)redBorderRect1.Height / 2, Green);

			var redBorderRect2 = canvas.Get_CanvasBorderRed2();
			ImageAssert.HasColorAtChild(bitmap, redBorderRect2, (float)redBorderRect2.Width / 2, (float)redBorderRect2.Height / 2, Green);

			var redBorderRect3 = canvas.Get_CanvasBorderRed3();
			ImageAssert.HasColorAtChild(bitmap, redBorderRect3, (float)redBorderRect3.Width / 2, (float)redBorderRect3.Height / 2, Green);

			var greenBorderRect1 = canvas.Get_CanvasBorderGreen1();
			ImageAssert.HasColorAtChild(bitmap, greenBorderRect1, (float)greenBorderRect1.Width / 2, (float)greenBorderRect1.Height / 2, Brown);
			ImageAssert.HasColorAtChild(bitmap, greenBorderRect1, (float)greenBorderRect1.Width - 1, (float)greenBorderRect1.Height / 2, Blue);

			var greenBorderRect2 = canvas.Get_CanvasBorderGreen1();
			ImageAssert.HasColorAtChild(bitmap, greenBorderRect2, (float)greenBorderRect2.Width / 2, (float)greenBorderRect2.Height / 2, Brown);
			ImageAssert.HasColorAtChild(bitmap, greenBorderRect2, (float)greenBorderRect2.Width - 1, greenBorderRect2.Height / 2, Blue);

			var greenBorderRect3 = canvas.Get_CanvasBorderGreen3();
			ImageAssert.HasColorAtChild(bitmap, greenBorderRect3, (float)greenBorderRect3.Width / 2, (float)greenBorderRect3.Height / 2, Brown);
			ImageAssert.HasColorAtChild(bitmap, greenBorderRect3, (float)greenBorderRect3.Width - 1, (float)greenBorderRect3.Height / 2, Blue);

		}

		[TestMethod]
		public async Task When_Verify_Canvas_In_Canvas()
		{
#if __MACOS__
			Assert.Inconclusive(); //Color are not interpreted the same way in Mac
#endif
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
			}

			var canvas = new Canvas_In_Canvas();
			WindowHelper.WindowContent = canvas;
			await WindowHelper.WaitForLoaded(canvas);

			var renderer = new RenderTargetBitmap();
			await WindowHelper.WaitForIdle();
			await renderer.RenderAsync(canvas);
			var bitmap = await RawBitmap.From(renderer, canvas);
			var clippedLocation = canvas.Get_CanvasBorderBlue1();

			ImageAssert.HasColorAtChild(bitmap, clippedLocation, (float)clippedLocation.Width / 2, clippedLocation.Height / 2, Blue);
		}

	}
}

