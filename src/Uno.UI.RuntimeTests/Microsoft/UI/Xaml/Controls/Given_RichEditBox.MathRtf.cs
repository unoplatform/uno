#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Storage.Streams;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	private const string NativeFractionMathRtf = @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1033"
		+ @"{\fonttbl{\f0\fnil\fcharset0 Cambria Math;}{\f1\fnil Segoe UI Variable;}}"
		+ @"{\colortbl ;\red0\green0\blue0;}"
		+ @"{\*\generator Riched20 3.2.0000}{\*\mmathPr\mmathFont0\mdefJc3\mwrapIndent1440 }"
		+ @"\viewkind4\uc1 \pard\tx720"
		+ @"{\mmath{\*\moMathPara{\*\moMath\i\f0\fs21"
		+ @"{\mf{\mfPr{\mctrlPr\i\f0\fs21 }}"
		+ @"{\mnum\i\u-10187?\u-9138?}"
		+ @"{\mden\cf0\i\f0\u-10187?\u-9137?}}}}}"
		+ @"\par}";

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MathOnly_Document_GetText_Requires_Rtf()
	{
		var editor = await CreateSharedMathEditor();
		try
		{
			editor.Document.SetMathML(WrapMath("<mi>x</mi>"));
			var flags = new[]
			{
				TextGetOptions.AdjustCrlf,
				TextGetOptions.UseCrlf,
				TextGetOptions.UseObjectText,
				TextGetOptions.AllowFinalEop,
				TextGetOptions.NoHidden,
				TextGetOptions.IncludeNumbering,
				TextGetOptions.UseLf,
			};

			for (var mask = 0; mask < 1 << flags.Length; mask++)
			{
				var options = TextGetOptions.None;
				for (var index = 0; index < flags.Length; index++)
				{
					if ((mask & (1 << index)) != 0)
					{
						options |= flags[index];
					}
				}

				var error = Assert.ThrowsExactly<ArgumentException>(() => editor.Document.GetText(options, out _));
				Assert.AreEqual(unchecked((int)0x80070057), error.HResult, options.ToString());
			}

			foreach (var options in new[]
			{
				TextGetOptions.FormatRtf,
				TextGetOptions.FormatRtf | TextGetOptions.NoHidden,
				TextGetOptions.FormatRtf | TextGetOptions.UseLf,
				TextGetOptions.FormatRtf | TextGetOptions.NoHidden | TextGetOptions.UseLf,
			})
			{
				editor.Document.GetText(options, out var rtf);
				StringAssert.StartsWith(rtf, @"{\rtf1");
				StringAssert.Contains(rtf, @"\mmath");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Math_Rtf_Native_Controls_RoundTrip()
	{
		var editor = await CreateSharedMathEditor();
		try
		{
			var cases = new[]
			{
				new MathRtfCase("token", "<mi>x</mi>", new[] { @"\mmath", @"\moMathPara", @"\moMath", @"\mr" }),
				new MathRtfCase("fraction", "<mfrac><mi>a</mi><mi>b</mi></mfrac>", new[] { @"\mf", @"\mnum", @"\mden" }),
				new MathRtfCase("sub", "<msub><mi>x</mi><mi>i</mi></msub>", new[] { @"\msSub", @"\me", @"\msub" }),
				new MathRtfCase("sup", "<msup><mi>x</mi><mn>2</mn></msup>", new[] { @"\msSup", @"\me", @"\msup" }),
				new MathRtfCase("subsup", "<msubsup><mi>x</mi><mi>i</mi><mn>2</mn></msubsup>", new[] { @"\msSubSup", @"\msub", @"\msup" }),
				new MathRtfCase("sqrt", "<msqrt><mi>x</mi></msqrt>", new[] { @"\mrad", @"\mdegHide", @"\mdeg", @"\me" }),
				new MathRtfCase("root", "<mroot><mi>x</mi><mn>3</mn></mroot>", new[] { @"\mrad", @"\mdeg", @"\me" }),
				new MathRtfCase("fenced", "<mfenced open=\"[\" close=\"]\"><mi>x</mi><mi>y</mi></mfenced>", new[] { @"\md", @"\mbegChr", @"\mendChr" }),
				new MathRtfCase("matrix", "<mtable><mtr><mtd><mi>a</mi></mtd><mtd><mi>b</mi></mtd></mtr><mtr><mtd><mi>c</mi></mtd><mtd><mi>d</mi></mtd></mtr></mtable>", new[] { @"\mm", @"\mmr", @"\mcount 2" }),
				new MathRtfCase("over", "<mover><mi>x</mi><mo>¯</mo></mover>", new[] { @"\macc", @"\mchr" }),
				new MathRtfCase("under", "<munder><mi>x</mi><mo>_</mo></munder>", new[] { @"\mbar", @"\mbarPr" }),
				new MathRtfCase("nary", "<munderover><mo>∑</mo><mi>i</mi><mi>n</mi></munderover>", new[] { @"\mnary", @"\mlimLoc", @"\msub", @"\msup" }),
				new MathRtfCase("multiscripts", "<mmultiscripts><mi>T</mi><mi>i</mi><mn>2</mn><mprescripts/><mi>j</mi><mn>3</mn></mmultiscripts>", new[] { @"\msPre", @"\msSubSup" }),
			};

			foreach (var item in cases)
			{
				editor.Document.SetMathML(WrapMath(item.Body));
				editor.Document.GetMathML(out var expected);
				editor.Document.GetText(TextGetOptions.FormatRtf, out var rtf);

				foreach (var control in item.Controls)
				{
					StringAssert.Contains(rtf, control, item.Name);
				}
				Assert.IsFalse(rtf.Contains(@"\unomathml", StringComparison.Ordinal), item.Name);

#if HAS_UNO
				editor.Document.SetText(TextSetOptions.FormatRtf, rtf);
				editor.Document.GetMathML(out var actual);
#else
				using var stream = new InMemoryRandomAccessStream();
				editor.Document.SaveToStream(TextGetOptions.FormatRtf, stream);
				stream.Seek(0);
				var target = new RichEditBox();
				target.Document.SetMathMode(RichEditMathMode.MathOnly);
				target.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);
				target.Document.GetMathML(out var actual);
#endif
				AssertMathEquivalent(expected, actual, item.Name);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Native_Math_Rtf_Is_Imported_As_Structured_Math()
	{
		var editor = await CreateSharedMathEditor();
		try
		{
			editor.Document.SetText(TextSetOptions.FormatRtf, NativeFractionMathRtf);

			editor.Document.GetMathML(out var actual);
			AssertMathEquivalent(
				WrapMath("<mfrac><mi>a</mi><mi>b</mi></mfrac>"),
				actual,
				"native fraction");
			Assert.AreEqual(
				"\uFDD0\U0001D44E\uFDEE\U0001D44F\uFDEF\r",
				editor.Document.GetRange(0, int.MaxValue).Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Math_Rtf_Stream_RoundTrips_Structure()
	{
		var source = await CreateSharedMathEditor();
		try
		{
			source.Document.SetMathML(WrapMath(
				"<mfrac><msup><mi>x</mi><mn>2</mn></msup><mroot><mi>y</mi><mn>3</mn></mroot></mfrac>"));
			source.Document.GetMathML(out var expected);
			using var stream = new InMemoryRandomAccessStream();

			source.Document.SaveToStream(TextGetOptions.FormatRtf, stream);
			stream.Seek(0);

			var target = new RichEditBox();
			target.Document.SetMathMode(RichEditMathMode.MathOnly);
			target.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);
			target.Document.GetMathML(out var actual);
			AssertMathEquivalent(expected, actual, "stream");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Malformed_Math_Rtf_Import_Is_Atomic()
	{
		var editor = await CreateSharedMathEditor();
		try
		{
			editor.Document.SetMathML(WrapMath("<mi>x</mi>"));
			editor.Document.GetMathML(out var before);
			editor.Document.ClearUndoRedoHistory();
			editor.Document.Selection.SetRange(1, 1);
			const string malformed = @"{\rtf1\ansi{\mmath{\*\moMathPara{\*\moMath"
				+ @"{\mf{\mfPr{\mctrlPr }}{\mnum x}}}}}}";

#if HAS_UNO
			Assert.ThrowsExactly<ArgumentException>(() =>
				editor.Document.SetText(TextSetOptions.FormatRtf, malformed));

			editor.Document.GetMathML(out var after);
			AssertMathEquivalent(before, after, "atomicity");
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
			Assert.AreEqual(1, editor.Document.Selection.EndPosition);
			Assert.IsFalse(editor.Document.CanUndo());
#else
			editor.Document.SetText(TextSetOptions.FormatRtf, malformed);
			editor.Document.GetMathML(out var after);
			Assert.AreEqual(string.Empty, after);
#endif
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static void AssertMathEquivalent(string expected, string actual, string message)
		=> Assert.AreEqual(
			NormalizeMath(XDocument.Parse(expected).Root!),
			NormalizeMath(XDocument.Parse(actual).Root!),
			$"{message}{Environment.NewLine}Expected: {expected}{Environment.NewLine}Actual: {actual}");

	private sealed record MathRtfCase(string Name, string Body, IReadOnlyList<string> Controls);
}
