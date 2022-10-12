using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using static Private.Infrastructure.TestServices;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Extensions;
using System.Collections.Specialized;
using Windows.Foundation.Metadata;
using SamplesApp.UITests.TestFramework;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Clipping
	{
		private const string Red = "#FFFF0000";
		private const string Blue = "#FF0000FF";
		
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Clip_Is_Set_On_Container_Element()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Clipping652();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var screenshot = await RawBitmap.TakeScreenshot(SUT);
			await WindowHelper.WaitForIdle();
			var rect1 = SUT.GetRelativeCoords(SUT.ClippingGrid1);

			ImageAssert.HasColorAt(screenshot, rect1.Right + 8, rect1.Y + 75, Blue);
			ImageAssert.HasColorAt(screenshot, rect1.X + 75, rect1.Bottom + 8, Blue);

			var scrn = await RawBitmap.TakeScreenshot(SUT);
			await WindowHelper.WaitForIdle();
			var rect2 = SUT.GetRelativeCoords(SUT.Rect2);

			ImageAssert.HasColorAt(scrn, rect2.Right + 8, rect2.Y + 75, Blue);
			ImageAssert.HasColorAt(scrn, rect2.X + 75, rect2.Bottom + 8, Blue);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Clipped_Rounded_Corners()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Clipping4273();
			var screenshot = await RawBitmap.TakeScreenshot(SUT);
			var grid = SUT.GetRelativeCoords(SUT.RoundedGrid);
			float offset = (float)5;

			await WindowHelper.WaitForIdle();
			ImageAssert.HasColorAt(screenshot, grid.CenterX, grid.CenterY, Blue);
			ImageAssert.HasColorAt(screenshot, grid.X + offset, grid.Y + offset, Red);
		}
	}
}

