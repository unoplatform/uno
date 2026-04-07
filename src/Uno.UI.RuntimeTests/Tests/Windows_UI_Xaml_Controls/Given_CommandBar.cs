using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;
using Uno.UI.Toolkit.DevTools.Input;

using static Uno.UI.Extensions.ViewExtensions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.CommandBarPages;

#if __APPLE_UIKIT__
using Uno.UI.Controls;
using Uno.UI.Helpers.WinUI;
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_CommandBar
	{
		[TestMethod]
		public async Task TestNativeCommandBarIcon()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new CommandBarTests();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var renderer = new RenderTargetBitmap();
			await renderer.RenderAsync(SUT);
			var result = await RawBitmap.From(renderer, SUT);
			ImageAssert.HasColorInRectangle(result, new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), result.Size), Color.FromArgb(0xFF, 0xFF, 0x0, 0x0));
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Popup_Open_Then_Click_Outside()
		{
			var SUT = new CommandBar
			{
				SecondaryCommands =
				{
					new AppBarButton
					{
						Label = "secondary",
						Name = "SecondaryButton"
					}
				}
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var moreButton = (Button)SUT.FindName("MoreButton");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			Point GetCenter(Rect rect) => new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
			finger.Press(GetCenter(moreButton.GetAbsoluteBounds()));
			finger.Release();

			await WindowHelper.WaitForIdle();

			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot);
			Assert.HasCount(1, popups);

			var secondaryButton = (AppBarButton)SUT.FindName("SecondaryButton");
			var bounds = secondaryButton.GetAbsoluteBounds();
			finger.Press(bounds.Bottom + 10, (bounds.Left + bounds.Right) / 2);
			finger.Release();

			await WindowHelper.WaitForIdle();

			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot));
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("VerticalAlignment asserts fail. Might be because of different timing.")]
#endif
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Expanded_Then_Collapsed_MoreButton_VerticalAlignment()
		{
			var SUT = new CommandBar
			{
				PrimaryCommands =
				{
					new AppBarButton
					{
						Content = "PrimaryCommand"
					}
				},
				SecondaryCommands =
				{
					new AppBarButton
					{
						Content="SecondaryCommand"
					}
				}
			};

			await UITestHelper.Load(SUT);

			var moreButton = (Button)SUT.FindName("MoreButton");
#if !__ANDROID__ // layout timings are different on android
			Assert.AreEqual(48, moreButton.ActualHeight);
#endif
			Assert.AreEqual(VerticalAlignment.Top, moreButton.VerticalAlignment);

			SUT.IsOpen = true;
			await WindowHelper.WaitForIdle();
#if !__ANDROID__ // layout timings are different on android
			Assert.AreEqual(64, moreButton.ActualHeight);
#endif
			Assert.AreEqual(VerticalAlignment.Stretch, moreButton.VerticalAlignment);

			SUT.IsOpen = false;
			await Task.Delay(1000); // wait for animations
#if !__ANDROID__ // layout timings are different on android
			Assert.AreEqual(48, moreButton.ActualHeight);
#endif
			Assert.AreEqual(VerticalAlignment.Top, moreButton.VerticalAlignment);
		}

#if __APPLE_UIKIT__
		[TestMethod]
		[RequiresFullWindow]

		public async Task Can_Navigate_Forward_And_Backwards()
		{
			var frame = new Frame() { Width = 400, Height = 400 };
			var content = new Grid { Children = { frame } };

			WindowHelper.WindowContent = content;
			await WindowHelper.WaitForIdle();

			var firstNavBar = await frame.NavigateAndGetNavBar<CommandBarFirstPage>();

			await WindowHelper.WaitForLoaded(firstNavBar);

			var secondNavBar = await frame.NavigateAndGetNavBar<CommandBarSecondPage>();

			await WindowHelper.WaitForLoaded(secondNavBar);

			await Task.Delay(1000);

			frame.GoBack();

			await WindowHelper.WaitForLoaded(firstNavBar);
		}
