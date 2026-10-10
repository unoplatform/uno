#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Text;
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

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_Language_Writer_Uses_Standard_Controls_And_Precise_Fallback()
		{
			var source = new RichEditBox();
			source.Document.SetText(TextSetOptions.None, "AB");
			var japanese = source.Document.GetRange(0, 1).CharacterFormat;
			japanese.LanguageTag = "ja-JP";
			japanese.TextScript = TextScript.ShiftJis;
			var privateLanguage = source.Document.GetRange(1, 2).CharacterFormat;
			privateLanguage.LanguageTag = "x-uno-private";
			privateLanguage.TextScript = TextScript.Default;

			source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);

			StringAssert.Contains(rtf, @"\langfe1041\dbch");
			StringAssert.Contains(rtf, @"{\*\unochar ");
			var target = new RichEditBox();
			target.Document.SetText(TextSetOptions.FormatRtf, rtf);
			Assert.AreEqual("ja-JP", target.Document.GetRange(0, 1).CharacterFormat.LanguageTag);
			Assert.AreEqual(TextScript.ShiftJis, target.Document.GetRange(0, 1).CharacterFormat.TextScript);
			Assert.AreEqual("x-uno-private", target.Document.GetRange(1, 2).CharacterFormat.LanguageTag);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_Marker_Writer_Preserves_Wingding_And_Unicode_Families()
		{
			AssertWrittenMarker(
				MarkerType.Bullet,
				1,
				@"{\f1\fnil\fcharset0 Segoe UI Symbol;}",
				@"{\leveltext\'01\f1 \u8226?;}");
			AssertWrittenMarker(
				MarkerType.BlackCircleWingding,
				1,
				@"{\f1\fnil\fcharset2 Wingdings;}",
				@"{\leveltext\'01\f1 l;}");
			AssertWrittenMarker(
				MarkerType.WhiteCircleWingding,
				1,
				@"{\f1\fnil\fcharset2 Wingdings;}",
				@"{\leveltext\'01\f1 n;}");
			AssertWrittenMarker(
				MarkerType.UnicodeSequence,
				0x2744,
				@"{\f1\fnil\fcharset0 Segoe UI Symbol;}",
				@"{\leveltext\'01\f1 \u10052?;}");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Native_Legacy_Pn_Markers_Are_Imported()
		{
			AssertMarkerType(
				@"{\rtf1{\pntext\u10122?\tab}{\*\pn\pnlvlbody\pnstart1\pnbcnum }item}",
				MarkerType.BlackCircleWingding,
				expectedStart: 1);
			AssertMarkerType(
				@"{\rtf1{\pntext\u10112?\tab}{\*\pn\pnlvlbody\pnstart1\pnwcnum }item}",
				MarkerType.WhiteCircleWingding,
				expectedStart: 1);
			AssertMarkerType(
				@"{\rtf1{\pntext\u10052?\tab}{\*\pn\pnlvlbody\pnstart10052\pnseq }item}",
				MarkerType.UnicodeSequence,
				expectedStart: 0x2744);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Standard_Metadata_Destinations_Are_Bounded_And_Hidden()
		{
			var document = new RichEditBox().Document;

			document.SetText(
				TextSetOptions.FormatRtf,
				@"{\rtf1 before"
				+ @"{\fontemb font}{\fontfile file}{\filetbl{\file{\fname name}file}}"
				+ @"{\userprops{\propname property}{\staticval value}}{\*\generator generator}"
				+ @"{\xmlopen xml}{\formfield{\ffname field}}"
				+ @"after}");

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("beforeafter", text);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Unsupported_Rtf_Lcid_Uses_Safe_Empty_Fallback()
		{
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, @"{\rtf1\ansi\lang70000\hich A}");

			var format = document.GetRange(0, 1).CharacterFormat;
			Assert.AreEqual(string.Empty, format.LanguageTag);
			Assert.AreEqual(TextScript.Default, format.TextScript);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_Import_Budget_Is_Exceeded_Import_Is_Atomic()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "original");
			document.ClearUndoRedoHistory();

			Assert.ThrowsExactly<ArgumentException>(() =>
				document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1 {new string('x', 8 * 1024 * 1024 + 1)}}}"));

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("original", text);
			Assert.IsFalse(document.CanUndo());
		}

		[TestMethod]
		public void When_Plain_Stream_Import_Is_Not_Limited_By_Rtf_Budget()
		{
			var expected = new string('x', 8 * 1024 * 1024 + 1);
			using var backing = new MemoryStream(Encoding.Unicode.GetBytes(expected));
			using var stream = backing.AsRandomAccessStream();
			var document = new RichEditBox().Document;

			document.LoadFromStream(TextSetOptions.None, stream);

			GetTextWithoutFinalEop(document, out var actual);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Stream_Write_Throws_NonIo_Exception_Content_Is_Rolled_Back()
		{
			AssertRollbackPreservesOriginal(new InvalidOperationException("write failed"));
			AssertRollbackPreservesOriginal(new ObjectDisposedException("stream"));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Any_Nonfatal_PostMutation_Stream_Step_Fails_Content_Is_Rolled_Back()
		{
			AssertPostMutationRollback(PostMutationFailureStage.Write);
			AssertPostMutationRollback(PostMutationFailureStage.Flush);
			AssertPostMutationRollback(PostMutationFailureStage.SetLength);
			AssertPostMutationRollback(PostMutationFailureStage.Position);
		}

		private static void AssertWrittenMarker(
			MarkerType type,
			int start,
			string expectedFirst,
			string expectedSecond)
		{
			var source = new RichEditBox();
			source.Document.SetText(TextSetOptions.None, "item");
			var format = source.Document.GetRange(0, 4).ParagraphFormat;
			format.ListType = type;
			format.ListStyle = MarkerStyle.Plain;
			format.ListLevelIndex = 0;
			format.ListStart = start;
			source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
			StringAssert.Contains(rtf, expectedFirst);
			StringAssert.Contains(rtf, expectedSecond);

			var target = new RichEditBox();
			target.Document.SetText(TextSetOptions.FormatRtf, rtf);
			var imported = target.Document.GetRange(0, 4).ParagraphFormat;
			Assert.AreEqual(type, imported.ListType);
			Assert.AreEqual(start, imported.ListStart);
		}

		private static void AssertRollbackPreservesOriginal(Exception expected)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "replacement");
			var backing = new RollbackFaultStream(Encoding.ASCII.GetBytes("keep"), expected);
			using var stream = backing.AsRandomAccessStream();

			Exception? actual = null;
			try
			{
				document.SaveToStream(TextGetOptions.None, stream);
			}
			catch (Exception error)
			{
				actual = error;
			}

			Assert.IsNotNull(actual);
			Assert.AreEqual(expected.GetType(), actual.GetType());
			Assert.AreEqual(expected.Message, actual.Message);
			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
		}

		private static void AssertPostMutationRollback(PostMutationFailureStage stage)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "replacement");
			var expected = new InvalidOperationException($"{stage} failed");
			var backing = new PostMutationFaultStream(Encoding.ASCII.GetBytes("keep"), stage, expected)
			{
				Position = 2,
			};
			using var stream = backing.AsRandomAccessStream();

			var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
				document.SaveToStream(TextGetOptions.None, stream));

			Assert.AreEqual(expected.Message, actual.Message);
			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
			Assert.AreEqual(2, backing.Position);
		}

		private sealed class DelayedClipboardProvider
		{
			private readonly TaskCompletionSource<bool> _requested = new(TaskCreationOptions.RunContinuationsAsynchronously);
			private DataProviderRequest? _request;
			private DataProviderDeferral? _deferral;

			internal DelayedClipboardProvider(string format)
			{
				Package = new DataPackage();
				Package.SetDataProvider(format, request =>
				{
					_request = request;
					_deferral = request.GetDeferral();
					_requested.TrySetResult(true);
				});
			}

			internal DataPackage Package { get; }

			internal bool WasRequested => _requested.Task.IsCompleted;

			internal Task WaitUntilRequested() => _requested.Task;

			internal void Complete(object value)
			{
				_request!.SetData(value);
				_deferral!.Complete();
			}
		}

		private sealed class RollbackFaultStream : Stream
		{
			private readonly MemoryStream _inner = new();
			private readonly Exception _writeFailure;
			private readonly Exception? _rollbackFailure;
			private int _writeAttempt;

			internal RollbackFaultStream(byte[] original, Exception writeFailure, Exception? rollbackFailure = null)
			{
				_inner.Write(original, 0, original.Length);
				_inner.Position = 0;
				_writeFailure = writeFailure;
				_rollbackFailure = rollbackFailure;
			}

			public override bool CanRead => true;
			public override bool CanSeek => true;
			public override bool CanWrite => true;
			public override long Length => _inner.Length;
			public override long Position
			{
				get => _inner.Position;
				set => _inner.Position = value;
			}

			internal byte[] ToArray() => _inner.ToArray();

			public override void Flush() => _inner.Flush();
			public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
			public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
			public override void SetLength(long value) => _inner.SetLength(value);

			public override void Write(byte[] buffer, int offset, int count)
			{
				_writeAttempt++;
				if (_writeAttempt == 1)
				{
					var partial = Math.Min(2, count);
					_inner.Write(buffer, offset, partial);
					throw _writeFailure;
				}
				if (_writeAttempt == 2 && _rollbackFailure is not null)
				{
					throw _rollbackFailure;
				}
				_inner.Write(buffer, offset, count);
			}
		}

		private enum PostMutationFailureStage
		{
			Write,
			Flush,
			SetLength,
			Position,
		}

		private sealed class PostMutationFaultStream : Stream
		{
			private readonly MemoryStream _inner = new();
			private readonly PostMutationFailureStage _stage;
			private readonly Exception _failure;
			private bool _mutationStarted;
			private bool _failed;

			internal PostMutationFaultStream(
				byte[] original,
				PostMutationFailureStage stage,
				Exception failure)
			{
				_inner.Write(original, 0, original.Length);
				_inner.Position = 0;
				_stage = stage;
				_failure = failure;
			}

			public override bool CanRead => true;
			public override bool CanSeek => true;
			public override bool CanWrite => true;
			public override long Length => _inner.Length;
			public override long Position
			{
				get => _inner.Position;
				set
				{
					if (_stage == PostMutationFailureStage.Position
						&& _mutationStarted
						&& !_failed
						&& value == 2)
					{
						_failed = true;
						throw _failure;
					}
					_inner.Position = value;
				}
			}

			internal byte[] ToArray() => _inner.ToArray();

			public override void Flush()
			{
				if (_stage == PostMutationFailureStage.Flush && _mutationStarted && !_failed)
				{
					_failed = true;
					throw _failure;
				}
				_inner.Flush();
			}

			public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
			public override long Seek(long offset, SeekOrigin origin)
			{
				if (origin == SeekOrigin.Begin)
				{
					Position = offset;
					return Position;
				}
				return _inner.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				if (_stage == PostMutationFailureStage.SetLength && _mutationStarted && !_failed)
				{
					_failed = true;
					throw _failure;
				}
				_inner.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_mutationStarted = true;
				if (_stage == PostMutationFailureStage.Write && !_failed)
				{
					_failed = true;
					var partial = Math.Min(2, count);
					_inner.Write(buffer, offset, partial);
					throw _failure;
				}
				_inner.Write(buffer, offset, count);
			}
		}
	}
}
