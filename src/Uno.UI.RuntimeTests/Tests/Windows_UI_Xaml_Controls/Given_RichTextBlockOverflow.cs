using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlockOverflow
	{
		private const string LongText =
			"Line one of the content. Line two of the content. Line three of the content. " +
			"Line four of the content. Line five of the content. Line six of the content. " +
			"Line seven of the content. Line eight of the content. Line nine of the content.";

		[TestMethod]
		public void When_OverflowContentTarget_Closes_A_Cycle()
		{
			// The chain walks follow OverflowContentTarget until it is null, so a cycle never terminates.
			// WinUI rejects the assignment outright rather than letting one form.
			var self = new RichTextBlockOverflow();

			Assert.ThrowsExactly<ArgumentException>(
				() => self.OverflowContentTarget = self,
				"A self-referencing OverflowContentTarget should be rejected");
			Assert.IsNull(self.OverflowContentTarget, "The rejected value must not be committed");

			var first = new RichTextBlockOverflow();
			var second = new RichTextBlockOverflow();
			first.OverflowContentTarget = second;

			Assert.ThrowsExactly<ArgumentException>(
				() => second.OverflowContentTarget = first,
				"Linking back onto an earlier link in the chain should be rejected");
			Assert.IsNull(second.OverflowContentTarget, "The rejected value must not be committed");
			Assert.AreEqual(second, first.OverflowContentTarget, "The valid forward link should survive the rejection");
		}

		[TestMethod]
		public async Task When_Content_Overflows_To_Target()
		{
			var master = new RichTextBlock { Width = 180, MaxLines = 2 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = LongText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 180 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.HasOverflowContent, "Master should report overflow content (MaxLines=2 with long text)");
				Assert.AreEqual(master, overflow.ContentSource, "Overflow ContentSource should be the master");
				Assert.IsTrue(overflow.ActualHeight > 0, $"Overflow should render its content slice (height {overflow.ActualHeight})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Content_Fits_No_Overflow()
		{
			var master = new RichTextBlock { Width = 400 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Short text that fits." });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 400 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(master.HasOverflowContent, "Master should not report overflow when content fits");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Overflow_BaselineOffset_Reflects_Content()
		{
			var master = new RichTextBlock { Width = 180, MaxLines = 2, FontSize = 24 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = LongText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 180 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.HasOverflowContent, "Master should overflow into the target");
				// The overflow's first-line baseline ≈ the font ascent of the flowed 24pt content.
				Assert.IsTrue(overflow.BaselineOffset > master.FontSize * 0.5 && overflow.BaselineOffset < master.FontSize * 1.5,
					$"Overflow BaselineOffset {overflow.BaselineOffset} should be a plausible first-line ascent for font size {master.FontSize}");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Overflow_Exposes_Content_Pointers()
		{
			// ContentStart, ContentEnd and GetPositionFromPoint used to throw, so pagination readers and
			// column hit-testing crashed on every column after the first.
			var master = new RichTextBlock { Width = 180, MaxLines = 2, FontSize = 24 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = LongText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 180, Height = 200 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.HasOverflowContent, "Master should overflow into the target");

				var start = overflow.ContentStart;
				var end = overflow.ContentEnd;

				Assert.IsNotNull(start, "The overflow should expose the start of its content slice");
				Assert.IsNotNull(end, "The overflow should expose the end of its content slice");

				// The slice is the continuation, so it does not start at the container's beginning.
				Assert.IsTrue(start!.Offset > 0, $"The slice should start after the master's content (offset {start.Offset})");
				Assert.IsTrue(end!.Offset >= start.Offset, $"End {end.Offset} should not precede start {start.Offset}");

				var hit = overflow.GetPositionFromPoint(new Point(20, 10));
				Assert.IsNotNull(hit, "Hit-testing inside the overflow should yield a position");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[DataRow(4, 400)]
		[DataRow(8, 400)]
		[DataRow(2, 120)]
		public async Task When_Overflow_Resumes_At_Master_Break(int maxLines, int overflowWidth)
		{
			// A link formats from the previous link's character break at its own width; line boundaries of the
			// master's narrower layout do not carry over.
			var master = new RichTextBlock { Width = 120, MaxLines = maxLines };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = LongText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = overflowWidth };
			master.OverflowContentTarget = overflow;

			var reference = new RichTextBlock { Width = 400 };
			var referenceParagraph = new Paragraph();
			referenceParagraph.Inlines.Add(new Run { Text = LongText });
			reference.Blocks.Add(referenceParagraph);

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);
			panel.Children.Add(reference);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.HasOverflowContent, "Master should overflow into the target");

				var masterEnd = master.ContentEnd;
				var overflowStart = overflow.ContentStart;
				if (masterEnd is null || overflowStart is null)
				{
					Assert.Fail($"The content pointers should be non-null (master end is null: {masterEnd is null}, overflow start is null: {overflowStart is null})");
					return;
				}

				Assert.AreEqual(masterEnd.Offset, overflowStart.Offset, "The overflow should start where the master ends");

				var firstHit = overflow.GetPositionFromPoint(new Point(0, 1));
				Assert.AreEqual(masterEnd.Offset, firstHit?.Offset, "The overflow's first line should start at the master's break");

				Assert.AreEqual(reference.ContentEnd?.Offset, overflow.ContentEnd?.Offset, "The overflow should end with the paragraph's last character");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
