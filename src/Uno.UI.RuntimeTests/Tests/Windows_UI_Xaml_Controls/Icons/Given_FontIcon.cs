using System;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
[RunsOnUIThread]
public class Given_FontIcon
{
	[TestMethod]
	public void When_Defaults()
	{
		var fontIcon = new FontIcon();
		Assert.AreEqual("", fontIcon.Glyph);
		Assert.AreEqual(20.0, fontIcon.FontSize);
		Assert.AreNotEqual(new TextBlock().FontFamily.Source, fontIcon.FontFamily.Source);
		Assert.AreEqual(FontWeights.Normal.Weight, fontIcon.FontWeight.Weight);
		Assert.AreEqual(FontStyle.Normal, fontIcon.FontStyle);
		Assert.IsTrue(fontIcon.IsTextScaleFactorEnabled);
	}

	[TestMethod]
	public async Task When_Themed()
	{
		var textBlock = new TextBlock() { Text = "test" };
		var fontIcon = new FontIcon() { Glyph = "\uE890" };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock,
				fontIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var fontIconBrush = (SolidColorBrush)fontIcon.Foreground;
		Assert.AreEqual(textBlockBrush.Color, fontIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			fontIconBrush = (SolidColorBrush)fontIcon.Foreground;
			Assert.AreEqual(textBlockBrush.Color, fontIconBrush.Color);
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
		var fontIcon = new FontIcon() { Glyph = "\uE890" };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock,
				fontIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var innerTextBlock = VisualTreeUtils.FindVisualChildByType<TextBlock>(fontIcon);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var fontIconBrush = (SolidColorBrush)innerTextBlock.Foreground;
		Assert.AreEqual(textBlockBrush.Color, fontIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			fontIconBrush = (SolidColorBrush)innerTextBlock.Foreground;
			Assert.AreEqual(textBlockBrush.Color, fontIconBrush.Color);
		}

		fontIcon.Foreground = new SolidColorBrush(Colors.Red);
		fontIconBrush = (SolidColorBrush)innerTextBlock.Foreground;
		Assert.AreEqual(Colors.Red, fontIconBrush.Color);
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
