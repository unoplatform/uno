#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class TextInputTextTests
{
	[TestMethod]
	public void ShortTextIsSentWholeWithByteOffsets()
	{
		// "é" is 2 bytes and "😀" is a surrogate pair of 4 bytes.
		var surrounding = WaylandTextInputText.GetSurroundingText("aé😀b", cursor: 4, anchor: 1);
		Assert.AreEqual("aé😀b", surrounding.Text);
		Assert.AreEqual(0, surrounding.Start);
		Assert.AreEqual(7, surrounding.Cursor);
		Assert.AreEqual(1, surrounding.Anchor);
	}

	[TestMethod]
	public void LongTextIsWindowedAroundTheCursorWithinTheLimit()
	{
		var text = new string('x', 100) + "é" + new string('y', 100);
		var surrounding = WaylandTextInputText.GetSurroundingText(text, cursor: 101, anchor: 101, maxBytes: 11);
		Assert.IsTrue(System.Text.Encoding.UTF8.GetByteCount(surrounding.Text) <= 11);
		Assert.AreEqual(surrounding.Text, text.Substring(surrounding.Start, surrounding.Text.Length));
		Assert.AreEqual(WaylandTextInputText.CharIndexToByteOffset(surrounding.Text, 101 - surrounding.Start), surrounding.Cursor);
		StringAssert.Contains(surrounding.Text, "é");
	}

	[TestMethod]
	public void OversizedSelectionDoesNotFabricateAnAnchor()
	{
		var text = new string('x', 50);
		var surrounding = WaylandTextInputText.GetSurroundingText(text, cursor: 40, anchor: 0, maxBytes: 10);
		Assert.IsFalse(surrounding.IsSupported);
		Assert.AreEqual(((0, 0), (0, 0)), WaylandTextInputText.GetDeletionRanges(surrounding, 1, 1));
	}

	[TestMethod]
	public void WindowNeverSplitsSurrogatePairs()
	{
		var text = string.Concat(System.Linq.Enumerable.Repeat("😀", 20));
		var surrounding = WaylandTextInputText.GetSurroundingText(text, cursor: 21, anchor: 21, maxBytes: 10);
		Assert.AreEqual(4, surrounding.Text.Length);
		Assert.AreEqual(0, surrounding.Start % 2);
		Assert.AreEqual(0, surrounding.Cursor % 4);
	}

	[TestMethod]
	public void ByteOffsetsMapToCodePointStarts()
	{
		const string Text = "aé😀b";
		Assert.AreEqual(0, WaylandTextInputText.ByteOffsetToCharIndex(Text, 0));
		Assert.AreEqual(1, WaylandTextInputText.ByteOffsetToCharIndex(Text, 1));
		Assert.AreEqual(1, WaylandTextInputText.ByteOffsetToCharIndex(Text, 2));
		Assert.AreEqual(2, WaylandTextInputText.ByteOffsetToCharIndex(Text, 3));
		Assert.AreEqual(2, WaylandTextInputText.ByteOffsetToCharIndex(Text, 6));
		Assert.AreEqual(4, WaylandTextInputText.ByteOffsetToCharIndex(Text, 7));
		Assert.AreEqual(5, WaylandTextInputText.ByteOffsetToCharIndex(Text, 100));
	}

	[TestMethod]
	public void DeletionRangesExcludeTheSelection()
	{
		// Window starts at index 10 of the full text; "bc" is selected.
		var surrounding = new WaylandTextInputText.SurroundingText("aébcd😀e", Start: 10, Cursor: 5, Anchor: 3);
		var (before, after) = WaylandTextInputText.GetDeletionRanges(surrounding, beforeLength: 2, afterLength: 5);
		Assert.AreEqual((11, 1), before);
		Assert.AreEqual((14, 3), after);
	}

	[TestMethod]
	public void DeletionRangesClampToTheWindow()
	{
		var surrounding = new WaylandTextInputText.SurroundingText("abc", Start: 0, Cursor: 1, Anchor: 1);
		var (before, after) = WaylandTextInputText.GetDeletionRanges(surrounding, beforeLength: 10, afterLength: 10);
		Assert.AreEqual((0, 1), before);
		Assert.AreEqual((1, 2), after);
	}
	[TestMethod]
	public void DefaultWindowFitsTheProtocolLimitWithMultibyteText()
	{
		var text = string.Concat(System.Linq.Enumerable.Repeat("é😀", 2000));
		var surrounding = WaylandTextInputText.GetSurroundingText(text, 3000, 3000);
		Assert.IsTrue(surrounding.IsSupported);
		Assert.IsTrue(System.Text.Encoding.UTF8.GetByteCount(surrounding.Text) <= 4000);
		Assert.AreEqual(3000, surrounding.Start + WaylandTextInputText.ByteOffsetToCharIndex(surrounding.Text, surrounding.Cursor));
		Assert.AreEqual(surrounding.Cursor, surrounding.Anchor);
	}

}
