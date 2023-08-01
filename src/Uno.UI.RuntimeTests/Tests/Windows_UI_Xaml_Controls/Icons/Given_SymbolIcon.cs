using System.Threading.Tasks;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
[RunsOnUIThread]
public class Given_SymbolIcon
{
	[TestMethod]
	public void When_Defaults()
	{
		var symbolIcon = new SymbolIcon();
		Assert.AreEqual(Symbol.Emoji, symbolIcon.Symbol);
	}

	[TestMethod]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public async Task Validate_Size()
	{
		var symbolIcon = new SymbolIcon() { Symbol = Symbol.Home };
		TestServices.WindowHelper.WindowContent = symbolIcon;
		await TestServices.WindowHelper.WaitForLoaded(symbolIcon);

		Assert.AreEqual(20.0, symbolIcon.ActualWidth);

#if __ANDROID__
		// This should be 20.0, but for unknown reason the symbol is measured with 22 Height.
		Assert.AreEqual(22.0, symbolIcon.ActualHeight);
#else
		Assert.AreEqual(20.0, symbolIcon.ActualHeight);
#endif
	}

	[TestMethod]
	public async Task When_Themed()
	{
		var textBlock = new TextBlock() { Text = "test" };
		var symbolIcon = new SymbolIcon() { Symbol = Symbol.Home };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock,
				symbolIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var symbolIconBrush = (SolidColorBrush)symbolIcon.Foreground;
		Assert.AreEqual(textBlockBrush.Color, symbolIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			symbolIconBrush = (SolidColorBrush)symbolIcon.Foreground;
			Assert.AreEqual(textBlockBrush.Color, symbolIconBrush.Color);
		}
	}

	[TestMethod]
	public async Task When_Themed_Fluent()
	{
		using (StyleHelper.UseFluentStyles())
		{
			await When_Themed();
		}
	}

	[TestMethod]
	public async Task When_Themed_TextBlock()
	{
		var textBlock = new TextBlock() { Text = "test" };
		var symbolIcon = new SymbolIcon() { Symbol = Symbol.Home };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock,
				symbolIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var innerTextBlock = VisualTreeUtils.FindVisualChildByType<TextBlock>(symbolIcon);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var symbolIconBrush = (SolidColorBrush)innerTextBlock.Foreground;
		Assert.AreEqual(textBlockBrush.Color, symbolIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			symbolIconBrush = (SolidColorBrush)innerTextBlock.Foreground;
			Assert.AreEqual(textBlockBrush.Color, symbolIconBrush.Color);
		}

		symbolIcon.Foreground = new SolidColorBrush(Colors.Red);
		symbolIconBrush = (SolidColorBrush)innerTextBlock.Foreground;
		Assert.AreEqual(Colors.Red, symbolIconBrush.Color);
	}

	[TestMethod]
	public async Task When_Themed_TextBlock_Fluent()
	{
		using (StyleHelper.UseFluentStyles())
		{
			await When_Themed_TextBlock();
		}
	}
}
