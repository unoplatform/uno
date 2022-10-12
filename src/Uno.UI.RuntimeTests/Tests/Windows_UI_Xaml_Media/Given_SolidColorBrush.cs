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
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class SolidColorBrush_Tests
	{

		[TestMethod]
		public async Task When_SolidColorBrush_Color_Changed()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			const string Green = "#FF008000";
			const string Chestnut = "#FFCD5C5C";
			const string Violet = "#FFEE82EE";
			const string Gold = "#FFB8860B";
			const string Blue = "#FF0000FF";
#if Android
			const int offset = 0;
#else 
			const int offset = 2;
#endif
			var SUT = new SolidColorBrush_ColorChanged();
			WindowHelper.WindowContent = SUT;
			var initial = await RawBitmap.TakeScreenshot(SUT);

			var border = SUT.GetRelativeCoords(SUT.ToggleableBorder);
			var grid = SUT.GetRelativeCoords(SUT.ToggleableGrid);
			var ellipse = SUT.GetRelativeCoords(SUT.ToggleableEllipse);
			var fillEllipse = SUT.GetRelativeCoords(SUT.ToggleableFillEllipse);

			var data = new (RelativeCoords view, string InitialColor)[]
			{
				(border, Green),
				(grid, Chestnut),
				(ellipse, Violet),
			};

			await WindowHelper.WaitForIdle();

			foreach (var (view, initialColor) in data)
			{
				ImageAssert.HasColorAt(initial, view.CenterX, view.Y + offset, initialColor);
			}
			
			ImageAssert.HasColorAt(initial, fillEllipse.CenterX, fillEllipse.CenterY, Gold);

			WindowHelper.WindowContent = SUT;
			SUT.PaintItBlue();
			await WindowHelper.WaitForIdle();

			var borderChanged = await RawBitmap.TakeScreenshot(SUT);

			foreach (var (view, _) in data)
			{
				ImageAssert.HasColorAt(borderChanged, view.CenterX, view.Y + 2, Blue);
			}

			await WindowHelper.WaitForIdle();
			ImageAssert.HasColorAt(borderChanged, fillEllipse.CenterX, fillEllipse.CenterY, Blue);
		}
	}
}
