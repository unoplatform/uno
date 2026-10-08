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
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{

		[TestMethod]
		public void When_Hidden_Rtf_Destination_Does_Not_Consume_Text_Budget_Or_Reenter_Body()
		{
			var fragment = RichTextRtfCodec.Read(
				$@"{{\rtf1 A{{\header {new string('x', 64 * 1024)}"
					+ @"{\object\objemb{\result leaked}}}B}",
				maxCharacters: 2);

			Assert.AreEqual("AB", fragment.Text);
		}

		[TestMethod]
		[DataRow(2 * 1024 * 1024)]
		[DataRow(8 * 1024 * 1024)]
		public void When_Default_Rtf_Policy_Imports_Multi_MiB_Text(int length)
		{
			var document = new RichEditBox().Document;

			document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1 {new string('x', length)}}}");

			Assert.AreEqual(length, document.TextLength);
		}

		[TestMethod]
		public void When_Rtf_Upr_Fallback_Does_Not_Consume_Unicode_Import_Budget()
		{
			var fragment = RichTextRtfCodec.Read(
				$@"{{\rtf1 A{{\upr{{{new string('x', 64)}}}{{\*\ud\u945?}}}}B}}",
				maxCharacters: 4);

			Assert.AreEqual("AαB", fragment.Text);
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
			package.SetBitmap(CreateBitmapReference(CreatePng(Microsoft.UI.Colors.CornflowerBlue)));
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
				package.SetBitmap(CreateBitmapReference(CreatePng(Microsoft.UI.Colors.Goldenrod)));
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	}
}
