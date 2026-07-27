#nullable enable

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
		[DataRow(TextGetOptions.None, "41000D00E9000D00")]
		[DataRow(TextGetOptions.UseLf, "41000A00E900")]
		[DataRow(TextGetOptions.UseCrlf, "41000D000A00E900")]
		public void When_Plain_Stream_Save_Uses_Utf16Le_And_Restores_Position(
			TextGetOptions options,
			string expectedHex)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "A\r\u00E9");
			var stream = CreateContractStream(Encoding.ASCII.GetBytes("prefix"));
			stream.Seek(3);

			document.SaveToStream(options, stream);

			Assert.AreEqual(3ul, stream.Position);
			Assert.AreEqual(expectedHex, ReadContractHex(stream));
		}

		[TestMethod]
		[DataRow("utf8-bom", "TEXT")]
		[DataRow("utf16-bom", "TEXT")]
		[DataRow("utf16-no-bom", "TEXT")]
		public void When_Plain_Stream_Load_Detects_Bom_And_Defaults_To_Utf16Le(
			string encoding,
			string expected)
		{
			byte[] bytes = encoding switch
			{
				"utf8-bom" => CombineContractBytes(Encoding.UTF8.GetPreamble(), Encoding.UTF8.GetBytes(expected)),
				"utf16-bom" => CombineContractBytes(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes(expected)),
				_ => Encoding.Unicode.GetBytes(expected),
			};
			var stream = CreateContractStream(bytes);
			stream.Seek(encoding == "utf16-no-bom" ? 2ul : 0ul);
			var originalPosition = stream.Position;
			var document = new RichEditBox().Document;

			document.LoadFromStream(TextSetOptions.None, stream);

			Assert.AreEqual(originalPosition, stream.Position);
			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual(expected, text);
		}

		[TestMethod]
		public void When_Rtf_Stream_Save_Uses_Byte_Rtf_And_Restores_Position()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "text");
			var stream = CreateContractStream(Encoding.ASCII.GetBytes("prefix"));
			stream.Seek(3);

			document.SaveToStream(TextGetOptions.FormatRtf, stream);

			var bytes = ReadContractBytes(stream);
			Assert.AreEqual(3ul, stream.Position);
			Assert.AreEqual(@"{\rtf", Encoding.ASCII.GetString(bytes, 0, 5));
			Assert.IsTrue(bytes.AsSpan().EndsWith("\r\n"u8));
			Assert.AreNotEqual(0, bytes[^1]);
		}

		[TestMethod]
		public void When_Empty_Document_Rtf_Stream_Has_Native_Null_Terminator()
		{
			var document = new RichEditBox().Document;
			var stream = CreateContractStream(Encoding.ASCII.GetBytes("prefix"));
			stream.Seek(3);

			document.SaveToStream(TextGetOptions.FormatRtf, stream);

			var bytes = ReadContractBytes(stream);
			Assert.AreEqual(3ul, stream.Position);
			Assert.IsTrue(bytes.AsSpan(0, bytes.Length - 1).EndsWith("\r\n"u8));
			Assert.AreEqual(0, bytes[^1]);
		}

		[TestMethod]
		public void When_Invalid_Get_Options_Leave_Stream_Unchanged()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "text");
			var stream = CreateContractStream(Encoding.ASCII.GetBytes("keep"));
			stream.Seek(2);

			Assert.ThrowsExactly<ArgumentException>(() =>
				document.SaveToStream(TextGetOptions.UseLf | TextGetOptions.UseCrlf, stream));

			Assert.AreEqual(2ul, stream.Position);
			Assert.AreEqual("6B656570", ReadContractHex(stream));
		}

		[TestMethod]
		[DataRow(false, "abc\u202EDEF")]
		[DataRow(true, "abc")]
		public void When_Document_CheckTextLimit_Is_OptIn(bool checkLimit, string expected)
		{
			var box = new RichEditBox { MaxLength = 3 };
			var options = TextSetOptions.UnicodeBidi;
			if (checkLimit)
			{
				options |= TextSetOptions.CheckTextLimit;
			}

			box.Document.SetText(options, "abc\u202EDEF");

			GetTextWithoutFinalEop(box.Document, out var text);
			Assert.AreEqual(expected, text);
		}

		[TestMethod]
		[DataRow(TextSetOptions.None, TextScript.Default)]
		[DataRow(TextSetOptions.UnicodeBidi, TextScript.Hebrew)]
		public void When_UnicodeBidi_Projects_Strong_Rtl_Script_Formatting(
			TextSetOptions options,
			TextScript expectedScript)
		{
			var document = new RichEditBox().Document;

			document.SetText(options, "abc אבג");

			Assert.AreEqual(expectedScript, document.GetRange(4, 7).CharacterFormat.TextScript);
		}

		[TestMethod]
		public void When_Range_Stream_UnicodeBidi_Projects_Strong_Rtl_Script_Formatting()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "AB");
			var stream = CreateContractStream(Encoding.Unicode.GetBytes("אבג"));
			var range = document.GetRange(1, 1);

			range.SetTextViaStream(TextSetOptions.UnicodeBidi, stream);

			Assert.AreEqual(TextScript.Hebrew, document.GetRange(1, 4).CharacterFormat.TextScript);
		}

		[TestMethod]
		[DataRow(false, "xyabcdef", 8)]
		[DataRow(true, "xyab", 4)]
		public void When_Range_CheckTextLimit_Is_OptIn(bool checkLimit, string expected, int expectedEnd)
		{
			var box = new RichEditBox { MaxLength = 4 };
			box.Document.SetText(TextSetOptions.None, "xy");
			var range = box.Document.GetRange(2, 2);

			range.SetText(checkLimit ? TextSetOptions.CheckTextLimit : TextSetOptions.None, "abcdef");

			GetTextWithoutFinalEop(box.Document, out var text);
			Assert.AreEqual(expected, text);
			Assert.AreEqual(2, range.StartPosition);
			Assert.AreEqual(expectedEnd, range.EndPosition);
		}

		[TestMethod]
		[DataRow(false, "text\r", ParagraphAlignment.Left)]
		[DataRow(true, "text", ParagraphAlignment.Right)]
		public void When_ApplyRtfDocumentDefaults_Consumes_The_Explicit_Final_Paragraph(
			bool applyDefaults,
			string expected,
			ParagraphAlignment expectedTerminalAlignment)
		{
			const string rtf = @"{\rtf1\ansi\pard\qr text\par}";
			var options = TextSetOptions.FormatRtf;
			if (applyDefaults)
			{
				options |= TextSetOptions.ApplyRtfDocumentDefaults;
			}
			var document = new RichEditBox().Document;

			document.SetText(options, rtf);

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual(expected, text);
			Assert.AreEqual(
				expectedTerminalAlignment,
				document.GetRange(document.Selection.StoryLength - 1, document.Selection.StoryLength - 1).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public void When_ApplyRtfDocumentDefaults_Does_Not_Replace_Managed_Default_Format_Objects()
		{
			var document = new RichEditBox().Document;
			document.GetDefaultCharacterFormat().Name = "Segoe UI";
			document.GetDefaultCharacterFormat().Size = 11;
			document.GetDefaultParagraphFormat().Alignment = ParagraphAlignment.Center;
			var defaultName = document.GetDefaultCharacterFormat().Name;
			var defaultSize = document.GetDefaultCharacterFormat().Size;
			var defaultAlignment = document.GetDefaultParagraphFormat().Alignment;

			document.SetText(
				TextSetOptions.FormatRtf | TextSetOptions.ApplyRtfDocumentDefaults,
				@"{\rtf1\ansi\deff1{\fonttbl{\f0 Arial;}{\f1 Courier New;}}\fs40\pard\qr text\par}");

			Assert.AreEqual("Courier New", document.GetRange(0, 1).CharacterFormat.Name);
			Assert.AreEqual(20f, document.GetRange(0, 1).CharacterFormat.Size);
			Assert.AreEqual(ParagraphAlignment.Right, document.GetRange(0, 1).ParagraphFormat.Alignment);
			Assert.AreEqual(defaultName, document.GetDefaultCharacterFormat().Name);
			Assert.AreEqual(defaultSize, document.GetDefaultCharacterFormat().Size);
			Assert.AreEqual(defaultAlignment, document.GetDefaultParagraphFormat().Alignment);
		}

		[TestMethod]
		[DataRow(false, "Atext\rB")]
		[DataRow(true, "Atext\rB")]
		public void When_Range_ApplyRtfDocumentDefaults_Does_Not_Consume_Explicit_Final_Paragraph(
			bool applyDefaults,
			string expected)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "AB");
			var options = TextSetOptions.FormatRtf;
			if (applyDefaults)
			{
				options |= TextSetOptions.ApplyRtfDocumentDefaults;
			}

			document.GetRange(1, 1).SetText(options, @"{\rtf1\ansi\pard\qr text\par}");

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual(expected, text);
		}

		[TestMethod]
		public void When_Range_Stream_Load_Collapses_To_The_Inserted_End()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "XY");
			var stream = CreateContractStream(CombineContractBytes(
				Encoding.Unicode.GetPreamble(),
				Encoding.Unicode.GetBytes("abc")));
			stream.Seek(2);
			var range = document.GetRange(1, 1);

			range.SetTextViaStream(TextSetOptions.None, stream);

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("XabcY", text);
			Assert.AreEqual(2ul, stream.Position);
			Assert.AreEqual(4, range.StartPosition);
			Assert.AreEqual(4, range.EndPosition);
		}

		[TestMethod]
		public void When_Invalid_Rtf_Is_A_Deliberate_Security_Divergence()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "original");
			document.ClearUndoRedoHistory();

