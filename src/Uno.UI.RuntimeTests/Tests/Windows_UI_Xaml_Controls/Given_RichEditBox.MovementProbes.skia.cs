#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Interactive_Mixed_Height_UpDown_Uses_Visual_Lines()
	{
		var editor = new RichEditBox { Width = 260, TextWrapping = TextWrapping.NoWrap };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "abcd\rxy\rabcd");
			editor.Document.GetRange(5, 7).CharacterFormat.Size = 54;
			editor.Document.GetRange(5, 7).ParagraphFormat.SetLineSpacing(LineSpacingRule.Exactly, 66);
			editor.Focus(FocusState.Programmatic);
			editor.Document.Selection.SetRange(3, 3);
			await WindowHelper.WaitForIdle();

			var lines = GetDisplayBlock(editor).ParsedText;
			Assert.AreEqual(3, lines.VisualLineCount);
			Assert.IsTrue(lines.GetVisualLine(1).Bounds.Height > lines.GetVisualLine(0).Bounds.Height);
			Assert.IsTrue(lines.GetVisualLine(1).Baseline > lines.GetVisualLine(0).Baseline);

			RaiseKey(editor, VirtualKey.Down);
			Assert.IsTrue(editor.Document.Selection.StartPosition is >= 5 and <= 7);
			RaiseKey(editor, VirtualKey.Down);
			RaiseKey(editor, VirtualKey.Up);
			RaiseKey(editor, VirtualKey.Up);
			Assert.AreEqual(3, editor.Document.Selection.StartPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Interactive_Rtl_Wrap_Image_And_Paragraph_Spacing_RoundTrip()
	{
		var editor = new RichEditBox
		{
			Width = 170,
			Height = 140,
			FlowDirection = FlowDirection.RightToLeft,
			TextWrapping = TextWrapping.Wrap,
		};
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(
				TextSetOptions.None,
				"אבג דהו זחט יכל מנס עףצ קרש ת\rפסקה שניה ארוכה עם מילים");
			using (var image = CreateImageStream(SKColors.Cyan))
			{
				editor.Document.GetRange(6, 6).InsertImage(
					24,
					18,
					13,
					VerticalCharacterAlignment.Baseline,
					"navigation image",
					image);
			}
			var firstParagraph = editor.Document.GetRange(0, 0).ParagraphFormat;
			firstParagraph.SetLineSpacing(LineSpacingRule.Exactly, 32);
			firstParagraph.SpaceAfter = 11;
			var secondStart = editor.Document.GetUnitBoundaries(TextRangeUnit.Paragraph)![1].Start;
			editor.Document.GetRange(secondStart, secondStart).ParagraphFormat.SpaceBefore = 7;
			editor.Focus(FocusState.Programmatic);
			editor.Document.Selection.SetRange(4, 4);
			await WindowHelper.WaitForIdle();

			var parsed = GetDisplayBlock(editor).ParsedText;
			Assert.IsGreaterThanOrEqualTo(4, parsed.VisualLineCount);
			var original = editor.Document.Selection.StartPosition;
			var originalGeometry = parsed.GetGeometryPosition(original);
			var originalLine = parsed.GetLineAt(original).lineIndex;
			RaiseKey(editor, VirtualKey.Down);
			RaiseKey(editor, VirtualKey.Down);
			RaiseKey(editor, VirtualKey.Up);
			RaiseKey(editor, VirtualKey.Up);

			var final = editor.Document.Selection.StartPosition;
			var finalGeometry = parsed.GetGeometryPosition(final);
			Assert.AreEqual(originalLine, parsed.GetLineAt(final).lineIndex);
			Assert.AreEqual(originalGeometry.CaretRect.X, finalGeometry.CaretRect.X, 12);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Ctrl_UpDown_Uses_Tom_Paragraph_Boundaries()
	{
		var editor = new RichEditBox { Width = 260, TextWrapping = TextWrapping.Wrap };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "aa words\rbbbb words\rcc");
			editor.Focus(FocusState.Programmatic);
			editor.Document.Selection.SetRange(2, 2);
			await WindowHelper.WaitForIdle();

			RaiseKey(editor, VirtualKey.Down, VirtualKeyModifiers.Control);
			Assert.AreEqual(9, editor.Document.Selection.StartPosition);

			RaiseKey(
				editor,
				VirtualKey.Down,
				VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
			Assert.AreEqual(9, editor.Document.Selection.StartPosition);
			Assert.AreEqual(20, editor.Document.Selection.EndPosition);

			RaiseKey(editor, VirtualKey.Up, VirtualKeyModifiers.Control);
			Assert.AreEqual(0, editor.Document.Selection.StartPosition);
			Assert.AreEqual(0, editor.Document.Selection.EndPosition);

			editor.Document.Selection.SetRange(12, 12);
			RaiseKey(editor, VirtualKey.Up, VirtualKeyModifiers.Control);
			Assert.AreEqual(9, editor.Document.Selection.StartPosition);
			Assert.AreEqual(9, editor.Document.Selection.EndPosition);

			RaiseKey(
				editor,
				VirtualKey.Up,
				VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
			Assert.AreEqual(0, editor.Document.Selection.StartPosition);
			Assert.AreEqual(9, editor.Document.Selection.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
