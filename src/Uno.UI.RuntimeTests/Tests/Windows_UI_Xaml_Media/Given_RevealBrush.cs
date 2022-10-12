using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RevealBrush
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_RevealBrush_Assigned()
		{
			// Basic smoke test - RevealBrush is currently unimplemented, but it shouldn't break the layout

			var siblingBorder = new Border { Width = 43, Height = 22 };
			var gridWithRevealBackground = new Grid
			{
				Width = 57,
				Height = 34,
				Background = new RevealBackgroundBrush() { Color = Colors.Brown, FallbackColor = Colors.Brown },
			};
			var parentSP = new StackPanel
			{
				Children =
				{
					gridWithRevealBackground,
					siblingBorder
				}
			};

			WindowHelper.WindowContent = parentSP;
			await WindowHelper.WaitForLoaded(parentSP);

			await WindowHelper.WaitForEqual(57, () => gridWithRevealBackground.ActualWidth);
			Assert.AreEqual(34, gridWithRevealBackground.ActualHeight);

			// If `siblingBorder.X` is fractional (not always visible inside `Frame`) then the `Width` will be
			// Different screen size of high-res (scale > 1) devices can trigger this (fractional `X`) condition
			// along where the canvas start position is (relative to the unit test runner)
			// See https://github.com/unoplatform/uno/pull/9179 for more details
			var delta = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel == 2 ? 0.5d : 0.0d;

			Assert.AreEqual(43, siblingBorder.ActualWidth, delta);
			Assert.AreEqual(22, siblingBorder.ActualHeight);
		}

		[TestMethod]
		public async Task When_FallbackColor_Set()
		{
#if __MACOS__
			Assert.Inconclusive(); //Currently fails on macOS, part of #9282 epic. Coodinates outside of image.
#endif
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			const string Orange = "#FFFFA500";
			const string ForestGreen = "#FF228B22";

			var SUT = new RevealBrush_Fallback();
			var initial = await RawBitmap.TakeScreenshot(SUT);

			var grid = SUT.RevealGrid;
			var gridCR = SUT.RevealGridCR;
			var border = SUT.RevealBorder;
			var borderCR = SUT.RevealBorderCR;
			await WindowHelper.WaitForIdle();

			var views = new FrameworkElement[]
			{
				grid,
				gridCR,
				border,
				borderCR,
			};

			foreach (var view in views)
			{
				AssertHasColor(view, initial, Orange);
			}

			WindowHelper.WindowContent = SUT;
			SUT.MakeItGreen();
			await WindowHelper.WaitForIdle();

			var after = await RawBitmap.TakeScreenshot(SUT);

			foreach (var view in views)
			{
				AssertHasColor(view, after, ForestGreen);
			}
		}

		private void AssertHasColor(FrameworkElement element, RawBitmap screenshot, string expectedColor)
		{
			ImageAssert.HasColorAtChild(screenshot, element, (float)element.Width / 2, (float)element.Height / 2, expectedColor);
		}
	}
}

