#nullable disable

using Private.Infrastructure;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
[RunsOnUIThread]
public class Given_BitmapIcon
{
	[TestMethod]
	public async Task When_Themed()
	{
		var textBlock = new TextBlock() { Text = "test" };
		var bitmapIcon = new BitmapIcon() { UriSource = new System.Uri("ms-appx:///Assets/Icons/search.png") };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock,
				bitmapIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var bitmapIconBrush = (SolidColorBrush)bitmapIcon.Foreground;
		Assert.AreEqual(textBlockBrush.Color, bitmapIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			bitmapIconBrush = (SolidColorBrush)bitmapIcon.Foreground;
			Assert.AreEqual(textBlockBrush.Color, bitmapIconBrush.Color);
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
}
