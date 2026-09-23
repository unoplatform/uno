using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Text;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_SkiaTextFormatter
	{
		// A minimal TextSource that feeds the Skia formatter the same inputs the
		// real ParagraphTextSource will (the leaf inlines + resolved settings).
		private sealed class TestParagraphSource : TextSource, ISkiaParagraphSource
		{
			private readonly Inline[] _inlines;

			public TestParagraphSource(Inline[] inlines, float defaultLineHeight)
			{
				_inlines = inlines;
				DefaultLineHeight = defaultLineHeight;
			}

			public Inline[] GetLeafInlines() => _inlines;
			public float DefaultLineHeight { get; }
			public float LineHeight => 0;
			public LineStackingStrategy LineStackingStrategy => LineStackingStrategy.MaxHeight;

			public (ObjectRun Run, ObjectRunMetrics Metrics)? FormatInlineObject(InlineUIContainer container, float paragraphWidth) => null;

			public override TextRun GetTextRun(uint characterIndex) => throw new NotSupportedException();
			public override IEmbeddedElementHost? GetEmbeddedElementHost() => null;
		}

		[TestMethod]
		public async Task When_FormatLine_Vends_All_Lines()
		{
			var run = new Run { Text = "The quick brown fox jumps over the lazy dog and keeps on running past the edge of the block" };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { Width = 120 };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var inlines = paragraph.Inlines.TraversedTree.leafTree;
				var (defaultFont, _) = FontDetailsCache.GetFont(SUT.FontFamily?.Source, (float)SUT.FontSize, SUT.FontWeight, SUT.FontStretch, SUT.FontStyle);

				var source = new TestParagraphSource(inlines, defaultFont.FontSize);
				var runProperties = new TextRunProperties(defaultFont, SUT.FontSize, false, false, 0, null, CultureInfo.CurrentCulture, CultureInfo.CurrentCulture);
				var paragraphProperties = new TextParagraphProperties(FlowDirection.LeftToRight, runProperties, 0, TextWrapping.Wrap, TextLineBounds.Full, TextAlignment.Left);

				var wrappingWidth = SUT.ActualWidth;

				// Oracle: a direct single-pass parse of the same inputs.
				var expected = ParsedText.ParseText(
					new Size(wrappingWidth, double.PositiveInfinity),
					inlines,
					defaultFont.FontSize,
					maxLines: 0,
					lineHeight: 0,
					LineStackingStrategy.MaxHeight,
					paragraphProperties.TextLineBounds,
					TextAlignment.Left,
					TextWrapping.Wrap,
					FlowDirection.LeftToRight,
					out _);

				// The formatter must vend exactly those lines via the FormatLine loop.
				var lines = new List<TextLine>();
				TextLineBreak? previousBreak = null;
				do
				{
					var line = SkiaTextFormatter.Instance.FormatLine(source, 0, wrappingWidth, paragraphProperties, previousBreak, null);
					lines.Add(line);
					previousBreak = line.TextLineBreak;
				}
				while (previousBreak is not null);

				Assert.IsTrue(expected.RenderLines.Count > 1, "Expected the text to wrap onto multiple lines");
				Assert.AreEqual(expected.RenderLines.Count, lines.Count, "Formatter should vend one TextLine per RenderLine");

				for (var i = 0; i < lines.Count; i++)
				{
					Assert.AreEqual(expected.RenderLines[i].WidthWithoutTrailingSpaces, lines[i].Width, 0.01, $"Width mismatch at line {i}");
					Assert.AreEqual(expected.RenderLines[i].Height, lines[i].Height, 0.01, $"Height mismatch at line {i}");
				}

				// The continuation token terminates on the last line and advances otherwise.
				Assert.IsNull(lines[lines.Count - 1].TextLineBreak, "Last line must end the loop");
				for (var i = 0; i < lines.Count - 1; i++)
				{
					Assert.IsInstanceOfType(lines[i].TextLineBreak, typeof(SkiaTextLineBreak));
					Assert.AreEqual(i + 1, ((SkiaTextLineBreak)lines[i].TextLineBreak!).NextLineIndex, $"Break index mismatch at line {i}");
				}
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
			var run = new Run { Text = "The quick brown fox jumps over the lazy dog and keeps on running past the edge of the block" };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { Width = 120 };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var inlines = paragraph.Inlines.TraversedTree.leafTree;
				var (defaultFont, _) = FontDetailsCache.GetFont(SUT.FontFamily?.Source, (float)SUT.FontSize, SUT.FontWeight, SUT.FontStretch, SUT.FontStyle);

				var source = new TestParagraphSource(inlines, defaultFont.FontSize);
				var runProperties = new TextRunProperties(defaultFont, SUT.FontSize, false, false, 0, null, CultureInfo.CurrentCulture, CultureInfo.CurrentCulture);
				var wrap = new TextParagraphProperties(FlowDirection.LeftToRight, runProperties, 0, TextWrapping.Wrap, TextLineBounds.Full, TextAlignment.Left);
				var noWrap = new TextParagraphProperties(FlowDirection.LeftToRight, runProperties, 0, TextWrapping.NoWrap, TextLineBounds.Full, TextAlignment.Left);

				var wrappingWidth = SUT.ActualWidth;

				Assert.IsNotNull(SkiaTextFormatter.Instance.FormatLine(source, 0, wrappingWidth, wrap, null, null).TextLineBreak, "Precondition: the text wraps");
				Assert.IsNull(SkiaTextFormatter.Instance.FormatLine(source, 0, wrappingWidth, noWrap, null, null).TextLineBreak, "NoWrap at the same width must format a single line");
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
			var run = new Run { Text = string.Concat(Enumerable.Repeat("a\U0001F600b ", 12)) };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { Width = 120 };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var inlines = paragraph.Inlines.TraversedTree.leafTree;
				var (defaultFont, _) = FontDetailsCache.GetFont(SUT.FontFamily?.Source, (float)SUT.FontSize, SUT.FontWeight, SUT.FontStretch, SUT.FontStyle);

				var source = new TestParagraphSource(inlines, defaultFont.FontSize);
				var runProperties = new TextRunProperties(defaultFont, SUT.FontSize, false, false, 0, null, CultureInfo.CurrentCulture, CultureInfo.CurrentCulture);
				var paragraphProperties = new TextParagraphProperties(FlowDirection.LeftToRight, runProperties, 0, TextWrapping.Wrap, TextLineBounds.Full, TextAlignment.Left);
				var wrappingWidth = SUT.ActualWidth;

				var lines = new List<TextLine>();
				TextLineBreak? previousBreak = null;
				var firstCharIndex = 0u;
				do
				{
					var line = SkiaTextFormatter.Instance.FormatLine(source, firstCharIndex, wrappingWidth, paragraphProperties, previousBreak, null);
					lines.Add(line);
					firstCharIndex += line.Length;
					previousBreak = line.TextLineBreak;
				}
				while (previousBreak is not null);

				Assert.IsTrue(lines.Count > 1, "Precondition: the text wraps");
				Assert.AreEqual((uint)run.Text.Length, firstCharIndex, "The lines should cover every UTF-16 code unit of the paragraph");

				// Resuming at a character index, as the next link of a chain does, starts at that character.
				var resumed = SkiaTextFormatter.Instance.FormatLine(source, lines[0].Length, wrappingWidth, paragraphProperties, null, null);
				Assert.AreEqual(lines[1].Length, resumed.Length, "A line resumed at the second line's first character should match it");

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
	}
}
