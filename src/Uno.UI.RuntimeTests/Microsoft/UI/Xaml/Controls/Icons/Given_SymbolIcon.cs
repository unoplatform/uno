using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI;
using Windows.UI;

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
	[Ignore("Fails on all targets https://github.com/unoplatform/uno/issues/9080")]
	public async Task Validate_Size()
	{
		var symbolIcon = new SymbolIcon() { Symbol = Symbol.Home };
		TestServices.WindowHelper.WindowContent = symbolIcon;
		await TestServices.WindowHelper.WaitForLoaded(symbolIcon);

		Assert.AreEqual(20.0, symbolIcon.ActualWidth);
		Assert.AreEqual(20.0, symbolIcon.ActualHeight);
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
	public async Task When_Themed_Uwp()
	{
		using var _ = StyleHelper.UseUwpStyles();
		await When_Themed();
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
	public async Task When_Themed_TextBlock_Uwp()
	{
		using var _ = StyleHelper.UseUwpStyles();
		await When_Themed_TextBlock();
	}

	[TestMethod]
	public void When_Default_Constructor_Then_Symbol_Not_Set_Locally()
	{
		var symbolIcon = new SymbolIcon();

		Assert.AreEqual(DependencyProperty.UnsetValue, symbolIcon.ReadLocalValue(SymbolIcon.SymbolProperty));
		Assert.AreEqual(Symbol.Emoji, symbolIcon.Symbol);
	}

	[TestMethod]
	public async Task When_Style_Sets_Symbol()
	{
		var style = new Style(typeof(SymbolIcon));
		style.Setters.Add(new Setter(SymbolIcon.SymbolProperty, Symbol.Home));
		var symbolIcon = new SymbolIcon() { Style = style };

		await UITestHelper.Load(symbolIcon);

		Assert.AreEqual(Symbol.Home, symbolIcon.Symbol);
	}

	[TestMethod]
	public async Task When_Inside_Bold_Parent_Then_Glyph_Not_Bold()
	{
		var symbolIcon = new SymbolIcon(Symbol.Home);
		var host = new ContentControl() { FontWeight = FontWeights.Bold, Content = symbolIcon };

		await UITestHelper.Load(host);

		var textBlock = (TextBlock)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(symbolIcon, 0), 0);
		Assert.AreEqual(FontWeights.Normal.Weight, textBlock.FontWeight.Weight);
	}
}
