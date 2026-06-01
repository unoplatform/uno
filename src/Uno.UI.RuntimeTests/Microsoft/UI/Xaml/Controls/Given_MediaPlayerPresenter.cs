using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using SamplesApp.UITests.TestFramework;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_MediaPlayerPresenter
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
	public void When_Reparented_To_FrameworkElement()
	{
		// Reparent (7.0 breaking change): MediaPlayerPresenter derives directly from
		// FrameworkElement (matching WinUI), dropping the extra Border level and its
		// leaked Child/BorderBrush/BorderThickness/CornerRadius/Padding surface.
		Assert.AreEqual(typeof(FrameworkElement), typeof(MediaPlayerPresenter).BaseType);
		Assert.AreNotEqual(typeof(Border), typeof(MediaPlayerPresenter).BaseType);
	}

#if HAS_UNO
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
	public async Task When_Child_Is_Measured_And_Arranged()
	{
		// The video surface is hosted through the internal Child slot, which the presenter
		// now measures and arranges itself instead of inheriting that from Border.
		var child = new Border();
		var SUT = new MediaPlayerPresenter { Width = 320, Height = 180, Child = child };

		try
		{
			await UITestHelper.Load(SUT);

			Assert.AreEqual(320, child.ActualWidth);
			Assert.AreEqual(180, child.ActualHeight);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
#endif

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
	public async Task When_Templated_Presenter_Is_Not_A_Border()
	{
		var SUT = new MediaPlayerElement { Width = 320, Height = 180 };

		try
		{
			// The presenter stays collapsed until a media source opens, so the default
			// non-zero-size check would never settle.
			await UITestHelper.Load(SUT, x => x.IsLoaded);

			var presenter = SUT.FindVisualChildByType<MediaPlayerPresenter>();

			Assert.IsNotNull(presenter);
			Assert.IsNotInstanceOfType(presenter, typeof(Border));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
	public async Task When_No_Source_Letterbox_Is_Black()
	{
		// The presenter no longer paints a Background, so the black letterbox has to come
		// from the template root.
		var SUT = new MediaPlayerElement { Width = 320, Height = 180 };

		try
		{
			await UITestHelper.Load(SUT, x => x.IsLoaded);

			var screenshot = await UITestHelper.ScreenShot(SUT);

			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2, screenshot.Height / 2, Microsoft.UI.Colors.Black, tolerance: 5);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
