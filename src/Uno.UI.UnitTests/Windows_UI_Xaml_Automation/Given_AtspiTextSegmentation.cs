using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.X11;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Tests for <see cref="AtspiTextSegmentation"/>, the X11 AT-SPI backend's text offsets. AT-SPI counts
/// characters (code points) while .NET strings index UTF-16 units, so a surrogate pair is one character.
/// </summary>
[UITestClass]
public class Given_AtspiTextSegmentation
{
	private const string Emoji = "a\U0001F600b"; // 3 characters, 4 UTF-16 units
	private const uint BoundaryChar = 0;
	private const uint BoundaryWordStart = 1;
	private const uint BoundaryLineStart = 5;
	private const uint GranularityWord = 1;

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_OffsetCrossesSurrogatePair_Then_Utf16IndexSkipsBothUnits()
	{
		Assert.AreEqual(0, AtspiTextSegmentation.Utf16Index(Emoji, 0));
		Assert.AreEqual(1, AtspiTextSegmentation.Utf16Index(Emoji, 1));
		Assert.AreEqual(3, AtspiTextSegmentation.Utf16Index(Emoji, 2));
		Assert.AreEqual(4, AtspiTextSegmentation.Utf16Index(Emoji, 3));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_OffsetIsOutOfRange_Then_Utf16IndexClamps()
	{
		Assert.AreEqual(0, AtspiTextSegmentation.Utf16Index(Emoji, -1));
		Assert.AreEqual(4, AtspiTextSegmentation.Utf16Index(Emoji, 10));
		Assert.AreEqual(0, AtspiTextSegmentation.Utf16Index("", 3));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_CharacterCountOfUtf16Index_Then_RoundTrips()
	{
		for (var offset = 0; offset <= 3; offset++)
		{
			Assert.AreEqual(offset, AtspiTextSegmentation.CharacterCount(Emoji, AtspiTextSegmentation.Utf16Index(Emoji, offset)));
		}

		Assert.AreEqual(3, AtspiTextSegmentation.CharacterCount(Emoji, 100));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_CharBoundaryOnSurrogatePair_Then_RangeIsOneCharacter()
	{
		Assert.AreEqual((1, 2), AtspiTextSegmentation.GetTextAtOffset(Emoji, 1, BoundaryChar));
		Assert.AreEqual((3, 3), AtspiTextSegmentation.GetTextAtOffset(Emoji, 3, BoundaryChar));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_WordStartBoundary_Then_RangeRunsToNextWord()
	{
		const string text = "Hello world";

		Assert.AreEqual((0, 6), AtspiTextSegmentation.GetTextAtOffset(text, 0, BoundaryWordStart));
		Assert.AreEqual((6, 11), AtspiTextSegmentation.GetTextAtOffset(text, 7, BoundaryWordStart));
		Assert.AreEqual((0, 6), AtspiTextSegmentation.GetTextBeforeOffset(text, 7, BoundaryWordStart));
		Assert.AreEqual((6, 11), AtspiTextSegmentation.GetTextAfterOffset(text, 0, BoundaryWordStart));
		Assert.AreEqual((11, 11), AtspiTextSegmentation.GetTextAfterOffset(text, 7, BoundaryWordStart));
		Assert.AreEqual((6, 11), AtspiTextSegmentation.GetStringAtOffset(text, 8, GranularityWord));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_LineBoundary_Then_RangeIncludesTerminatingNewline()
	{
		const string text = "one\ntwo";

		Assert.AreEqual((0, 4), AtspiTextSegmentation.GetTextAtOffset(text, 1, BoundaryLineStart));
		Assert.AreEqual((4, 7), AtspiTextSegmentation.GetTextAtOffset(text, 5, BoundaryLineStart));
		Assert.AreEqual((0, 4), AtspiTextSegmentation.GetTextBeforeOffset(text, 5, BoundaryLineStart));
	}
}
