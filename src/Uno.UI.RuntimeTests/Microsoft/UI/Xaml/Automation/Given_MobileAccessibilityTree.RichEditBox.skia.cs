#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

public partial class Given_MobileAccessibilityTree
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Then_PeerTree_Includes_TextObjects_And_Excludes_Placeholder()
	{
		var richEditBox = new RichEditBox { Width = 320, Height = 120, PlaceholderText = "Type here" };
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "prefix link suffix");
		richEditBox.Document.GetRange(7, 11).Link = "\"https://example.com\"";
		await UITestHelper.WaitForIdle();

		var nodes = AccessibilityPeerHelper.GetPeerTree(richEditBox);

		Assert.IsTrue(
			nodes.Any(node => node.ProviderPeer.GetAutomationControlType() == AutomationControlType.Hyperlink),
			"The RichEditBox hyperlink must be part of the peer tree.");
		Assert.IsFalse(
			nodes.Any(node => node.Owner is TextBlock textBlock && textBlock.GetTemplatedParent() is RichEditBox),
			"The placeholder TextBlock must not appear next to the editor, which already reports it.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Has_Spelling_Error_Then_PeerTree_Keeps_It_As_Annotation()
	{
		UnicodeText.SpellCheckingServiceOverrideForTesting = new TypoSpellCheckingService();
		try
		{
			var richEditBox = new RichEditBox { Width = 320, Height = 120 };
			await UITestHelper.Load(richEditBox);
			richEditBox.Document.SetText(TextSetOptions.None, "correct typo");
			await UITestHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox)!;
			var provider = (ITextProvider)peer.GetPattern(PatternInterface.Text)!;
			var annotations = (IRawElementProviderSimple[])provider.DocumentRange.GetAttributeValue(
				(int)AutomationTextAttributesEnum.AnnotationObjectsAttribute);
			Assert.HasCount(1, annotations, "The misspelled word must still be reported through the Text pattern.");

			var nodes = AccessibilityPeerHelper.GetPeerTree(richEditBox);

			Assert.IsFalse(
				nodes.Any(node => node.Peer is RichEditBoxSpellingErrorAutomationPeer),
				"A spelling error must not become a separate node next to the editor.");
			Assert.IsTrue(nodes.Any(node => node.Peer == peer));
		}
		finally
		{
			UnicodeText.SpellCheckingServiceOverrideForTesting = null;
		}
	}

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
				corrections.Add(word.Trim() == "typo" ? (offset, offset + 4) : null);
				wordStart = wordEnd;
			}

			return corrections;
		}

		public (int replaceIndexStart, int replaceIndexEnd, List<string> suggestions)? GetSpellCheckSuggestions(
			string text, List<int> wordBoundaries, int correctionStart, int correctionEnd)
			=> null;
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_RichEditBox_IsReadOnly_Then_Text_ReadOnly_State_Follows(bool isReadOnly)
	{
		var richEditBox = new RichEditBox { IsReadOnly = isReadOnly };
		await UITestHelper.Load(richEditBox);
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox)!;

		Assert.IsNull(peer.GetPattern(PatternInterface.Value), "Like WinUI, RichEditBox exposes no Value pattern.");
		Assert.AreEqual(isReadOnly, AccessibilityPeerHelper.IsTextReadOnly(peer));
		Assert.IsFalse(AccessibilityPeerHelper.CanSetText(peer));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_AcceptsReturn_Then_Text_Is_Multiline()
	{
		var richEditBox = new RichEditBox();
		await UITestHelper.Load(richEditBox);
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox)!;

		Assert.IsTrue(AccessibilityPeerHelper.IsMultilineText(peer));

		richEditBox.AcceptsReturn = false;
		Assert.IsFalse(AccessibilityPeerHelper.IsMultilineText(peer));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Selection_Is_Set_Backward_Then_Direction_Round_Trips()
	{
		var richEditBox = new RichEditBox();
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "Alpha beta gamma");
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox)!;

		Assert.IsTrue(AccessibilityPeerHelper.TrySetTextSelection(
			peer, 10, 6, allowReversed: true, out var actualStart, out var actualEnd));
		Assert.AreEqual(10, actualStart);
		Assert.AreEqual(6, actualEnd);

		Assert.IsTrue(AccessibilityPeerHelper.TryGetTextSelection(peer, out var start, out var end));
		Assert.AreEqual(10, start);
		Assert.AreEqual(6, end);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Word_Segment_Is_Queried_Then_It_Matches_Uia_Word_Range()
	{
		var richEditBox = new RichEditBox { Width = 320 };
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "Alpha beta gamma");
		await UITestHelper.WaitForIdle();
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox)!;
		var provider = (ITextProvider)peer.GetPattern(PatternInterface.Text)!;

		var wordRange = provider.DocumentRange.Clone();
		wordRange.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, 6);
		wordRange.MoveEndpointByRange(TextPatternRangeEndpoint.End, wordRange, TextPatternRangeEndpoint.Start);
		wordRange.ExpandToEnclosingUnit(TextUnit.Word);

		Assert.IsTrue(AccessibilityPeerHelper.TryGetTextSegment(
			peer, TextUnit.Word, 6, forward: true, out var segmentStart, out var segmentEnd));
		Assert.AreEqual(wordRange.GetText(-1), "Alpha beta gamma"[segmentStart..segmentEnd]);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focused_RichEditBox_Has_Selection_Then_Copy_And_Cut_Follow_ReadOnly()
	{
		var richEditBox = new RichEditBox();
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "Alpha beta");
		richEditBox.Focus(FocusState.Programmatic);
		await UITestHelper.WaitForIdle();
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox)!;

		Assert.IsFalse(AccessibilityPeerHelper.CanCopyText(peer), "An empty selection has nothing to copy.");

		richEditBox.Document.Selection.SetRange(0, 5);
		Assert.IsTrue(AccessibilityPeerHelper.CanCopyText(peer));
		Assert.IsTrue(AccessibilityPeerHelper.CanCutText(peer));

		richEditBox.IsReadOnly = true;
		Assert.IsTrue(AccessibilityPeerHelper.CanCopyText(peer));
		Assert.IsFalse(AccessibilityPeerHelper.CanCutText(peer));
		Assert.IsFalse(AccessibilityPeerHelper.CanPasteText(peer));
	}
}
