#if HAS_UNO
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Text;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
public class Given_FontDetailsCache
{
	private const string UnknownFamily = "Uno Test Font That Does Not Exist";

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20828")]
	public async Task When_Family_Unknown_Then_Default_Text_Font_Is_Used()
	{
		var (_, defaultTask) = FontDetailsCache.GetFont(null, 36, FontWeights.Bold, FontStretch.Normal, FontStyle.Normal);
		var (_, unknownTask) = FontDetailsCache.GetFont(UnknownFamily, 36, FontWeights.Bold, FontStretch.Normal, FontStyle.Normal);

		var defaultDetails = await defaultTask;
		var unknownDetails = await unknownTask;

		Assert.AreEqual(defaultDetails.SKFont.Typeface.FamilyName, unknownDetails.SKFont.Typeface.FamilyName);
		Assert.AreEqual(defaultDetails.SKFont.Typeface.FontWeight, unknownDetails.SKFont.Typeface.FontWeight);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20828")]
	public async Task When_Family_Unknown_Then_TextBlock_Measures_Like_Default()
	{
		var defaultText = new TextBlock { Text = "5.0", FontSize = 36, FontWeight = FontWeights.Bold };
		var unknownText = new TextBlock { Text = "5.0", FontSize = 36, FontWeight = FontWeights.Bold, FontFamily = new FontFamily(UnknownFamily) };
		var panel = new StackPanel { Children = { defaultText, unknownText } };

		try
		{
			await UITestHelper.Load(panel);
			await WindowHelper.WaitFor(() => unknownText.ActualWidth > 0 && System.Math.Abs(unknownText.ActualWidth - defaultText.ActualWidth) < 0.5, 3000, "unknown family measured like the default font");

			Assert.AreEqual(defaultText.ActualWidth, unknownText.ActualWidth, 0.5);
			Assert.AreEqual(defaultText.ActualHeight, unknownText.ActualHeight, 0.5);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
#endif