#if HAS_UNO
			Assert.ThrowsExactly<ArgumentException>(() =>
				document.SetText(TextSetOptions.FormatRtf, "invalid"));
			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("original", text);
			Assert.IsFalse(document.CanUndo());
#else
			document.SetText(TextSetOptions.FormatRtf, "invalid");
			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("invalid", text);
#endif
		}

		private static InMemoryRandomAccessStream CreateContractStream(byte[] bytes)
		{
			var stream = new InMemoryRandomAccessStream();
			var writer = stream.AsStreamForWrite();
			writer.Write(bytes, 0, bytes.Length);
			writer.Flush();
			stream.Seek(0);
			return stream;
		}

		private static byte[] ReadContractBytes(IRandomAccessStream stream)
		{
			var position = stream.Position;
			stream.Seek(0);
			using var memory = new MemoryStream();
			stream.AsStreamForRead().CopyTo(memory);
			stream.Seek(position);
			return memory.ToArray();
		}

		private static string ReadContractHex(IRandomAccessStream stream)
			=> Convert.ToHexString(ReadContractBytes(stream));

		private static byte[] CombineContractBytes(byte[] first, byte[] second)
		{
			var result = new byte[first.Length + second.Length];
			System.Buffer.BlockCopy(first, 0, result, 0, first.Length);
			System.Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
			return result;
		}
	}
}
