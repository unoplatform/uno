using System;
using Windows.UI;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
				Windows.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups();
#endif
			}
		}

		[TestMethod]
		public Task When_Switch_Theme_UWP() => When_Switch_Theme_Inner(brush => (brush as SolidColorBrush).Color);

		[TestMethod]
		public async Task When_Switch_Theme_Fluent()
		{
			using var _ = StyleHelper.UseFluentStyles();
			await When_Switch_Theme_Inner(brush => (brush as AcrylicBrush).TintColor);
		}

		private async Task When_Switch_Theme_Inner(Func<Brush, Color> backgroundColorGetter)
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

				SUT.IsOpen = true;

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
#endif
			}
		}
	}
}
