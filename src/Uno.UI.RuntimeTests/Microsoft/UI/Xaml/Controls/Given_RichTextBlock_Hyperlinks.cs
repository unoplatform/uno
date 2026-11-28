using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// The hyperlink list a RichTextBlock keeps for its pointer handling: what a content change costs, and that every
	// reader sees the links of the current content.
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Hyperlinks
	{
		private static Paragraph CreateParagraph(string text)
		{
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = text });
			return paragraph;
		}

		private static Hyperlink CreateLink(string text)
		{
			var link = new Hyperlink();
			link.Inlines.Add(new Run { Text = text });
			return link;
		}

		private static long AllocatedByAddingParagraphs(int count)
		{
			var paragraphs = Enumerable.Range(0, count).Select(i => CreateParagraph("lorem ipsum dolor sit amet " + i)).ToArray();
			var SUT = new RichTextBlock();

			var before = GC.GetAllocatedBytesForCurrentThread();
			foreach (var paragraph in paragraphs)
			{
				SUT.Blocks.Add(paragraph);
			}

			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		[TestMethod]
		public void When_Blocks_Added_Allocations_Grow_Linearly()
		{
			// Each block change used to scan every paragraph for links, about 90 bytes per paragraph scanned: adding N
			// blocks allocated in proportion to N². With bytes(n) = a·n + q·n²/2, bytes(2N) - 2·bytes(N) = q·N², so q is
			// what each pair of paragraphs costs beyond the linear part. Registering a block with its parent still
			// copies the parent's child list, 8 bytes per pair, which the bound leaves room for.
			const int N = 400;
			AllocatedByAddingParagraphs(50); // Warm up.
			var single = AllocatedByAddingParagraphs(N);
			var twice = AllocatedByAddingParagraphs(2 * N);

			var perPair = (double)(twice - 2 * single) / ((double)N * N);
			Assert.IsTrue(perPair < 24, $"Adding {2 * N} paragraphs allocated {twice:N0} bytes against {single:N0} for {N}: {perPair:0.0} bytes per pair of paragraphs beyond linear growth.");
		}

		private static long AllocatedByAddingRuns(int blockCount)
		{
			var SUT = new RichTextBlock();
			for (var i = 0; i < blockCount; i++)
			{
				SUT.Blocks.Add(CreateParagraph("lorem ipsum dolor sit amet " + i));
			}

			var first = (Paragraph)SUT.Blocks[0];
			var runs = Enumerable.Range(0, 50).Select(i => new Run { Text = " more" + i }).ToArray();

			var before = GC.GetAllocatedBytesForCurrentThread();
			foreach (var run in runs)
			{
				first.Inlines.Add(run);
			}

			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		[TestMethod]
		public void When_Inlines_Added_Cost_Does_Not_Depend_On_Block_Count()
		{
			// An inline added to one paragraph is a content change of the whole RichTextBlock, which used to scan every
			// paragraph for links.
			AllocatedByAddingRuns(10); // Warm up.
			var few = AllocatedByAddingRuns(200);
			var many = AllocatedByAddingRuns(1600);

			var ratio = (double)many / few;
			Assert.IsTrue(ratio < 1.5, $"Adding 50 runs next to 1,600 paragraphs allocated {many:N0} bytes, {ratio:0.00} times the {few:N0} bytes next to 200.");
		}

#if HAS_UNO
		// GetHyperlinks and GetFocusableChildren are Uno's.
		[TestMethod]
		public async Task When_Content_Changes_The_Links_Follow()
		{
			var SUT = new RichTextBlock { Width = 400 };
			var existing = CreateParagraph("Plain text ");
			SUT.Blocks.Add(existing);
			SUT.Blocks.Add(CreateParagraph("Second paragraph"));

			try
			{
				await UITestHelper.Load(SUT);
				CollectionAssert.AreEqual(Array.Empty<Hyperlink>(), SUT.GetHyperlinks().ToArray());

				// A block added; read before any layout runs.
				var inBlock = CreateLink("in a block");
				var linkParagraph = new Paragraph();
				linkParagraph.Inlines.Add(inBlock);
				SUT.Blocks.Add(linkParagraph);
				CollectionAssert.AreEqual(new[] { inBlock }, SUT.GetHyperlinks().ToArray(), "A block added");

				// An inline added to an existing paragraph, and a link inside a Span.
				var inline = CreateLink("inline");
				existing.Inlines.Add(inline);
				var inSpan = CreateLink("in a span");
				var span = new Span();
				span.Inlines.Add(inSpan);
				existing.Inlines.Add(span);
				CollectionAssert.AreEqual(new[] { inline, inSpan, inBlock }, SUT.GetHyperlinks().ToArray(), "Inlines added to an existing paragraph");

				// A block replaced, and one removed.
				var replacing = CreateLink("replacing");
				var replacement = new Paragraph();
				replacement.Inlines.Add(replacing);
				SUT.Blocks[0] = replacement;
				SUT.Blocks.Remove(linkParagraph);
				CollectionAssert.AreEqual(new[] { replacing }, SUT.GetHyperlinks().ToArray(), "A block replaced and one removed");

				await WindowHelper.WaitForIdle();

				CollectionAssert.AreEqual(new DependencyObject[] { replacing }, SUT.GetFocusableChildren().ToArray(), "Focusable children");

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				var linkPeers = (peer.GetChildren() ?? new List<AutomationPeer>()).OfType<HyperlinkAutomationPeer>().ToArray();
				Assert.AreEqual(1, linkPeers.Length, "Automation peers");
				Assert.AreSame(replacing.GetOrCreateAutomationPeer(), linkPeers[0]);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
#endif
	}
}
