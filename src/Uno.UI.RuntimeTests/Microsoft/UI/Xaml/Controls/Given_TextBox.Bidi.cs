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
}
