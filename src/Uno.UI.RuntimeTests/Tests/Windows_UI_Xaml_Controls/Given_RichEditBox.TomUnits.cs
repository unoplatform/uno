#nullable enable

using System;
using System.Text;
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
	[RunsOnUIThread]
	[DataRow(0, "E=1,2,3,  M+=1,5,5 M-=-1,2,2 MS=1,3,5 ME=-1,2,4 GI=3 SI=0,0")]
	[DataRow(1, "E=3,0,3,Á  M+=1,5,5 M-=-1,0,0 MS=1,3,5 ME=-1,2,4 GI=1 SI=0,0")]
	[DataRow(2, "E=6,0,6,Á B.\\r M+=1,6,6 M-=-1,0,0 MS=1,6,6 ME=-1,0,0 GI=1 SI=0,0")]
	[DataRow(3, "E=6,0,6,Á B.\\r M+=1,6,6 M-=-1,0,0 MS=1,6,6 ME=-1,0,0 GI=1 SI=0,0")]
	[DataRow(4, "E=6,0,6,Á B.\\r M+=1,6,6 M-=-1,0,0 MS=1,6,6 ME=-1,0,0 GI=1 SI=0,0")]
	[DataRow(5, "E=11,0,11,Á B.\\rC D!\\r M+=1,10,10 M-=-1,0,0 MS=1,10,10 ME=-1,0,0 GI=1 SI=0,0")]
	[DataRow(6, "E=!NotImplementedException:80004001 M+=!NotImplementedException:80004001 M-=!NotImplementedException:80004001 MS=!NotImplementedException:80004001 ME=!NotImplementedException:80004001 GI=!NotImplementedException:80004001 SI=!NotImplementedException:80004001")]
	[DataRow(7, "E=!NotImplementedException:80004001 M+=!NotImplementedException:80004001 M-=!NotImplementedException:80004001 MS=!NotImplementedException:80004001 ME=!NotImplementedException:80004001 GI=!NotImplementedException:80004001 SI=!NotImplementedException:80004001")]
	[DataRow(8, "E=0,2,2, M+=!NotImplementedException:80004001 M-=!NotImplementedException:80004001 MS=!NotImplementedException:80004001 ME=!NotImplementedException:80004001 GI=!NotImplementedException:80004001 SI=!NotImplementedException:80004001")]
	[DataRow(9, "E=2,2,4, B M+=1,5,5 M-=-1,2,2 MS=1,4,5 ME=-1,2,4 GI=2 SI=0,0")]
	[DataRow(10, "E=11,0,11,Á B.\\rC D!\\r M+=1,10,10 M-=-1,0,0 MS=1,10,10 ME=-1,0,0 GI=1 SI=0,0")]
	[DataRow(11, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=0,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(12, "E=6,0,6,Á B.\\r M+=1,6,6 M-=-1,0,0 MS=1,6,6 ME=-1,0,0 GI=1 SI=0,0")]
	[DataRow(13, "E=1,2,3,  M+=1,5,5 M-=-1,2,2 MS=1,3,5 ME=-1,2,4 GI=2 SI=0,0")]
	[DataRow(14, "E=-2,0,2,Á M+=1,5,5 M-=-1,0,0 MS=1,2,5 ME=-1,0,0 GI=1 SI=0,0")]
	[DataRow(15, "E=2,2,4, B M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=-1,2,2 GI=0 SI=0,0")]
	[DataRow(16, "E=0,2,2, M+=1,7,7 M-=-1,2,2 MS=1,4,5 ME=-1,2,4 GI=0 SI=0,0")]
	[DataRow(17, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(18, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(19, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(20, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(21, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(22, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,7,7 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(23, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(24, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(25, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(26, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(27, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(28, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(29, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(30, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(31, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,2,5 ME=0,2,5 GI=0 SI=0,0")]
	[DataRow(32, "E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=0,2,5 ME=0,2,5 GI=1 SI=0,0")]
	public void When_Tom_Unit_Operations_Match_WinUI(int unitValue, string expected)
	{
		var unit = (TextRangeUnit)unitValue;

		Assert.AreEqual(expected, ProbeTomUnit(unit), unit.ToString());
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Tom_Link_Unit_Uses_The_Next_Link_Run()
	{
		var document = CreateTomUnitDocument();
		document.GetRange(9, 10).Link = "\"https://example.com\"";

		Assert.AreEqual(
			"E=0,2,2, M+=1,5,5 M-=-1,2,2 MS=1,9,9 ME=0,2,5 GI=0 SI=0,0",
			ProbeTomUnit(TextRangeUnit.Link, () => document));
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(14)]
	[DataRow(15)]
	[DataRow(16)]
	[DataRow(17)]
	[DataRow(18)]
	[DataRow(20)]
	[DataRow(21)]
	[DataRow(22)]
	[DataRow(23)]
	[DataRow(28)]
	[DataRow(29)]
	public void When_Tom_Modeled_Effect_Expands_Active_Run(int unitValue)
	{
		var unit = (TextRangeUnit)unitValue;
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abcd");
		ApplyModeledEffect(document.GetRange(1, 3), unit);

		var range = document.GetRange(2, 2);
		Assert.AreEqual(2, range.Expand(unit), unit.ToString());
		Assert.AreEqual(1, range.StartPosition);
		Assert.AreEqual(3, range.EndPosition);
		Assert.AreEqual(0, document.GetRange(2, 2).GetIndex(unit));
		Assert.AreEqual(0, document.GetRange(3, 3).GetIndex(unit));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Tom_Unicode_Cluster_Delete_And_Selection_Match_WinUI()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "A\u0301 B");

		var caret = document.GetRange(0, 0);
		Assert.AreEqual(1, caret.Delete(TextRangeUnit.Cluster, 1));
		Assert.AreEqual(" B", GetTomText(document, TextGetOptions.UseLf));

		document.SetText(TextSetOptions.None, "A\u0301 B");
		var selection = document.Selection;
		selection.SetRange(0, 0);
		Assert.AreEqual(1, selection.MoveRight(TextRangeUnit.Cluster, 1, false));
		Assert.AreEqual(2, selection.StartPosition);
		Assert.AreEqual(2, selection.EndPosition);
		Assert.AreEqual(1, selection.MoveLeft(TextRangeUnit.Cluster, 1, true));
		Assert.AreEqual(0, selection.StartPosition);
		Assert.AreEqual(2, selection.EndPosition);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Tom_Delete_Uses_Units_And_Unsupported_Effects_NoOp()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "one two");
		Assert.AreEqual(1, document.GetRange(0, 0).Delete(TextRangeUnit.Word, 1));
		Assert.AreEqual("two", GetTomText(document, TextGetOptions.UseLf));

		document.SetText(TextSetOptions.None, "abcd");
		document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;
		Assert.AreEqual(-1, document.GetRange(2, 2).Delete(TextRangeUnit.Bold, -1));
		Assert.AreEqual("cd", GetTomText(document, TextGetOptions.UseLf));

		var unsupported = document.GetRange(1, 1);
		Assert.AreEqual(0, unsupported.Delete(TextRangeUnit.Shadow, 1));
		Assert.AreEqual("cd", GetTomText(document, TextGetOptions.UseLf));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Tom_Invalid_Units_Throw_EInvalidArg()
	{
		var invalid = (TextRangeUnit)999;
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abc");
		var range = document.GetRange(1, 2);

		AssertTomException<ArgumentException>(() => range.Expand(invalid), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => range.Move(invalid, 1), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => range.MoveStart(invalid, 1), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => range.MoveEnd(invalid, 1), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => range.GetIndex(invalid), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => range.SetIndex(invalid, 1, false), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => range.Delete(invalid, 1), unchecked((int)0x80070057));

		var selection = document.Selection;
		AssertTomException<ArgumentException>(() => selection.MoveLeft(invalid, 1, false), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => selection.MoveRight(invalid, 1, false), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => selection.HomeKey(invalid, false), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => selection.EndKey(invalid, false), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => selection.MoveUp(invalid, 1, false), unchecked((int)0x80070057));
		AssertTomException<ArgumentException>(() => selection.MoveDown(invalid, 1, false), unchecked((int)0x80070057));
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(6)]
	[DataRow(7)]
	public void When_Tom_Unsupported_Units_Throw_ENotImpl(int unitValue)
	{
		var unit = (TextRangeUnit)unitValue;
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abc");
		var range = document.GetRange(1, 2);

		AssertTomException<NotImplementedException>(() => range.Expand(unit), unchecked((int)0x80004001));
		AssertTomException<NotImplementedException>(() => range.Move(unit, 1), unchecked((int)0x80004001));
		AssertTomException<NotImplementedException>(() => range.MoveStart(unit, 1), unchecked((int)0x80004001));
		AssertTomException<NotImplementedException>(() => range.MoveEnd(unit, 1), unchecked((int)0x80004001));
		AssertTomException<NotImplementedException>(() => range.GetIndex(unit), unchecked((int)0x80004001));
		AssertTomException<NotImplementedException>(() => range.SetIndex(unit, 1, false), unchecked((int)0x80004001));
		Assert.AreEqual(1, range.Delete(unit, 1));
		Assert.AreEqual(1, range.StartPosition);
		Assert.AreEqual(1, range.EndPosition);
		Assert.AreEqual("ac", GetTomText(document, TextGetOptions.UseLf));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Tom_LinkProtected_Uses_The_Modeled_Link_Run()
	{
#if HAS_UNO
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abcd");
		var formatted = document.GetRange(1, 3);
		formatted.Link = "\"https://example.com\"";
		formatted.CharacterFormat.ProtectedText = FormatEffect.On;

		var range = document.GetRange(2, 2);
		Assert.AreEqual(2, range.Expand(TextRangeUnit.LinkProtected));
		Assert.AreEqual(1, range.StartPosition);
		Assert.AreEqual(3, range.EndPosition);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Tom_FontBound_Uses_The_Modeled_Font_Run()
	{
#if HAS_UNO
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abcd");
		document.GetRange(1, 3).CharacterFormat.Name = "Arial";

		var range = document.GetRange(2, 2);
		Assert.AreEqual(2, range.Expand(TextRangeUnit.FontBound));
		Assert.AreEqual(1, range.StartPosition);
		Assert.AreEqual(3, range.EndPosition);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Tom_Object_Unit_Uses_Inline_Image_Run()
	{
#if HAS_UNO
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abcd");
		document.GetRange(2, 2).InsertImage(
			2,
			2,
			1,
			VerticalCharacterAlignment.Baseline,
			"image",
			CreateImageStream(SkiaSharp.SKColors.Red));

		var expanded = document.GetRange(3, 3);
		Assert.AreEqual(-1, expanded.Expand(TextRangeUnit.Object));
		Assert.AreEqual(2, expanded.StartPosition);
		Assert.AreEqual(3, expanded.EndPosition);
		Assert.AreEqual(1, document.GetRange(3, 3).GetIndex(TextRangeUnit.Object));

		var start = document.GetRange(0, 4);
		Assert.AreEqual(1, start.MoveStart(TextRangeUnit.Object, 1));
		Assert.AreEqual(2, start.StartPosition);

		var indexed = document.GetRange(0, 0);
		indexed.SetIndex(TextRangeUnit.Object, 1, false);
		Assert.AreEqual(2, indexed.StartPosition);

		Assert.AreEqual(-1, document.GetRange(3, 3).Delete(TextRangeUnit.Object, -1));
		Assert.AreEqual("abcd", GetTomText(document, TextGetOptions.UseLf));
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Tom_Window_Unit_Uses_Loaded_Viewport()
	{
		var richEditBox = new RichEditBox
		{
			Width = 180,
			Height = 80,
			TextWrapping = TextWrapping.Wrap,
		};
		try
		{
			WindowHelper.WindowContent = richEditBox;
			await WindowHelper.WaitForLoaded(richEditBox);
			richEditBox.Document.SetText(
				TextSetOptions.None,
				"one two three four five six seven eight nine ten\rsecond paragraph\rthird paragraph");
			await WindowHelper.WaitForIdle();

			var range = richEditBox.Document.GetRange(5, 5);
			range.Expand(TextRangeUnit.Window);
			Assert.IsTrue(range.StartPosition <= 5);
			Assert.IsTrue(range.EndPosition > 5);
			Assert.IsTrue(range.EndPosition <= range.StoryLength);
			Assert.AreEqual(1, range.GetIndex(TextRangeUnit.Window));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Tom_Unit_Navigation_Preserves_The_Virtual_Final_Eop()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abc");

		var range = document.GetRange(3, 3);
		Assert.AreEqual(0, range.Move(TextRangeUnit.Paragraph, 1));
		Assert.AreEqual(0, range.MoveStart(TextRangeUnit.Paragraph, 1));
		Assert.AreEqual(0, range.Delete(TextRangeUnit.Paragraph, 1));
		Assert.AreEqual("abc", GetTomText(document, TextGetOptions.UseLf));

		var selection = document.Selection;
		selection.SetRange(3, 3);
		AssertTomException<ArgumentException>(
			() => selection.MoveRight(TextRangeUnit.Paragraph, 1, false),
			unchecked((int)0x80070057));
		Assert.AreEqual(3, selection.StartPosition);
		Assert.AreEqual(3, selection.EndPosition);
	}

	private static string ProbeTomUnit(TextRangeUnit unit, Func<RichEditTextDocument>? documentFactory = null)
	{
		var output = new StringBuilder();
		ProbeTomOperation(output, "E", documentFactory, document =>
		{
			var range = document.GetRange(2, 2);
			var result = range.Expand(unit);
			return $"{result},{range.StartPosition},{range.EndPosition},{EscapeTomText(range.Text)}";
		});
		ProbeTomOperation(output, "M+", documentFactory, document =>
		{
			var range = document.GetRange(2, 5);
			var result = range.Move(unit, 1);
			return $"{result},{range.StartPosition},{range.EndPosition}";
		});
		ProbeTomOperation(output, "M-", documentFactory, document =>
		{
			var range = document.GetRange(2, 5);
			var result = range.Move(unit, -1);
			return $"{result},{range.StartPosition},{range.EndPosition}";
		});
		ProbeTomOperation(output, "MS", documentFactory, document =>
		{
			var range = document.GetRange(2, 5);
			var result = range.MoveStart(unit, 1);
			return $"{result},{range.StartPosition},{range.EndPosition}";
		});
		ProbeTomOperation(output, "ME", documentFactory, document =>
		{
			var range = document.GetRange(2, 5);
			var result = range.MoveEnd(unit, -1);
			return $"{result},{range.StartPosition},{range.EndPosition}";
		});
		ProbeTomOperation(output, "GI", documentFactory, document =>
			document.GetRange(2, 2).GetIndex(unit).ToString());
		ProbeTomOperation(output, "SI", documentFactory, document =>
		{
			var range = document.GetRange(2, 2);
			range.SetIndex(unit, 1, false);
			return $"{range.StartPosition},{range.EndPosition}";
		});
		return output.ToString();
	}

	private static void ProbeTomOperation(
		StringBuilder output,
		string name,
		Func<RichEditTextDocument>? documentFactory,
		Func<RichEditTextDocument, string> operation)
	{
		if (output.Length > 0)
		{
			output.Append(' ');
		}
		output.Append(name).Append('=');
		try
		{
			output.Append(operation(documentFactory?.Invoke() ?? CreateTomUnitDocument()));
		}
		catch (Exception error)
		{
			output.Append('!').Append(error.GetType().Name).Append(':').Append(error.HResult.ToString("X8"));
		}
	}

	private static RichEditTextDocument CreateTomUnitDocument()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "A\u0301 B.\r\nC D!");
		document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;
		document.GetRange(2, 4).CharacterFormat.Italic = FormatEffect.On;
		document.GetRange(4, 7).CharacterFormat.Underline = UnderlineType.Single;
		document.GetRange(7, 9).CharacterFormat.Hidden = FormatEffect.On;
		document.GetRange(0, 7).ParagraphFormat.Alignment = ParagraphAlignment.Center;
		return document;
	}

	private static void ApplyModeledEffect(ITextRange range, TextRangeUnit unit)
	{
		switch (unit)
		{
			case TextRangeUnit.Bold:
				range.CharacterFormat.Bold = FormatEffect.On;
				break;
			case TextRangeUnit.Italic:
				range.CharacterFormat.Italic = FormatEffect.On;
				break;
			case TextRangeUnit.Underline:
				range.CharacterFormat.Underline = UnderlineType.Single;
				break;
			case TextRangeUnit.Strikethrough:
				range.CharacterFormat.Strikethrough = FormatEffect.On;
				break;
			case TextRangeUnit.ProtectedText:
				range.CharacterFormat.ProtectedText = FormatEffect.On;
				break;
			case TextRangeUnit.SmallCaps:
				range.CharacterFormat.SmallCaps = FormatEffect.On;
				break;
			case TextRangeUnit.AllCaps:
				range.CharacterFormat.AllCaps = FormatEffect.On;
				break;
			case TextRangeUnit.Hidden:
				range.CharacterFormat.Hidden = FormatEffect.On;
				break;
			case TextRangeUnit.Outline:
				range.CharacterFormat.Outline = FormatEffect.On;
				break;
			case TextRangeUnit.Subscript:
				range.CharacterFormat.Subscript = FormatEffect.On;
				break;
			case TextRangeUnit.Superscript:
				range.CharacterFormat.Superscript = FormatEffect.On;
				break;
			case TextRangeUnit.FontBound:
				range.CharacterFormat.Name = "Arial";
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(unit));
		}
	}

	private static void AssertTomException<TException>(Action action, int expectedHResult)
		where TException : Exception
	{
		var error = Assert.ThrowsExactly<TException>(action);
		Assert.AreEqual(expectedHResult, error.HResult);
	}

	private static string EscapeTomText(string value)
		=> value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
}
