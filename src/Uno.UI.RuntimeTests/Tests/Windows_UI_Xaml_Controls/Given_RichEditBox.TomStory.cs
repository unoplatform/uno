#nullable enable

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[DataRow("", false)]
	[DataRow("abc", true)]
	public void When_Final_Eop_Is_Addressable(string text, bool selectionCanIncludeEop)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, text);
		var textLength = text.Length;
		var storyLength = textLength + 1;

		var eop = document.GetRange(textLength, storyLength);
		Assert.AreEqual(textLength, eop.StartPosition);
		Assert.AreEqual(storyLength, eop.EndPosition);
		Assert.AreEqual(1, eop.Length);
		Assert.AreEqual(storyLength, eop.StoryLength);
		Assert.AreEqual('\r', eop.Character);
		Assert.AreEqual("\r", eop.Text);

		var collapsedPastEop = document.GetRange(storyLength, storyLength);
		Assert.AreEqual(textLength, collapsedPastEop.StartPosition);
		Assert.AreEqual(textLength, collapsedPastEop.EndPosition);

		eop.SetRange(storyLength, textLength);
		Assert.AreEqual(textLength, eop.StartPosition);
		Assert.AreEqual(storyLength, eop.EndPosition);

		var setters = document.GetRange(0, 0);
		setters.EndPosition = storyLength;
		Assert.AreEqual(storyLength, setters.EndPosition);
		setters.StartPosition = storyLength;
		Assert.AreEqual(textLength, setters.StartPosition);
		Assert.AreEqual(textLength, setters.EndPosition);

		document.Selection.SetRange(textLength, storyLength);
		Assert.AreEqual(textLength, document.Selection.StartPosition);
		Assert.AreEqual(selectionCanIncludeEop ? storyLength : textLength, document.Selection.EndPosition);
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("abc")]
	public void When_Final_Eop_GetText_Options_Match_WinUI(string text)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, text);
		var eop = document.GetRange(text.Length, text.Length + 1);
		var content = document.GetRange(0, text.Length);

		Assert.AreEqual(text, GetTomText(content, TextGetOptions.AllowFinalEop));
		Assert.AreEqual("\r", GetTomText(eop, TextGetOptions.None));
		Assert.AreEqual("\r", GetTomText(eop, TextGetOptions.AllowFinalEop));
		Assert.AreEqual("\n", GetTomText(eop, TextGetOptions.AllowFinalEop | TextGetOptions.UseLf));
		Assert.AreEqual("\r\n", GetTomText(eop, TextGetOptions.AllowFinalEop | TextGetOptions.UseCrlf));

		Assert.AreEqual(text + "\r", GetTomText(document, TextGetOptions.None));
		Assert.AreEqual(text + "\r", GetTomText(document, TextGetOptions.AllowFinalEop));
		Assert.AreEqual(text, GetTomText(document, TextGetOptions.UseLf));
		Assert.AreEqual(text + "\n", GetTomText(document, TextGetOptions.UseLf | TextGetOptions.AllowFinalEop));
		Assert.AreEqual(text, GetTomText(document, TextGetOptions.UseCrlf));
		Assert.AreEqual(text + "\r\n", GetTomText(document, TextGetOptions.UseCrlf | TextGetOptions.AllowFinalEop));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("abc")]
	public void When_Final_Eop_Character_And_Paragraph_Units_Match_WinUI(string text)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, text);
		var textLength = text.Length;
		var storyLength = textLength + 1;

		var collapsed = document.GetRange(textLength, textLength);
		Assert.AreEqual(0, collapsed.Move(TextRangeUnit.Character, 1));
		Assert.AreEqual(textLength, collapsed.StartPosition);
		Assert.AreEqual(1, collapsed.Expand(TextRangeUnit.Character));
		Assert.AreEqual(textLength, collapsed.StartPosition);
		Assert.AreEqual(storyLength, collapsed.EndPosition);
		Assert.AreEqual("\r", collapsed.Text);

		var eop = document.GetRange(textLength, storyLength);
		Assert.AreEqual(-1, eop.Move(TextRangeUnit.Character, 1));
		Assert.AreEqual(textLength, eop.StartPosition);
		Assert.AreEqual(textLength, eop.EndPosition);

		var endpoint = document.GetRange(textLength, textLength);
		Assert.AreEqual(1, endpoint.MoveEnd(TextRangeUnit.Character, 1));
		Assert.AreEqual(storyLength, endpoint.EndPosition);
		Assert.AreEqual(0, endpoint.MoveStart(TextRangeUnit.Character, 1));

		var indexed = document.GetRange(textLength, textLength);
		Assert.AreEqual(storyLength, indexed.GetIndex(TextRangeUnit.Character));
		indexed.SetIndex(TextRangeUnit.Character, storyLength, false);
		Assert.AreEqual(textLength, indexed.StartPosition);
		Assert.AreEqual(textLength, indexed.EndPosition);
		indexed.SetRange(0, 0);
		indexed.SetIndex(TextRangeUnit.Story, -1, true);
		Assert.AreEqual(0, indexed.StartPosition);
		Assert.AreEqual(storyLength, indexed.EndPosition);

		var paragraph = document.GetRange(textLength, textLength);
		Assert.AreEqual(0, paragraph.Move(TextRangeUnit.Paragraph, 1));
		Assert.AreEqual(storyLength, paragraph.Expand(TextRangeUnit.Paragraph));
		Assert.AreEqual(0, paragraph.StartPosition);
		Assert.AreEqual(storyLength, paragraph.EndPosition);
		Assert.AreEqual(text + "\r", paragraph.Text);
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("abc")]
	public void When_Final_Eop_Is_Deleted_Or_Replaced(string text)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, text);
		var textLength = text.Length;
		var storyLength = textLength + 1;

		var deleted = document.GetRange(textLength, storyLength);
		Assert.AreEqual(0, deleted.Delete(TextRangeUnit.Character, 1));
		Assert.AreEqual(textLength, deleted.StartPosition);
		Assert.AreEqual(textLength, deleted.EndPosition);
		Assert.AreEqual(text + "\r", GetTomText(document, TextGetOptions.None));

		var replaced = document.GetRange(textLength, storyLength);
		replaced.Text = "X";
		Assert.AreEqual(textLength, replaced.StartPosition);
		Assert.AreEqual(textLength + 1, replaced.EndPosition);
		Assert.AreEqual("X", replaced.Text);
		Assert.AreEqual(text + "X\r", GetTomText(document, TextGetOptions.None));

		document.SetText(TextSetOptions.None, text);
		var character = document.GetRange(textLength, storyLength);
		character.Character = 'X';
		Assert.AreEqual(textLength, character.StartPosition);
		Assert.AreEqual(textLength + 2, character.EndPosition);
		Assert.AreEqual("X\r", character.Text);
		Assert.AreEqual(text + "X\r", GetTomText(document, TextGetOptions.None));
	}

	[TestMethod]
	[DataRow("", RangeGravity.Backward)]
	[DataRow("", RangeGravity.Forward)]
	[DataRow("", RangeGravity.Inward)]
	[DataRow("", RangeGravity.Outward)]
	[DataRow("", RangeGravity.UIBehavior)]
	[DataRow("abc", RangeGravity.Backward)]
	[DataRow("abc", RangeGravity.Forward)]
	[DataRow("abc", RangeGravity.Inward)]
	[DataRow("abc", RangeGravity.Outward)]
	[DataRow("abc", RangeGravity.UIBehavior)]
	public void When_Final_Eop_Range_Rebases_After_Adjacent_Insert(string text, RangeGravity gravity)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, text);
		var textLength = text.Length;
		var tracked = document.GetRange(textLength, textLength + 1);
		tracked.Gravity = gravity;

		document.GetRange(textLength, textLength).Text = "X";

		Assert.AreEqual(textLength, tracked.StartPosition);
		Assert.AreEqual(textLength + 2, tracked.EndPosition);
		Assert.AreEqual("X\r", tracked.Text);
	}

	private static string GetTomText(ITextRange range, TextGetOptions options)
	{
		range.GetText(options, out var value);
		return value;
	}

	private static string GetTomText(RichEditTextDocument document, TextGetOptions options)
	{
		document.GetText(options, out var value);
		return value;
	}
}
