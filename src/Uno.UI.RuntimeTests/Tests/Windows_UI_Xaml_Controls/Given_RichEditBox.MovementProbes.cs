#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Paragraph_Vertical_Move_And_Extend_Match_Native()
	{
		var editor = new RichEditBox { Width = 260, TextWrapping = TextWrapping.NoWrap };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "aa\rbbbb\rcc");
			await WindowHelper.WaitForIdle();

			var selection = editor.Document.Selection;
			selection.SetRange(1, 1);
			Assert.AreEqual(1, selection.MoveUp(TextRangeUnit.Paragraph, 1, false));
			Assert.AreEqual(0, selection.StartPosition);

			selection.SetRange(1, 1);
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Paragraph, 1, false));
			Assert.AreEqual(3, selection.StartPosition);

			selection.SetRange(4, 4);
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Paragraph, 1, true));
			Assert.AreEqual(4, selection.StartPosition);
			Assert.AreEqual(8, selection.EndPosition);

			selection.SetRange(4, 4);
			Assert.AreEqual(1, selection.MoveUp(TextRangeUnit.Paragraph, 1, true));
			Assert.AreEqual(3, selection.StartPosition);
			Assert.AreEqual(4, selection.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Mixed_Height_Visual_Line_Move_Preserves_Sticky_X()
	{
		var editor = new RichEditBox { Width = 260, TextWrapping = TextWrapping.NoWrap };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "abcd\rxy\rabcd");
			editor.Document.GetRange(5, 7).CharacterFormat.Size = 54;
			editor.Document.GetRange(5, 7).ParagraphFormat.SetLineSpacing(LineSpacingRule.Exactly, 66);
			await WindowHelper.WaitForIdle();

			var selection = editor.Document.Selection;
			selection.SetRange(3, 3);
			selection.GetRect(PointOptions.ClientCoordinates, out var original, out _);

			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Line, 1, false));
			Assert.IsTrue(selection.StartPosition is >= 5 and <= 7);
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Line, 1, false));
			Assert.AreEqual(2, selection.MoveUp(TextRangeUnit.Line, 2, false));

			selection.GetRect(PointOptions.ClientCoordinates, out var roundTrip, out _);
			Assert.IsTrue(selection.StartPosition is >= 3 and <= 4);
			Assert.AreEqual(original.X, roundTrip.X, 12);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
