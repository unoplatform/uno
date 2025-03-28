using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO && !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public partial class Given_PipsPager
{
	[TestMethod]
	[RunsOnUIThread]
#if __WASM__
	[Ignore("RenderTargetBitmap is not implemented on WASM.")]
#elif __SKIA__
	[Ignore("Fails even on Windows.")]
#endif
	public async Task When_MaxVisiblePips_GreaterThan_NumberOfPages_Horizontal()
	{
		var SUT = new PipsPager
		{
			NumberOfPages = 7,
			MaxVisiblePips = 5
		};

		await UITestHelper.Load(SUT);

		var initialScreenshot = await UITestHelper.ScreenShot(SUT);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width - 5, initialScreenshot.Height / 2);

		SUT.SelectedPageIndex = 3;
		await TestServices.WindowHelper.WaitForIdle();

		var scrolledScreenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(scrolledScreenshot, scrolledScreenshot.Width - 5, scrolledScreenshot.Height / 2, color);
	}

	[TestMethod]
	[RunsOnUIThread]
#if __WASM__
	[Ignore("RenderTargetBitmap is not implemented on WASM.")]
#endif
	public async Task When_MaxVisiblePips_GreaterThan_NumberOfPages_Vertical()
	{
		var SUT = new PipsPager
		{
			NumberOfPages = 7,
			MaxVisiblePips = 5
		};

		await UITestHelper.Load(SUT);

		var initialScreenshot = await UITestHelper.ScreenShot(SUT);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width / 2, initialScreenshot.Height - 5);

		SUT.SelectedPageIndex = 3;
		await TestServices.WindowHelper.WaitForIdle();

		var scrolledScreenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(scrolledScreenshot, scrolledScreenshot.Width / 2, scrolledScreenshot.Height - 5, color);
	}
}
