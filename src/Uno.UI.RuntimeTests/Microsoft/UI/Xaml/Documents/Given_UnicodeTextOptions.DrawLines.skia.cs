#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using TextDecorations = Windows.UI.Text.TextDecorations;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

public partial class Given_UnicodeTextOptions
{
	private delegate void PaintHandler(in Visual.PaintingSession session);

	// Paints whatever the test asks for, on top of its host element.
	private sealed class PaintVisual : ContainerVisual
	{
		private readonly PaintHandler _paint;

		public PaintVisual(Compositor compositor, PaintHandler paint) : base(compositor) => _paint = paint;

		internal override void Paint(in PaintingSession session) => _paint(session);

		internal override bool CanPaint() => true;

		internal override bool PaintsWithinOwnSize => true;
	}

	private static async Task<RawBitmap> Render(Border host, PaintHandler paint)
	{
		var visual = new PaintVisual(host.Visual.Compositor, paint) { Size = new Vector2((float)host.Width, (float)host.Height) };
		ElementCompositionPreview.SetElementChildVisual(host, visual);
		await WindowHelper.WaitForIdle();
		return await UITestHelper.ScreenShot(host);
	}

	// Marks every "typo" as misspelled.
	private sealed class TypoSpellCheckingService : ISpellCheckingService
	{
		public List<(int correctionStart, int correctionEnd)?> SpellCheck(List<int> wordBoundaries, string text)
		{
			var corrections = new List<(int correctionStart, int correctionEnd)?>(wordBoundaries.Count);
			var wordStart = 0;
			foreach (var wordEnd in wordBoundaries)
			{
				var word = text.Substring(wordStart, wordEnd - wordStart);
				var offset = word.IndexOf("typo", StringComparison.Ordinal);
				corrections.Add(offset >= 0 ? (offset, offset + 4) : null);
				wordStart = wordEnd;
			}
			return corrections;
		}

		public (int replaceIndexStart, int replaceIndexEnd, List<string> suggestions)? GetSpellCheckSuggestions(string text, List<int> wordBoundaries, int correctionStart, int correctionEnd)
			=> null;
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_DrawLines_Offsets_Every_Element(bool highContrast)
	{
		// Every element the engine places per line: glyphs, underline and strikethrough, character background, tab
		// leader, highlight and selection fills, high-contrast backplates, spell-check squiggles, the composition
		// underline, an inline image, list markers, and the caret on the ending line. Each line drawn at its own offset
		// must look exactly like that line drawn alone at the layout's position under a translated session.
		var originalAdjustment = Application.Current.HighContrastAdjustment;
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var host = new Border { Width = 420, Height = 520 };
		try
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = new TypoSpellCheckingService();
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = highContrast;
			Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.Auto;
			await UITestHelper.Load(host);

			var paragraph = new ParagraphLayoutInfo
			{
				LeftIndent = 10,
				IsList = true,
				MarkerText = "1.",
				ListTab = 24,
				Tabs = [new ParagraphTabLayoutInfo(150, TabAlignment.Left, TabLeader.Dots)],
			};
			Run P(string text, TextDecorations decorations = TextDecorations.None, Color? background = null)
			{
				var run = R(text);
				run.TextDecorations = decorations;
				run.Foreground = new SolidColorBrush(Colors.Black);
				run.ParagraphLayout = paragraph;
				run.CharacterBackground = background;
				return run;
			}
			var pixels = Enumerable.Repeat(new byte[] { 0, 0, 255, 255 }, 16 * 16).SelectMany(p => p).ToArray(); // opaque red, BGRA
			var image = ImageEncoderDecoder.Current.CreateImage(16, 16, pixels);
			var inlines = new Inline[]
			{
				P("Tab\there typo ", TextDecorations.Underline | TextDecorations.Strikethrough, Colors.LightGreen),
				new InlineUIContainer(),
				P(" fox jumps over the lazy typo dog and on\n"),
				P("Second paragraph typo text here"),
				new LineBreak(),
			};
			var options = new UnicodeTextOptions
			{
				InlineContainers = _ => new InlineObjectInfo(image, image, 16, 16, 12, VerticalCharacterAlignment.Baseline),
			};
			await WaitForFonts(inlines, 300);
			var layout = Layout(inlines, 300, options: options, spellCheck: true, endingParagraphLayout: paragraph);
			Assert.IsTrue(layout.LineCount >= 4, $"{layout.LineCount} lines");
			Assert.AreEqual(layout.LineCount + 1, layout.VisualLineCount, "The text ends with an ending line.");

			var length = string.Concat(inlines.Select(i => i switch { Run run => run.Text, LineBreak => "\n", _ => "￼" })).Length;
			var highlighters = new[]
			{
				new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow), Foreground = new SolidColorBrush(Colors.Blue), Ranges = { new TextRange { StartIndex = 8, Length = 30 } } },
				new TextHighlighter { Background = new SolidColorBrush(Colors.LightBlue), Ranges = { new TextRange { StartIndex = 50, Length = 25 } } },
			};
			var caret = (length, (CompositionBrush)host.Visual.Compositor.CreateColorBrush(Colors.Magenta), 2f);
			(int, int)? compositionRange = (0, 6);

