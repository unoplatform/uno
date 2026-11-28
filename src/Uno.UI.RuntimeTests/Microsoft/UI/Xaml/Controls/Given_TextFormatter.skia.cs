using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Drives the formatter the layout tree uses through the TextFormatter contract only, so the
	// assertions hold whichever text engine is behind it.
	[TestClass]
	[RunsOnUIThread]
	public class Given_TextFormatter
	{
		private const string WrappingText = "The quick brown fox jumps over the lazy dog and keeps on running past the edge of the block";

		[TestMethod]
		public async Task When_FormatLine_Vends_All_Lines()
		{
			var paragraph = CreateParagraph(WrappingText);
			var SUT = new RichTextBlock { Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
			SUT.Blocks.Add(paragraph);
			var root = new StackPanel();
			root.Children.Add(SUT);

			try
			{
				await UITestHelper.Load(root);

				var (source, formatter, properties) = GetPipeline(SUT, paragraph);
				var wrappingWidth = SUT.Width;
				var lines = FormatAllLines(formatter, source, wrappingWidth, properties, out var length);

				Assert.IsTrue(lines.Count > 1, "Precondition: the text wraps");
				Assert.AreEqual((uint)WrappingText.Length, length, "The lines should cover the whole paragraph");

				for (var i = 0; i < lines.Count; i++)
				{
					Assert.IsTrue(lines[i].Length > 0, $"Line {i} is empty");
					Assert.IsTrue(lines[i].Width > 0 && lines[i].Width <= wrappingWidth + 0.5, $"Line {i} is {lines[i].Width} wide for a wrapping width of {wrappingWidth}");
					Assert.IsTrue(lines[i].Height > 0, $"Line {i} has no height");
				}

				// No line breaks early: a line plus the next line's first word, laid out on its own without
				// wrapping, is wider than the wrapping width.
				var lineStart = 0;
				for (var i = 0; i < lines.Count - 1; i++)
				{
					var lineText = WrappingText.Substring(lineStart, (int)lines[i].Length).TrimEnd();
					lineStart += (int)lines[i].Length;
					var nextWord = WrappingText.Substring(lineStart).Split(' ')[0];
					var unwrappedWidth = await MeasureUnwrapped(root, lineText + " " + nextWord);
					Assert.IsTrue(unwrappedWidth > wrappingWidth, $"Line {i} '{lineText}' broke early: with '{nextWord}' it is {unwrappedWidth} wide, which fits {wrappingWidth}");
				}

				// The continuation token ends on the last line only.
				Assert.IsTrue(lines.Take(lines.Count - 1).All(l => l.TextLineBreak is not null), "Every line but the last should carry a break");
				Assert.IsNull(lines[lines.Count - 1].TextLineBreak, "The last line should end the loop");

				// The control lays out the same lines.
				var page = SUT.GetPageNode()!;
				Assert.AreEqual(lines.Max(l => l.Width), page.GetDesiredSize().Width, 1.0, "The desired width should be the widest line without trailing whitespace");
				Assert.AreEqual(lines.Sum(l => l.Height), SUT.DesiredSize.Height, 1.0, "The control should be as tall as its lines");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Paragraph_Properties_Change_At_Same_Width()
		{
			// Line Services formats the first line from the current inputs; a layout from an earlier pass must not leak in.
			var paragraph = CreateParagraph(WrappingText);
			var SUT = new RichTextBlock { Width = 120 };
			SUT.Blocks.Add(paragraph);

			try
			{
				await UITestHelper.Load(SUT);

				var (source, formatter, wrap) = GetPipeline(SUT, paragraph);
				var noWrap = new TextParagraphProperties(wrap.FlowDirection, wrap.DefaultTextRunProperties, 0, TextWrapping.NoWrap, wrap.TextLineBounds, wrap.TextAlignment);
				var wrappingWidth = SUT.ActualWidth;

				Assert.IsNotNull(formatter.FormatLine(source, 0, wrappingWidth, wrap, null, null).TextLineBreak, "Precondition: the text wraps");
				Assert.IsNull(formatter.FormatLine(source, 0, wrappingWidth, noWrap, null, null).TextLineBreak, "NoWrap at the same width must format a single line");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Clusters_Span_Several_Code_Units()
		{
			// TextLine lengths and character hits are source positions, so a surrogate pair covers two of them.
			var text = string.Concat(Enumerable.Repeat("a\U0001F600b ", 12));
			var paragraph = CreateParagraph(text);
			var SUT = new RichTextBlock { Width = 120 };
			SUT.Blocks.Add(paragraph);

			try
			{
				await UITestHelper.Load(SUT);

				var (source, formatter, properties) = GetPipeline(SUT, paragraph);
				var wrappingWidth = SUT.ActualWidth;
				var lines = FormatAllLines(formatter, source, wrappingWidth, properties, out var length);

				Assert.IsTrue(lines.Count > 1, "Precondition: the text wraps");
				Assert.AreEqual((uint)text.Length, length, "The lines should cover every UTF-16 code unit of the paragraph");

				// Formatting from a character index without the previous line's break starts at that character: every line,
				// the last and shorter one included, comes back as the line-by-line pass made it.
				var start = 0u;
				for (var i = 0; i < lines.Count; i++)
				{
					var resumed = formatter.FormatLine(source, start, wrappingWidth, properties, null, null);
					Assert.AreEqual((int)start, resumed.GetCharacterHitFromDistance(0).FirstCharacterIndex, $"A line formatted from line {i}'s first character should start there");
					Assert.AreEqual(lines[i].Length, resumed.Length, $"A line formatted from line {i}'s first character should match it");
					start += (uint)lines[i].Length;
				}

				// "a" is at 0, the surrogate pair at 1-2 and "b" at 3.
				var first = lines[0];
				Assert.AreEqual(new CharacterHit(1, 2), first.GetNextCaretCharacterHit(new CharacterHit(1, 0)), "Moving forward over the pair should cover both code units");
				Assert.AreEqual(new CharacterHit(3, 1), first.GetNextCaretCharacterHit(new CharacterHit(1, 2)), "Moving forward from the pair's trailing edge should cover the next character");
				Assert.AreEqual(new CharacterHit(1, 0), first.GetPreviousCaretCharacterHit(new CharacterHit(3, 0)), "Moving back from the next character should land before the pair");
				Assert.AreEqual(first.GetDistanceFromCharacterHit(new CharacterHit(3, 0)), first.GetDistanceFromCharacterHit(new CharacterHit(1, 2)), 0.01, "The pair's trailing edge is the next character's leading edge");
				Assert.IsTrue(first.GetDistanceFromCharacterHit(new CharacterHit(3, 0)) > first.GetDistanceFromCharacterHit(new CharacterHit(1, 0)), "The pair should have a width");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static Paragraph CreateParagraph(string text)
		{
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = text });
			return paragraph;
		}

		// The width of the text on one line, as the control lays it out.
		// Hosted next to the control under test, which keeps its own layout.
		private static async Task<double> MeasureUnwrapped(Panel host, string text)
		{
			var rtb = new RichTextBlock { TextWrapping = TextWrapping.NoWrap, HorizontalAlignment = HorizontalAlignment.Left };
			rtb.Blocks.Add(CreateParagraph(text));
			host.Children.Add(rtb);
			try
			{
				await WindowHelper.WaitForLoaded(rtb);
				await WindowHelper.WaitForIdle();
				return rtb.DesiredSize.Width;
			}
			finally
			{
				host.Children.Remove(rtb);
			}
		}

		// The formatter, text source and paragraph properties the layout tree builds for this paragraph.
		private static (TextSource Source, TextFormatter Formatter, TextParagraphProperties Properties) GetPipeline(RichTextBlock owner, Paragraph paragraph)
		{
			BlockLayoutHelpers.GetTextFormatter(owner, out var formatter);
			BlockLayoutHelpers.GetParagraphProperties(paragraph, owner, out var properties);
			return (new ParagraphTextSource(paragraph, owner), formatter, properties);
		}

		// Formats the paragraph line by line as ParagraphNode does: each line starts where the previous one ended.
		internal static List<TextLine> FormatAllLines(TextFormatter formatter, TextSource source, double wrappingWidth, TextParagraphProperties properties, out uint length)
		{
			var lines = new List<TextLine>();
			TextLineBreak? previousBreak = null;
			length = 0;
			do
			{
				var line = formatter.FormatLine(source, length, wrappingWidth, properties, previousBreak, null);
				lines.Add(line);
				length += line.Length;
				previousBreak = line.TextLineBreak;
				Assert.IsTrue(lines.Count < 1000, "The line loop doesn't end");
			}
			while (previousBreak is not null);

			return lines;
		}
	}
}
