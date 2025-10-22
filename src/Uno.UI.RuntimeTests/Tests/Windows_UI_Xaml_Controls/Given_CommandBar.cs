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

using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.CommandBarPages;

#if __APPLE_UIKIT__
using Uno.UI.Controls;
using Uno.UI.Helpers.WinUI;
using UIKit;
#endif

#if __ANDROID__
using Uno.UI.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_CommandBar
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
			Assert.AreEqual(1, popups.Count);

			var secondaryButton = (AppBarButton)SUT.FindName("SecondaryButton");
			var bounds = secondaryButton.GetAbsoluteBounds();
			finger.Press(bounds.Bottom + 10, (bounds.Left + bounds.Right) / 2);
			finger.Release();

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot).Count);
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("VerticalAlignment asserts fail. Might be because of different timing.")]
#endif
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
			Assert.AreEqual(1, popups.Count);

			var secondary = (AppBarButton)SUT.FindName("SecondaryButton");
			var sb = secondary.GetAbsoluteBounds();
			finger.Press(sb.Bottom + 10, (sb.Left + sb.Right) / 2);
			finger.Release();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot).Count);

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
			Assert.AreEqual(1, popups.Count);
		}

		[TestMethod]
#if !__ANDROID__
		[Ignore("This test is specific to Android native CommandBar.")]
#endif
		public async Task When_CommandBar_Has_Long_Content_Then_PrimaryCommands_Remain_Visible()
		{
			var contentTextBlock = new TextBlock
			{
				Text = "This is a very long text that should be truncated to prevent primary commands from being pushed off screen",
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				VerticalAlignment = VerticalAlignment.Center
			};

			var primaryButton = new AppBarButton
			{
				Label = "Action",
				Name = "PrimaryButton"
			};

			var SUT = new CommandBar
			{
				Content = contentTextBlock,
				PrimaryCommands = { primaryButton }
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			// Wait for layout to complete
			await Task.Delay(500);
			await WindowHelper.WaitForIdle();

#if __ANDROID__
			// Get the native toolbar
			var renderer = SUT.GetRenderer(() => throw new InvalidOperationException("Renderer not found"));
			var toolbar = renderer?.Native as AndroidX.AppCompat.Widget.Toolbar;
			Assert.IsNotNull(toolbar, "Native toolbar should be available");

			// Get the content container (Border)
			var contentContainer = SUT.FindName("CommandBarRendererContentHolder") as Border;
			if (contentContainer == null)
			{
				// Try to find it through visual tree search
				contentContainer = FindInVisualTree<Border>(SUT, b => b.Name == "CommandBarRendererContentHolder");
			}
			Assert.IsNotNull(contentContainer, "Content container should exist");

			// Verify that MaxWidth is set and reasonable
			Assert.IsTrue(contentContainer.MaxWidth > 0, $"Content MaxWidth should be greater than 0, but was {contentContainer.MaxWidth}");
			Assert.IsTrue(contentContainer.MaxWidth < toolbar.Width, $"Content MaxWidth ({contentContainer.MaxWidth}) should be less than toolbar width ({toolbar.Width})");

			// Verify that content is actually constrained
			var contentBounds = contentTextBlock.GetAbsoluteBounds();
			var toolbarBounds = SUT.GetAbsoluteBounds();
			
			// Content width should not exceed the toolbar width
			Assert.IsTrue(contentBounds.Width <= toolbarBounds.Width, 
				$"Content width ({contentBounds.Width}) should not exceed toolbar width ({toolbarBounds.Width})");

			// Verify primary button is visible and within bounds
			var buttonBounds = primaryButton.GetAbsoluteBounds();
			Assert.IsTrue(buttonBounds.Right <= toolbarBounds.Right, 
				$"Primary button right edge ({buttonBounds.Right}) should be within toolbar bounds ({toolbarBounds.Right})");
#endif
		}

		private static T? FindInVisualTree<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
		{
			if (parent == null)
			{
				return null;
			}

			var childCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childCount; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T typed && (predicate == null || predicate(typed)))
				{
					return typed;
				}

				var result = FindInVisualTree(child, predicate);
				if (result != null)
				{
					return result;
				}
			}

			return null;
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
}
