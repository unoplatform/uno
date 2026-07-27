#nullable enable

using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void When_Final_Eop_Range_Is_Collapsed(bool collapseToStart)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abc");
		var range = document.GetRange(3, 4);

		range.Collapse(collapseToStart);

		Assert.AreEqual(3, range.StartPosition);
		Assert.AreEqual(3, range.EndPosition);
		Assert.AreEqual(string.Empty, range.Text);
	}

	[TestMethod]
	public void When_ChangeCase_Maps_Unicode_Without_Moving_The_Range()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "a\u1F80b");
		var range = document.GetRange(1, 2);

		range.ChangeCase(LetterCase.Upper);

		GetTextWithoutFinalEop(document, out var text);
		Assert.AreEqual("a\u1F88b", text);
		Assert.AreEqual(1, range.StartPosition);
		Assert.AreEqual(2, range.EndPosition);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Malformed_MathML_Matches_Native_Clear_And_Undo_Behavior()
	{
		const string valid = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>x</mi></math>";
		const string malformed = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>bad</math>";
		var richEditBox = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = richEditBox;
			await WindowHelper.WaitForLoaded(richEditBox);
			richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
			richEditBox.Document.SetMathML(valid);
			richEditBox.Document.GetMathML(out var canonical);
			richEditBox.Document.ClearUndoRedoHistory();
			richEditBox.Document.Selection.SetRange(0, 1);

			Assert.ThrowsExactly<ArgumentException>(() => richEditBox.Document.SetMathML(malformed));

			richEditBox.Document.GetMathML(out var afterFailure);
			Assert.AreEqual(string.Empty, afterFailure);
			Assert.AreEqual(0, richEditBox.Document.Selection.StartPosition);
			Assert.AreEqual(0, richEditBox.Document.Selection.EndPosition);
			Assert.IsTrue(richEditBox.Document.CanUndo());

			richEditBox.Document.Undo();
			richEditBox.Document.GetMathML(out var afterUndo);
			Assert.AreEqual(
				XDocument.Parse(canonical).ToString(SaveOptions.DisableFormatting),
				XDocument.Parse(afterUndo).ToString(SaveOptions.DisableFormatting));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
