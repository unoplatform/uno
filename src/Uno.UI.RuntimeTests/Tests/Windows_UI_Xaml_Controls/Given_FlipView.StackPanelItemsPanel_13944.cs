using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FlipView_StackPanelItemsPanel_13944
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/13944
		// Setting <FlipView.ItemsPanel> to a horizontal StackPanel was reported to:
		//   - throw on iOS
		//   - render with wrong layout on Android
		//   - render nothing on Skia / WASM
		// Each FlipViewItem has a fixed Height=500. WinUI auto-sizes the FlipView
		// to that height; Uno renders nothing on Skia.
		[TestMethod]
		public async Task When_FlipView_Has_StackPanel_ItemsPanel_13944()
		{
			var template = (ItemsPanelTemplate)XamlReader.Load(
				"""
				<ItemsPanelTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<StackPanel Orientation="Horizontal" />
				</ItemsPanelTemplate>
				""");

			var flipView = new FlipView
			{
				ItemsPanel = template,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Top,
			};

			for (var i = 0; i < 3; i++)
			{
				flipView.Items.Add(new FlipViewItem
				{
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.LightBlue),
						Height = 500,
						Child = new TextBlock { Text = $"Item {i}" },
					},
				});
			}

			var host = new StackPanel { Children = { flipView } };

			WindowHelper.WindowContent = host;
			await WindowHelper.WaitForLoaded(host);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(
				flipView.IsLoaded,
				"FlipView should load when its ItemsPanel is a StackPanel.");

			Assert.IsTrue(
				flipView.ActualHeight > 0,
				$"FlipView should auto-size to its content height (Item Border Height=500). Actual={flipView.ActualHeight}. " +
				$"See https://github.com/unoplatform/uno/issues/13944");
		}
	}
}
