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
#if __MACOS__
	[Ignore("That test cause a Runtime error with MACOS")]
#endif
	public partial class ImageBrush_WriteableBitmap
	{
		[TestMethod]
		public async Task When_UseWriteableBitmapAsImageBrushSource()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			const string BlueViolet = "#FF8A2BE2";
			var imageBrush = new ImageBrush_WriteableBitmap();
			
			var result = await RawBitmap.TakeScreenshot(imageBrush);
			var sut = imageBrush.GetRelativeCoords(imageBrush.SUT);
			
			ImageAssert.HasColorAt(result, sut.CenterX, sut.CenterY, BlueViolet);
		}
	}
}
