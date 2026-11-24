using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Text;
using Windows.UI.Text;

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
	public async Task When_Themed_Uwp()
	{
		using var _ = StyleHelper.UseUwpStyles();
		await When_Themed();
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
	public async Task When_Themed_TextBlock_Uwp()
	{
		using var _ = StyleHelper.UseUwpStyles();
		await When_Themed_TextBlock();
	}

	[TestMethod]
	public async Task When_Inside_Bold_Parent_Then_Glyph_Not_Bold()
	{
		var fontIcon = new FontIcon() { Glyph = "\uE890" };
		var host = new ContentControl() { FontWeight = FontWeights.Bold, Content = fontIcon };

		await UITestHelper.Load(host);

		Assert.AreEqual(FontWeights.Normal.Weight, GetGlyphTextBlock(fontIcon).FontWeight.Weight);
	}

	[TestMethod]
	public async Task When_Loaded_Then_Glyph_TextBlock_Layout()
	{
		var fontIcon = new FontIcon() { Glyph = "\uE890" };

		await UITestHelper.Load(fontIcon);

		var textBlock = GetGlyphTextBlock(fontIcon);
		Assert.AreEqual(HorizontalAlignment.Stretch, textBlock.HorizontalAlignment);
		Assert.IsFalse(textBlock.IsTextScaleFactorEnabled);
	}

	[TestMethod]
	public async Task When_MirroredWhenRightToLeft_In_RightToLeft_Parent()
	{
		var fontIcon = new FontIcon() { Glyph = "\uE72B", MirroredWhenRightToLeft = true };
		var host = new StackPanel() { FlowDirection = FlowDirection.RightToLeft, Children = { fontIcon } };

		await UITestHelper.Load(host);

		var scaleTransform = fontIcon.RenderTransform as ScaleTransform;
		Assert.IsNotNull(scaleTransform);
		Assert.AreEqual(-1.0, scaleTransform.ScaleX);

		host.FlowDirection = FlowDirection.LeftToRight;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1.0, scaleTransform.ScaleX);
	}

	private static TextBlock GetGlyphTextBlock(FontIcon fontIcon) =>
		(TextBlock)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(fontIcon, 0), 0);
}
