// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextElement.cpp / InlineCollection.cpp / BlockCollection.cpp / TextBoxHelpers.cpp,
// tag winui3/release/1.8.2, commit 4a1c6184c

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Text
{
	[TestClass]
	public class Given_TextContainer_Invalidation
	{
		private static InlineCollection HostedInlines(params Inline[] inlines)
		{
			var textBlock = new TextBlock();
			foreach (var inline in inlines)
			{
				textBlock.Inlines.Add(inline);
			}
			return textBlock.Inlines;
		}

		[TestMethod]
		public void When_Nested_Span_Collection_Change_Invalidates_Ancestor_Caches()
		{
			// RichTextBlock -> Blocks -> Paragraph -> Inlines -> Span -> Run "AB"
			var span = new Span();
			span.Inlines.Add(new Run { Text = "AB" });
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(span);
			var rtb = new RichTextBlock();
			rtb.Blocks.Add(paragraph);

			rtb.Blocks.GetPositionCount(out var before);
			Assert.AreEqual(8u, before); // paragraph.Inlines = 2 + (2 + (2+2))

			// Mutating the deeply nested span's inlines must invalidate paragraph.Inlines and the block cache.
			span.Inlines.Add(new Run { Text = "CD" });

			rtb.Blocks.GetPositionCount(out var after);
			Assert.AreEqual(12u, after); // paragraph.Inlines = 2 + (2 + (2+2) + (2+2))
		}

		[TestMethod]
		public void When_RichTextBlock_Run_Text_Shrinks_Block_Cache_Is_Refreshed()
		{
			var run = new Run { Text = "Hello" };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var rtb = new RichTextBlock();
			rtb.Blocks.Add(paragraph);

			// Populate the position-count caches as a selection / UIA read would.
			rtb.Blocks.GetPositionCount(out var before);
			Assert.AreEqual(9u, before); // paragraph = 2 + ("Hello"=5 + 2)
			paragraph.Inlines.GetPositionCount(out _);

			// Before the fix this left every ancestor cache stale, so Run.GetRun would slice past the
			// shrunk text (ArgumentOutOfRangeException). MarkDirty must clear the whole parent chain.
			run.Text = "Hi";

			rtb.Blocks.GetPositionCount(out var after);
			Assert.AreEqual(6u, after); // paragraph = 2 + ("Hi"=2 + 2)

			rtb.Blocks.GetRun(2, out _, out _, out _, out var nested, out var characters, out _);
			Assert.AreSame(run, nested);
			Assert.AreEqual("Hi", characters.ToString());
		}

		[TestMethod]
		public void When_GetText_With_Newlines_Separates_Paragraphs()
		{
			var rtb = new RichTextBlock();
			var p1 = new Paragraph();
			p1.Inlines.Add(new Run { Text = "AB" });
			var p2 = new Paragraph();
			p2.Inlines.Add(new Run { Text = "CD" });
			rtb.Blocks.Add(p1);
			rtb.Blocks.Add(p2);

			rtb.Blocks.GetPositionCount(out var count);
			Assert.AreEqual(12u, count);

			// insertNewlines substitutes "\r\n" for each paragraph close edge (clipboard / UIA text).
			Assert.AreEqual("AB\r\nCD\r\n", rtb.Blocks.GetText(0, count, true));
			Assert.AreEqual("ABCD", rtb.Blocks.GetText(0, count, false));
		}

		[TestMethod]
		public void When_GetText_With_Newlines_Replaces_LineBreak()
		{
			var inlines = HostedInlines(
				new Run { Text = "A" },
				new LineBreak(),
				new Run { Text = "B" });

			inlines.GetPositionCount(out var count);

			Assert.AreEqual("A\r\nB", inlines.GetText(0, count, true));

			// Without insertNewlines the LineBreak contributes its raw U+2028 line separator.
			var lineSeparator = ((char)0x2028).ToString();
			Assert.AreEqual("A" + lineSeparator + "B", inlines.GetText(0, count, false));
		}

		[TestMethod]
		public void When_Empty_Paragraph_GetContainingElement_Returns_Paragraph()
		{
			// Empty <Paragraph/> — interior position 1 (ContentEnd) must fall back to the paragraph
			// itself instead of dereferencing the never-cached (null) position-count array.
			var paragraph = new Paragraph();

			paragraph.GetContainingElement(1, out var element);

			Assert.AreSame(paragraph, element);
		}

		[TestMethod]
		public void When_Empty_Span_GetContainingElement_Returns_Span()
		{
			var span = new Span();

			span.GetContainingElement(1, out var element);

			Assert.AreSame(span, element);
		}

		[TestMethod]
		public void When_GetRun_Position_Out_Of_Range_Throws()
		{
			var inlines = HostedInlines(new Run { Text = "Hi" });
			inlines.GetPositionCount(out var count); // 6

			// IFCEXPECT in WinUI (always active) — an out-of-range position throws instead of walking
			// off the end in release as a debug-only assert would.
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
				inlines.GetRun(count + 1, out _, out _, out _, out _, out _, out _));
		}

		[TestMethod]
		public void When_Typography_Unset_Snapshot_Is_Default()
		{
			// StandardLigatures/ContextualLigatures/ContextualAlternates/Kerning default to true in
			// WinUI, so an untouched element's inherited snapshot must report the default typography.
			var run = new Run { Text = "x" };

			run.GetInheritedProperties(out var inherited);

			Assert.IsNotNull(inherited);
			Assert.IsTrue(inherited!.Typography.IsTypographyDefault());
		}
	}
}
