using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.TextInkHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_TextBox
{
	[TestMethod]
	public async Task When_Rtl_TextBox_Latin_Keeps_Period_At_End()
	{
		var sut = new TextBox
		{
			Width = 300,
			FontSize = 20,
			FlowDirection = FlowDirection.RightToLeft,
			Text = "Hello world......",
			Foreground = new SolidColorBrush(Colors.Red),
			BorderThickness = new Thickness(0),
		};

		try
		{
			var (_, bitmap) = await Show(sut);
			var ink = Letters(bitmap, Colors.Red);

			// As on WinUI: left-to-right text keeps its trailing dots at its end, and stays on the left.
			Assert.IsTrue(DotsOnTheRight(ink), $"The dots should be right of the letters: ink {ink.Ink}, letters {ink.Letters}.");
			Assert.IsTrue(ink.Ink.Min < 60, $"The text should stay left-aligned: ink {ink.Ink}.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(0x1C, FlowDirection.LeftToRight)]
	[DataRow(0x1C, FlowDirection.RightToLeft)]
	[DataRow(0x1D, FlowDirection.LeftToRight)]
	[DataRow(0x1D, FlowDirection.RightToLeft)]
	[DataRow(0x1E, FlowDirection.LeftToRight)]
	[DataRow(0x1E, FlowDirection.RightToLeft)]
	public async Task When_Information_Separator_In_Text(int separator, FlowDirection flowDirection)
	{
		// U+001C-U+001E are paragraph separators to the bidi algorithm, but, as on WinUI, the text stays on one line.
		// The "é" keeps the left-to-right layout off the ASCII shortcut, so it goes through ICU too.
		var text = "abc" + (char)separator + "déf";
		var sut = new TextBox
		{
			Width = 300,
			FontSize = 20,
			FlowDirection = flowDirection,
			Text = text,
			Foreground = new SolidColorBrush(Colors.Red),
			BorderThickness = new Thickness(0),
		};

		try
		{
			var (_, bitmap) = await Show(sut);
			var (top, bottom) = InkY(bitmap, Colors.Red);

			Assert.AreEqual(text, sut.Text);
			Assert.IsTrue(bottom >= 0 && bottom - top < 30, $"The text should be drawn on one line: rows {top}..{bottom}.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_Information_Separator_In_Revealed_Password(FlowDirection flowDirection)
	{
		// A masked password lays out bullets only; a revealed one lays out its characters.
		var sut = new PasswordBox
		{
			Width = 300,
			FontSize = 20,
			FlowDirection = flowDirection,
			PasswordRevealMode = PasswordRevealMode.Visible,
			Password = "abc\u001Edéf",
			Foreground = new SolidColorBrush(Colors.Red),
			BorderThickness = new Thickness(0),
		};

		try
		{
			var (_, bitmap) = await Show(sut);
			var (top, bottom) = InkY(bitmap, Colors.Red);

			// Bullets are about half as tall as "déf" (8 rows to 14 here), whose accent and ascenders reach above them.
			Assert.IsTrue(bottom - top > 11, $"The password's letters should be drawn, not bullets: rows {top}..{bottom}.");
			Assert.IsTrue(bottom - top < 30, $"The password should be drawn on one line: rows {top}..{bottom}.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static TextBox CreateBidiTextBox(string text) => new()
	{
		Width = 300,
		FontSize = 32,
		FlowDirection = FlowDirection.RightToLeft,
		Text = text,
		Foreground = new SolidColorBrush(Colors.Red),
		BorderThickness = new Thickness(0),
	};

	[TestMethod]
	public async Task When_Rtl_TextBox_Arabic_Ends_With_Number()
	{
		// unoplatform/uno#25979 in a TextBox: the words are drawn as without the number after them.
		var sut = CreateBidiTextBox("نیا چاند 1");
		var alone = CreateBidiTextBox("نیا چاند");

		try
		{
			var bitmap = await Given_TextBlock_Bidi.ShowWithFonts(sut);
			// The number is drawn left of the words.
			var pieces = Pieces(bitmap, Colors.Red);
			Assert.IsTrue(pieces.Count >= 2, $"Expected the number and the words apart: {string.Join(", ", pieces)}.");
			var words = Profile(bitmap, Colors.Red, left: pieces[1].Min);
			var expectedBitmap = await Given_TextBlock_Bidi.ShowWithFonts(alone);

			Given_TextBlock_Bidi.AssertSameShape(Profile(expectedBitmap, Colors.Red), words, "The words");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Rtl_TextBox_Bracket_Before_Number_Is_Mirrored()
	{
		var sut = CreateBidiTextBox("(1");
		sut.FontSize = 48;

		try
		{
			var (_, bitmap) = await Show(sut);
			var pieces = Pieces(bitmap, Colors.Red);

			// The bracket is drawn right of the number, shaped right to left and mirrored, as ")".
			Assert.AreEqual(2, pieces.Count, $"Expected the number and the bracket: {string.Join(", ", pieces)}.");
			Assert.IsTrue(BulgesRight(bitmap, Colors.Red, pieces[1]), $"The bracket ({pieces[1]}) should be drawn mirrored.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
