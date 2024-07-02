#if __SKIA__

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Documents.TextFormatting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	public class Given_Run
	{

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_SingleLine()
		{
			var expected = new ExpectedSegment[] {
				new("  Test ", 2, 1, 0, true),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.Segments);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_SingleLineWith1CharLineBreak()
		{
			var expected = new ExpectedSegment[] {
				new("  Test \n", 2, 1, 1, true),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.Segments);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_SingleLineWith2CharLineBreak()
		{
			var expected = new ExpectedSegment[] {
				new("Test  \r\n", 0, 2, 2, true),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.Segments);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_Multiline()
		{
			var expected = new ExpectedSegment[] {
				new("Test1\n", 0, 0, 1, false),
				new(" Test2  ", 1, 2, 0, true),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.Segments);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_MultiwordWithSpaces()
		{
			var expected = new ExpectedSegment[] {
				new(" Test1 ", 1, 1, 0, true),
				new("Test2 ", 0, 1, 0, true),
				new("Test3   ", 0, 3, 0, true),
				new("Test4", 0, 0, 0, false),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.Segments);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_MultiwordWithHyphens()
		{
			var expected = new ExpectedSegment[] {
				new(" Test1- ", 1, 1, 0, true),
				new("Test2---", 0, 0, 0, true),
				new("Test3 ", 0, 1, 0, true),
				new("-", 0, 0, 0, true),
				new("Test4", 0, 0, 0, false),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.Segments);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_MultiwordWithNumericHyphens()
		{
			var expected = new ExpectedSegment[] {
				new("Test1-$123-", 0, 0, 0, true),
				new("Test2 ", 0, 1, 0, true),
				new("-.456", 0, 0, 0, false),
			};

			Run run = new() { Text = GetText(expected) };
			AssertSegmentsMatch(expected, run.Segments);
		}

		private static string GetText(ExpectedSegment[] expectedSegments) => string.Concat(expectedSegments.Select(s => s.Text));

		private static void AssertSegmentsMatch(ExpectedSegment[] expectedSegments, IReadOnlyList<Segment> resultSegments)
		{
			Assert.AreEqual(expectedSegments.Length, resultSegments.Count);

			int start = 0;

			foreach (var (expected, result) in expectedSegments.Zip(resultSegments, (t, r) => (t, r)))
			{
				Assert.AreEqual(start, result.Start);
				Assert.AreEqual(expected.Text.Length, result.Length);

				Assert.AreEqual(expected.LeadingSpaces, result.LeadingSpaces);
				Assert.AreEqual(expected.TrailingSpaces, result.TrailingSpaces);

				Assert.AreEqual(expected.WordBreakAfter, result.WordBreakAfter);

				Assert.AreEqual(expected.LineBreakLength, result.LineBreakLength);
				Assert.AreEqual(expected.LineBreakAfter, result.LineBreakAfter);

				if (expected.LineBreakLength == 2)
				{
					Assert.AreEqual(expected.Text.Length - 1, result.Glyphs.Count);
					Assert.AreEqual(start + expected.Text.Length - 2, result.Glyphs[result.Glyphs.Count - 1].Cluster);
				}
				else
				{
					Assert.AreEqual(expected.Text.Length, result.Glyphs.Count);
					Assert.AreEqual(start + expected.Text.Length - 1, result.Glyphs[result.Glyphs.Count - 1].Cluster);
				}

				Assert.AreEqual(start, result.Glyphs[0].Cluster);

				start += expected.Text.Length;
			}
		}

		[DebuggerDisplay("{Text}")]
		private class ExpectedSegment
		{
			public string Text { get; }

			public int LeadingSpaces { get; }

			public int TrailingSpaces { get; }

			public int LineBreakLength { get; }

			public bool LineBreakAfter => LineBreakLength > 0;

			public bool WordBreakAfter { get; }

			public ExpectedSegment(string text, int leadingSpaces, int trailingSpaces, int lineBreakLength, bool wordBreakAfter)
			{
				Text = text;
				LeadingSpaces = leadingSpaces;
				TrailingSpaces = trailingSpaces;
				LineBreakLength = lineBreakLength;
				WordBreakAfter = wordBreakAfter;
			}
		}
	}
}
#endif
