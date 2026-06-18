#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Flyout_PropertyPropagation
{
	// Uno live-propagates the Flyout's AllowFocusOnInteraction/AllowFocusWhenDisabled to its content
	// (Inherits metadata + FlyoutPresenter), re-applying them whenever the Flyout's values change. Native
	// WinUI forwards these to the presenter only once during the open sequence (FlyoutBase.PrepareState) and
	// not on later changes, so this propagation test is Uno-only.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23409")]
	public async Task When_Focus_Properties_Set_On_Flyout_Propagate_To_Content()
	{
		var SUT = new Button();
		var flyoutContent = new Grid
		{
			Children = { SUT }
		};

		var flyout = new Flyout
		{
			AllowFocusOnInteraction = false,
			AllowFocusWhenDisabled = true,
			Content = flyoutContent,
		};

		var flyoutOwner = new Button { Flyout = flyout };

		TestServices.WindowHelper.WindowContent = flyoutOwner;
		await TestServices.WindowHelper.WaitForLoaded(flyoutOwner);

		try
		{
			flyout.ShowAt(flyoutOwner);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.AllowFocusOnInteraction);
			Assert.IsTrue(SUT.AllowFocusWhenDisabled);

			flyout.AllowFocusOnInteraction = true;
			flyout.AllowFocusWhenDisabled = false;

			Assert.IsTrue(SUT.AllowFocusOnInteraction);
			Assert.IsFalse(SUT.AllowFocusWhenDisabled);
		}
		finally
		{
			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	// flyout._popup is Uno-internal; WinUI exposes no public API for a Flyout's popup, so this test
	// is Uno-only. Keep the method visible for test discovery and skip it at runtime on native WinUI
	// via [PlatformCondition]; only the body is gated with #if so it still compiles on the WinUI build.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23409")]
	public async Task When_Focus_Properties_Set_On_Flyout_Propagate_To_Popup()
	{
#if HAS_UNO
		var flyoutContent = new Grid
		{
			Children = { new Button() }
		};

		var flyout = new Flyout
		{
			AllowFocusOnInteraction = false,
			AllowFocusWhenDisabled = true,
			Content = flyoutContent,
		};

		var flyoutOwner = new Button { Flyout = flyout };

		TestServices.WindowHelper.WindowContent = flyoutOwner;
		await TestServices.WindowHelper.WaitForLoaded(flyoutOwner);

		try
		{
			flyout.ShowAt(flyoutOwner);
			await TestServices.WindowHelper.WaitForIdle();

			var popup = flyout._popup;

			Assert.IsFalse(popup.AllowFocusOnInteraction);
			Assert.IsTrue(popup.AllowFocusWhenDisabled);

			flyout.AllowFocusOnInteraction = true;
			flyout.AllowFocusWhenDisabled = false;

			Assert.IsTrue(popup.AllowFocusOnInteraction);
			Assert.IsFalse(popup.AllowFocusWhenDisabled);
		}
		finally
		{
			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();
		}
#endif
	}
}
