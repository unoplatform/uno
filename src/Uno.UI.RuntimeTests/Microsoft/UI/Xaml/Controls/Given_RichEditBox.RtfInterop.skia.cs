#nullable enable

using System;
using System.IO;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public void When_Standard_Rtf_Paragraph_Controls_RoundTrip()
		{
			const string rtf = @"{\rtf1\ansi\pard\sl-360\slmult0\tqc\tldot\tx720"
				+ @"\rtlpar\keep\keepn\pagebb\noline\widctlpar text}";
			var source = new RichEditBox();

			source.Document.SetText(TextSetOptions.FormatRtf, rtf);

			var format = source.Document.GetRange(0, 4).ParagraphFormat;
			Assert.AreEqual(LineSpacingRule.Exactly, format.LineSpacingRule);
			Assert.AreEqual(18f, format.LineSpacing);
			Assert.AreEqual(FormatEffect.On, format.RightToLeft);
			Assert.AreEqual(FormatEffect.On, format.KeepTogether);
			Assert.AreEqual(FormatEffect.On, format.KeepWithNext);
			Assert.AreEqual(FormatEffect.On, format.PageBreakBefore);
			Assert.AreEqual(FormatEffect.On, format.NoLineNumber);
			Assert.AreEqual(FormatEffect.On, format.WidowControl);
			Assert.AreEqual(1, format.TabCount);
			format.GetTab(0, out var position, out var alignment, out var leader);
			Assert.AreEqual(36f, position);
			Assert.AreEqual(TabAlignment.Center, alignment);
			Assert.AreEqual(TabLeader.Dots, leader);

			source.Document.GetText(TextGetOptions.FormatRtf, out var exported);
			StringAssert.Contains(exported, @"\sl-360\slmult0");
			StringAssert.Contains(exported, @"\tqc\tldot\tx720");
			StringAssert.Contains(exported, @"\rtlpar");
			StringAssert.Contains(exported, @"\keep");
			StringAssert.Contains(exported, @"\keepn");
			StringAssert.Contains(exported, @"\pagebb");
			StringAssert.Contains(exported, @"\noline");
			StringAssert.Contains(exported, @"\widctlpar");
		}

		[TestMethod]
		public void When_Rtf_List_Writer_Uses_Standard_Tables_And_ListText()
		{
			var source = new RichEditBox();
			source.Document.SetText(TextSetOptions.None, "one\rtwo");
			Configure(source.Document.GetRange(0, 3).ParagraphFormat);
			Configure(source.Document.GetRange(4, 7).ParagraphFormat);

			source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);

			StringAssert.Contains(rtf, @"{\*\listtable");
			StringAssert.Contains(rtf, @"{\*\listoverridetable");
			StringAssert.Contains(rtf, @"{\listtext ");
			StringAssert.Contains(rtf, @"\ls1\ilvl0");
			var target = new RichEditBox();
			target.Document.SetText(TextSetOptions.FormatRtf, rtf);
			target.Document.GetRange(0, 7).GetText(TextGetOptions.IncludeNumbering, out var numbered);
			Assert.AreEqual("3.\tone\r4.\ttwo", numbered);

			static void Configure(ITextParagraphFormat format)
			{
				format.ListType = MarkerType.Arabic;
				format.ListStyle = MarkerStyle.Period;
				format.ListAlignment = MarkerAlignment.Left;
				format.ListLevelIndex = 0;
				format.ListStart = 3;
				format.ListTab = 36;
			}
		}

		[TestMethod]
		public void When_Native_Legacy_Pn_List_Output_Is_Imported_Without_Duplicate_Marker_Text()
		{
			const string rtf = @"{\rtf1\ansi\pard"
				+ @"{\pntext 3.\tab}{\*\pn\pnlvlbody\pnstart3\pndec{\pntxta.}}first\par"
				+ @"{\pntext 4.\tab}{\*\pn\pnlvlbody\pnstart3\pndec{\pntxta.}}second\par}";
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("first\rsecond\r", text);
			Assert.AreEqual(MarkerType.Arabic, SUT.Document.GetRange(0, 5).ParagraphFormat.ListType);
			SUT.Document.GetRange(0, 12).GetText(TextGetOptions.IncludeNumbering, out var numbered);
			Assert.AreEqual("3.\tfirst\r4.\tsecond", numbered);
		}

		[TestMethod]
		public void When_Rtf_Color_Table_Auto_And_Omitted_Components_Are_Structural()
		{
			const string rtf = @"{\rtf1{\colortbl;\red255;;\green128\blue64;}"
				+ @"\cf1 R\cf2 A\cf3 G}";

			var fragment = RichTextRtfCodec.Read(rtf);

			Assert.AreEqual(Windows.UI.Color.FromArgb(255, 255, 0, 0), fragment.GetCharacterFormatAt(0).Foreground);
			Assert.IsNull(fragment.GetCharacterFormatAt(1).Foreground);
			Assert.AreEqual(Windows.UI.Color.FromArgb(255, 0, 128, 64), fragment.GetCharacterFormatAt(2).Foreground);
		}

		[TestMethod]
		public void When_Unicode_Fallback_Remaining_In_Child_Group_Does_Not_Leak()
		{
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1\ansi\uc1 A{\uc2\u945?}B}");

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("AαB", text);
		}

		[TestMethod]
		public void When_Standard_Punctuation_SoftLine_And_Table_Controls_Are_Distinct()
		{
			var SUT = new RichEditBox();

			SUT.Document.SetText(
				TextSetOptions.FormatRtf,
				@"{\rtf1\lquote a\rquote\emdash\ldblquote b\rdblquote\softline c\cell d\row e}");

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("‘a’—“b”\nc\td\re", text);
		}

		[TestMethod]
		public void When_Rtf_Picture_Uses_Binary_Payload()
		{
			using var imageStream = CreateImageStream(SKColors.Orange);
			using var imageBytes = new MemoryStream();
			imageStream.AsStreamForRead().CopyTo(imageBytes);
			var bytes = imageBytes.ToArray();
			using var rtf = CreateRtfStream(
				Encoding.ASCII.GetBytes($@"{{\rtf1 A{{\pict\pngblip\picw2\pich2\bin{bytes.Length} "),
				bytes,
				Encoding.ASCII.GetBytes("}B}"));
			var SUT = new RichEditBox();

			SUT.Document.LoadFromStream(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("A\ufffcB", text);
		}

		[TestMethod]
		public void When_Standard_Rtf_Object_Imports_Safe_Result_Text()
		{
			const string rtf = @"{\rtf1 before{\object\objemb{\*\objclass Package}"
				+ @"{\*\objdata 01020304}{\result fallback}}after}";
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("beforefallbackafter", text);
		}

		[TestMethod]
		public void When_Standard_Rtf_Object_Prefers_Result_Picture_Without_Ole_Payload()
		{
			const string bmpBase64 = "Qk06AAAAAAAAADYAAAAoAAAAAQAAAAEAAAABABgAAAAAAAAAAADEDgAAxA4AAAAAAAAAAAAA686HAA==";
			var dibHex = Convert.ToHexString(Convert.FromBase64String(bmpBase64).AsSpan(14));
			var rtf = $@"{{\rtf1 before{{\object\objemb{{\*\objclass Package}}"
				+ $@"{{\*\objdata 01020304}}{{\result fallback{{\pict\dibitmap0 {dibHex}}}}}}}after}}";
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			GetTextWithoutFinalEop(SUT.Document, TextGetOptions.UseObjectText, out var objectText);
			Assert.AreEqual("before\ufffcafter", text);
			Assert.AreEqual("beforePackageafter", objectText);

			SUT.Document.GetText(TextGetOptions.FormatRtf, out var exported);
			Assert.IsFalse(exported.Contains(@"\objdata", StringComparison.Ordinal));
			var roundTrip = new RichEditBox();
			roundTrip.Document.SetText(TextSetOptions.FormatRtf, exported);
			GetTextWithoutFinalEop(roundTrip.Document, TextGetOptions.UseObjectText, out var roundTripText);
			Assert.AreEqual("beforePackageafter", roundTripText);
		}

		[TestMethod]
		public void When_Standard_Rtf_Object_Has_No_Result_Uses_Bounded_Alternate_Fallback()
		{
			const string rtf = @"{\rtf1 before{\object\objemb\objw240\objh300"
				+ @"{\*\objclass Package}{\*\objdata 01020304}}after}";
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			GetTextWithoutFinalEop(SUT.Document, TextGetOptions.UseObjectText, out var objectText);
			Assert.AreEqual("before\ufffcafter", text);
			Assert.AreEqual("beforePackageafter", objectText);

			SUT.Document.GetText(TextGetOptions.FormatRtf, out var exported);
			StringAssert.Contains(exported, @"{\*\unoobject ");
			Assert.IsFalse(exported.Contains(@"\objdata", StringComparison.Ordinal));
			var roundTrip = new RichEditBox();
			roundTrip.Document.SetText(TextSetOptions.FormatRtf, exported);
			GetTextWithoutFinalEop(roundTrip.Document, TextGetOptions.UseObjectText, out var roundTripText);
			Assert.AreEqual("beforePackageafter", roundTripText);
		}

		[TestMethod]
		public void When_Rtf_Object_Result_Exceeds_Budget_Import_Is_Atomic()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "original");
			SUT.Document.ClearUndoRedoHistory();
			var result = new string('x', 64 * 1024 + 1);

			Assert.ThrowsExactly<ArgumentException>(() =>
				SUT.Document.SetText(
					TextSetOptions.FormatRtf,
					$@"{{\rtf1{{\object\objemb{{\*\objclass Package}}{{\result {result}}}}}}}"));

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("original", text);
			Assert.IsFalse(SUT.Document.CanUndo());
		}

		[TestMethod]
		public void When_Bmp_And_Dib_Are_Decoded_And_Rtf_Writer_Transcodes_To_Png()
		{
			const string bmpBase64 = "Qk06AAAAAAAAADYAAAAoAAAAAQAAAAEAAAABABgAAAAAAAAAAADEDgAAxA4AAAAAAAAAAAAA686HAA==";
			var bmp = Convert.FromBase64String(bmpBase64);
			var source = new RichEditBox();
			source.Document.GetRange(0, 0).InsertImage(
				1,
				1,
				1,
				VerticalCharacterAlignment.Baseline,
				"bmp",
				new MemoryStream(bmp).AsRandomAccessStream());

			source.Document.GetText(TextGetOptions.FormatRtf, out var exported);

			StringAssert.Contains(exported, @"\pngblip");
			var target = new RichEditBox();
			target.Document.SetText(TextSetOptions.FormatRtf, exported);
			GetTextWithoutFinalEop(target.Document, TextGetOptions.UseObjectText, out var objectText);
			Assert.AreEqual("bmp", objectText);

			var dibHex = Convert.ToHexString(bmp.AsSpan(14));
			var dibTarget = new RichEditBox();
			dibTarget.Document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1{{\pict\dibitmap0 {dibHex}}}}}");
			GetTextWithoutFinalEop(dibTarget.Document, out var dibText);
			Assert.AreEqual("\ufffc", dibText);
		}

		[TestMethod]
		public void When_Rtf_SetText_Unlink_And_Unhide_Are_Applied_Atomically()
		{
			const string rtf = @"{\rtf1\v{\field{\*\fldinst HYPERLINK ""custom:target""}{\fldrslt hidden}}}";
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "old");

			SUT.Document.GetRange(0, 3).SetText(
				TextSetOptions.FormatRtf | TextSetOptions.Unlink | TextSetOptions.Unhide,
				rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("hidden", text);
			Assert.AreEqual(string.Empty, SUT.Document.GetRange(0, 1).Link);
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 6).CharacterFormat.Hidden);
		}

		[TestMethod]
		public void When_Rtf_Hyperlink_Anchor_Is_Preserved_On_Write()
		{
			const string rtf = @"{\rtf1{\field{\*\fldinst HYPERLINK ""docs/page"" \l ""part-1""}{\fldrslt relative}}}";
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			SUT.Document.GetText(TextGetOptions.FormatRtf, out var exported);

			StringAssert.Contains(exported, @"HYPERLINK ""docs/page"" \l ""part-1""");
		}
	}
}
