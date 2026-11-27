using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.TextInkHelper;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// How RichTextBlock's paragraphs are laid out by UnicodeText: the layouts the formatter caches and reuses, objects, which
// take no position in the layout tree, and hit-tests between layout passes. These read Uno internals.
[TestClass]
[RunsOnUIThread]
public class Given_RichTextBlock_Layout
{
	private static Run R(string text, Color? color = null)
		=> color is { } c ? new Run { Text = text, Foreground = new SolidColorBrush(c) } : new Run { Text = text };

	private static Paragraph P(params Inline[] inlines)
	{
		var paragraph = new Paragraph();
		foreach (var inline in inlines)
		{
			paragraph.Inlines.Add(inline);
		}
		return paragraph;
	}

	// The lines a page's paragraph records when drawn now: a page whose arrange was bypassed doesn't draw again by itself.
	private static IReadOnlyList<RecordedTextDrawingContext.LineRecord> GetRecordedLines(PageNode page, int paragraph = 0)
	{
		page.Draw(forceDraw: true);
		var node = page.GetFirstChild();
		for (var i = 0; i < paragraph; i++)
		{
			node = node!.GetNext();
		}

		return ((ParagraphDrawingContext)((ParagraphNode)node!).GetDrawingContext()!).GetTextDrawingContext().Lines;
	}

