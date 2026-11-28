using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.TextInkHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Visual order of RichEditBox paragraphs, read from the colour of the ink, where Uno matches WinUI.
// WinUI lays out every RichEditBox paragraph on a left-to-right base and only right-aligns a right-to-left
// paragraph. Uno doesn't yet for a right-to-left paragraph in a left-to-right control, a Hebrew-first paragraph,
// or the alignment of right-to-left paragraphs, so those cases aren't asserted here.
public partial class Given_RichEditBox
{
	private const string HebrewWord1 = "\u05E9\u05DC\u05D5\u05DD";
	private const string HebrewWord2 = "\u05E2\u05D5\u05DC\u05DD";

	private static async Task<(Grid Host, RichEditBox RichEditBox)> ShowRichEditBox(FlowDirection flowDirection)
	{
		var richEditBox = new RichEditBox { Width = 300, FontSize = 20, FlowDirection = flowDirection, BorderThickness = new Thickness(0) };
		var (host, _) = await Show(richEditBox);
		return (host, richEditBox);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Rtl_RichEditBox_Latin_Keeps_Dots_At_End(bool rightToLeftParagraph)
	{
		try
		{
			var (host, sut) = await ShowRichEditBox(FlowDirection.RightToLeft);
			const string text = "Hello world......";
			sut.Document.SetText(TextSetOptions.None, text);
			sut.Document.GetRange(0, text.Length).CharacterFormat.ForegroundColor = Colors.Red;
			if (rightToLeftParagraph)
			{
				sut.Document.GetRange(0, text.Length).ParagraphFormat.RightToLeft = FormatEffect.On;
			}

			var ink = Letters(await Capture(host), Colors.Red);

			Assert.IsTrue(DotsOnTheRight(ink), $"The dots should be right of the letters: ink {ink.Ink}, letters {ink.Letters}.");
			if (!rightToLeftParagraph)
			{
				Assert.IsTrue(ink.Ink.Min < 60, $"The text should be left-aligned: ink {ink.Ink}.");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_Information_Separator_In_Text(FlowDirection flowDirection)
	{
		// U+001E is a paragraph separator to the bidi algorithm, but, as on WinUI, the text stays on one line.
		try
		{
			var (host, sut) = await ShowRichEditBox(flowDirection);
			const string text = "abc\u001Edéf";
			sut.Document.SetText(TextSetOptions.None, text);
			sut.Document.GetRange(0, text.Length).CharacterFormat.ForegroundColor = Colors.Red;

			var (top, bottom) = InkY(await Capture(host), Colors.Red);

			Assert.IsTrue(bottom >= 0 && bottom - top < 30, $"The text should be drawn on one line: rows {top}..{bottom}.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	// Needs a font with Hebrew letters: Segoe UI has them, other platforms depend on their fallback fonts.
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Rtl_RichEditBox_Hebrew_Keeps_Ltr_Base(bool afterLatinParagraph)
	{
		try
		{
			var (host, sut) = await ShowRichEditBox(FlowDirection.RightToLeft);
			var hebrew = $"{HebrewWord1} 12 {HebrewWord2}......";
			var start = afterLatinParagraph ? "Hello\r".Length : 0;
			var text = (afterLatinParagraph ? "Hello\r" : "") + hebrew;
			sut.Document.SetText(TextSetOptions.None, text);
			sut.Document.GetRange(0, text.Length + 1).CharacterFormat.ForegroundColor = Colors.Blue;
			sut.Document.GetRange(start, start + hebrew.Length).CharacterFormat.ForegroundColor = Colors.Red;
			sut.Document.GetRange(start, start + HebrewWord1.Length).CharacterFormat.ForegroundColor = Colors.Green;

			var bitmap = await Capture(host);
			var red = Letters(bitmap, Colors.Red);
			var firstWord = InkX(bitmap, Colors.Green);

			// On a left-to-right base the Hebrew words and the number read right to left, so the first word is
			// drawn right of "12" and the second word, and the dots follow on the right.
			Assert.IsTrue(firstWord.Min > red.Letters.Min, $"The first word ({firstWord}) should be right of the second word and 12 ({red.Letters}).");
			Assert.IsTrue(red.Ink.Max > firstWord.Max, $"The dots ({red.Ink}) should be right of the first word ({firstWord}).");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
