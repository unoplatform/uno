#nullable enable

using System;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_Captured_Native_Math_Rtf_Controls_Are_Imported()
	{
		var cases = new[]
		{
			new NativeMathRtfCase("token", "<mi>x</mi>", @"{\mr\u-10187?\u-9115?}"),
			new NativeMathRtfCase(
				"fraction",
				"<mfrac><mi>a</mi><mi>b</mi></mfrac>",
				@"{\mf{\mfPr{\mctrlPr\i\f0\fs21 }}{\mnum\i\u-10187?\u-9138?}{\mden\cf0\i\f0\u-10187?\u-9137?}}"),
			new NativeMathRtfCase(
				"sub",
				"<msub><mi>x</mi><mi>i</mi></msub>",
				@"{\msSub{\msSubPr{\mctrlPr\i\f0\fs21 }}{\me\i\u-10187?\u-9115?}{\msub\cf0\i\f0\u-10187?\u-9130?}}"),
			new NativeMathRtfCase(
				"sup",
				"<msup><mi>x</mi><mn>2</mn></msup>",
				@"{\msSup{\msSupPr{\mctrlPr\i\f0\fs21 }}{\me\i\u-10187?\u-9115?}{\msup\cf0\i0\f0 2\i }}"),
			new NativeMathRtfCase(
				"subsup",
				"<msubsup><mi>x</mi><mi>i</mi><mn>2</mn></msubsup>",
				@"{\msSubSup{\msSubSupPr{\mctrlPr\i\f0\fs21 }}{\me\i\u-10187?\u-9115?}{\msub\cf0\i\f0\u-10187?\u-9130?}{\msup\i0 2\i }}"),
			new NativeMathRtfCase(
				"sqrt",
				"<msqrt><mi>x</mi></msqrt>",
				@"{\mrad{\mradPr{\mctrlPr\i\f0\fs21\tomAlign0 }{\mdegHide on}}{\mdeg\i }{\me\cf0\i\f0\u-10187?\u-9115?}}"),
			new NativeMathRtfCase(
				"root",
				"<mroot><mi>x</mi><mn>3</mn></mroot>",
				@"{\mrad{\mradPr{\mctrlPr\i\f0\fs21\tomAlign0 }}{\mdeg\i0 3\i }{\me\cf0\i\f0\u-10187?\u-9115?}}"),
			new NativeMathRtfCase(
				"fenced",
				"<mfenced open=\"[\" close=\"]\"><mi>x</mi><mi>y</mi></mfenced>",
				@"{\md{\mdPr{\mctrlPr\i\f0\fs21 }{\mbegChr [}{\mendChr ]}}{\me\i\u-10187?\u-9115?\cf1\i0 ,\cf0\i\u-10187?\u-9114?\kerning9 }}"),
			new NativeMathRtfCase(
				"matrix",
				"<mtable><mtr><mtd><mi>a</mi></mtd><mtd><mi>b</mi></mtd></mtr><mtr><mtd><mi>c</mi></mtd><mtd><mi>d</mi></mtd></mtr></mtable>",
				@"{\mm{\mmPr{\mctrlPr\i\f0\fs21\tomAlign0 }{\mplcHide on}{\mmcs{\mmc{\mmcPr{\mmcJc center}{\mcount 2}}}}}{\mmr{\me\i\u-10187?\u-9138?}{\me\cf0\i\f0\u-10187?\u-9137?}}{\mmr{\me\i\u-10187?\u-9136?}{\me\i\u-10187?\u-9135?}}}"),
			new NativeMathRtfCase(
				"over",
				"<mover><mi>x</mi><mo>¯</mo></mover>",
				@"{\macc{\maccPr{\mctrlPr\i\f0\fs21 }{\mchr \u773? }}{\me\i\u-10187?\u-9115?}}"),
			new NativeMathRtfCase(
				"under",
				"<munder><mi>x</mi><mo>_</mo></munder>",
				@"{\mbar{\mbarPr{\mctrlPr\i\f0\fs21 }}{\me\i\u-10187?\u-9115?}}"),
			new NativeMathRtfCase(
				"nary",
				"<munderover><mo>∑</mo><mi>i</mi><mi>n</mi></munderover>",
				@"{\mnary{\mnaryPr{\mctrlPr\i\f0\fs21\tomAlign129 }{\mchr\u8721 ?}{\mlimLoc undOvr}}{\msub\i\u-10187?\u-9130?}{\msup\cf0\i\f0\u-10187?\u-9125?}{\me{\maln\cf0\i\f0 }}}"),
			new NativeMathRtfCase(
				"multiscripts",
				"<mmultiscripts><mi>T</mi><mi>i</mi><mn>2</mn><mprescripts/><mi>j</mi><mn>3</mn></mmultiscripts>",
				@"{\msPre{\msPrePr{\mctrlPr\i\f0\fs21 }}{\msub\i\u-10187?\u-9129?}{\msup\cf0\i0\f0 3\i }{\me{\msSubSup{\msSubSupPr{\mctrlPr\cf0\i\f0 }}{\me\i\u-10187?\u-9145?}{\msub\i\u-10187?\u-9130?}{\msup\i0 2\i }}\i }}"),
		};
		var expectedDocument = new RichEditBox().Document;
		expectedDocument.SetMathMode(RichEditMathMode.MathOnly);
		var targetDocument = new RichEditBox().Document;
		targetDocument.SetMathMode(RichEditMathMode.MathOnly);

		foreach (var item in cases)
		{
			expectedDocument.SetMathML(WrapMath(item.Body));
			expectedDocument.GetMathML(out var expected);

			targetDocument.SetText(TextSetOptions.FormatRtf, CreateNativeMathRtf(item.RtfBody));
			targetDocument.GetMathML(out var actual);

			AssertMathEquivalent(expected, actual, item.Name);
		}
	}

	[TestMethod]
	public void When_Unexpressible_Math_Rtf_Uses_Ignorable_Fallback()
	{
		var source = new RichEditBox();
		source.Document.SetMathMode(RichEditMathMode.MathOnly);
		source.Document.SetMathML(WrapMath("<mtext>x</mtext>"));

		source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);

		StringAssert.Contains(rtf, @"\mmath");
		StringAssert.Contains(rtf, @"\mr");
		StringAssert.Contains(rtf, @"{\*\unomathml ");
		var target = new RichEditBox();
		target.Document.SetMathMode(RichEditMathMode.MathOnly);
		target.Document.SetText(TextSetOptions.FormatRtf, rtf);
		target.Document.GetMathML(out var actual);
		AssertMathEquivalent(WrapMath("<mtext>x</mtext>"), actual, "fallback");
	}

	[TestMethod]
	public void When_Unsafe_Math_Rtf_Fallback_Is_Rejected_Atomically()
	{
		var document = new RichEditBox().Document;
		document.SetMathMode(RichEditMathMode.MathOnly);
		document.SetMathML(WrapMath("<mi>x</mi>"));
		document.GetMathML(out var before);
		document.ClearUndoRedoHistory();
		var unsafeMathML = "<!DOCTYPE math [<!ENTITY x SYSTEM \"file:///ignored\">]>"
			+ "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>&x;</mi></math>";
		var fallback = Convert.ToBase64String(Encoding.UTF8.GetBytes(unsafeMathML));
		var rtf = @"{\rtf1\ansi"
			+ $@"{{\*\unomathml {fallback}}}"
			+ @"{\mmath{\*\moMathPara{\*\moMath{\mr x}}}}}";

		Assert.ThrowsExactly<ArgumentException>(() =>
			document.SetText(TextSetOptions.FormatRtf, rtf));

		document.GetMathML(out var after);
		AssertMathEquivalent(before, after, "unsafe fallback");
		Assert.IsFalse(document.CanUndo());
	}

	private static string CreateNativeMathRtf(string body)
		=> @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1033"
			+ @"{\fonttbl{\f0\fnil\fcharset0 Cambria Math;}{\f1\fnil Segoe UI Variable;}}"
			+ @"{\colortbl ;\red0\green0\blue0;}"
			+ @"{\*\generator Riched20 3.2.0000}{\*\mmathPr\mmathFont0\mdefJc3\mwrapIndent1440 }"
			+ @"\viewkind4\uc1 \pard\tx720{\mmath{\*\moMathPara{\*\moMath\i\f0\fs21"
			+ body
			+ @"}}}\par}";

	private sealed record NativeMathRtfCase(string Name, string Body, string RtfBody);
}