			// Distinct whole-pixel offsets, so drawing at an offset and translating the session rasterize alike.
			var offsets = Enumerable.Range(0, layout.VisualLineCount).Select(i => new Vector2(9 * (i + 1), 31 * (i + 1))).ToArray();
			// Last line first: the lines may come in any order.
			var lines = Enumerable.Range(0, layout.VisualLineCount).Reverse().Select(i =>
			{
				var own = layout.GetLineDraw(i);
				return own with { Translation = offsets[i], BandTop = own.BandTop + offsets[i].Y };
			}).ToList();

			var offsetLines = await Render(host, (in Visual.PaintingSession session) =>
			{
				layout.DrawLines(host, session, lines, caret, highlighters, compositionRange, false, null);
				layout.DrawSpellCheckSquiggles(session.Session, Colors.Red);
			});
			var linesAlone = await Render(host, (in Visual.PaintingSession session) =>
			{
				for (var i = 0; i < layout.VisualLineCount; i++)
				{
					session.Session.Save();
					session.Session.Translate(offsets[i].X, offsets[i].Y);
					layout.DrawLines(host, session, [layout.GetLineDraw(i)], caret, highlighters, compositionRange, false, null);
					layout.DrawSpellCheckSquiggles(session.Session, Colors.Red);
					session.Session.Restore();
				}
			});
			var inPlace = await Render(host, (in Visual.PaintingSession session) =>
			{
				layout.DrawLines(host, session, null, caret, highlighters, compositionRange, false, null);
				layout.DrawSpellCheckSquiggles(session.Session, Colors.Red);
			});

