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
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
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
		public void When_Hidden_Rtf_Destination_Does_Not_Consume_Text_Budget_Or_Reenter_Body()
		{
			var previous = global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters;
			try
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = 2;
				var document = new RichEditBox().Document;

				document.SetText(
					TextSetOptions.FormatRtf,
					$@"{{\rtf1 A{{\header {new string('x', 64 * 1024)}"
						+ @"{\object\objemb{\result leaked}}}B}");

				GetTextWithoutFinalEop(document, out var text);
				Assert.AreEqual("AB", text);
			}
			finally
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = previous;
			}
		}

		[TestMethod]
		public void When_Unsupported_Rtf_Lcid_Uses_Safe_Empty_Fallback()
		{
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, @"{\rtf1\ansi\lang70000\hich A}");

			var format = document.GetRange(0, 1).CharacterFormat;
			Assert.AreEqual(string.Empty, format.LanguageTag);
			Assert.AreEqual(TextScript.Default, format.TextScript);
		}

		[TestMethod]
		public void When_Default_Rtf_Policy_Imports_Multi_MiB_Text()
		{
			const int length = 2 * 1024 * 1024;
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1 {new string('x', length)}}}");

			Assert.AreEqual(length, document.TextLength);
		}

		[TestMethod]
		public void When_Rtf_Upr_Fallback_Does_Not_Consume_Unicode_Import_Budget()
		{
			var previous = global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters;
			try
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = 4;
				var document = new RichEditBox().Document;

				document.SetText(
					TextSetOptions.FormatRtf,
					$@"{{\rtf1 A{{\upr{{{new string('x', 64)}}}{{\*\ud\u945?}}}}B}}");

				GetTextWithoutFinalEop(document, out var text);
				Assert.AreEqual("AαB", text);
			}
			finally
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = previous;
			}
		}

		[TestMethod]
		public void When_Configured_Rtf_Policy_Is_Exceeded_Import_Is_Atomic()
		{
			var previous = global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters;
			try
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = 300_000;
				var document = new RichEditBox().Document;
				document.SetText(TextSetOptions.None, "original");
				document.ClearUndoRedoHistory();

				Assert.ThrowsExactly<ArgumentException>(() =>
					document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1 {new string('x', 300_001)}}}"));

				GetTextWithoutFinalEop(document, out var text);
				Assert.AreEqual("original", text);
				Assert.IsFalse(document.CanUndo());
			}
			finally
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = previous;
			}
		}

		[TestMethod]
		public void When_Rtf_Policy_Is_Lowered_Plain_Stream_Import_Is_Unchanged()
		{
			var previous = global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters;
			try
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = 300_000;
				var expected = new string('x', 300_001);
				using var backing = new MemoryStream(Encoding.Unicode.GetBytes(expected));
				using var stream = backing.AsRandomAccessStream();
				var document = new RichEditBox().Document;

				document.LoadFromStream(TextSetOptions.None, stream);

				GetTextWithoutFinalEop(document, out var actual);
				Assert.AreEqual(expected.Length, actual.Length);
			}
			finally
			{
				global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters = previous;
			}
		}

		[TestMethod]
		public async Task When_Clipboard_Rtf_Provider_Fails_Text_Is_Used()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new COMException("RTF unavailable")));
			package.SetText("text");
			var document = new RichEditBox().Document;
			var range = document.GetRange(0, 0);

			var result = await document.ReadClipboardContentAsync(package.GetView(), range);

			Assert.IsNull(result.Fragment);
			Assert.AreEqual("text", result.Text);
		}

		[TestMethod]
		public async Task When_Clipboard_Rtf_Fails_RtfWithoutObjects_Is_Used_Before_Text()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new IOException("RTF unavailable")));
			package.SetData("Rich Text Format Without Objects", @"{\rtf1\i no-objects}");
			package.SetText("plain");
			var document = new RichEditBox().Document;
			var range = document.GetRange(0, 0);

			var result = await document.ReadClipboardContentAsync(package.GetView(), range);

			Assert.IsNotNull(result.Fragment);
			Assert.AreEqual("no-objects", result.Fragment.Text);
			Assert.IsTrue(result.Fragment.CharacterRuns[0].Format.Italic);
			Assert.IsNull(result.Text);
		}

		[TestMethod]
		public async Task When_Advertised_Rtf_And_Text_Fail_Bitmap_Is_Attempted()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new InvalidDataException("RTF unavailable")));
			package.SetDataProvider(
				StandardDataFormats.Text,
				(DataProviderHandler)(_ => throw new IOException("Text unavailable")));
			package.SetBitmap(CreateBitmapReference(CreatePng(SKColors.CornflowerBlue)));
			var document = new RichEditBox().Document;
			var range = document.GetRange(0, 0);

			var result = await document.ReadClipboardContentAsync(package.GetView(), range);

			Assert.IsNotNull(result.Fragment);
			Assert.IsTrue(RichEditTextDocument.IsImageOnlyFragment(result.Fragment));
			Assert.IsNull(result.Text);
		}

		[TestMethod]
		public async Task When_Aggregated_Clipboard_Representation_Failures_Are_All_Recoverable_Text_Is_Used()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new AggregateException(
					new IOException("I/O unavailable"),
					new COMException("COM unavailable"))));
			package.SetText("text");
			var document = new RichEditBox().Document;

			var result = await document.ReadClipboardContentAsync(
				package.GetView(),
				document.GetRange(0, 0));

			Assert.IsNull(result.Fragment);
			Assert.AreEqual("text", result.Text);
		}

		[TestMethod]
		public async Task When_Clipboard_Security_Failure_Is_Recoverable_Text_Is_Used()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new SecurityException("RTF access denied")));
			package.SetText("text");
			var document = new RichEditBox().Document;

			var result = await document.ReadClipboardContentAsync(
				package.GetView(),
				document.GetRange(0, 0));

			Assert.IsNull(result.Fragment);
			Assert.AreEqual("text", result.Text);
		}

		[TestMethod]
		public async Task When_Clipboard_Format_Is_Specific_No_Other_Representation_Is_Used()
		{
			var package = new DataPackage();
			package.SetRtf(@"{\rtf1\b rich}");
			package.SetData("Rich Text Format Without Objects", @"{\rtf1\i no-objects}");
			package.SetText("plain");
			var document = new RichEditBox().Document;
			var range = document.GetRange(0, 0);

			var text = await document.ReadClipboardContentAsync(
				package.GetView(),
				range,
				TomClipboardFormat.UnicodeText);
			var oemText = await document.ReadClipboardContentAsync(
				package.GetView(),
				range,
				TomClipboardFormat.OemText);
			var rtf = await document.ReadClipboardContentAsync(
				package.GetView(),
				range,
				TomClipboardFormat.Rtf);
			var rtfWithoutObjects = await document.ReadClipboardContentAsync(
				package.GetView(),
				range,
				TomClipboardFormat.RtfWithoutObjects);

			Assert.AreEqual("plain", text.Text);
			Assert.IsNull(text.Fragment);
			Assert.IsNull(oemText.Fragment);
			Assert.IsNull(oemText.Text);
			Assert.IsNotNull(rtf.Fragment);
			Assert.AreEqual("rich", rtf.Fragment.Text);
			Assert.IsNull(rtf.Text);
			Assert.IsNotNull(rtfWithoutObjects.Fragment);
			Assert.AreEqual("no-objects", rtfWithoutObjects.Fragment.Text);
			Assert.IsTrue(rtfWithoutObjects.Fragment.CharacterRuns[0].Format.Italic);
			Assert.IsNull(rtfWithoutObjects.Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Tom_Paste_Format_Uses_Exact_DataPackage_Representation()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				var package = new DataPackage();
				package.SetRtf(@"{\rtf1\b rich}");
				package.SetData("Rich Text Format Without Objects", @"{\rtf1\i no-objects}");
				package.SetText("plain");
				var view = package.GetView();
				Assert.IsTrue(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.Best));
				Assert.IsTrue(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.UnicodeText));
				Assert.IsFalse(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.OemText));
				Assert.IsTrue(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.Rtf));
				Assert.IsTrue(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.RtfWithoutObjects));
				Assert.IsFalse(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.Bitmap));

				var textRange = (UnoTextRange)editor.Document.GetRange(0, 0);
				editor.Document.BeginPasteFromClipboard(
					view,
					textRange,
					_ => { },
					requireEditable: false,
					TomClipboardFormat.UnicodeText);
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var value);
					return value == "plain";
				});

				editor.Document.SetText(TextSetOptions.None, string.Empty);
				var rtfRange = (UnoTextRange)editor.Document.GetRange(0, 0);
				editor.Document.BeginPasteFromClipboard(
					view,
					rtfRange,
					_ => { },
					requireEditable: false,
					TomClipboardFormat.Rtf);
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var value);
					return value == "rich";
				});
				Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, 4).CharacterFormat.Bold);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Tom_Unsupported_Paste_Formats_Are_NoOp()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				var package = new DataPackage();
				package.SetText("text");
				package.SetBitmap(CreateBitmapReference(CreatePng(SKColors.Goldenrod)));
				var view = package.GetView();

				Assert.IsFalse(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.Bitmap));
				Assert.IsFalse(TomClipboardFormat.IsAvailable(view, TomClipboardFormat.OemText));
				Assert.IsFalse(TomClipboardFormat.IsAvailable(view, 0x7fff));

				foreach (var format in new[] { TomClipboardFormat.Bitmap, TomClipboardFormat.OemText, 0x7fff })
				{
					editor.Document.SetText(TextSetOptions.None, "keep");
					var callbackInvoked = false;
					editor.Document.BeginPasteFromClipboard(
						view,
						(UnoTextRange)editor.Document.GetRange(0, 4),
						_ => callbackInvoked = true,
						requireEditable: false,
						format);
					await WindowHelper.WaitForIdle();

					GetTextWithoutFinalEop(editor.Document, out var text);
					Assert.AreEqual("keep", text);
					Assert.IsFalse(callbackInvoked);
				}
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Clipboard_Provider_Is_Canceled_Cancellation_Propagates()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new OperationCanceledException()));
			package.SetText("must not be used");
			var document = new RichEditBox().Document;

			await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
				await document.ReadClipboardContentAsync(package.GetView(), document.GetRange(0, 0)));
		}

		[TestMethod]
		public async Task When_Aggregated_Clipboard_Provider_Is_Canceled_Cancellation_Propagates()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new AggregateException(
					new IOException("recoverable"),
					new OperationCanceledException())));
			package.SetText("must not be used");
			var document = new RichEditBox().Document;

			await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
				await document.ReadClipboardContentAsync(package.GetView(), document.GetRange(0, 0)));
		}

		[TestMethod]
		public async Task When_Aggregated_Clipboard_Provider_Has_Fatal_Error_Fatal_Error_Propagates()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new AggregateException(
					new IOException("recoverable"),
					new BadImageFormatException("fatal"))));
			package.SetText("must not be used");
			var document = new RichEditBox().Document;

			await Assert.ThrowsExactlyAsync<BadImageFormatException>(async () =>
				await document.ReadClipboardContentAsync(package.GetView(), document.GetRange(0, 0)));
		}

		[TestMethod]
		public async Task When_Clipboard_Provider_Throws_Unexpected_Exception_It_Propagates()
		{
			var package = new DataPackage();
			package.SetDataProvider(
				StandardDataFormats.Rtf,
				(DataProviderHandler)(_ => throw new ApplicationException("fatal provider failure")));
			package.SetText("must not be used");
			var document = new RichEditBox().Document;

			await Assert.ThrowsExactlyAsync<ApplicationException>(async () =>
				await document.ReadClipboardContentAsync(package.GetView(), document.GetRange(0, 0)));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Delayed_Paste_Rebases_Live_Operation_Range()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "abcdef");
				var provider = new DelayedClipboardProvider(StandardDataFormats.Text);
				var operationRange = (UnoTextRange)editor.Document.GetRange(2, 4);
				editor.Document.BeginPasteFromClipboard(
					provider.Package.GetView(),
					operationRange,
					caret => operationRange.SetRange(caret, caret),
					requireEditable: false,
					TomClipboardFormat.Best);
				await provider.WaitUntilRequested();

				editor.Document.GetRange(0, 0).Text = "!";
				provider.Complete("X");

				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var text);
					return text == "!abXef";
				});
				Assert.AreEqual(4, operationRange.StartPosition);
				Assert.AreEqual(4, operationRange.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Paste_Begins_Provider_Retrieval_Before_Dispatch()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				var provider = new DelayedClipboardProvider(StandardDataFormats.Text);

				editor.Document.BeginPasteFromClipboard(
					provider.Package.GetView(),
					(UnoTextRange)editor.Document.GetRange(0, 0),
					_ => { },
					requireEditable: false,
					TomClipboardFormat.Best);

				Assert.IsTrue(provider.WasRequested);
				provider.Complete("X");
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var text);
					return text == "X";
				});
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Newer_Paste_Supersedes_Slower_Paste()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "abcd");
				var slow = new DelayedClipboardProvider(StandardDataFormats.Text);
				var slowRange = (UnoTextRange)editor.Document.GetRange(1, 1);
				editor.Document.BeginPasteFromClipboard(
					slow.Package.GetView(),
					slowRange,
					_ => { },
					requireEditable: false,
					TomClipboardFormat.Best);
				await slow.WaitUntilRequested();

				var fast = new DataPackage();
				fast.SetText("Y");
				var fastRange = (UnoTextRange)editor.Document.GetRange(3, 3);
				editor.Document.BeginPasteFromClipboard(
					fast.GetView(),
					fastRange,
					_ => { },
					requireEditable: false,
					TomClipboardFormat.Best);
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var text);
					return text == "abcYd";
				});

				slow.Complete("X");
				await WindowHelper.WaitForIdle();
				GetTextWithoutFinalEop(editor.Document, out var finalText);
				Assert.AreEqual("abcYd", finalText);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Older_Paste_Completes_First_It_Waits_For_Newer_Intent()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "abcd");
				var older = new DelayedClipboardProvider(StandardDataFormats.Text);
				editor.Document.BeginPasteFromClipboard(
					older.Package.GetView(),
					(UnoTextRange)editor.Document.GetRange(1, 1),
					_ => { },
					requireEditable: false,
					TomClipboardFormat.Best);
				await older.WaitUntilRequested();

				var newer = new DelayedClipboardProvider(StandardDataFormats.Text);
				editor.Document.BeginPasteFromClipboard(
					newer.Package.GetView(),
					(UnoTextRange)editor.Document.GetRange(3, 3),
					_ => { },
					requireEditable: false,
					TomClipboardFormat.Best);
				await newer.WaitUntilRequested();

				older.Complete("X");
				await WindowHelper.WaitForIdle();
				GetTextWithoutFinalEop(editor.Document, out var beforeNewerCompletes);
				Assert.AreEqual("abcd", beforeNewerCompletes);

				newer.Complete("Y");
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(editor.Document, out var text);
					return text == "abcYd";
				});
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public void When_Stream_Write_Throws_NonIo_Exception_Content_Is_Rolled_Back()
		{
			AssertRollbackPreservesOriginal(new InvalidOperationException("write failed"));
			AssertRollbackPreservesOriginal(new ObjectDisposedException("stream"));
		}

		[TestMethod]
		public void When_Stream_Rollback_Also_Fails_Diagnostics_Preserve_Both_Errors()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "replacement");
			var backing = new RollbackFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				new InvalidOperationException("write failed"),
				new ObjectDisposedException("rollback"));
			using var stream = backing.AsRandomAccessStream();

			var error = Assert.ThrowsExactly<InvalidOperationException>(() =>
				document.SaveToStream(TextGetOptions.None, stream));

			Assert.AreEqual("write failed", error.Message);
			Assert.IsInstanceOfType<ObjectDisposedException>(GetRollbackFailure(error));
		}

		[TestMethod]
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
