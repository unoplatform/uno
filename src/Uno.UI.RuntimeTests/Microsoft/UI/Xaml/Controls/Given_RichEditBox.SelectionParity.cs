#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[DataRow(0, 16)]
	[DataRow(1, 17)]
	[DataRow(2, 18)]
	[DataRow(4, 20)]
	[DataRow(7, 23)]
	[DataRow(8, 16)]
	[DataRow(16, 16)]
	[DataRow(31, 23)]
	[DataRow(32, 16)]
	[DataRow(-1, 23)]
	public void When_Selection_Options_Mask_Matches_Native(int value, int expected)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abcdef");
		var selection = document.Selection;
		selection.SetRange(1, 4);

		selection.Options = (SelectionOptions)unchecked((uint)value);

		Assert.AreEqual((SelectionOptions)expected, selection.Options);
		Assert.AreEqual(SelectionType.Normal, selection.Type);
	}

	[TestMethod]
	public void When_Selection_Direction_And_Defaults_Match_Native()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abcdef");
		var selection = document.Selection;

		Assert.AreEqual(SelectionOptions.StartActive | SelectionOptions.Replace, selection.Options);
		Assert.AreEqual(SelectionType.InsertionPoint, selection.Type);

		selection.SetRange(1, 4);
		Assert.AreEqual(SelectionOptions.Replace, selection.Options);

		selection.SetRange(4, 1);
		Assert.AreEqual(1, selection.StartPosition);
		Assert.AreEqual(4, selection.EndPosition);
		Assert.AreEqual(SelectionOptions.StartActive | SelectionOptions.Replace, selection.Options);

		selection.SetRange(2, 2);
		selection.Options = (SelectionOptions)0;
		Assert.AreEqual(SelectionOptions.StartActive | SelectionOptions.Replace, selection.Options);
	}

	[TestMethod]
	[DataRow(0, 17)]
	[DataRow(4, 21)]
	[DataRow(16, 17)]
	[DataRow(20, 21)]
	public void When_TypeText_Replaces_A_Normal_Selection(int optionValue, int expectedOptions)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abcdef");
		var selection = document.Selection;
		selection.SetRange(2, 4);
		selection.Options = (SelectionOptions)optionValue;

		selection.TypeText("X");

		GetTextWithoutFinalEop(document, out var text);
		Assert.AreEqual("abXef", text);
		Assert.AreEqual(3, selection.StartPosition);
		Assert.AreEqual(3, selection.EndPosition);
		Assert.AreEqual((SelectionOptions)expectedOptions, selection.Options);
	}

	[TestMethod]
	[DataRow("A\u0301B", 0, "X", "XB", 1)]
	[DataRow("A\U0001F600B", 1, "X", "AXB", 2)]
	[DataRow("A\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466B", 1, "X", "AXB", 2)]
	[DataRow("A\u0301\U0001F600Z", 0, "XY", "XYZ", 2)]
	public void When_Overtype_Replaces_Unicode_Clusters(
		string source,
		int caret,
		string typed,
		string expected,
		int expectedCaret)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, source);
		var selection = document.Selection;
		selection.SetRange(caret, caret);
		selection.Options = SelectionOptions.Overtype;

		selection.TypeText(typed);

		GetTextWithoutFinalEop(document, out var text);
		Assert.AreEqual(expected, text);
		Assert.AreEqual(expectedCaret, selection.StartPosition);
		Assert.AreEqual(expectedCaret, selection.EndPosition);
		Assert.AreEqual(
			SelectionOptions.StartActive | SelectionOptions.Overtype | SelectionOptions.Replace,
			selection.Options);
	}

	[TestMethod]
	[DataRow(0, -2, true, 10)]
	[DataRow(0, 2, true, 6)]
	[DataRow(0, 2, false, 10)]
	[DataRow(1, -1, true, 11)]
	[DataRow(1, 2, false, 17)]
	[DataRow(13, 2, true, 6)]
	public void When_Horizontal_Movement_Counts_Match_Native(
		int unitValue,
		int count,
		bool moveLeft,
		int expectedPosition)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "A\u0301 one two three four");
		var selection = document.Selection;
		selection.SetRange(8, 8);

		var moved = moveLeft
			? selection.MoveLeft((TextRangeUnit)unitValue, count, false)
			: selection.MoveRight((TextRangeUnit)unitValue, count, false);

		Assert.AreEqual(Math.Sign(count) * Math.Abs(count), moved);
		Assert.AreEqual(expectedPosition, selection.StartPosition);
		Assert.AreEqual(expectedPosition, selection.EndPosition);
	}

	[TestMethod]
	[DataRow(0, 1, true, 8)]
	[DataRow(0, 2, true, 7)]
	[DataRow(1, 1, true, 7)]
	[DataRow(1, 2, true, 3)]
	[DataRow(13, 1, false, 13)]
	[DataRow(13, 2, false, 14)]
	public void When_Horizontal_Movement_Collapses_Ranges_Like_Native(
		int unitValue,
		int count,
		bool moveLeft,
		int expectedPosition)
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "A\u0301 one two three four");
		var selection = document.Selection;
		selection.SetRange(8, 13);

		var moved = moveLeft
			? selection.MoveLeft((TextRangeUnit)unitValue, count, false)
			: selection.MoveRight((TextRangeUnit)unitValue, count, false);

		Assert.AreEqual(count, moved);
		Assert.AreEqual(expectedPosition, selection.StartPosition);
		Assert.AreEqual(expectedPosition, selection.EndPosition);
	}

	[TestMethod]
	public void When_Selection_Unit_Validation_Matches_Native()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abc\rdef");
		var selection = document.Selection;

		for (var value = 0; value <= 32; value++)
		{
			var unit = (TextRangeUnit)value;
			if (unit is TextRangeUnit.Character or TextRangeUnit.Word or TextRangeUnit.Cluster)
			{
				selection.MoveLeft(unit, 0, false);
				selection.MoveRight(unit, 0, false);
			}
			else
			{
				SelectionParityAssertInvalid(() => selection.MoveLeft(unit, 1, false));
				SelectionParityAssertInvalid(() => selection.MoveRight(unit, 1, false));
			}

			if (unit is TextRangeUnit.Line or TextRangeUnit.Story)
			{
				selection.HomeKey(unit, false);
				selection.EndKey(unit, false);
			}
			else
			{
				SelectionParityAssertInvalid(() => selection.HomeKey(unit, false));
				SelectionParityAssertInvalid(() => selection.EndKey(unit, false));
			}

			if (unit is TextRangeUnit.Paragraph or TextRangeUnit.Line or TextRangeUnit.Screen or TextRangeUnit.Window)
			{
				selection.MoveUp(unit, 0, false);
				selection.MoveDown(unit, 0, false);
			}
			else
			{
				SelectionParityAssertInvalid(() => selection.MoveUp(unit, 1, false));
				SelectionParityAssertInvalid(() => selection.MoveDown(unit, 1, false));
			}
		}

		var invalid = (TextRangeUnit)999;
		SelectionParityAssertInvalid(() => selection.MoveLeft(invalid, 1, false));
		SelectionParityAssertInvalid(() => selection.HomeKey(invalid, false));
		SelectionParityAssertInvalid(() => selection.MoveUp(invalid, 1, false));
	}

	[TestMethod]
	public async Task When_Vertical_Movement_Uses_Native_Range_Edge_Rules()
	{
		var sut = new RichEditBox { Width = 300, Height = 100, TextWrapping = TextWrapping.NoWrap };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "first\rsecond\rthird");
			await WindowHelper.WaitForIdle();

			var selection = sut.Document.Selection;
			selection.SetRange(2, 8);
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Paragraph, 1, false));
			Assert.AreEqual(13, selection.StartPosition);
			Assert.AreEqual(13, selection.EndPosition);

			selection.SetRange(2, 8);
			selection.Options = SelectionOptions.StartActive;
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Paragraph, 1, false));
			Assert.AreEqual(13, selection.StartPosition);
			Assert.AreEqual(13, selection.EndPosition);

			selection.SetRange(8, 8);
			Assert.AreEqual(-1, selection.MoveDown(TextRangeUnit.Paragraph, -1, false));
			Assert.AreEqual(6, selection.StartPosition);

			selection.SetRange(0, 0);
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Screen, 1, true));
			Assert.IsTrue(selection.EndPosition > 0);

			selection.SetRange(0, 0);
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Window, 1, true));
			Assert.IsTrue(selection.EndPosition > 0);
			Assert.IsTrue(selection.EndPosition <= selection.StoryLength);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Home_End_And_Active_End_Match_Native()
	{
		var sut = new RichEditBox { Width = 300, TextWrapping = TextWrapping.NoWrap };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "abc\rdef");
			await WindowHelper.WaitForIdle();

			var selection = sut.Document.Selection;
			selection.SetRange(5, 5);
			Assert.AreEqual(-1, selection.HomeKey(TextRangeUnit.Line, false));
			Assert.AreEqual(4, selection.StartPosition);

			selection.SetRange(5, 5);
			Assert.AreEqual(2, selection.EndKey(TextRangeUnit.Line, false));
			Assert.AreEqual(7, selection.StartPosition);
			Assert.IsTrue(selection.Options.HasFlag(SelectionOptions.AtEndOfLine));

			selection.SetRange(2, 6);
			selection.Options = SelectionOptions.StartActive;
			Assert.AreEqual(-2, selection.HomeKey(TextRangeUnit.Story, true));
			Assert.AreEqual(0, selection.StartPosition);
			Assert.AreEqual(6, selection.EndPosition);
			Assert.IsTrue(selection.Options.HasFlag(SelectionOptions.StartActive));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Programmatic_Selection_Events_Match_Native()
	{
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "abcdef");
			await WindowHelper.WaitForIdle();

			var changing = 0;
			var changed = 0;
			var cancel = false;
			sut.SelectionChanging += (_, args) =>
			{
				changing++;
				args.Cancel = cancel;
			};
			sut.SelectionChanged += (_, _) => changed++;

			sut.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, changing);
			Assert.AreEqual(1, changed);

			changing = changed = 0;
			sut.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, changing);
			Assert.AreEqual(0, changed);

			sut.Document.Selection.Options = SelectionOptions.StartActive;
			Assert.AreEqual(0, changing);
			Assert.AreEqual(0, changed);

			cancel = true;
			sut.Document.Selection.SetRange(1, 4);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, changing);
			Assert.AreEqual(0, changed);
			Assert.AreEqual(2, sut.Document.Selection.StartPosition);
			Assert.AreEqual(5, sut.Document.Selection.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Inline_Image_Selection_Type_Matches_Native()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "ab");
		using var stream = new MemoryStream(Convert.FromBase64String(SelectionParityPngBase64)).AsRandomAccessStream();
		document.GetRange(1, 1).InsertImage(
			2,
			2,
			1,
			VerticalCharacterAlignment.Baseline,
			"image",
			stream);

		document.Selection.SetRange(1, 2);
		Assert.AreEqual(SelectionType.InlineShape, document.Selection.Type);

		document.Selection.SetRange(0, 2);
		Assert.AreEqual(SelectionType.Normal, document.Selection.Type);

		var link = document.GetRange(0, 1);
		link.Link = "\"https://example.com\"";
		document.Selection.SetRange(link.StartPosition, link.EndPosition);
		Assert.AreEqual(SelectionType.Normal, document.Selection.Type);
	}

	private const string SelectionParityPngBase64 =
		"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4AWJiZmT6DwAAAP//EKnFGgAAAAZJREFUAwABIQEIIJGZrwAAAABJRU5ErkJggg==";

	private static void SelectionParityAssertInvalid(Action action)
	{
		var error = Assert.ThrowsExactly<ArgumentException>(action);
		Assert.AreEqual(unchecked((int)0x80070057), error.HResult);
	}
}
