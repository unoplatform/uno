using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using Uno.UI.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_CommandBar
	{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaTvOS)]
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
			Assert.AreEqual(48, moreButton.ActualHeight);
			Assert.AreEqual(VerticalAlignment.Top, moreButton.VerticalAlignment);

			SUT.IsOpen = true;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(64, moreButton.ActualHeight);
			Assert.AreEqual(VerticalAlignment.Stretch, moreButton.VerticalAlignment);

			SUT.IsOpen = false;
			await Task.Delay(1000); // wait for animations
			Assert.AreEqual(48, moreButton.ActualHeight);
			Assert.AreEqual(VerticalAlignment.Top, moreButton.VerticalAlignment);
		}

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

}
