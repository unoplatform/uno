using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
	[PlatformCondition(Include, NativeIOS | NativeAndroid | SkiaWin32 | SkiaIOS | Wasm)]
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
	[PlatformCondition(Include, NativeAndroid)]
	public async Task When_GetSet_Clipboard_Uri()
	{
		var package = new DataPackage();
		var uri = new Uri(UriAddress);
		package.SetUri(uri);
		Clipboard.SetContent(package);

		await WaitForClipboardAsync(() => Clipboard.GetContent().Contains(StandardDataFormats.Uri));

		var view = Clipboard.GetContent();
		var result = await view.GetUriAsync();
		Assert.AreEqual(uri, result);
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

		SkiaImageAssert.ArePixelsEqual(bytes, results);
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

		SkiaImageAssert.ArePixelsEqual(bytes, results);
	}
#endif

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, Wasm)]
	public void When_SetContent_Null()
		=> Assert.ThrowsExactly<ArgumentNullException>(() => Clipboard.SetContent(null));

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, Wasm)]
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
	[PlatformCondition(Include, Wasm)]
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
	[PlatformCondition(Include, Wasm)]
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
	[PlatformCondition(Include, Wasm)]
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
	[PlatformCondition(Include, Wasm)]
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
	[PlatformCondition(Include, Wasm)]
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
	[PlatformCondition(Include, Wasm)]
	public async Task When_Paste_Event_With_Files()
	{
#if HAS_UNO
		DispatchSyntheticPaste(
			"""
			const dt = new DataTransfer();
			dt.items.add('paste-text-payload', 'text/plain');
			dt.items.add(new File(['file-content-1'], 'first.txt', { type: 'text/plain' }));
			dt.items.add(new File(['file-content-2'], 'second.txt', { type: 'text/plain' }));
			""");

		var view = Clipboard.GetContent();

		Assert.IsTrue(view.Contains(StandardDataFormats.Text));
		Assert.IsTrue(view.Contains(StandardDataFormats.StorageItems));

		Assert.AreEqual("paste-text-payload", await view.GetTextAsync());

		var items = await view.GetStorageItemsAsync();
		Assert.AreEqual(2, items.Count);
		Assert.AreEqual("first.txt", items[0].Name);
		Assert.AreEqual("second.txt", items[1].Name);

		var file = (Windows.Storage.StorageFile)items[0];
		using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
		Assert.AreEqual("file-content-1", Encoding.UTF8.GetString(await ToBytesAsync(stream)));
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, Wasm)]
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
		for (var i = 0; i < 60 && !condition(); i++)
		{
			await Task.Delay(50);
		}

		Assert.IsTrue(condition(), "The expected clipboard state was not reached.");
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
