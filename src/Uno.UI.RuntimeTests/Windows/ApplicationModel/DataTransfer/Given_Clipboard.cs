using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage.Streams;
using static Microsoft.VisualStudio.TestTools.UnitTesting.ConditionMode;
using static Microsoft.VisualStudio.TestTools.UnitTesting.RuntimeTestPlatforms;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
public partial class Given_Clipboard;
partial class Given_Clipboard // setup and cleanup
{
	// limit cross contamination, and (external pollution while running manually)
	[TestInitialize]
	public void Setup() => Clipboard.Clear();

#if !DEBUG // when running in debug, we want to still be able to inspect the clipboard content after a test failure
	[TestCleanup]
	public void Cleanup() => Clipboard.Clear();
#endif
}

partial class Given_Clipboard
{
	private const string TestString = "test-string-raw";
	private const string UriAddress = "https://platform.uno";
	private readonly byte[] TestByteArray = [3, 1, 2];
	private const string TestBmpBase64 = "Qk06AAAAAAAAADYAAAAoAAAAAQAAAAEAAAABABgAAAAAAAAAAADEDgAAxA4AAAAAAAAAAAAA686HAA==";
	private const string TestPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4AWJiZmT6DwAAAP//EKnFGgAAAAZJREFUAwABIQEIIJGZrwAAAABJRU5ErkJggg==";

	private const string OctetStreamFormat = "application/octet-stream";

	[TestMethod]
	[RunsOnUIThread]
	// On wasm the read is served from the last-write cache, so no clipboard-read permission is needed.
	[PlatformCondition(Include, SkiaWin32 | SkiaIOS | SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23962")]
	public async Task When_GetSet_Clipboard_Text()
	{
		var package = new DataPackage();
		package.SetText(TestString);

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Text));

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(StandardDataFormats.Text));

		var text = await view.GetTextAsync();

		Assert.AreEqual(TestString, text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWin32)]
	public async Task When_GetSet_Clipboard_ByteArray()
	{
		var package = new DataPackage();
		package.SetData(OctetStreamFormat, ToRAStream(TestByteArray));

		Clipboard.SetContent(package);

		var view = Clipboard.GetContent();
		var stream = await view.GetDataAsync(OctetStreamFormat) as IRandomAccessStream;
		var bytes = ToBytes(stream);

		CollectionAssert.AreEqual(TestByteArray, bytes);
	}

#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWin32 | SkiaWasm)]
	public async Task When_GetSet_Clipboard_Bitmap_With_Png()
	{
		var package = new DataPackage();
		var bytes = Convert.FromBase64String(TestPngBase64);
		package.SetBitmap(await ToRAReferenceAsync(bytes));

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Bitmap));

		var view = Clipboard.GetContent();
		var reference = await view.GetBitmapAsync();
		using var stream = await reference.OpenReadAsync();
		var results = await ToBytesAsync(stream);

#if UNO_DRAWING_SKIA
		SkiaImageAssert.ArePixelsEqual(bytes, results);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	// On wasm the image is transcoded to PNG for the browser clipboard; pixel equality still holds.
	[PlatformCondition(Include, SkiaWin32 | SkiaWasm)]
	public async Task When_GetSet_Clipboard_Bitmap_With_Bmp()
	{
		var package = new DataPackage();
		var bytes = Convert.FromBase64String(TestBmpBase64);
		package.SetBitmap(await ToRAReferenceAsync(bytes));

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Bitmap));

		var view = Clipboard.GetContent();
		var reference = await view.GetBitmapAsync();
		using var stream = await reference.OpenReadAsync();
		var results = await ToBytesAsync(stream);

#if UNO_DRAWING_SKIA
		SkiaImageAssert.ArePixelsEqual(bytes, results);
#endif
	}
