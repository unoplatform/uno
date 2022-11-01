using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class LinearGradientBrush_Tests
	{
		[TestMethod]
		public async Task When_Opacity_Is_Specified()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}
			const string Coral = "#FFFF8080";
			var SUT = new LinearGradientBrush_Opacity();
			var result = await TakeScreenshot(SUT);
			var grid = SUT.GetRelativeCoords(SUT.TestGrid);

			ImageAssert.HasColorAt(result, grid.CenterX, grid.CenterY, Coral, tolerance: 20);
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
