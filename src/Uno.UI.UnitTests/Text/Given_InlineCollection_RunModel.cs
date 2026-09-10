// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InlineCollection.cpp / CRun.cpp / Inline.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Text
{
	[TestClass]
	public class Given_InlineCollection_RunModel
	{
		// Builds a TextBlock host whose InlineCollection contains the given inlines, so the
		// run model can be exercised including the past-end (EOP) position which is only valid
		// off the owning TextBlock.
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
		public void When_Empty_PositionCount_Is_Zero()
		{
			var inlines = HostedInlines();

			inlines.GetPositionCount(out var count);

			// CInlineCollection::CachePositionCounts leaves m_cCollectionPositions = 0 when empty.
			Assert.AreEqual(0u, count);
		}

		[TestMethod]
		public void When_Single_Run_PositionCount_Is_TextLength_Plus_Reserved()
		{
			var run = new Run { Text = "Hello" };
			var inlines = HostedInlines(run);

			// Run = text length + 2 (CRun::GetPositionCount).
			run.GetPositionCount(out var runCount);
			Assert.AreEqual(7u, runCount);

			// Collection = 2 (collection ends) + sum of inline position counts.
			inlines.GetPositionCount(out var count);
			Assert.AreEqual(9u, count);
		}

		[TestMethod]
		public void When_Position_Zero_Returns_OpenNesting_Reserved()
		{
			var run = new Run { Text = "Hello" };
			var inlines = HostedInlines(run);

			inlines.GetRun(
				0,
				out var formatting,
				out var inherited,
				out var nesting,
				out var nested,
				out var characters,
				out var cCharacters);

			Assert.AreEqual(TextNestingType.OpenNesting, nesting);
			Assert.IsTrue(characters.IsEmpty);
			Assert.AreEqual(1u, cCharacters);
			// The collection does not know its owner; the caller sets nested for the boundaries.
			Assert.IsNull(nested);
			Assert.IsNull(formatting);
			Assert.IsNull(inherited);
		}

		[TestMethod]
		public void When_Position_Last_Returns_CloseNesting_Reserved()
		{
			var run = new Run { Text = "Hello" };
			var inlines = HostedInlines(run);

			inlines.GetPositionCount(out var count);

			inlines.GetRun(
				count - 1,
				out _,
				out _,
				out var nesting,
				out var nested,
				out var characters,
				out var cCharacters);

			Assert.AreEqual(TextNestingType.CloseNesting, nesting);
			Assert.IsTrue(characters.IsEmpty);
			Assert.AreEqual(1u, cCharacters);
			Assert.IsNull(nested);
		}

		[TestMethod]
		public void When_Position_At_Run_Start_Returns_OpenNesting_With_NestedRun()
		{
			var run = new Run { Text = "Hello" };
			var inlines = HostedInlines(run);

			// Position 1 is the reserved start of the first (and only) Run.
			inlines.GetRun(
				1,
				out _,
				out _,
				out var nesting,
				out var nested,
				out var characters,
				out var cCharacters);

			Assert.AreEqual(TextNestingType.OpenNesting, nesting);
			Assert.IsTrue(characters.IsEmpty);
			Assert.AreEqual(1u, cCharacters);
			Assert.AreSame(run, nested);
		}

		[TestMethod]
		public void When_Position_Inside_Run_Returns_Characters_Slice()
		{
			var run = new Run { Text = "Hello" };
			var inlines = HostedInlines(run);

			// Position 2 is the first text character of the Run ('H').
			inlines.GetRun(
				2,
				out _,
				out _,
				out var nesting,
				out var nested,
				out var characters,
				out var cCharacters);

			Assert.AreEqual(TextNestingType.NestedContent, nesting);
			Assert.AreSame(run, nested);
			// Longest contiguous same-format run = the whole remaining text "Hello".
			Assert.AreEqual("Hello", characters.ToString());
			Assert.AreEqual(5u, cCharacters);
		}

		[TestMethod]
		public void When_Position_Inside_Run_Offset_Returns_Suffix_Slice()
		{
			var run = new Run { Text = "Hello" };
			var inlines = HostedInlines(run);

			// Position 4 maps to text offset 2 ('l') -> remaining suffix "llo".
			inlines.GetRun(
				4,
				out _,
				out _,
				out var nesting,
				out _,
				out var characters,
				out var cCharacters);

			Assert.AreEqual(TextNestingType.NestedContent, nesting);
			Assert.AreEqual("llo", characters.ToString());
			Assert.AreEqual(3u, cCharacters);
		}

		[TestMethod]
		public void When_Position_Past_End_Returns_Eop_Separator()
		{
			var run = new Run { Text = "Hello" };
			var inlines = HostedInlines(run);

			inlines.GetPositionCount(out var count);

			// Position == count is the past-end (EOP) request, valid off the owning TextBlock.
			inlines.GetRun(
				count,
				out var formatting,
				out _,
				out var nesting,
				out var nested,
				out var characters,
				out var cCharacters);

			Assert.AreEqual(TextNestingType.NestedContent, nesting);
			Assert.AreEqual("\x2029", characters.ToString());
			Assert.AreEqual(1u, cCharacters);
			Assert.IsNull(nested);
			// EOP run uses the owning TextBlock's resolved formatting.
			Assert.IsNotNull(formatting);
		}

		[TestMethod]
		public void When_Multiple_Runs_Positions_Map_To_Correct_Child()
		{
			var first = new Run { Text = "AB" };   // positions: 1=open, 2='A', 3='B', 4=close
			var second = new Run { Text = "CD" };  // positions: 5=open, 6='C', 7='D', 8=close
			var inlines = HostedInlines(first, second);

			// Collection = 2 + (2+2) + (2+2) = 10
			inlines.GetPositionCount(out var count);
			Assert.AreEqual(10u, count);

			// Position 2 -> inside first Run ("AB").
			inlines.GetRun(2, out _, out _, out _, out var nestedA, out var charsA, out _);
			Assert.AreSame(first, nestedA);
			Assert.AreEqual("AB", charsA.ToString());

			// Position 6 -> inside second Run ("CD").
			inlines.GetRun(6, out _, out _, out _, out var nestedB, out var charsB, out _);
			Assert.AreSame(second, nestedB);
			Assert.AreEqual("CD", charsB.ToString());

			// Position 5 -> reserved start of the second Run.
			inlines.GetRun(5, out _, out _, out var nestingOpen, out var nestedOpen, out _, out _);
			Assert.AreEqual(TextNestingType.OpenNesting, nestingOpen);
			Assert.AreSame(second, nestedOpen);
		}

		[TestMethod]
		public void When_Nested_Span_Open_Close_Offsets_Are_Correct()
		{
			// <Span><Run>Hi</Run></Span> hosted in a TextBlock.
			var run = new Run { Text = "Hi" };       // Run = 2 + 2 = 4 positions
			var span = new Span();
			span.Inlines.Add(run);
			var inlines = HostedInlines(span);

			// Span delegates to its inner InlineCollection: inner = 2 + 4 = 6.
			span.GetPositionCount(out var spanCount);
			Assert.AreEqual(6u, spanCount);

			// Outer collection = 2 + 6 = 8.
			inlines.GetPositionCount(out var count);
			Assert.AreEqual(8u, count);

			// Position 1 -> reserved start of the Span -> OpenNesting, nested = Span.
			inlines.GetRun(1, out _, out _, out var nestingOpen, out var nestedOpen, out _, out _);
			Assert.AreEqual(TextNestingType.OpenNesting, nestingOpen);
			Assert.AreSame(span, nestedOpen);

			// Position 6 -> last position of the Span -> CloseNesting, nested = Span.
			inlines.GetRun(6, out _, out _, out var nestingClose, out var nestedClose, out _, out _);
			Assert.AreEqual(TextNestingType.CloseNesting, nestingClose);
			Assert.AreSame(span, nestedClose);

			// Position 3 -> inside the nested Run's text "Hi".
			inlines.GetRun(3, out _, out _, out var nestingText, out var nestedText, out var charsText, out _);
			Assert.AreEqual(TextNestingType.NestedContent, nestingText);
			Assert.AreSame(run, nestedText);
			Assert.AreEqual("Hi", charsText.ToString());
		}

		[TestMethod]
		public void When_LineBreak_Emits_Line_Separator()
		{
			var lineBreak = new LineBreak();

			// LineBreak inherits the base position count of 2 (open/close).
			lineBreak.GetPositionCount(out var count);
			Assert.AreEqual(2u, count);

			// Position 0 -> open, hidden (no characters).
			lineBreak.GetRun(0, out _, out _, out var nestingOpen, out var nestedOpen, out var charsOpen, out var cOpen);
			Assert.AreEqual(TextNestingType.OpenNesting, nestingOpen);
			Assert.IsTrue(charsOpen.IsEmpty);
			Assert.AreEqual(1u, cOpen);
			Assert.AreSame(lineBreak, nestedOpen);

			// Position 1 -> the U+2028 line separator.
			lineBreak.GetRun(1, out _, out _, out var nestingChar, out _, out var charsChar, out var cChar);
			Assert.AreEqual(TextNestingType.NestedContent, nestingChar);
			Assert.AreEqual("\x2028", charsChar.ToString());
			Assert.AreEqual(1u, cChar);
		}

		[TestMethod]
		public void When_PositionCounts_Invalidated_On_Mutation()
		{
			var inlines = HostedInlines(new Run { Text = "AB" });

			inlines.GetPositionCount(out var before);
			Assert.AreEqual(6u, before); // 2 + (2+2)

			inlines.Add(new Run { Text = "CD" });

			inlines.GetPositionCount(out var after);
			Assert.AreEqual(10u, after); // 2 + (2+2) + (2+2)
		}
	}
}