	[TestMethod]
	public async Task When_Remeasured_At_Same_Width_Reuses_Layout()
	{
		var sut = new RichTextBlock { Width = 200, FontSize = 20 };
		sut.Blocks.Add(P(R("The quick brown fox jumps over the lazy dog")));

		try
		{
			await Show(sut);
			var page = sut.GetPageNode()!;
			var before = GetRecordedLines(page)[0].Line.Layout;

			page.InvalidateMeasure();
			sut.InvalidateMeasure();
			await WindowHelper.WaitForIdle();
			Assert.AreSame(before, GetRecordedLines(page)[0].Line.Layout, "A re-measure at the same width should reuse the layout");

			sut.Width = 150;
			await WindowHelper.WaitForIdle();
			Assert.AreNotSame(before, GetRecordedLines(page)[0].Line.Layout, "Another width should lay the paragraph out again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Paragraph_Across_Pages_Reuses_Layouts(bool narrowerOverflows)
	{
		// Pages as wide as the master continue its layout; a narrower page resumes a layout of its own where the previous
		// page broke. Re-measuring the chain reuses them all instead of laying the paragraph out again.
		var text = string.Join(" ", Enumerable.Repeat("The quick brown fox jumps over the lazy dog.", 6));
		var master = new RichTextBlock { Width = 200, Height = 60, FontSize = 20 };
		master.Blocks.Add(P(R(text)));
		var width = narrowerOverflows ? 160 : 200;
		var first = new RichTextBlockOverflow { Width = width, Height = 60 };
		var second = new RichTextBlockOverflow { Width = width, Height = 60 };
		master.OverflowContentTarget = first;
		first.OverflowContentTarget = second;
		var panel = new StackPanel();
		panel.Children.Add(master);
		panel.Children.Add(first);
		panel.Children.Add(second);

		try
		{
			await Show(panel, 240, 260);
			var pages = new[] { master.GetPageNode()!, first.GetPageNode()!, second.GetPageNode()! };
			var layouts = pages.Select(page => GetRecordedLines(page)[0].Line.Layout).ToArray();
			Assert.AreEqual(0, layouts[0].LayoutKey.ResumeIndex, "The master lays out from the start");
			if (narrowerOverflows)
			{
				var previous = GetRecordedLines(pages[0])[^1].Line;
				Assert.AreEqual(previous.Layout.Text.GetLineInfo(previous.EngineLine).End, layouts[1].LayoutKey.ResumeIndex, "The first overflow resumes where the master broke");
				Assert.AreSame(layouts[1], layouts[2], "The second overflow, as wide as the first, continues its layout");
			}
			else
			{
				Assert.IsTrue(layouts.All(layout => ReferenceEquals(layout, layouts[0])), "Pages as wide as the master continue its layout");
			}

			foreach (var page in pages)
			{
				page.InvalidateMeasure();
			}
			master.InvalidateMeasure();
			first.InvalidateMeasure();
			second.InvalidateMeasure();
			await WindowHelper.WaitForIdle();

			for (var i = 0; i < pages.Length; i++)
			{
				Assert.AreSame(layouts[i], GetRecordedLines(pages[i])[0].Line.Layout, $"Page {i} should reuse its layout");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_HitTest_At_Collapsed_Width_Reuses_Arranged_Twin()
	{
		// A trimmed line is read from a twin layout. Hit-tests reformat the line at its collapsed width, which the twin
		// made for the arrange width covers, so they reuse it.
		var sut = new RichTextBlock { Width = 120, FontSize = 20, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
		sut.Blocks.Add(P(R("The quick brown fox jumps over the lazy dog")));

		try
		{
			await Show(sut);
			var line = GetRecordedLines(sut.GetPageNode()!)[0].Line;
			Assert.IsTrue(line.HasCollapsed && line.Info.HasEllipsis, "Precondition: the line is trimmed");
			var twin = line.Layout;

			var position = sut.GetPositionFromPoint(new Point(60, 10));
			Assert.IsNotNull(position);
			Assert.AreEqual(1, twin.Base.TwinsForTests.Count, "The hit-test should make no twin of its own");
			Assert.AreSame(twin, twin.Base.TwinsForTests[0], "The hit-test should reuse the arranged twin");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Trimmed_Line_Is_Wider_Than_Its_Width()
	{
		// Too narrow for "W…", the line keeps its first character and is wider than the control. Its hit-tests collapse it
		// again at that wider width: they must still get the drawn line, not lay a twin out for that width.
		var probe = new TextBlock { Text = "W", FontSize = 40 };
		await Show(probe);
		var glyph = probe.ActualWidth;
		WindowHelper.WindowContent = null;

		var sut = new RichTextBlock { Width = glyph + 2, FontSize = 40, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
		sut.Blocks.Add(P(R("Wiiiii")));

		try
		{
			await Show(sut);
			var line = GetRecordedLines(sut.GetPageNode()!)[0].Line;
			Assert.IsTrue(line.HasCollapsed && line.Info.HasEllipsis && line.Width > sut.Width, $"Precondition: the trimmed line ({line.Width}) is wider than the control ({sut.Width})");
			var twin = line.Layout;

			Assert.IsNotNull(sut.GetPositionFromPoint(new Point(sut.Width - 2, 20)));
			var again = GetRecordedLines(sut.GetPageNode()!)[0].Line;
			Assert.AreEqual(1, twin.Base.TwinsForTests.Count, "The hit-test should reuse the arranged twin");
			Assert.AreSame(twin, again.Layout, "The line drawn after the hit-test should still be the trimmed one");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Resized_Keeps_Only_Current_Widths()
	{
		// The cache keeps the layout of the width the page lays out at now, not one per width it was ever given.
		var sut = new RichTextBlock { Width = 300, FontSize = 20, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis };
		sut.Blocks.Add(P(R(string.Join(" ", Enumerable.Repeat("The quick brown fox jumps over the lazy dog.", 4)))));

		try
		{
			await Show(sut, 340, 400);
			var node = (ParagraphNode)sut.GetPageNode()!.GetFirstChild()!;
			var cache = (UnicodeTextRunCache)node.TextRunCacheForTests!;
			for (var width = 290; width >= 200; width -= 10)
			{
				sut.Width = width;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, cache.CountForTests, $"At width {width}, the cache should hold the current layout only");
			}

			var layout = GetRecordedLines(sut.GetPageNode()!)[0].Line.Layout.Base;
			Assert.AreSame(layout, cache.Find(layout.LayoutKey), "The current width's layout should be cached");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Overflows_Resized_Keep_Only_Current_Widths()
	{
		// A master and three narrower overflows, resized several times: the cache keeps at most one layout per page, each
		// for a page that still shows the paragraph, never one for an old width or a deleted node.
		var text = string.Join(" ", Enumerable.Repeat("The quick brown fox jumps over the lazy dog.", 12));
		var master = new RichTextBlock { Width = 200, Height = 60, FontSize = 20 };
		master.Blocks.Add(P(R(text)));
		var panel = new StackPanel();
		panel.Children.Add(master);
		var overflows = new RichTextBlockOverflow[3];
		for (var i = 0; i < overflows.Length; i++)
		{
			overflows[i] = new RichTextBlockOverflow { Width = 180, Height = 60 };
			if (i == 0)
			{
				master.OverflowContentTarget = overflows[i];
			}
			else
			{
				overflows[i - 1].OverflowContentTarget = overflows[i];
			}
			panel.Children.Add(overflows[i]);
		}

		try
		{
			await Show(panel, 240, 300);
			var node = (ParagraphNode)master.GetPageNode()!.GetFirstChild()!;
			var cache = (UnicodeTextRunCache)node.TextRunCacheForTests!;
			foreach (var width in new[] { 190, 170, 210, 200 })
			{
				master.Width = width;
				foreach (var overflow in overflows)
				{
					overflow.Width = width - 20;
				}
				await WindowHelper.WaitForIdle();
			}

			var pages = new[] { master.GetPageNode()! }.Concat(overflows.Select(o => o.GetPageNode()!)).ToArray();
			string Owner(UnicodeParagraphLayout layout)
			{
				for (var i = 0; i < pages.Length; i++)
				{
					if (pages[i].GetFirstChild() is ParagraphNode node && ReferenceEquals(node.TextSourceForTests, layout.LayoutKey.Source))
					{
						return $"page {i}";
					}
				}
				return "no page";
			}
			var owners = cache.LayoutsForTests.Select(Owner).ToList();
			var layouts = string.Join(", ", cache.LayoutsForTests.Select(l => $"{Owner(l)} width {l.LayoutKey.WrappingWidth} resume {l.LayoutKey.ResumeIndex}"));
			Assert.IsFalse(owners.Contains("no page"), $"Every cached layout should belong to a page: {layouts}");
			Assert.AreEqual(owners.Count, owners.Distinct().Count(), $"A page should keep one layout: {layouts}");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Overflow_Empties_And_Refills_Releases_Layouts()
	{
		// An overflow narrower than its master lays the paragraph out at its own width. When everything fits in the master,
		// the overflow drops its page; it must let go of that layout too, or each round trip (and each re-link) would leave
		// one more whole-paragraph layout in the master's cache.
		var text = string.Join(" ", Enumerable.Repeat("The quick brown fox jumps over the lazy dog.", 12));
		var master = new RichTextBlock { Width = 200, Height = 60, FontSize = 20 };
		master.Blocks.Add(P(R(text)));
		var overflow = new RichTextBlockOverflow { Width = 180, Height = 600 };
		master.OverflowContentTarget = overflow;
		var panel = new StackPanel();
		panel.Children.Add(master);
		panel.Children.Add(overflow);

		try
		{
			await Show(panel, 240, 700);
			var node = (ParagraphNode)master.GetPageNode()!.GetFirstChild()!;
			var cache = (UnicodeTextRunCache)node.TextRunCacheForTests!;
			Assert.AreEqual(2, cache.CountForTests, "Precondition: the master's layout and the overflow's");

			for (var i = 0; i < 4; i++)
			{
				master.Height = 2000;
				await WindowHelper.WaitForIdle();
				Assert.IsNull(overflow.GetPageNode(), "Precondition: with everything in the master, the overflow drops its page");
				master.Height = 60;
				await WindowHelper.WaitForIdle();
			}

			Assert.AreSame(cache, ((ParagraphNode)master.GetPageNode()!.GetFirstChild()!).TextRunCacheForTests, "Precondition: the master kept its paragraph");
			Assert.AreEqual(2, cache.CountForTests, "After the round trips, still the master's layout and the overflow's");

			master.OverflowContentTarget = null;
			await WindowHelper.WaitForIdle();
			master.OverflowContentTarget = overflow;
			await WindowHelper.WaitForIdle();

			cache = (UnicodeTextRunCache)((ParagraphNode)master.GetPageNode()!.GetFirstChild()!).TextRunCacheForTests!;
			Assert.AreEqual(2, cache.CountForTests, "After a re-link, still the master's layout and the overflow's");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Text_And_Brush_Change_Before_Layout()
	{
		// A run's text and another's brush change before the layout pass: the cached layout's runs no longer map to the
		// inlines, so it keeps its brushes rather than shifting them, and the next layout draws the new ones.
		var first = R("ab", Colors.Red);
		var second = R("cd", Colors.Blue);
		var sut = new RichTextBlock { Width = 300, FontSize = 20 };
		sut.Blocks.Add(P(first, second));

		try
		{
			var (host, _) = await Show(sut);
			var layout = GetRecordedLines(sut.GetPageNode()!)[0].Line.Layout;

			first.Text = "";
			second.Foreground = new SolidColorBrush(Colors.Green);
			Assert.IsFalse(layout.UpdateForegrounds(sut.ForegroundVersion), "The layout's runs no longer map to the inlines");

			var bitmap = await Capture(host);
			DrawnX(bitmap, Colors.Green);
			Assert.IsTrue(InkX(bitmap, Colors.Red).IsEmpty && InkX(bitmap, Colors.Blue).IsEmpty, "Only the new text and brush should be drawn");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Fallback_Font_Arrives_Lays_Out_Again()
	{
		// A font the layout fell back from has loaded: the cached layouts were made without it.
		var sut = new RichTextBlock { Width = 300, FontSize = 20 };
		sut.Blocks.Add(P(R("Hello world")));

		try
		{
			await Show(sut);
			var before = GetRecordedLines(sut.GetPageNode()!)[0].Line.Layout;

			((UnicodeText.IFontCacheUpdateListener)sut).Invalidate();
			await WindowHelper.WaitForIdle();
			Assert.AreNotSame(before, GetRecordedLines(sut.GetPageNode()!)[0].Line.Layout, "The paragraph should be laid out again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Bounds_After_Object_Is_One_Span()
	{
		// An object takes no position: the character after it is one span, without the object, also in a right-to-left
		// paragraph where the two sit on either side of each other.
		var child = new Border { Width = 30, Height = 16, Background = new SolidColorBrush(Colors.Blue) };
		var sut = new RichTextBlock { Width = 300, FontSize = 20, FlowDirection = FlowDirection.RightToLeft };
		sut.Blocks.Add(P(R("אב"), new InlineUIContainer { Child = child }, R("cd")));

		try
		{
			var (host, _) = await Show(sut);
			var node = (ParagraphNode)sut.GetPageNode()!.GetFirstChild()!;
			var bounds = new List<TextBounds>();
			node.GetTextBounds(2, 1, bounds);
			Assert.AreEqual(1, bounds.Count, "One span for the character after the object");

			var childBounds = child.TransformToVisual(sut).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
			var characterWidth = bounds[0].Rect.Width;
			Assert.IsTrue(characterWidth > 0 && characterWidth < 20, $"The span should hold 'c' alone (width {characterWidth})");
			Assert.IsFalse(bounds[0].Rect.Right > childBounds.Left + 1 && bounds[0].Rect.Left < childBounds.Right - 1, $"The span {bounds[0].Rect} should not cover the object {childBounds}");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow("paragraph end")]
	[DataRow("middle")]
	public async Task When_Object_Alone_On_A_Line(string row)
	{
		// An object alone on a line after a wrap has no position of its own, so that line has none: hit-tests on it
		// resolve to the position before or after the object, and the paragraph after it still renders.
		var child = new Border { Width = 90, Height = 20, Background = new SolidColorBrush(Colors.Red) };
		var sut = new RichTextBlock { Width = 100, FontSize = 20 };
		var trailing = row == "middle" ? R("cd", Colors.Black) : null;
		sut.Blocks.Add(trailing is not null
			? P(R("ab", Colors.Black), new InlineUIContainer { Child = child }, trailing)
			: P(R("ab ", Colors.Black), new InlineUIContainer { Child = child }));
		var next = R("next", Colors.Blue);
		sut.Blocks.Add(P(next));

		try
		{
			var (host, bitmap) = await Show(sut, 140, 160);
			var childBounds = child.TransformToVisual(sut).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
			Assert.IsTrue(childBounds.Top > 15, $"Precondition: the object ({childBounds}) wraps to its own line");
			Assert.IsFalse(InkX(bitmap, Colors.Blue).IsEmpty, "The next paragraph should be drawn");

			var position = sut.GetPositionFromPoint(new Point(childBounds.Left + childBounds.Width / 2, childBounds.Top + childBounds.Height / 2));
			Assert.IsNotNull(position);

			if (row == "middle")
			{
				// The position after the object, which is the next character's.
				Assert.AreEqual(Positions(sut, trailing!)[0].Offset, position!.Offset, "The hit should land after the object");
			}
			else
			{
				// Known limit: the object's line has no position of its own, and its paragraph's end resolves to the next
				// paragraph's start (WinUI gives the object two positions, so it lands after it).
				Assert.AreEqual(Positions(sut, next)[0].Offset, position!.Offset, "The hit lands at the next paragraph's start");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The positions inside a run, in order.
	private static List<TextPointer> Positions(RichTextBlock sut, Run run)
	{
		var positions = new List<TextPointer>();
		for (var position = sut.ContentStart; position is not null && position.Offset < sut.ContentEnd!.Offset; position = position.GetPositionAtOffset(1, LogicalDirection.Forward))
		{
			if (ReferenceEquals(position.Parent, run))
			{
				positions.Add(position);
			}
		}
		return positions;
	}

	[TestMethod]
	public async Task When_Child_Resizes_Before_HitTest()
	{
		// A hosted child that grows between layout passes is measured by the hit-test's reformat, which doesn't lay the
		// paragraph out again: the old layout answers until the scheduled measure, which then moves the text.
		var child = new Border { Width = 40, Height = 16, Background = new SolidColorBrush(Colors.Red) };
		var sut = new RichTextBlock { Width = 300, FontSize = 20 };
		var trailing = R(" cd");
		sut.Blocks.Add(P(R("ab "), new InlineUIContainer { Child = child }, trailing));

		try
		{
			await Show(sut);
			var c = Positions(sut, trailing)[1];
			var before = c.GetCharacterRect(LogicalDirection.Forward).X;

			child.Width = 120;
			var hit = sut.GetPositionFromPoint(new Point(before + 2, 10));
			Assert.IsNotNull(hit, "A hit-test before the next layout pass should still answer");
			Assert.AreEqual(c.Offset, hit!.Offset, 1, "The hit-test should still read the previous layout");

			await WindowHelper.WaitForIdle();
			var after = Positions(sut, trailing)[1].GetCharacterRect(LogicalDirection.Forward).X;
			Assert.AreEqual(before + 80, after, 2, "The text after the child should move by its growth");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Child_Resizes_Before_HitTest_In_Overflow()
	{
		// The same on an overflow that continues the master's layout: until the scheduled measure, a hit-test keeps that
		// layout rather than laying the paragraph out again. After the measure, which lays the overflow's paragraph out with
		// the child's new size, drawing and hit-tests use that layout, not the master's.
		var child = new Border { Width = 40, Height = 16, Background = new SolidColorBrush(Colors.Red) };
		var master = new RichTextBlock { Width = 300, Height = 30, FontSize = 20 };
		var blueRun = R(" ef", Colors.Blue);
		master.Blocks.Add(P(R("ab"), new LineBreak(), R("cd "), new InlineUIContainer { Child = child }, blueRun));
		var overflow = new RichTextBlockOverflow { Width = 300, Height = 60 };
		master.OverflowContentTarget = overflow;
		var panel = new StackPanel();
		panel.Children.Add(master);
		panel.Children.Add(overflow);

		try
		{
			var (host, bitmap) = await Show(panel, 340, 120);
			var page = overflow.GetPageNode()!;
			var shared = GetRecordedLines(page)[0].Line.Layout;
			Assert.AreSame(GetRecordedLines(master.GetPageNode()!)[0].Line.Layout, shared, "Precondition: the overflow continues the master's layout");

			var top = (int)overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).Y;
			var blue = DrawnX(bitmap, Colors.Blue, top);
			var point = new Point(blue.Min + 6, 10);
			var hitBefore = overflow.GetPositionFromPoint(point);
			Assert.IsNotNull(hitBefore);

			child.Width = 120;
			var hit = overflow.GetPositionFromPoint(point);
			Assert.AreEqual(hitBefore!.Offset, hit?.Offset, "The hit-test should still read the previous layout");
			Assert.AreSame(shared, GetRecordedLines(page)[0].Line.Layout, "The hit-test should not lay the paragraph out again");

			bitmap = await Capture(host);
			var moved = DrawnX(bitmap, Colors.Blue, top);
			Assert.AreEqual(blue.Min + 80, moved.Min, 2, "The text after the child should move by its growth");
			Assert.AreNotSame(shared, GetRecordedLines(page)[0].Line.Layout, "After its measure, the overflow should draw from a layout with the child's new size");
			Assert.AreSame(blueRun, overflow.GetPositionFromPoint(new Point(moved.Min + 6, 10))?.Parent, "A hit-test on the moved text should land in it");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
