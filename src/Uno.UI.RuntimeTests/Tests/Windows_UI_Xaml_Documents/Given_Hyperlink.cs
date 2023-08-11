using System.Linq;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
public class Given_Hyperlink
{
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(true, false, "#FF0078D7")]
	[DataRow(false, false, "#FF0078D7")]
	[DataRow(true, true, "#FFA6D8FF")]
	[DataRow(false, true, "#FF004275")]
	public async Task TestSimpleHyperlink(bool useDark, bool useFluent, string expectedColorCode)
	{
		var expectedColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode);
		using (useDark ? ThemeHelper.UseDarkTheme() : null)
		{
			using (useFluent ? StyleHelper.UseFluentStyles() : null)
			{
				var tb = (TextBlock)XamlReader.Load("""
				<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<Hyperlink>Hello</Hyperlink>
				</TextBlock>
				""");
				WindowHelper.WindowContent = tb;
				await WindowHelper.WaitForLoaded(tb);

				var run = (Run)((Hyperlink)tb.Inlines.Single()).Inlines.Single();
				Assert.AreEqual(expectedColor, ((SolidColorBrush)run.Foreground).Color);
			}
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(true, false, "#FF0078D7")]
	[DataRow(false, false, "#FF0078D7")]
	[DataRow(true, true, "#FFA6D8FF")]
	[DataRow(false, true, "#FF004275")]
	public async Task TestNotInheritedFromTextBlock(bool useDark, bool useFluent, string expectedColorCode)
	{
		var expectedColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode);
		using (useDark ? ThemeHelper.UseDarkTheme() : null)
		{
			using (useFluent ? StyleHelper.UseFluentStyles() : null)
			{
				var tb = (TextBlock)XamlReader.Load("""
					<TextBlock TextDecorations='Strikethrough' Foreground='Red' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
						<Hyperlink>Not red</Hyperlink><Run>Red</Run>
					</TextBlock>
					""");
				WindowHelper.WindowContent = tb;
				await WindowHelper.WaitForLoaded(tb);

				Assert.AreEqual(2, tb.Inlines.Count);
				var hyperlink = (Hyperlink)tb.Inlines[0];
				var hyperlinkRun = (Run)hyperlink.Inlines.Single();
				var redRun = (Run)tb.Inlines[1];

				Assert.AreEqual(Colors.Red, ((SolidColorBrush)redRun.Foreground).Color);
				Assert.AreEqual(TextDecorations.Strikethrough, redRun.TextDecorations);

				Assert.AreEqual(expectedColor, ((SolidColorBrush)hyperlinkRun.Foreground).Color);
				Assert.AreEqual(TextDecorations.Underline, hyperlinkRun.TextDecorations);
			}
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task TestRespectHyperlinkForeground()
	{
		const string HyperlinkForeground = nameof(HyperlinkForeground);

		var appLevelResources = new ResourceDictionary();
		appLevelResources[HyperlinkForeground] = new SolidColorBrush(Colors.Orange);
		using (StyleHelper.UseAppLevelResources(appLevelResources))
		{
			var tb = (TextBlock)XamlReader.Load("""
						<TextBlock TextDecorations='Strikethrough' Foreground='Red' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<Hyperlink>Not red</Hyperlink><Run>Red</Run>
						</TextBlock>
						""");
			WindowHelper.WindowContent = tb;
			await WindowHelper.WaitForLoaded(tb);

			Assert.AreEqual(2, tb.Inlines.Count);
			var hyperlink = (Hyperlink)tb.Inlines[0];
			var hyperlinkRun = (Run)hyperlink.Inlines.Single();
			var redRun = (Run)tb.Inlines[1];

			Assert.AreEqual(Colors.Red, ((SolidColorBrush)redRun.Foreground).Color);
			Assert.AreEqual(TextDecorations.Strikethrough, redRun.TextDecorations);

			Assert.AreEqual(Colors.Orange, ((SolidColorBrush)hyperlinkRun.Foreground).Color);
			Assert.AreEqual(TextDecorations.Underline, hyperlinkRun.TextDecorations);

		}
	}
}