			await ImageAssert.AreNotEqualAsync(offsetLines, inPlace);
			// The WebGPU renderer anti-aliases a stroked path (the squiggles) a little differently at another position, by up
			// to 7 of 255 per channel. An element left at its own place differs by up to 255 in pixels several px away.
			await ImageAssert.AreEqualAsync(offsetLines, linesAlone, tolerance: 12);
		}
		finally
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = null;
			Application.Current.HighContrastAdjustment = originalAdjustment;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_DrawLines_Fills_The_Band(bool highContrast)
	{
		// Each line gets a band that starts 3 px above its own box and ends 4 px below it. The fills and the caret cover the
		// band, so they show in the rows just outside the line box, and stop at the band's edge.
		var originalAdjustment = Application.Current.HighContrastAdjustment;
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var host = new Border { Width = 420, Height = 400 };
		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = highContrast;
			Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.Auto;
			await UITestHelper.Load(host);

			var runs = new Inline[] { R("The quick brown fox jumps over the lazy dog and"), R(" the five boxing wizards jump quickly away"), new LineBreak() };
			foreach (var run in runs.OfType<Run>())
			{
				run.CharacterBackground = Colors.LightGreen;
			}
			await WaitForFonts(runs, 260);
			var layout = Layout(runs, 260);
			Assert.IsTrue(layout.LineCount >= 3, $"{layout.LineCount} lines");
			var line1 = layout.GetLineInfo(1);
			var highlighters = new[] { new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow), Ranges = { new TextRange { StartIndex = line1.Start, Length = line1.End - line1.Start } } } };
			var caretBrush = (CompositionBrush)host.Visual.Compositor.CreateColorBrush(Colors.Magenta);

			// 20 px between lines, so a band never reaches into another line's box, and 10 px above the first.
			static float Dy(int line) => 10 + 20 * line;
			var lines = Enumerable.Range(0, layout.VisualLineCount).Select(i =>
			{
				var own = layout.GetLineDraw(i);
				return own with { Translation = new Vector2(0, Dy(i)), BandTop = own.BandTop + Dy(i) - 3, BandHeight = own.BandHeight + 7 };
			}).ToList();
			// Wide enough that, at any scaling, the pixels at its middle are fully covered.
			const float CaretThickness = 6;
			async Task<RawBitmap> Draw(int caretIndex)
				=> await Render(host, (in Visual.PaintingSession session) =>
					layout.DrawLines(host, session, lines, (caretIndex, caretBrush, CaretThickness), highlighters, null, false, null));

			var hc = host.GetHighContrastTextColors();
			var line2Cluster = layout.GetLineForTests(2).Clusters[2];
			var length = layout.GetLineInfo(layout.VisualLineCount - 1).End;
			var checks = new (string what, int line, float x, Color color, int caretIndex)[]
			{
				("character background", 0, layout.GetLineForTests(0).Clusters[1].X + 2, highContrast ? hc.background : Colors.LightGreen, length),
				("highlighter", 1, layout.GetLineForTests(1).Clusters[1].X + 2, highContrast ? hc.selectionBackground : Colors.Yellow, length),
				("caret", 2, line2Cluster.X + CaretThickness / 2, Colors.Magenta, line2Cluster.Start),
				("caret on the ending line", layout.VisualLineCount - 1, layout.GetLineInfo(layout.VisualLineCount - 1).AlignmentOffset + CaretThickness / 2, Colors.Magenta, length),
			};
			foreach (var (what, line, x, color, caretIndex) in checks)
			{
				var bitmap = await Draw(caretIndex);
				var info = layout.GetLineInfo(line);
				var boxTop = info.Top + Dy(line);
				var boxBottom = boxTop + info.Height;
				// The row just above the box and the one just below the box's last row, both at least 2 px inside the band.
				ImageAssert.HasColorAt(bitmap, x, MathF.Floor(boxTop) - 1, color, tolerance: 10);
				ImageAssert.HasColorAt(bitmap, x, MathF.Floor(boxBottom) + 2, color, tolerance: 10);
				var outside = bitmap.GetPixel((int)x, (int)MathF.Floor(boxTop) - 7);
				Assert.AreNotEqual(color, outside, $"The {what} should stop at the band's top.");
			}
		}
		finally
		{
			Application.Current.HighContrastAdjustment = originalAdjustment;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_DrawLines_Jumps_Back_Or_Repeats_A_Line()
	{
		// Lines in order continue from where the previous one ended; a line that starts earlier, or again, starts over.
		// Lines 1, 2, then 0, then 2 again, each at its own offset, must look like each line drawn alone at that offset:
		// the runs' colours, decorations and highlights of line 0 and of the repeated line 2 included.
		var host = new Border { Width = 420, Height = 520 };
		try
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = new TypoSpellCheckingService();
			await UITestHelper.Load(host);

			Run P(string text, Color color, TextDecorations decorations = TextDecorations.None)
			{
				var run = R(text);
				run.Foreground = new SolidColorBrush(color);
				run.TextDecorations = decorations;
				return run;
			}
			var inlines = new Inline[]
			{
				P("First typo words in red ", Colors.Red, TextDecorations.Underline),
				P("then green typo words that wrap ", Colors.Green),
				P("and blue typo words to the end", Colors.Blue, TextDecorations.Strikethrough),
			};
			await WaitForFonts(inlines, 200);
			var layout = Layout(inlines, 200, spellCheck: true);
			Assert.IsTrue(layout.LineCount >= 3, $"{layout.LineCount} lines");

			var highlighters = new[]
			{
				new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow), Ranges = { new TextRange { StartIndex = 3, Length = 10 } } },
				new TextHighlighter { Background = new SolidColorBrush(Colors.LightBlue), Ranges = { new TextRange { StartIndex = 40, Length = 20 } } },
			};
			var order = new[] { 1, 2, 0, 2 };
			var offsets = order.Select((_, k) => new Vector2(7 * (k + 1), 110 * k)).ToArray();
			var lines = order.Select((line, k) =>
			{
				var own = layout.GetLineDraw(line);
				return own with { Translation = offsets[k], BandTop = own.BandTop + offsets[k].Y };
			}).ToList();

			var together = await Render(host, (in Visual.PaintingSession session) =>
				layout.DrawLines(host, session, lines, null, highlighters, null, false, null));
			var alone = await Render(host, (in Visual.PaintingSession session) =>
			{
				for (var k = 0; k < order.Length; k++)
				{
					session.Session.Save();
					session.Session.Translate(offsets[k].X, offsets[k].Y);
					layout.DrawLines(host, session, [layout.GetLineDraw(order[k])], null, highlighters, null, false, null);
					session.Session.Restore();
				}
			});

			await ImageAssert.AreEqualAsync(together, alone, tolerance: 12);
		}
		finally
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = null;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_DrawLines_Identity_Matches_Draw()
	{
		// A TextBlock's own layout, drawn by Draw and by DrawLines with every line at the layout's position.
		var textBlock = new TextBlock { Width = 260, TextWrapping = TextWrapping.Wrap, FontSize = 20, TextAlignment = TextAlignment.Center };
		textBlock.Inlines.Add(new Run { Text = "The quick brown fox ", TextDecorations = TextDecorations.Underline });
		textBlock.Inlines.Add(new Run { Text = "jumps over the lazy dog, then\nruns far away from the ", Foreground = new SolidColorBrush(Colors.DarkGreen) });
		textBlock.Inlines.Add(new Run { Text = "five boxing wizards", TextDecorations = TextDecorations.Strikethrough });
		textBlock.TextHighlighters.Add(new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow), Ranges = { new TextRange { StartIndex = 10, Length = 30 } } });
		var host = new Border { Width = 300, Height = 260 };
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { textBlock, host } });
			var text = (UnicodeText)textBlock.ParsedText;
			Assert.IsTrue(text.LineCount >= 3, $"{text.LineCount} lines");
			var lines = Enumerable.Range(0, text.VisualLineCount).Select(text.GetLineDraw).ToList();

			var draw = await Render(host, (in Visual.PaintingSession session) =>
				text.Draw(textBlock, session, null, textBlock.TextHighlighters, null));
			var drawLines = await Render(host, (in Visual.PaintingSession session) =>
				text.DrawLines(textBlock, session, lines, null, textBlock.TextHighlighters, null, false, null));

			await ImageAssert.AreEqualAsync(drawLines, draw);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}
}
#endif
