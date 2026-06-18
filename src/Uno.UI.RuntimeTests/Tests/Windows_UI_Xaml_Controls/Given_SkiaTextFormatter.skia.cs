using System;
using System.Collections.Generic;
using System.Globalization;
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

				var source = new TestParagraphSource(inlines, defaultFont.SKFontSize);
				var runProperties = new TextRunProperties(defaultFont, SUT.FontSize, false, false, 0, null, CultureInfo.CurrentCulture, CultureInfo.CurrentCulture);
				var paragraphProperties = new TextParagraphProperties(FlowDirection.LeftToRight, runProperties, 0, TextWrapping.Wrap, TextLineBounds.Full, TextAlignment.Left);

				var wrappingWidth = SUT.ActualWidth;

				// Oracle: a direct single-pass parse of the same inputs.
				var expected = ParsedText.ParseText(
					new Size(wrappingWidth, double.PositiveInfinity),
					inlines,
					defaultFont.SKFontSize,
					maxLines: 0,
					lineHeight: 0,
					LineStackingStrategy.MaxHeight,
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
	}
}
