using System;
using System.IO;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public void When_Rtf_Stream_Binary_Payload_Contains_Syntax_Bytes()
		{
			var stream = CreateRtfStream(
				Encoding.ASCII.GetBytes(@"{\rtf1\ansi before\bin5 "),
				new byte[] { (byte)'{', (byte)'\\', (byte)'}', (byte)'x', (byte)'y' },
				Encoding.ASCII.GetBytes(" after}"));
			var SUT = new RichEditBox();

			SUT.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("before after", text);
		}

		[TestMethod]
		[DataRow(@"{\rtf1\ansi before\bin after}")]
		[DataRow(@"{\rtf1\ansi before\bin-1 x}")]
		[DataRow(@"{\rtf1\ansi before\bin-0 x}")]
		[DataRow(@"{\rtf1\ansi before\bin5 ab}")]
		[DataRow(@"{\rtf1\ansi before\bin999999999999999999 x}")]
		public void When_Rtf_Binary_Length_Is_Malformed_Import_Fails_Atomically(string rtf)
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "original");

			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetText(TextSetOptions.FormatRtf, rtf));

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("original", text);
		}

		[TestMethod]
		public void When_Rtf_Root_Framing_Is_Strict()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "original");

			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1 first}{\rtf1 second}"));
			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1 text}junk"));

			GetTextWithoutFinalEop(SUT.Document, out var unchanged);
			Assert.AreEqual("original", unchanged);

			SUT.Document.SetText(TextSetOptions.FormatRtf, " \t\r\n{\\rtf1 valid}\0 \r\n");
			GetTextWithoutFinalEop(SUT.Document, out var valid);
			Assert.AreEqual("valid", valid);
		}

		[TestMethod]
		public void When_Rtf_Stream_Uses_Windows1251_Escaped_And_Raw_Bytes()
		{
			var stream = CreateRtfStream(
				Encoding.ASCII.GetBytes(@"{\rtf1\ansi\ansicpg1251 \'cf\'f0"),
				new byte[] { 0xe8, 0xe2, 0xe5, 0xf2 },
				Encoding.ASCII.GetBytes("}"));
			var SUT = new RichEditBox();

			SUT.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("Привет", text);
		}

		[TestMethod]
		public void When_Rtf_Stream_Uses_ShiftJis_DoubleByte_Sequences()
		{
			var stream = CreateRtfStream(
				Encoding.ASCII.GetBytes(@"{\rtf1\ansi\ansicpg932 \'82\'a0"),
				new byte[] { 0x82, 0xa2 },
				Encoding.ASCII.GetBytes("}"));
			var SUT = new RichEditBox();

			SUT.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("あい", text);
		}

		[TestMethod]
		public void When_Rtf_Font_Charsets_And_Explicit_CodePage_Are_Scoped()
		{
			const string rtf = @"{\rtf1\ansi\ansicpg1252\deff2"
				+ @"{\fonttbl"
				+ @"{\f0\fnil\fcharset204 Arial;}"
				+ @"{\f1\fnil\fcharset128 MS Gothic;}"
				+ @"{\f2\fnil\fcharset0 Segoe UI;}}"
				+ @"\f0\'cf{\f1\'82\'a0}\'f0\f2\cpg1251\'e8}";
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("Пあри", text);
			Assert.AreEqual("Arial", SUT.Document.GetRange(0, 1).CharacterFormat.Name);
			Assert.AreEqual("MS Gothic", SUT.Document.GetRange(1, 2).CharacterFormat.Name);
			Assert.AreEqual("Arial", SUT.Document.GetRange(2, 3).CharacterFormat.Name);
			Assert.AreEqual("Segoe UI", SUT.Document.GetRange(3, 4).CharacterFormat.Name);
		}

		[TestMethod]
		public void When_Rtf_Unicode_Fallback_Skips_ShiftJis_Bytes()
		{
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1\ansi\ansicpg932\uc2\u12354\'82\'a0X}");

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("あX", text);
		}

		[TestMethod]
		public void When_Rtf_Range_Stream_Failure_Is_Atomic()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var range = SUT.Document.GetRange(2, 4);
			var stream = CreateRtfStream(Encoding.ASCII.GetBytes(@"{\rtf1\ansi broken\bin8 xy}"));

			Assert.ThrowsExactly<ArgumentException>(() => range.SetTextViaStream(TextSetOptions.FormatRtf, stream));

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("abcdef", text);
			Assert.AreEqual(2, range.StartPosition);
			Assert.AreEqual(4, range.EndPosition);
		}

		[TestMethod]
		public void When_Rtf_Incomplete_DoubleByte_Character_Fails_Atomically()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "original");
			var stream = CreateRtfStream(
				Encoding.ASCII.GetBytes(@"{\rtf1\ansi\ansicpg932 "),
				new byte[] { 0x82 },
				Encoding.ASCII.GetBytes("}"));

			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.LoadFromStream(TextSetOptions.FormatRtf, stream));

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("original", text);
		}

		private static InMemoryRandomAccessStream CreateRtfStream(params byte[][] segments)
		{
			var stream = new InMemoryRandomAccessStream();
			var writer = stream.AsStreamForWrite();
			foreach (var segment in segments)
			{
				writer.Write(segment, 0, segment.Length);
			}
			writer.Flush();
			stream.Seek(0);
			return stream;
		}
	}
}