#endif

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public void When_SetContent_Null()
		=> Assert.ThrowsExactly<ArgumentNullException>(() => Clipboard.SetContent(null));

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_SetContent_ContentChanged()
	{
		var raised = 0;
		EventHandler<object> onContentChanged = (_, _) => raised++;
		Clipboard.ContentChanged += onContentChanged;

		try
		{
			var package = new DataPackage();
			package.SetText(TestString);

			Clipboard.SetContent(package);

			await WaitForClipboardAsync(() => raised > 0);
			Assert.IsTrue(raised > 0);
		}
		finally
		{
			Clipboard.ContentChanged -= onContentChanged;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaAndroid)]
	public async Task When_SetContent_Then_ContentChanged_Raised_Once()
	{
		await DelayForClipboard();

		var raised = 0;
		EventHandler<object> handler = (_, _) => raised++;
		Clipboard.ContentChanged += handler;
		try
		{
			var package = new DataPackage();
			package.SetText(TestString);
			Clipboard.SetContent(package);

			Assert.AreEqual(TestString, await ClipboardHelper.WaitForTextAsync(TestString));

			// Lets Android deliver its own clipboard callback for the write.
			await DelayForClipboard();

			Assert.AreEqual(1, raised);
		}
		finally
		{
			Clipboard.ContentChanged -= handler;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaAndroid)]
	public async Task When_Clear_Then_ContentChanged_Raised_Once()
	{
		await ClipboardHelper.SeedDummyData();
		await DelayForClipboard();

		var raised = 0;
		EventHandler<object> handler = (_, _) => raised++;
		Clipboard.ContentChanged += handler;
		try
		{
			Clipboard.Clear();
			await DelayForClipboard();

			Assert.AreEqual(1, raised);
		}
		finally
		{
			Clipboard.ContentChanged -= handler;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_GetSet_Clipboard_Text_And_Html()
	{
		const string html = "<b>bold</b>";

		var package = new DataPackage();
		package.SetText(TestString);
		package.SetHtmlFormat(html);

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Html));

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(StandardDataFormats.Text));
		Assert.IsTrue(view.Contains(StandardDataFormats.Html));
		Assert.AreEqual(TestString, await view.GetTextAsync());
		Assert.AreEqual(html, await view.GetHtmlFormatAsync());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Clear_Contains_Nothing()
	{
		var package = new DataPackage();
		package.SetText(TestString);

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Text));

		Assert.IsTrue(Clipboard.GetContent().Contains(StandardDataFormats.Text));

		Clipboard.Clear();

		await WaitForClipboardAsync(() => !Clipboard.GetContent().Contains(StandardDataFormats.Text));

		var view = Clipboard.GetContent();
		Assert.IsFalse(view.Contains(StandardDataFormats.Text));
		Assert.IsFalse(view.Contains(StandardDataFormats.Html));
		Assert.IsFalse(view.Contains(StandardDataFormats.Bitmap));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_GetSet_Clipboard_CustomFormat()
	{
		const string customFormat = "application/x-uno-test";
		const string customPayload = "custom-payload";

		var package = new DataPackage();
		package.SetText(TestString);
		package.SetData(customFormat, customPayload);

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(customFormat));

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(customFormat));
		Assert.AreEqual(customPayload, await view.GetDataAsync(customFormat) as string);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_GetSet_Clipboard_WebLink()
	{
		var package = new DataPackage();
		var uri = new Uri(UriAddress);
		package.SetWebLink(uri);

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.WebLink));

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(StandardDataFormats.WebLink));
		Assert.IsTrue(view.Contains(StandardDataFormats.Text));
		Assert.AreEqual(uri, await view.GetWebLinkAsync());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[DataRow("uno-test://open/item")]
	[DataRow("httpx://open/item")]
	public async Task When_GetSet_Clipboard_ApplicationLink(string address)
	{
		var package = new DataPackage();
		var uri = new Uri(address);
		package.SetApplicationLink(uri);

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.ApplicationLink));

		// A non-web URI must come back as an application link, not be promoted to a web link.
		var view = Clipboard.GetContent();

		Assert.IsFalse(view.Contains(StandardDataFormats.WebLink));
		Assert.AreEqual(uri, await view.GetApplicationLinkAsync());
		Assert.AreEqual(uri.ToString(), await view.GetTextAsync());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Paste_Event_With_Malformed_Uri_List()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('paste-text-payload', 'text/plain');
			dt.items.add('not a uri', 'text/uri-list');
			""");

		// A malformed link from another application is dropped; the other formats stay readable.
		var view = Clipboard.GetContent();

		Assert.IsFalse(view.Contains(StandardDataFormats.WebLink));
		Assert.IsFalse(view.Contains(StandardDataFormats.ApplicationLink));
		Assert.AreEqual("paste-text-payload", await view.GetTextAsync());
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Paste_Shortcut_Precedes_Paste_Event()
	{
#if HAS_UNO
		// A paste shortcut can reach managed code before the browser delivers the paste
		// event; the clipboard must bridge the gap and serve the incoming content.
		Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
			"""
			(function() {
				document.dispatchEvent(new KeyboardEvent('keydown', { key: 'v', ctrlKey: true, bubbles: true }));
				return 'ok';
			})()
			""");

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(StandardDataFormats.StorageItems));

		var itemsTask = view.GetStorageItemsAsync().AsTask();

		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add(new File(['bridged-content'], 'bridged.txt', { type: 'text/plain' }));
			""");

		var items = await itemsTask;

		Assert.AreEqual(1, items.Count);
		Assert.AreEqual("bridged.txt", items[0].Name);
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Paste_Event_With_Files()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			$$"""
			const bytes = Uint8Array.from(atob('{{TestPngBase64}}'), c => c.charCodeAt(0));
			const dt = new DataTransfer();
			dt.items.add('paste-text-payload', 'text/plain');
			dt.items.add(new File(['file-content-1'], 'first.txt', { type: 'text/plain' }));
			dt.items.add(new File(['file-content-2'], 'second.txt', { type: 'text/plain' }));
			dt.items.add(new File([bytes], 'third.png', { type: 'image/png' }));
			""");

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(StandardDataFormats.Text));
		Assert.IsTrue(view.Contains(StandardDataFormats.StorageItems));

		// A multi-file paste is a file transfer; no Bitmap is synthesized from the image file.
		Assert.IsFalse(view.Contains(StandardDataFormats.Bitmap));

		Assert.AreEqual("paste-text-payload", await view.GetTextAsync());

		var items = await view.GetStorageItemsAsync();
		Assert.AreEqual(3, items.Count);
		Assert.AreEqual("first.txt", items[0].Name);
		Assert.AreEqual("second.txt", items[1].Name);
		Assert.AreEqual("third.png", items[2].Name);

		var file = (Windows.Storage.StorageFile)items[0];
		using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
		Assert.AreEqual("file-content-1", Encoding.UTF8.GetString(await ToBytesAsync(stream)));
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Paste_Event_With_Image()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			$$"""
			const bytes = Uint8Array.from(atob('{{TestPngBase64}}'), c => c.charCodeAt(0));
			const dt = new DataTransfer();
			dt.items.add(new File([bytes], 'image.png', { type: 'image/png' }));
			""");

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(StandardDataFormats.Bitmap));
		Assert.IsTrue(view.Contains(StandardDataFormats.StorageItems));

		var reference = await view.GetBitmapAsync();
		using var stream = await reference.OpenReadAsync();

		CollectionAssert.AreEqual(Convert.FromBase64String(TestPngBase64), await ToBytesAsync(stream));
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Paste_Event_With_Svg_File()
	{
#if HAS_UNO
		// The Bitmap format is a raster image: a single SVG file is pasted as a file only.
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add(new File(['<svg xmlns="http://www.w3.org/2000/svg"/>'], 'drawing.svg', { type: 'image/svg+xml' }));
			""");

		var view = Clipboard.GetContent();

		Assert.IsFalse(view.Contains(StandardDataFormats.Bitmap));
		Assert.IsTrue(view.Contains(StandardDataFormats.StorageItems));

		var items = await view.GetStorageItemsAsync();
		Assert.AreEqual("drawing.svg", items.Single().Name);
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_SetContent_After_Paste()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('older-pasted-content', 'text/plain');
			dt.items.add('<b>older</b>', 'text/html');
			""");

		Assert.IsTrue(Clipboard.GetContent().Contains(StandardDataFormats.Html));

		var package = new DataPackage();
		package.SetText("new-own-content");
		await SetContentAndWaitAsync(package);

		// The write replaces the captured paste as soon as it lands, however fresh the paste still is.
		var view = Clipboard.GetContent();
		Assert.IsFalse(view.Contains(StandardDataFormats.Html));
		Assert.IsTrue(view.Contains(StandardDataFormats.Text));
		Assert.AreEqual("new-own-content", await view.GetTextAsync());
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_View_Outlives_Clipboard_Change()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('older-pasted-content', 'text/plain');
			dt.items.add('<b>older</b>', 'text/html');
			""");

		var pasteView = Clipboard.GetContent();

		var package = new DataPackage();
		package.SetText("new-own-content");
		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => !Clipboard.GetContent().Contains(StandardDataFormats.Html));

		var ownView = Clipboard.GetContent();
		Clipboard.Clear();

		await WaitForClipboardAsync(() => !Clipboard.GetContent().Contains(StandardDataFormats.Text));

		// A view keeps the content it was created with.
		Assert.AreEqual("older-pasted-content", await pasteView.GetTextAsync());
		Assert.AreEqual("<b>older</b>", await pasteView.GetHtmlFormatAsync());
		Assert.AreEqual("new-own-content", await ownView.GetTextAsync());
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Paste_Supersedes_Own_Content()
	{
#if HAS_UNO
		var package = new DataPackage();
		package.SetText(TestString);
		package.SetHtmlFormat("<b>own</b>");
		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Html));

		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('pasted-content', 'text/plain');
			""");

		var view = Clipboard.GetContent();
		Assert.IsFalse(view.Contains(StandardDataFormats.Html));
		Assert.AreEqual("pasted-content", await view.GetTextAsync());

		// Once the paste is no longer fresh, the clipboard state is unknown: the own write it
		// replaced must not come back. (Unknown content advertises the Bitmap format optimistically.)
		await Task.Delay(2200);

		view = Clipboard.GetContent();
		Assert.IsTrue(view.Contains(StandardDataFormats.Bitmap));
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Empty_Paste_Follows_Visible_Content()
	{
#if HAS_UNO
		var package = new DataPackage();
		package.SetText(TestString);
		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Text));

		// A paste would show the text written; one showing nothing means the clipboard changed and the
		// text is no longer there.
		DispatchSyntheticPaste("const dt = new DataTransfer();");
		Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Text));

		// The same goes for what an earlier paste showed.
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('pasted-content', 'text/plain');
			""");
		Assert.IsTrue(Clipboard.GetContent().Contains(StandardDataFormats.Text));

		DispatchSyntheticPaste("const dt = new DataTransfer();");
		Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Text));
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Empty_Paste_Without_Shortcut()
	{
#if HAS_UNO
		InstallDeniedClipboardRead();
		try
		{
			// A paste gesture with no shortcut before it, like a context menu's Paste. Browsers show a paste
			// the standard formats whenever the clipboard holds them, so one showing nothing says the clipboard
			// holds none of them: they are reported absent, without a permission-gated read.
			// An in-page copy first leaves the clipboard unknown, so only the paste can tell.
			InvokeJs("document.dispatchEvent(new ClipboardEvent('copy', { bubbles: true })); return 'ok';");
			DispatchSyntheticPaste("const dt = new DataTransfer();");

			var view = Clipboard.GetContent();
			Assert.IsFalse(view.Contains(StandardDataFormats.Text));
			Assert.IsFalse(view.Contains(StandardDataFormats.Html));
			Assert.IsFalse(view.Contains(StandardDataFormats.Bitmap));
			Assert.IsFalse(view.Contains(StandardDataFormats.StorageItems));
			Assert.AreEqual(0, GetClipboardReadCount());
		}
		finally
		{
			RemoveDeniedClipboardRead();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Clipboard_Read_Returns_Non_Raster_Image()
	{
#if HAS_UNO
		InvokeJs(
			$$"""
			const item = (type, blob) => ({ types: [type], getType: () => Promise.resolve(blob) });
			window.__unoSvgItem = item('image/svg+xml', new Blob(['<svg xmlns="http://www.w3.org/2000/svg"/>'], { type: 'image/svg+xml' }));
			const png = Uint8Array.from(atob('{{TestPngBase64}}'), c => c.charCodeAt(0));
			window.__unoPngItem = item('image/png', new Blob([png], { type: 'image/png' }));
			// An in-page copy leaves the content unknown, so the reads go to the browser.
			document.dispatchEvent(new ClipboardEvent('copy', { bubbles: true }));
			return 'ok';
			""");
		try
		{
			// Chromium reads an SVG on the clipboard as image/svg+xml, which is not a raster image.
			InvokeJs("navigator.clipboard.read = () => Promise.resolve([window.__unoSvgItem]); return 'ok';");
			var view = Clipboard.GetContent();
			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await view.GetBitmapAsync());
			Assert.IsNotInstanceOfType<UnauthorizedAccessException>(exception.InnerException);

			// A PNG is the Bitmap.
			InvokeJs("navigator.clipboard.read = () => Promise.resolve([window.__unoPngItem]); return 'ok';");
			var reference = await Clipboard.GetContent().GetBitmapAsync();
			using var stream = await reference.OpenReadAsync();
			CollectionAssert.AreEqual(Convert.FromBase64String(TestPngBase64), await ToBytesAsync(stream));
		}
		finally
		{
			InvokeJs("delete navigator.clipboard.read; delete window.__unoSvgItem; delete window.__unoPngItem; return 'ok';");
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Empty_Paste_Follows_Own_Custom_Format()
	{
#if HAS_UNO
		const string customFormat = "application/x-uno-test";

		var package = new DataPackage();
		package.SetData(customFormat, "custom-payload");
		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(customFormat));

		// A paste never shows web custom formats, so one showing nothing says nothing about them.
		// This is the paste Chromium delivers for such content: a single empty text item.
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('', 'text/plain');
			""");

		var view = Clipboard.GetContent();
		Assert.IsTrue(view.Contains(customFormat));
		Assert.IsFalse(view.Contains(StandardDataFormats.Html));
		Assert.AreEqual("custom-payload", await view.GetDataAsync(customFormat) as string);
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Copy_Event_Invalidates_Paste()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('pasted-content', 'text/plain');
			""");

		Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Html));

		// A copy within the page replaces the clipboard content; the captured paste no longer
		// describes it, so the state is unknown (which advertises HTML optimistically).
		InvokeJs("document.dispatchEvent(new ClipboardEvent('copy', { bubbles: true, cancelable: true })); return 'ok';");

		Assert.IsTrue(Clipboard.GetContent().Contains(StandardDataFormats.Html));
		await Task.CompletedTask;
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_SetContent_Superseded_Before_Write()
	{
#if HAS_UNO
		InstallClipboardWriteRecorder();
		try
		{
			// A write whose data takes a while to prepare is overtaken by the one issued after it:
			// only the later one may reach the clipboard and the cache.
			var slow = new DataPackage();
			slow.SetDataProvider(StandardDataFormats.Text, async request =>
			{
				var deferral = request.GetDeferral();
				await Task.Delay(300);
				request.SetData("slow");
				deferral.Complete();
			});

			var fast = new DataPackage();
			fast.SetText("fast");

			Clipboard.SetContent(slow);
			Clipboard.SetContent(fast);

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Contains("item:text/plain"));
			await Task.Delay(1000);

			Assert.AreEqual(1, GetRecordedClipboardWrites().Count(write => write == "item:text/plain"));
			Assert.AreEqual("fast", await Clipboard.GetContent().GetTextAsync());

			// A bitmap is transcoded to PNG before it is written; a Clear issued meanwhile must
			// not be overwritten by it either.
			var bitmap = new DataPackage();
			bitmap.SetBitmap(await ToRAReferenceAsync(Convert.FromBase64String(TestBmpBase64)));

			Clipboard.SetContent(bitmap);
			Clipboard.Clear();

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Last() == "text:");
			await Task.Delay(1000);

			Assert.AreEqual("text:", GetRecordedClipboardWrites().Last());
			Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Bitmap));
		}
		finally
		{
			RemoveClipboardWriteRecorder();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_ContentChanged_Handler_Clears_During_SetContent()
	{
#if HAS_UNO
		InstallClipboardWriteRecorder();
		var cleared = false;
		EventHandler<object> onContentChanged = (_, _) =>
		{
			if (!cleared)
			{
				cleared = true;
				Clipboard.Clear();
			}
		};
		Clipboard.ContentChanged += onContentChanged;
		try
		{
			// The handler reacts to the write by clearing the clipboard, which is the newer
			// call; the write it reacted to must not land on top of it.
			var package = new DataPackage();
			package.SetText(TestString);
			Clipboard.SetContent(package);

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Contains("text:"));
			await Task.Delay(1000);

			CollectionAssert.AreEqual(new[] { "text:" }, GetRecordedClipboardWrites());
			Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Text));
		}
		finally
		{
			Clipboard.ContentChanged -= onContentChanged;
			RemoveClipboardWriteRecorder();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Clipboard_Api_Is_Unavailable()
	{
#if HAS_UNO
		// Outside a secure context there is no navigator.clipboard, and writes fall back to the copy command, which
		// writes what a copy event handler hands it. The command needs a user gesture, so it is stood in for here by
		// raising the copy event as the browser does and recording what the handler wrote.
		InvokeJs(
			"""
			Object.defineProperty(navigator, 'clipboard', { get: () => undefined, configurable: true });
			window.__unoCommandCopies = [];
			window.__unoExecCommand = document.execCommand;
			document.execCommand = command => {
				const dt = new DataTransfer();
				const event = new ClipboardEvent(command, { clipboardData: dt, bubbles: true, cancelable: true });
				document.dispatchEvent(event);
				const written = event.defaultPrevented && dt.types.includes('text/plain');
				window.__unoCommandCopies.push(written ? 'text:' + dt.getData('text/plain') : 'selection');
				return true;
			};
			return 'ok';
			""");
		try
		{
			var package = new DataPackage();
			package.SetText(TestString);
			Clipboard.SetContent(package);
			await WaitForClipboardAsync(() => GetCommandCopies().Length == 1);

			// Clear writes empty text the same way, since browsers cannot empty the clipboard.
			Clipboard.Clear();
			await WaitForClipboardAsync(() => GetCommandCopies().Length == 2);

			CollectionAssert.AreEqual(new[] { "text:" + TestString, "text:" }, GetCommandCopies());
		}
		finally
		{
			InvokeJs(
				"""
				delete navigator.clipboard;
				document.execCommand = window.__unoExecCommand;
				delete window.__unoExecCommand;
				delete window.__unoCommandCopies;
				return 'ok';
				""");
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Clear_While_Write_Pending()
	{
#if HAS_UNO
		InstallClipboardWriteRecorder(writeDelayMs: 300);
		try
		{
			// The text has been handed to the browser, which is still committing it, when Clear
			// is issued; the clear must reach the clipboard after the text, not race it.
			var package = new DataPackage();
			package.SetText(TestString);
			Clipboard.SetContent(package);

			await WaitForClipboardAsync(IsClipboardWriteIssued);
			Clipboard.Clear();

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Length == 2);
			await Task.Delay(1000);

			CollectionAssert.AreEqual(new[] { "item:text/plain", "text:" }, GetRecordedClipboardWrites());
			Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Text));
		}
		finally
		{
			RemoveClipboardWriteRecorder();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_SetContent_Write_Is_Issued_Before_Data_Is_Ready()
	{
#if HAS_UNO
		InstallClipboardWriteRecorder();
		try
		{
			// The browser only accepts the write inside the user activation SetContent runs in,
			// so it is issued at once and the data, here from a provider that has not run yet,
			// follows.
			var ready = new TaskCompletionSource();
			var package = new DataPackage();
			package.SetDataProvider(StandardDataFormats.Text, async request =>
			{
				var deferral = request.GetDeferral();
				await ready.Task;
				request.SetData("provided");
				deferral.Complete();
			});

			Clipboard.SetContent(package);

			await WaitForClipboardAsync(IsClipboardWriteIssued);
			Assert.AreEqual(0, GetRecordedClipboardWrites().Length);

			ready.SetResult();

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Contains("item:text/plain"));
			Assert.AreEqual("provided", await Clipboard.GetContent().GetTextAsync());
		}
		finally
		{
			RemoveClipboardWriteRecorder();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Paste_Shortcut_Follows_Fresh_Paste()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('first', 'text/plain');
			""");

		Assert.AreEqual("first", await Clipboard.GetContent().GetTextAsync());

		// A second shortcut inside the freshness window announces new content; the view built
		// for it must not be bound to the previous paste.
		InvokeJs("document.dispatchEvent(new KeyboardEvent('keydown', { key: 'v', ctrlKey: true, bubbles: true })); return 'ok';");
		var view = Clipboard.GetContent();

		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('second', 'text/plain');
			""");

		Assert.AreEqual("second", await view.GetTextAsync());
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Paste_Shortcut_Gets_Empty_Paste()
	{
#if HAS_UNO
		var package = new DataPackage();
		package.SetText(TestString);
		Clipboard.SetContent(package);
		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Text));

		InstallDeniedClipboardRead();
		try
		{
			InvokeJs("document.dispatchEvent(new KeyboardEvent('keydown', { key: 'v', ctrlKey: true, bubbles: true })); return 'ok';");
			var waitingView = Clipboard.GetContent();
			var laterView = Clipboard.GetContent();
			var waitingText = waitingView.GetTextAsync().AsTask();

			// The paste the shortcut announced shows nothing, so the clipboard no longer holds the text written
			// before it. That is the answer to the shortcut, whether the read waited for the paste or started
			// after it: nothing, without a permission-gated read.
			DispatchSyntheticPaste("const dt = new DataTransfer();");

			Assert.AreEqual("", await waitingText);
			Assert.AreEqual("", await laterView.GetTextAsync());
			Assert.AreEqual(0, GetClipboardReadCount());
		}
		finally
		{
			RemoveDeniedClipboardRead();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Paste_Shortcut_Gets_Empty_Paste_Over_Own_Custom_Format()
	{
#if HAS_UNO
		const string customFormat = "application/x-uno-test";

		var package = new DataPackage();
		package.SetData(customFormat, "custom-payload");
		Clipboard.SetContent(package);
		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(customFormat));

		InstallDeniedClipboardRead();
		try
		{
			InvokeJs("document.dispatchEvent(new KeyboardEvent('keydown', { key: 'v', ctrlKey: true, bubbles: true })); return 'ok';");
			var view = Clipboard.GetContent();
			var text = view.GetTextAsync().AsTask();

			// Chromium's paste for content made only of web custom formats: a single empty text item.
			DispatchSyntheticPaste(
				"""
				const dt = new DataTransfer();
				dt.items.add('', 'text/plain');
				""");

			Assert.AreEqual("", await text);

			// A paste never shows web custom formats, so the one written stays known once the shortcut is answered.
			var after = Clipboard.GetContent();
			Assert.IsTrue(after.Contains(customFormat));
			Assert.AreEqual("custom-payload", await after.GetDataAsync(customFormat) as string);
			Assert.AreEqual(0, GetClipboardReadCount());
		}
		finally
		{
			RemoveDeniedClipboardRead();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Paste_Shortcut_Gets_No_Paste()
	{
#if HAS_UNO
		var package = new DataPackage();
		package.SetText(TestString);
		Clipboard.SetContent(package);
		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Text));

		InstallDeniedClipboardRead();
		try
		{
			// No paste event arrives (a browser that delivers none, or one too late), and nothing showed the
			// clipboard changed since the application wrote to it: the read is answered from that content.
			InvokeJs("document.dispatchEvent(new KeyboardEvent('keydown', { key: 'v', ctrlKey: true, bubbles: true })); return 'ok';");
			Assert.AreEqual(TestString, await Clipboard.GetContent().GetTextAsync());
			Assert.AreEqual(0, GetClipboardReadCount());

			// Once an in-page copy leaves the content unknown, the read goes to the browser.
			InvokeJs("document.dispatchEvent(new ClipboardEvent('copy', { bubbles: true })); return 'ok';");
			InvokeJs("document.dispatchEvent(new KeyboardEvent('keydown', { key: 'v', ctrlKey: true, bubbles: true })); return 'ok';");
			var view = Clipboard.GetContent();
			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await view.GetTextAsync());
			Assert.IsInstanceOfType<UnauthorizedAccessException>(exception.InnerException);
			Assert.AreEqual(1, GetClipboardReadCount());
		}
		finally
		{
			RemoveDeniedClipboardRead();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Custom_Format_Is_Standard_Mime_Type()
	{
#if HAS_UNO
		InstallClipboardWriteRecorder();
		try
		{
			// A custom format named like a standard representation would collide with it on
			// both the system clipboard and the read-back; it is left out.
			var package = new DataPackage();
			package.SetText("standard");
			package.SetData("text/plain", "custom");

			Clipboard.SetContent(package);

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Contains("item:text/plain"));

			CollectionAssert.AreEqual(new[] { "item:text/plain" }, GetRecordedClipboardWrites());
			var view = Clipboard.GetContent();
			Assert.IsFalse(view.Contains("text/plain"));
			Assert.AreEqual("standard", await view.GetTextAsync());
		}
		finally
		{
			RemoveClipboardWriteRecorder();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Bitmap_Transcoded_Without_OffscreenCanvas()
	{
#if HAS_UNO
		InstallClipboardWriteRecorder();
		InvokeJs(
			"""
			window.__unoOffscreenCanvas = window.OffscreenCanvas;
			window.OffscreenCanvas = undefined;
			window.createImageBitmap = undefined;
			return 'ok';
			""");
		try
		{
			// Engines without OffscreenCanvas still get the bitmap as PNG, through a canvas element.
			var package = new DataPackage();
			package.SetText(TestString);
			package.SetBitmap(await ToRAReferenceAsync(Convert.FromBase64String(TestBmpBase64)));

			Clipboard.SetContent(package);

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Length > 0);
			CollectionAssert.AreEqual(new[] { "item:image/png,text/plain" }, GetRecordedClipboardWrites());
		}
		finally
		{
			InvokeJs("window.OffscreenCanvas = window.__unoOffscreenCanvas; delete window.__unoOffscreenCanvas; return 'ok';");
			RemoveClipboardWriteRecorder();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Clipboard_Representation_Fails_To_Read()
	{
#if HAS_UNO
		InvokeJs(
			"""
			navigator.clipboard.read = () => Promise.resolve([{
				types: ['text/plain', 'text/html'],
				getType: type => type === 'text/html'
					? Promise.reject(new DOMException('The representation is gone.', 'DataError'))
					: Promise.resolve(new Blob(['from-browser'], { type: 'text/plain' })),
			}]);
			document.dispatchEvent(new ClipboardEvent('copy', { bubbles: true }));
			return 'ok';
			""");
		try
		{
			// One representation failing to load must not take the others down with it.
			var view = Clipboard.GetContent();
			Assert.AreEqual("from-browser", await view.GetTextAsync());
			Assert.AreEqual("", await view.GetHtmlFormatAsync());
		}
		finally
		{
			InvokeJs("delete navigator.clipboard.read; return 'ok';");
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	[DataRow("DataError")]
	[DataRow("SecurityError")]
	public async Task When_Clipboard_Read_Fails(string exceptionName)
	{
#if HAS_UNO
		InvokeJs(
			$$"""
			navigator.clipboard.read = () => Promise.reject(new DOMException('The clipboard is unreadable.', '{{exceptionName}}'));
			document.dispatchEvent(new ClipboardEvent('copy', { bubbles: true }));
			return 'ok';
			""");
		try
		{
			InstallConsoleRecorder("The clipboard is unreadable.");

			// Browsers refuse a read with a NotAllowedError only: any other failure is not reported as a denial,
			// and is logged as an error.
			var view = Clipboard.GetContent();
			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await view.GetTextAsync());
			Assert.IsNotInstanceOfType<UnauthorizedAccessException>(exception.InnerException);
			Assert.AreEqual("error", GetRecordedConsoleLevels());
		}
		finally
		{
			RemoveConsoleRecorder();
			InvokeJs("delete navigator.clipboard.read; return 'ok';");
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10032")]
	public async Task When_Clipboard_Read_Is_Denied()
	{
#if HAS_UNO
		InvokeJs(
			"""
			const denied = () => Promise.reject(new DOMException('Read permission denied.', 'NotAllowedError'));
			navigator.clipboard.read = denied;
			navigator.clipboard.readText = denied;
			// An in-page copy leaves the content unknown, so the read goes to the browser.
			document.dispatchEvent(new ClipboardEvent('copy', { bubbles: true }));
			return 'ok';
			""");
		try
		{
			InstallConsoleRecorder("Read permission denied.");

			// The formats are advertised, and a denied read is told apart from empty content
			// by the exception it surfaces. Being an expected outcome, it is logged at debug level.
			var view = Clipboard.GetContent();
			Assert.IsTrue(view.Contains(StandardDataFormats.Text));

			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await view.GetTextAsync());
			Assert.IsInstanceOfType<UnauthorizedAccessException>(exception.InnerException);
			Assert.AreEqual("debug", GetRecordedConsoleLevels());
		}
		finally
		{
			RemoveConsoleRecorder();
			InvokeJs("delete navigator.clipboard.read; delete navigator.clipboard.readText; return 'ok';");
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Paste_Handled_By_Target_Raises_ContentChanged()
	{
#if HAS_UNO
		var raised = false;
		EventHandler<object> onContentChanged = (_, _) => raised = true;
		Clipboard.ContentChanged += onContentChanged;
		try
		{
			// A control handling the paste itself stops it from bubbling; the snapshot is taken
			// in the capture phase regardless, and so must be the notification.
			raised = false;
			InvokeJs(
				"""
				const target = document.createElement('div');
				document.body.appendChild(target);
				target.addEventListener('paste', e => e.stopPropagation());
				const dt = new DataTransfer();
				dt.items.add('handled', 'text/plain');
				target.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true }));
				target.remove();
				return 'ok';
				""");

			Assert.IsTrue(raised);
			Assert.AreEqual("handled", await Clipboard.GetContent().GetTextAsync());
		}
		finally
		{
			Clipboard.ContentChanged -= onContentChanged;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_SetContent_Empty_Package()
	{
#if HAS_UNO
		var package = new DataPackage();
		package.SetText(TestString);
		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Text));

		InstallClipboardWriteRecorder();
		try
		{
			// A package with nothing the browser can carry still replaces the clipboard content.
			Clipboard.SetContent(new DataPackage());

			await WaitForClipboardAsync(() => GetRecordedClipboardWrites().Contains("text:"));

			Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Text));
		}
		finally
		{
			RemoveClipboardWriteRecorder();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_GetSet_Clipboard_Custom_Image_Format()
	{
		const string svgFormat = "image/svg+xml";
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg'/>";

		var package = new DataPackage();
		package.SetText(TestString);
		package.SetData(svgFormat, svg);

		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(svgFormat));

		// A string custom format with an image MIME type is not the Bitmap format.
		var view = Clipboard.GetContent();
		Assert.IsFalse(view.Contains(StandardDataFormats.Bitmap));
		Assert.AreEqual(svg, await view.GetDataAsync(svgFormat) as string);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	public async Task When_Clipboard_Files_Are_Released()
	{
#if HAS_UNO
		// The image is registered when a view is built from the content; every view built from the
		// same content shares that registration, however many times it is read.
		var baseline = GetNativeStorageItemCount();

		var package = new DataPackage();
		package.SetBitmap(await ToRAReferenceAsync(Convert.FromBase64String(TestPngBase64)));
		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Bitmap));

		await ReadClipboardBitmapAsync(times: 20);
		Assert.AreEqual(baseline + 1, GetNativeStorageItemCount());

		// The registration is released once nothing can reach the views or the files they handed out.
		await WaitForCollectedAsync(() => GetNativeStorageItemCount() == baseline);

		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add(new File(['file-content-1'], 'first.txt', { type: 'text/plain' }));
			dt.items.add(new File(['file-content-2'], 'second.txt', { type: 'text/plain' }));
			""");

		baseline = GetNativeStorageItemCount();

		await ReadPastedFilesAsync(times: 5);
		Assert.AreEqual(baseline + 2, GetNativeStorageItemCount());

		await WaitForCollectedAsync(() => GetNativeStorageItemCount() == baseline);
