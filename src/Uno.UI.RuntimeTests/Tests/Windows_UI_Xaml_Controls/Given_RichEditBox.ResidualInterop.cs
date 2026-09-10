#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_NonBody_Destinations_Are_Not_Editable_Text()
		{
			var document = new RichEditBox().Document;

			document.SetText(
				TextSetOptions.FormatRtf,
				@"{\rtf1 before"
				+ @"{\header header{\object\objemb{\result leaked-object}}"
				+ @"{\field{\*\fldinst HYPERLINK ""https://example.com""}{\fldrslt leaked-field}}}"
				+ @"{\footer footer}{\footnote footnote}{\annotation annotation}"
				+ @"{\info{\title title}{\author author}}"
				+ @"after}");

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("beforeafter", text);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_Upr_Prefers_Unicode_Destination()
		{
			var document = new RichEditBox().Document;

			document.SetText(
				TextSetOptions.FormatRtf,
				@"{\rtf1 A{\upr{fallback}{\*\ud\u945?}}B}");

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("AαB", text);
		}

		[TestMethod]
		public void When_Rtf_Import_Exceeds_Legacy_262K_Ceiling()
		{
			const int length = 300_000;
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1 {new string('x', length)}}}");

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual(length, text.Length);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Standard_Rtf_Language_And_Script_Controls_Are_Imported()
		{
			var document = new RichEditBox().Document;

			document.SetText(
				TextSetOptions.FormatRtf,
				@"{\rtf1\ansi\lang1033\loch A\lang1032\hich B\langfe1041\dbch C\lang1025\rtlch D}");

			Assert.AreEqual("en-US", document.GetRange(0, 1).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.Ansi, document.GetRange(0, 1).CharacterFormat.TextScript);
			Assert.AreEqual("el-GR", document.GetRange(1, 2).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.Greek, document.GetRange(1, 2).CharacterFormat.TextScript);
			Assert.AreEqual("ja-JP", document.GetRange(2, 3).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.ShiftJis, document.GetRange(2, 3).CharacterFormat.TextScript);
			Assert.AreEqual("ar-SA", document.GetRange(3, 4).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.Arabic, document.GetRange(3, 4).CharacterFormat.TextScript);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Default_Rtf_Languages_Are_Applied_After_Plain_Reset()
		{
			var document = new RichEditBox().Document;

			document.SetText(
				TextSetOptions.FormatRtf,
				@"{\rtf1\ansi\deflang1033\deflangfe1041 A\lang1032\hich B\plain C\langfe1041\dbch D}");

			Assert.AreEqual("en-US", document.GetRange(0, 1).CharacterFormat.LanguageTag);
			Assert.AreEqual("el-GR", document.GetRange(1, 2).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.Greek, document.GetRange(1, 2).CharacterFormat.TextScript);
			Assert.AreEqual("en-US", document.GetRange(2, 3).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.Default, document.GetRange(2, 3).CharacterFormat.TextScript);
			Assert.AreEqual("ja-JP", document.GetRange(3, 4).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.ShiftJis, document.GetRange(3, 4).CharacterFormat.TextScript);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Standard_LevelText_Preserves_Wingding_And_Unicode_Markers()
		{
			AssertMarkerType(
				CreateStandardMarkerRtf("Wingdings", "l"),
				MarkerType.BlackCircleWingding,
				expectedStart: 1);
			AssertMarkerType(
				CreateStandardMarkerRtf("Wingdings", "n"),
				MarkerType.WhiteCircleWingding,
				expectedStart: 1);
			AssertMarkerType(
				CreateStandardMarkerRtf("Segoe UI Symbol", @"\u10052?"),
				MarkerType.UnicodeSequence,
				expectedStart: 0x2744);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Native_Marker_Rtf_Uses_Standard_Glyph_Data()
		{
			AssertNativeMarkerRtf(MarkerType.BlackCircleWingding, 1, @"\pnbcnum", @"\u10122?");
			AssertNativeMarkerRtf(MarkerType.WhiteCircleWingding, 1, @"\pnwcnum", @"\u10112?");
			AssertNativeMarkerRtf(MarkerType.UnicodeSequence, 0x2744, @"\pnseq", @"\u10052?");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Tom_Clipboard_Format_Ids_Query_Exact_Representations()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				var range = editor.Document.GetRange(0, 0);
				var rtfFormat = unchecked((int)RegisterClipboardFormat("Rich Text Format"));
				var rtfWithoutObjectsFormat = unchecked((int)RegisterClipboardFormat("Rich Text Format Without Objects"));
				Console.WriteLine(
					$"NATIVE_CLIPBOARD_FORMAT_IDS rtf={rtfFormat}; rtfWithoutObjects={rtfWithoutObjectsFormat}");

				var text = new DataPackage();
				text.SetText("text");
				Clipboard.SetContent(text);
				Clipboard.Flush();
				await WindowHelper.WaitFor(() => range.CanPaste(0));
				Assert.IsTrue(range.CanPaste(0));
				Assert.IsTrue(range.CanPaste(1));
				Assert.IsFalse(range.CanPaste(7));
				Assert.IsTrue(range.CanPaste(13));
				Assert.IsFalse(range.CanPaste(2));
				Assert.IsFalse(range.CanPaste(8));
				Assert.IsFalse(range.CanPaste(17));
				Assert.IsFalse(range.CanPaste(rtfFormat));
				Assert.IsFalse(range.CanPaste(0x7fff));

				var rtf = new DataPackage();
				rtf.SetRtf(@"{\rtf1\b rich}");
				Clipboard.SetContent(rtf);
				Clipboard.Flush();
				await WindowHelper.WaitFor(() => range.CanPaste(0));
				Assert.IsTrue(range.CanPaste(0));
				Assert.IsTrue(range.CanPaste(rtfFormat));
				Assert.IsFalse(range.CanPaste(13));
				Assert.IsFalse(range.CanPaste(2));

				using var bitmapStream = await CreateNativeClipboardBitmapStream();
				var bitmap = new DataPackage();
				bitmap.SetBitmap(RandomAccessStreamReference.CreateFromStream(bitmapStream));
				Clipboard.SetContent(bitmap);
				Clipboard.Flush();
				await WindowHelper.WaitFor(() => range.CanPaste(0));
				var bitmapAvailability = new[]
				{
					range.CanPaste(2),
					range.CanPaste(8),
					range.CanPaste(17),
				};
				Assert.IsFalse(bitmapAvailability[0]);
				Assert.IsTrue(bitmapAvailability[1]);
				Assert.IsTrue(bitmapAvailability[2]);
				foreach (var bitmapFormat in new[] { 8, 17 })
				{
					editor.Document.SetText(TextSetOptions.None, string.Empty);
					editor.Document.Selection.SetRange(0, 0);
					editor.Document.Selection.Paste(bitmapFormat);
					await WindowHelper.WaitFor(() =>
					{
						GetTextWithoutFinalEop(editor.Document, out var value);
						return value == "\ufffc";
					});
				}

				foreach (var unsupportedFormat in new[] { 2, 7, 0x7fff })
				{
					editor.Document.SetText(TextSetOptions.None, "keep");
					editor.Document.Selection.SetRange(0, 4);
					Exception? pasteError = null;
					try
					{
						editor.Document.Selection.Paste(unsupportedFormat);
					}
					catch (Exception error)
					{
						pasteError = error;
					}
					await WindowHelper.WaitForIdle();
					GetTextWithoutFinalEop(editor.Document, out var afterUnsupportedPaste);
					Assert.IsNull(pasteError);
					Assert.AreEqual("keep", afterUnsupportedPaste);
				}

				var rtfWithoutObjects = new DataPackage();
				rtfWithoutObjects.SetData("Rich Text Format Without Objects", @"{\rtf1\i no-objects}");
				Clipboard.SetContent(rtfWithoutObjects);
				Clipboard.Flush();
				await WindowHelper.WaitFor(() => range.CanPaste(0));
				Assert.IsTrue(range.CanPaste(rtfWithoutObjectsFormat));
				Assert.IsFalse(range.CanPaste(13));
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Tom_Paste_Format_Selects_Text_Or_Rtf_Exactly()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				var package = new DataPackage();
				package.SetText("plain");
				package.SetRtf(@"{\rtf1\b rich}");
				Clipboard.SetContent(package);
				Clipboard.Flush();
				await WindowHelper.WaitFor(() => editor.Document.Selection.CanPaste(0));
				var rtfFormat = unchecked((int)RegisterClipboardFormat("Rich Text Format"));

				foreach (var textFormat in new[] { 1, 13 })
				{
					editor.Document.SetText(TextSetOptions.None, string.Empty);
					editor.Document.Selection.SetRange(0, 0);
					editor.Document.Selection.Paste(textFormat);
					await WindowHelper.WaitFor(() =>
					{
						GetTextWithoutFinalEop(editor.Document, out var value);
						return value == "plain";
					});
					Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(0, 5).CharacterFormat.Bold);
				}

				editor.Document.SetText(TextSetOptions.None, string.Empty);
				editor.Document.Selection.SetRange(0, 0);
				editor.Document.Selection.Paste(rtfFormat);
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var value);
					return value == "rich";
				});
				Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, 4).CharacterFormat.Bold);

				var rtfWithoutObjectsFormat = unchecked((int)RegisterClipboardFormat("Rich Text Format Without Objects"));
				var exactPackage = new DataPackage();
				exactPackage.SetText("plain");
				exactPackage.SetRtf(@"{\rtf1\b with-objects}");
				exactPackage.SetData("Rich Text Format Without Objects", @"{\rtf1\i no-objects}");
				Clipboard.SetContent(exactPackage);
				Clipboard.Flush();
				await WindowHelper.WaitFor(() => editor.Document.Selection.CanPaste(rtfWithoutObjectsFormat));
				editor.Document.SetText(TextSetOptions.None, string.Empty);
				editor.Document.Selection.SetRange(0, 0);
				editor.Document.Selection.Paste(0);
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var value);
					return value is "with-objects" or "no-objects" or "plain";
				});
				GetTextWithoutFinalEop(editor.Document, out var bestText);
				Assert.AreEqual("with-objects", bestText);
				Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, bestText.Length).CharacterFormat.Bold);
				Console.WriteLine(
					$"NATIVE_BEST_PASTE text={bestText}; bold={editor.Document.GetRange(0, bestText.Length).CharacterFormat.Bold}; italic={editor.Document.GetRange(0, bestText.Length).CharacterFormat.Italic}");

				editor.Document.SetText(TextSetOptions.None, string.Empty);
				editor.Document.Selection.SetRange(0, 0);
				Exception? rtfWithoutObjectsPasteError = null;
				try
				{
					editor.Document.Selection.Paste(rtfWithoutObjectsFormat);
				}
				catch (Exception error)
				{
					rtfWithoutObjectsPasteError = error;
				}
				await WindowHelper.WaitForIdle();
				GetTextWithoutFinalEop(editor.Document, out var rtfWithoutObjectsPasteText);
				Assert.IsNull(rtfWithoutObjectsPasteError);
				Assert.AreEqual("{", rtfWithoutObjectsPasteText);
			}
			finally
			{
				try
				{
					Clipboard.Clear();
				}
				catch (COMException)
				{
				}
				WindowHelper.WindowContent = null;
			}
		}

		private static void AssertMarkerType(string rtf, MarkerType expectedType, int expectedStart)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf);
			var format = document.GetRange(0, 4).ParagraphFormat;
			Assert.AreEqual(expectedType, format.ListType);
			Assert.AreEqual(MarkerStyle.Plain, format.ListStyle);
			Assert.AreEqual(expectedStart, format.ListStart);
		}

		private static string CreateStandardMarkerRtf(string fontName, string levelText)
			=> @"{\rtf1\ansi"
				+ $@"{{\fonttbl{{\f0\fnil Segoe UI;}}{{\f1\fnil\fcharset2 {fontName};}}}}"
				+ @"{\*\listtable{\list\listtemplateid1\listhybrid"
				+ @"{\listlevel\levelnfc23\levelnfcn23\leveljc0\leveljcn0\levelfollow0\levelstartat1"
				+ $@"\levelspace0\levelindent0\f1{{\leveltext\'01{levelText};}}{{\levelnumbers;}}"
				+ @"\fi-360\li720\lin720\tx720}{\listname ;}\listid1}}"
				+ @"{\*\listoverridetable{\listoverride\listid1\listoverridecount0\ls1}}"
				+ @"\pard\plain\ls1\ilvl0 item}";

		private static void AssertNativeMarkerRtf(
			MarkerType type,
			int start,
			string expectedControl,
			string expectedGlyph)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "item");
			var format = document.GetRange(0, 4).ParagraphFormat;
			format.ListType = type;
			format.ListStyle = MarkerStyle.Plain;
			format.ListLevelIndex = 0;
			format.ListStart = start;

			document.GetText(TextGetOptions.FormatRtf, out var rtf);

			StringAssert.Contains(rtf, expectedControl);
			StringAssert.Contains(rtf, expectedGlyph);
		}

		private static async Task<IRandomAccessStream> CreateNativeClipboardBitmapStream()
		{
			const string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl8fP8AAAAASUVORK5CYII=";
			var stream = new InMemoryRandomAccessStream();
			using var writer = new DataWriter(stream.GetOutputStreamAt(0));
			writer.WriteBytes(Convert.FromBase64String(png));
			await writer.StoreAsync();
			writer.DetachStream();
			stream.Seek(0);
			return stream;
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint RegisterClipboardFormat(string format);
	}
}
