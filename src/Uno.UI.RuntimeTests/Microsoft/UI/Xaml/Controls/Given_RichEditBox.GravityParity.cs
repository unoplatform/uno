#nullable enable

using System;
using System.IO;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	[DataRow(4)]
	public void When_Range_Gravity_Edit_Matrix_Matches_Native(int gravityValue)
	{
		var gravity = (RangeGravity)gravityValue;
		var cases = new[]
		{
			new GravityEditCase("insert-before", 2, 5, 1, 1, "X", 3, 6, "234"),
			new GravityEditCase("insert-start", 2, 5, 2, 2, "X", 2, 6, "X234"),
			new GravityEditCase("insert-inside", 2, 5, 3, 3, "X", 2, 6, "2X34"),
			new GravityEditCase("insert-end", 2, 5, 5, 5, "X", 2, 5, "234"),
			new GravityEditCase("insert-after", 2, 5, 6, 6, "X", 2, 5, "234"),
			new GravityEditCase("delete-before", 2, 5, 0, 1, "", 1, 4, "234"),
			new GravityEditCase("delete-ending-start", 2, 5, 1, 2, "", 1, 4, "234"),
			new GravityEditCase("delete-overlap-start", 2, 5, 1, 3, "", 1, 3, "34"),
			new GravityEditCase("delete-inside", 2, 5, 3, 4, "", 2, 4, "24"),
			new GravityEditCase("delete-overlap-end", 2, 5, 4, 6, "", 2, 4, "23"),
			new GravityEditCase("delete-starting-end", 2, 5, 5, 6, "", 2, 5, "234"),
			new GravityEditCase("delete-after", 2, 5, 6, 7, "", 2, 5, "234"),
			new GravityEditCase("delete-cover", 2, 5, 1, 7, "", 1, 1, ""),
			new GravityEditCase("replace-overlap-start", 2, 5, 1, 3, "XY", 3, 5, "34"),
			new GravityEditCase("replace-inside", 2, 5, 3, 4, "XY", 2, 6, "2XY4"),
			new GravityEditCase("replace-overlap-end", 2, 5, 4, 6, "XY", 2, 4, "23"),
			new GravityEditCase("replace-cover", 2, 5, 1, 7, "XY", 1, 1, ""),
			new GravityEditCase("replace-before", 2, 5, 0, 2, "XYZ", 3, 6, "234"),
			new GravityEditCase("replace-after", 2, 5, 5, 7, "XYZ", 2, 5, "234"),
			new GravityEditCase("replace-noop", 2, 5, 2, 5, "234", 2, 5, "234"),
			new GravityEditCase("replace-noop-cover", 2, 4, 1, 5, "1234", 1, 1, ""),
		};

		foreach (var testCase in cases)
		{
			var document = CreateGravityDocument();
			var tracked = document.GetRange(testCase.RangeStart, testCase.RangeEnd);
			tracked.Gravity = gravity;

			document.GetRange(testCase.EditStart, testCase.EditEnd).Text = testCase.Replacement;

			Assert.AreEqual(testCase.ExpectedStart, tracked.StartPosition, testCase.Name);
			Assert.AreEqual(testCase.ExpectedEnd, tracked.EndPosition, testCase.Name);
			Assert.AreEqual(testCase.ExpectedText, tracked.Text, testCase.Name);
#if HAS_UNO && !__WASM__
			Assert.IsTrue(document.AreRunIndexesValid(), testCase.Name);
#endif
		}
	}

	[TestMethod]
	[DataRow(0, 0)]
	[DataRow(1, 1)]
	[DataRow(2, 2)]
	[DataRow(3, 3)]
	[DataRow(4, 0)]
	public void When_Range_Gravity_Validation_Matches_Native(int value, int expected)
	{
		var range = CreateGravityDocument().GetRange(2, 5);

		range.Gravity = (RangeGravity)value;

		Assert.AreEqual((RangeGravity)expected, range.Gravity);
	}

	[TestMethod]
	[DataRow(-1)]
	[DataRow(5)]
	[DataRow(999)]
	[DataRow(int.MinValue)]
	[DataRow(int.MaxValue)]
	public void When_Invalid_Range_Gravity_Throws_EInvalidArg(int value)
	{
		var range = CreateGravityDocument().GetRange(2, 5);

		var error = Assert.ThrowsExactly<ArgumentException>(() => range.Gravity = (RangeGravity)value);

		Assert.AreEqual(unchecked((int)0x80070057), error.HResult);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	[DataRow(4)]
	public void When_Range_Gravity_Special_Cases_Match_Native(int gravityValue)
	{
		var gravity = (RangeGravity)gravityValue;

		var sourceDocument = CreateGravityDocument();
		var source = sourceDocument.GetRange(2, 5);
		source.Gravity = gravity;
		source.Text = "XY";
		Assert.AreEqual(2, source.StartPosition);
		Assert.AreEqual(4, source.EndPosition);
		Assert.AreEqual("XY", source.Text);

		var insertionDocument = CreateGravityDocument();
		var insertionCaret = insertionDocument.GetRange(4, 4);
		insertionCaret.Gravity = gravity;
		insertionDocument.GetRange(4, 4).Text = "X";
		Assert.AreEqual((4, 4), (insertionCaret.StartPosition, insertionCaret.EndPosition));

		var replacementDocument = CreateGravityDocument();
		var replacementCaret = replacementDocument.GetRange(4, 4);
		replacementCaret.Gravity = gravity;
		replacementDocument.GetRange(3, 5).Text = "XYZ";
		Assert.AreEqual((3, 3), (replacementCaret.StartPosition, replacementCaret.EndPosition));

		var historyDocument = CreateGravityDocument();
		var history = historyDocument.GetRange(2, 5);
		history.Gravity = gravity;
		historyDocument.GetRange(2, 2).Text = "X";
		Assert.AreEqual((2, 6), (history.StartPosition, history.EndPosition));
		historyDocument.Undo();
		Assert.AreEqual((2, 5), (history.StartPosition, history.EndPosition));
		historyDocument.Redo();
		Assert.AreEqual((2, 6), (history.StartPosition, history.EndPosition));

		var eopDocument = CreateGravityDocument();
		var eop = eopDocument.GetRange(10, 11);
		eop.Gravity = gravity;
		eopDocument.GetRange(10, 10).Text = "X";
		Assert.AreEqual(10, eop.StartPosition);
		Assert.AreEqual(12, eop.EndPosition);
		Assert.AreEqual("X\r", eop.Text);

		var selectionDocument = CreateGravityDocument();
		var selection = selectionDocument.Selection;
		selection.SetRange(2, 5);
		selection.Gravity = gravity;
		selectionDocument.GetRange(2, 2).Text = "X";
		Assert.AreEqual(2, selection.StartPosition);
		Assert.AreEqual(6, selection.EndPosition);
		Assert.AreEqual(SelectionOptions.Replace, selection.Options);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	[DataRow(4)]
	public void When_Protected_Text_Range_Edit_Matches_Native(int gravityValue)
	{
		var document = CreateGravityDocument();
		document.GetRange(3, 4).CharacterFormat.ProtectedText = FormatEffect.On;
		var tracked = document.GetRange(2, 5);
		tracked.Gravity = (RangeGravity)gravityValue;

		document.GetRange(3, 4).Text = "X";

		GetTextWithoutFinalEop(document, out var text);
		Assert.AreEqual("012X456789", text);
		Assert.AreEqual(2, tracked.StartPosition);
		Assert.AreEqual(5, tracked.EndPosition);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	[DataRow(4)]
	public void When_Inline_Object_Gravity_Matches_Native(int gravityValue)
	{
		var document = CreateGravityDocument();
		using var stream = new MemoryStream(Convert.FromBase64String(GravityParityPngBase64)).AsRandomAccessStream();
		document.GetRange(4, 4).InsertImage(
			2,
			2,
			1,
			VerticalCharacterAlignment.Baseline,
			"image",
			stream);
		var tracked = document.GetRange(4, 5);
		tracked.Gravity = (RangeGravity)gravityValue;

		document.GetRange(4, 4).Text = "X";

		Assert.AreEqual(4, tracked.StartPosition);
		Assert.AreEqual(6, tracked.EndPosition);
		Assert.AreEqual('X', tracked.Text[0]);
	}

	private const string GravityParityPngBase64 =
		"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4AWJiZmT6DwAAAP//EKnFGgAAAAZJREFUAwABIQEIIJGZrwAAAABJRU5ErkJggg==";

	private static RichEditTextDocument CreateGravityDocument()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "0123456789");
		return document;
	}

	private readonly record struct GravityEditCase(
		string Name,
		int RangeStart,
		int RangeEnd,
		int EditStart,
		int EditEnd,
		string Replacement,
		int ExpectedStart,
		int ExpectedEnd,
		string ExpectedText);
}
