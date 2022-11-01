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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public partial class ImageBrush_WriteableBitmap
	{
		[TestMethod]
		public async Task When_UseWriteableBitmapAsImageBrushSource()
		{
#if !__MACOS__  //That test cause a Runtime error with MACOS

			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			const string BlueViolet = "#FF8A2BE2";
			var imageBrush = new ImageBrush_WriteableBitmap();
			WindowHelper.WindowContent = imageBrush;
			await WindowHelper.WaitForLoaded(imageBrush);
			var result = await TakeScreenshot(imageBrush);
			var sut = imageBrush.GetRelativeCoords(imageBrush.SUT);
			
			ImageAssert.HasColorAt(result, sut.CenterX, sut.CenterY, BlueViolet);
#endif
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
