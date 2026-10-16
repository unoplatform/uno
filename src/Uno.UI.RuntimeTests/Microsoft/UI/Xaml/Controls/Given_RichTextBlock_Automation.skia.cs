using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// HyperlinkAutomationPeer has no WinUI public counterpart, so these cannot build on the
	// WinAppSDK parity head.
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Automation_Skia
	{
		[TestMethod]
		public async Task When_Hyperlink_Reports_A_Clickable_Point()
		{
			// HyperlinkAutomationPeer::GetClickablePointCore resolves the link's content range through the
			// text view and returns the top-left of its first rect. The port returned the origin, so
			// assistive tech was handed (0,0) for every link.
			var SUT = new RichTextBlock { Width = 400, FontSize = 24, TextWrapping = TextWrapping.NoWrap };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Leading text " });
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "the link" });
			paragraph.Inlines.Add(hyperlink);
			SUT.Blocks.Add(paragraph);

			try
			{
				await UITestHelper.Load(SUT);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				var hyperlinkPeer = (peer.GetChildren() ?? new List<AutomationPeer>()).OfType<HyperlinkAutomationPeer>().SingleOrDefault();
				Assert.IsNotNull(hyperlinkPeer, "The RichTextBlock peer should expose a peer for the Hyperlink");

				var point = hyperlinkPeer!.GetClickablePoint();

				// GetClickablePoint is in the same world space as the control's own transformed origin.
				// The link follows the leading run, so its point sits strictly inside the control's box.
				var origin = SUT.TransformToVisual(null).TransformPoint(new Point(0, 0));
				Assert.IsTrue(point.X > origin.X, $"The clickable point should be past the leading run (got {point.X}, control origin {origin.X})");
				Assert.IsTrue(point.X < origin.X + SUT.ActualWidth, $"The clickable point should sit inside the control (got {point.X}, origin {origin.X}, width {SUT.ActualWidth})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Hyperlink_Reports_A_Bounding_Rectangle()
		{
			// CCoreServices::GetTextElementBoundingRect unions the link's text bounds and transforms them to
			// screen space. The port returned an empty rect, so a focusable link had zero bounds.
			var SUT = new RichTextBlock { Width = 400, FontSize = 24, TextWrapping = TextWrapping.NoWrap };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Leading text " });
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "the link" });
			paragraph.Inlines.Add(hyperlink);
			SUT.Blocks.Add(paragraph);

			try
			{
				await UITestHelper.Load(SUT);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				var hyperlinkPeer = (peer.GetChildren() ?? new List<AutomationPeer>()).OfType<HyperlinkAutomationPeer>().SingleOrDefault();
				Assert.IsNotNull(hyperlinkPeer, "The RichTextBlock peer should expose a peer for the Hyperlink");

				var rect = hyperlinkPeer!.GetBoundingRectangle();
				var origin = SUT.TransformToVisual(null).TransformPoint(new Point(0, 0));

				Assert.IsTrue(rect.Width > 0 && rect.Height > 0, $"The link should report a non-empty rectangle (got {rect})");
				Assert.IsTrue(rect.X > origin.X, $"The rectangle should start past the leading run (got {rect.X}, control origin {origin.X})");
				Assert.IsTrue(rect.Right <= origin.X + SUT.ActualWidth + 1, $"The rectangle should stay inside the control (got {rect.Right}, origin {origin.X}, width {SUT.ActualWidth})");
				Assert.IsTrue(rect.Height <= SUT.ActualHeight + 1, $"A single-line link should not be taller than the control (got {rect.Height}, control {SUT.ActualHeight})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public void When_Hyperlink_Peer_Requested_Repeatedly()
		{
			// TextElement::GetOrCreateAutomationPeer caches the peer in m_tpAP for the element's lifetime.
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "the link" });

			var first = hyperlink.GetOrCreateAutomationPeer();

			Assert.IsInstanceOfType(first, typeof(HyperlinkAutomationPeer));
			Assert.AreSame(first, hyperlink.GetOrCreateAutomationPeer());
		}

		[TestMethod]
		public async Task When_Hyperlink_Peer_Reached_Through_Children_Walk()
		{
			var SUT = new RichTextBlock { Width = 400 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Leading text " });
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "the link" });
			paragraph.Inlines.Add(hyperlink);
			SUT.Blocks.Add(paragraph);

			try
			{
				await UITestHelper.Load(SUT);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				var first = (peer.GetChildren() ?? new List<AutomationPeer>()).OfType<HyperlinkAutomationPeer>().SingleOrDefault();
				var second = (peer.GetChildren() ?? new List<AutomationPeer>()).OfType<HyperlinkAutomationPeer>().SingleOrDefault();

				Assert.IsNotNull(first, "The RichTextBlock peer should expose a peer for the Hyperlink");
				Assert.AreSame(first, second, "Repeated children walks should return the same live peer");
				Assert.AreSame(first, hyperlink.GetOrCreateAutomationPeer());
				Assert.AreSame(peer, first!.GetParent(), "The parent set by the children walk should persist");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
