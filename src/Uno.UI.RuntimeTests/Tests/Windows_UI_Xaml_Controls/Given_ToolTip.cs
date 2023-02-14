using System;
using Windows.UI;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ToolTip
	{
		[TestMethod]
		public async Task When_DataContext_Set_On_ToolTip_Owner()
		{
			try
			{
				var textBlock = new TextBlock();
				var SUT = new ToolTip();
				ToolTipService.SetToolTip(textBlock, SUT);
				var stackPanel = new StackPanel
				{
					Children =
					{
						textBlock,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", textBlock.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				SUT.IsOpen = true;

				stackPanel.DataContext = "DataContext2";

				Assert.AreEqual("DataContext2", textBlock.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups();
#endif
			}
		}

#if !__IOS__ // Disabled due to #10791
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		[TestMethod]
		public Task When_Switch_Theme_UWP() => When_Switch_Theme_Inner(brush => (brush as SolidColorBrush).Color);

#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		[TestMethod]
		public async Task When_Switch_Theme_Fluent()
		{
			using var _ = StyleHelper.UseFluentStyles();
			await When_Switch_Theme_Inner(brush => (brush as AcrylicBrush).TintColor);
		}

		private async Task When_Switch_Theme_Inner(Func<Brush, Color> backgroundColorGetter)
		{
#if HAS_UNO
			var originalToolTipsSetting = Uno.UI.FeatureConfiguration.ToolTip.UseToolTips;
			Uno.UI.FeatureConfiguration.ToolTip.UseToolTips = true;
#endif
			try
			{
				var textBlock = new TextBlock() { Text = "Test" };
				var SUT = new ToolTip()
				{
					Content = "I'm a ToolTip!"
				};
				ToolTipService.SetToolTip(textBlock, SUT);
				var stackPanel = new StackPanel
				{
					Children =
					{
						textBlock,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				await Task.Delay(1000);
				await TestServices.WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(textBlock.XamlRoot);
				var popup = popups[0];
				var toolTipChild = popup.Child as ToolTip;
				Assert.AreEqual(SUT, toolTipChild);
				var color = backgroundColorGetter(toolTipChild.Background);
				Assert.IsTrue(color.R > 100 && color.G > 100 && color.B > 100);

				using var _ = ThemeHelper.UseDarkTheme();

				await TestServices.WindowHelper.WaitForIdle();
				color = backgroundColorGetter(toolTipChild.Background);
				Assert.IsTrue(color.R < 100 && color.G < 100 && color.B < 100);
			}
			finally
			{
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups();
				Uno.UI.FeatureConfiguration.ToolTip.UseToolTips = originalToolTipsSetting;
#endif
			}
		}
#endif

		[TestMethod]
		public async Task When_ToolTip_Popup_XamlRoot()
		{
#if HAS_UNO
			var originalToolTipsSetting = Uno.UI.FeatureConfiguration.ToolTip.UseToolTips;
			Uno.UI.FeatureConfiguration.ToolTip.UseToolTips = true;
#endif
			var toolTip = new ToolTip();
			try
			{
				var host = new Button() { Content = "Asd" };
				toolTip.Content = new Button() { Content = "Test" };

				TestServices.WindowHelper.WindowContent = host;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForLoaded(host);

				ToolTipService.SetToolTip(host, toolTip);
				toolTip.IsOpen = true;

				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(host.XamlRoot, toolTip.XamlRoot);
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].Child.XamlRoot);
			}
			finally
			{
				toolTip.IsOpen = false;
#if HAS_UNO
				Uno.UI.FeatureConfiguration.ToolTip.UseToolTips = originalToolTipsSetting;
#endif
			}
		}
	}
}
