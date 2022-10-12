using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;
using Windows.UI.Xaml.Shapes;
using UITests.Windows_UI_Xaml_Controls;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using MUXControlsTestApp.Utilities;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_SplitView
	{
		[TestMethod]
		public async Task When_RightPanne_Clipped()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}
			const string Red = "#FFFF0000";
			const string Blue = "#FF0000FF";
			var SUT = new SplitViewClip();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			
			var compactScreenshot = await RawBitmap.TakeScreenshot(SUT);
			var targetGridRectangle = SUT.GetRelativeCoords(SUT.TargetRect);
			await WindowHelper.WaitForIdle();

			// Compact pane is 48 pixels wide
			ImageAssert.HasColorAt(compactScreenshot, targetGridRectangle.Right - 4, targetGridRectangle.CenterY, Blue);

			SUT.PanOpen();
			
			await WindowHelper.WaitForIdle();
			
			var expandedScreenshot = await RawBitmap.TakeScreenshot(SUT);
			var targetGridRectangle2 = SUT.GetRelativeCoords(SUT.TargetRect);
			await WindowHelper.WaitForIdle();

			ImageAssert.HasColorAt(expandedScreenshot, targetGridRectangle2.Right - 4, targetGridRectangle2.CenterY, Red);

			SUT.PanClose();
			await WindowHelper.WaitForIdle();

			var compactAgainScreenshot = await RawBitmap.TakeScreenshot(SUT);
			var targetGridRectangle3 = SUT.GetRelativeCoords(SUT.TargetRect);
			await WindowHelper.WaitForIdle();
			ImageAssert.HasColorAt(compactAgainScreenshot, targetGridRectangle3.Right - 4, targetGridRectangle3.CenterY, Blue);
		}
	}
}