#endif

		[TestMethod]
		public async Task When_IsOpen_True_LayoutCycle()
		{
			var SUT = new CommandBar
			{
				SecondaryCommands =
				{
					new AppBarButton { Content="SecondaryCommand" }
				}
			};
			await UITestHelper.Load(SUT);

			SUT.IsOpen = true;
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_LoadUnload_CommandBar_IsOpen_Resets_OnReload()
		{
			var SUT = new CommandBar
			{
				SecondaryCommands =
				{
					new AppBarButton { Label = "SecondaryCommand", Name = "SecondaryButton" }
				}
			};

			await UITestHelper.Load(SUT);
			await WindowHelper.WaitForIdle();
			await Task.Delay(1000);

			var moreBtn = (Button)SUT.FindName("MoreButton");
			Assert.IsNotNull(moreBtn);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var finger = injector.GetFinger();
			Point GetCenter(Rect rect) => new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
			finger.Press(GetCenter(moreBtn.GetAbsoluteBounds()));
			finger.Release();
			await Task.Delay(1000);
			await WindowHelper.WaitForIdle();
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot);
			Assert.HasCount(1, popups);

			var secondary = (AppBarButton)SUT.FindName("SecondaryButton");
			var sb = secondary.GetAbsoluteBounds();
			finger.Press(sb.Bottom + 10, (sb.Left + sb.Right) / 2);
			finger.Release();
			await WindowHelper.WaitForIdle();
			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot));

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();

			await UITestHelper.Load(SUT);
			await WindowHelper.WaitForIdle();

			moreBtn = (Button)SUT.FindName("MoreButton");
			Assert.IsNotNull(moreBtn);

			await Task.Delay(1000);
			finger.Press(GetCenter(moreBtn.GetAbsoluteBounds()));
			finger.Release();
			await WindowHelper.WaitForIdle();
			popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot);
			Assert.HasCount(1, popups);
		}
	}

#if __APPLE_UIKIT__
	public static class NavigationBarTestHelper
	{
		public static UINavigationBar GetNativeNavBar(this CommandBar navBar) => navBar
			?.TryGetNative<CommandBar, CommandBarRenderer, UINavigationBar>(out var native) ?? false ? native : null;

		public static UINavigationItem GetNativeNavItem(this CommandBar navBar) => navBar
			?.TryGetNative<CommandBar, CommandBarNavigationItemRenderer, UINavigationItem>(out var native) ?? false ? native : null;


		public static Task<CommandBar> NavigateAndGetNavBar<TPage>(this Frame frame) where TPage : Page
		{
			return frame.NavigateAndGetNavBar(typeof(TPage));
		}

		public static async Task<CommandBar> NavigateAndGetNavBar(this Frame frame, Type pageType)
		{
			frame.Navigate(pageType);
			await WindowHelper.WaitForIdle();

			var page = frame.Content as Page;
			await WindowHelper.WaitForLoaded(page!);
			return SharedHelpers.FindInVisualTreeByType<CommandBar>(page);
		}
	}
#endif

	// Repro tests for https://github.com/unoplatform/uno/issues/4396
	public partial class Given_CommandBar
	{
		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4396")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_CommandBarOverflowPresenterStyle_Is_Applied()
		{
			// Issue: CommandBar.CommandBarOverflowPresenterStyle is ignored.
			// Expected: The style set via CommandBarOverflowPresenterStyle should be
			// applied to the overflow presenter popup.

			var overflowStyle = new Style(typeof(CommandBarOverflowPresenter));
			overflowStyle.Setters.Add(new Setter(CommandBarOverflowPresenter.BackgroundProperty,
				new SolidColorBrush(Windows.UI.Colors.Red)));

			var sut = new CommandBar
			{
				Width = 300,
				CommandBarOverflowPresenterStyle = overflowStyle,
			};
			sut.SecondaryCommands.Add(new AppBarButton { Label = "Secondary 1" });
			sut.SecondaryCommands.Add(new AppBarButton { Label = "Secondary 2" });

			await UITestHelper.Load(sut);
			await UITestHelper.WaitForIdle();

			// Verify the style is stored on the CommandBar
			Assert.IsNotNull(sut.CommandBarOverflowPresenterStyle,
				"CommandBarOverflowPresenterStyle should be set on the CommandBar.");
			Assert.AreEqual(typeof(CommandBarOverflowPresenter),
				sut.CommandBarOverflowPresenterStyle.TargetType,
				"CommandBarOverflowPresenterStyle.TargetType should be CommandBarOverflowPresenter.");

			// Open the overflow popup to verify the style is applied
			sut.IsOpen = true;
			await UITestHelper.WaitForIdle();

			// Find the overflow presenter in the visual tree
			var overflow = sut.FindFirstDescendant<CommandBarOverflowPresenter>();
			if (overflow != null)
			{
				var bg = overflow.Background as SolidColorBrush;
				Assert.IsNotNull(bg, "CommandBarOverflowPresenter.Background should be set (Red from style).");
				Assert.AreEqual(Windows.UI.Colors.Red, bg.Color,
					$"Expected CommandBarOverflowPresenter.Background to be Red from the applied style, " +
					$"but got {bg?.Color}. This confirms CommandBarOverflowPresenterStyle is being ignored.");
			}

			sut.IsOpen = false;
		}
	}
}