#else
		await Task.CompletedTask;
#endif
	}

#if HAS_UNO
	// Reads in a method of their own so no local keeps a view or a file reachable afterwards.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task ReadClipboardBitmapAsync(int times)
	{
		var expected = Convert.FromBase64String(TestPngBase64);
		for (var i = 0; i < times; i++)
		{
			var reference = await Clipboard.GetContent().GetBitmapAsync();
			using var stream = await reference.OpenReadAsync();
			CollectionAssert.AreEqual(expected, await ToBytesAsync(stream));
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task ReadPastedFilesAsync(int times)
	{
		for (var i = 0; i < times; i++)
		{
			var items = await Clipboard.GetContent().GetStorageItemsAsync();
			Assert.AreEqual(2, items.Count);

			using var stream = await ((Windows.Storage.StorageFile)items[1]).OpenAsync(Windows.Storage.FileAccessMode.Read);
			Assert.AreEqual("file-content-2", Encoding.UTF8.GetString(await ToBytesAsync(stream)));
		}
	}

	private static async Task WaitForCollectedAsync(Func<bool> condition)
	{
		for (var i = 0; i < 50 && !condition(); i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			// The release runs on the dispatcher.
			await Task.Delay(100);
		}

		Assert.IsTrue(condition(), "The clipboard file registrations were not released.");
	}

	private static int GetNativeStorageItemCount()
		=> int.Parse(InvokeJs("return Uno.Storage.NativeStorageItem._guidToItemMap.size.toString();"), CultureInfo.InvariantCulture);

	private static string[] GetCommandCopies()
		=> InvokeJs("return window.__unoCommandCopies.join('\\n');").Split('\n', StringSplitOptions.RemoveEmptyEntries);

	// Makes the browser deny every clipboard read, as it does without permission, and counts the reads.
	private static void InstallDeniedClipboardRead()
		=> InvokeJs(
			"""
			window.__unoClipboardReads = 0;
			const denied = () => {
				window.__unoClipboardReads++;
				return Promise.reject(new DOMException('Read permission denied.', 'NotAllowedError'));
			};
			navigator.clipboard.read = denied;
			navigator.clipboard.readText = denied;
			return 'ok';
			""");

	private static int GetClipboardReadCount()
		=> int.Parse(InvokeJs("return String(window.__unoClipboardReads);"), CultureInfo.InvariantCulture);

	private static void RemoveDeniedClipboardRead()
		=> InvokeJs("delete navigator.clipboard.read; delete navigator.clipboard.readText; delete window.__unoClipboardReads; return 'ok';");

	// Records the system clipboard writes instead of performing them, which needs no user gesture,
	// and slows image decoding down so a transcode is still pending when a later call lands.
	// Records what reaches the browser clipboard, in completion order; writeDelayMs makes
	// ClipboardItem writes take that long to complete, like a browser still committing one.
	private static void InstallClipboardWriteRecorder(int writeDelayMs = 0)
		=> InvokeJs(
			$$"""
			const clipboard = navigator.clipboard;
			window.__unoClipboardWrites = [];
			window.__unoClipboardWriteIssued = false;
			window.__unoCreateImageBitmap = window.createImageBitmap;
			window.createImageBitmap = (...args) => new Promise(resolve => setTimeout(() => resolve(window.__unoCreateImageBitmap.apply(window, args)), 300));
			clipboard.write = async items => {
				window.__unoClipboardWriteIssued = true;
				// Like a browser, the write completes once every representation has resolved and
				// fails if one of them rejects.
				for (const item of items) {
					const blobs = await Promise.all(Array.from(item.types).map(type => item.getType(type)));
					const png = blobs[Array.from(item.types).indexOf('image/png')];
					if (png) {
						const head = new Uint8Array(await png.slice(0, 8).arrayBuffer());
						if (png.type !== 'image/png' || [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A].some((b, i) => head[i] !== b)) {
							window.__unoClipboardWrites.push('item:invalid-png');
							return;
						}
					}
				}
				if ({{writeDelayMs}} > 0) {
					await new Promise(resolve => setTimeout(resolve, {{writeDelayMs}}));
				}
				for (const item of items) {
					window.__unoClipboardWrites.push('item:' + Array.from(item.types).sort().join(','));
				}
			};
			clipboard.writeText = text => {
				window.__unoClipboardWrites.push('text:' + text);
				return Promise.resolve();
			};
			return 'ok';
			""");

	private static void RemoveClipboardWriteRecorder()
		=> InvokeJs(
			"""
			delete navigator.clipboard.write;
			delete navigator.clipboard.writeText;
			delete window.__unoClipboardWrites;
			delete window.__unoClipboardWriteIssued;
			window.createImageBitmap = window.__unoCreateImageBitmap;
			return 'ok';
			""");

	// True once a ClipboardItem write has been handed to the browser, complete or not.
	private static bool IsClipboardWriteIssued()
		=> InvokeJs("return String(window.__unoClipboardWriteIssued);") == "true";

	private static async Task SetContentAndWaitAsync(DataPackage package)
	{
		var written = new TaskCompletionSource();
		EventHandler<object> onContentChanged = (_, _) => written.TrySetResult();
		Clipboard.ContentChanged += onContentChanged;
		try
		{
			Clipboard.SetContent(package);
			await written.Task;
		}
		finally
		{
			Clipboard.ContentChanged -= onContentChanged;
		}
	}

	private static string[] GetRecordedClipboardWrites()
		=> InvokeJs("return window.__unoClipboardWrites.join('\\n');").Split('\n', StringSplitOptions.RemoveEmptyEntries);

	// Records the console level of each message that contains the marker.
	private static void InstallConsoleRecorder(string marker)
		=> InvokeJs(
			$$"""
			const state = window.__unoConsoleRecorder = { levels: [], console: {} };
			for (const level of ['error', 'warn', 'info', 'log', 'debug']) {
				const original = state.console[level] = console[level];
				console[level] = function (...args) {
					if (args.some(a => String(a).includes('{{marker}}'))) {
						state.levels.push(level);
					}
					return original.apply(console, args);
				};
			}
			return 'ok';
			""");

	private static void RemoveConsoleRecorder()
		=> InvokeJs(
			"""
			const state = window.__unoConsoleRecorder;
			if (!state) {
				return 'ok';
			}
			for (const level in state.console) {
				console[level] = state.console[level];
			}
			delete window.__unoConsoleRecorder;
			return 'ok';
			""");

	// The levels of the recorded messages, comma-separated.
	private static string GetRecordedConsoleLevels()
		=> InvokeJs("return window.__unoConsoleRecorder.levels.join(',');");

	private static string InvokeJs(string body)
		=> Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs($"(function() {{ {body} }})()");
#endif

#if HAS_UNO
	private static void DispatchSyntheticPaste(string setupScript) =>
		Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
			$$"""
			(function() {
				{{setupScript}}
				document.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true }));
				return 'ok';
			})()
			""");
#endif

	// Clipboard writes complete asynchronously on some platforms (wasm, Android, iOS), so poll
	// for the expected state instead of asserting immediately or waiting a fixed delay.
	private static async Task WaitForClipboardAsync(Func<bool> condition)
	{
		var success = condition();
		for (var i = 0; i < 60 && !success; i++)
		{
			await Task.Delay(50);
			success = condition();
		}

		Assert.IsTrue(success, "The expected clipboard state was not reached.");
	}

	private static async Task DelayForClipboard()
	{
		// On some platforms, clipboard operations are not immediately available.
		// On Android/iOS, SetContent dispatches the write asynchronously
		// via CoreDispatcher.Main.RunAsync. On Wasm, clipboard access is also async.
		var platform = RuntimeTestsPlatformHelper.CurrentPlatform;
		if ((RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS).HasFlag(platform))
		{
			await Task.Delay(1000);
		}
	}

	// for winui at least: use ToRASTream for SetData, use ToRAReferenceAsync for SetBitmap
	private static IRandomAccessStream ToRAStream(byte[] buffer) => new MemoryStream(buffer).AsRandomAccessStream();
	private static async Task<RandomAccessStreamReference> ToRAReferenceAsync(byte[] buffer)
	{
		var stream = new InMemoryRandomAccessStream();
		await stream.WriteAsync(buffer.AsBuffer());
		stream.Seek(0);

		return RandomAccessStreamReference.CreateFromStream(stream);
	}

	private static byte[] ToBytes(IRandomAccessStream ras)
	{
		using var stream = ras.AsStreamForRead();
		using var buffer = new MemoryStream((int)ras.Size);
		stream.CopyTo(buffer);

		return buffer.ToArray();
	}

	// Native wasm file streams are asynchronous-only, so tests running on wasm must not use ToBytes.
	private static async Task<byte[]> ToBytesAsync(IRandomAccessStream ras)
	{
		using var stream = ras.AsStreamForRead();
		using var buffer = new MemoryStream((int)ras.Size);
		await stream.CopyToAsync(buffer);

		return buffer.ToArray();
	}
}
