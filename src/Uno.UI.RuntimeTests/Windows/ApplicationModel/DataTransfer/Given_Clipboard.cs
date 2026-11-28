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

// Testing Append, Write and Read in one test method

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
	// note: do not enable this for wasm, without adjust default clipboard permission
	[PlatformCondition(Include, SkiaWin32 | SkiaIOS)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23962")]
	public async Task When_GetSet_Clipboard_Text()
	{
		var package = new DataPackage();
		package.SetText(TestString);

		Clipboard.SetContent(package);

		await DelayForClipboard();

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
	[PlatformCondition(Include, SkiaWin32)]
	public async Task When_GetSet_Clipboard_Bitmap_With_Png()
	{
		var package = new DataPackage();
		var bytes = Convert.FromBase64String(TestPngBase64);
		package.SetBitmap(await ToRAReferenceAsync(bytes));

		Clipboard.SetContent(package);

		var view = Clipboard.GetContent();
		var reference = await view.GetBitmapAsync();
		using var stream = await reference.OpenReadAsync();
		var results = ToBytes(stream);

#if UNO_DRAWING_SKIA
		SkiaImageAssert.ArePixelsEqual(bytes, results);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWin32)]
	public async Task When_GetSet_Clipboard_Bitmap_With_Bmp()
	{
		var package = new DataPackage();
		var bytes = Convert.FromBase64String(TestBmpBase64);
		package.SetBitmap(await ToRAReferenceAsync(bytes));

		Clipboard.SetContent(package);

		var view = Clipboard.GetContent();
		var reference = await view.GetBitmapAsync();
		using var stream = await reference.OpenReadAsync();
		var results = ToBytes(stream);

#if UNO_DRAWING_SKIA
		SkiaImageAssert.ArePixelsEqual(bytes, results);
#endif
	}
#endif

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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25733")]
	public async Task When_Read_Refused_By_Browser_Then_Not_Logged_As_Error()
	{
#if HAS_UNO
		// Browsers refuse a read without permission, focus or user activation: an expected outcome.
		var levels = await ReadWithFailingBrowserClipboard("NotAllowedError");

		Assert.AreEqual("debug,debug,debug", levels);
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25733")]
	public async Task When_Read_Fails_Then_Logged_As_Error()
	{
#if HAS_UNO
		var levels = await ReadWithFailingBrowserClipboard("DataError");

		Assert.AreEqual("error,error,error", levels);
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25733")]
	public async Task When_Item_Read_Not_Allowed_Then_Logged_As_Error()
	{
#if HAS_UNO
		// Once the browser granted the read, NotAllowedError means the data couldn't be read, not a refusal.
		var levels = await ReadWithFailingBrowserClipboard("NotAllowedError", failItemReads: true);

		Assert.AreEqual("error,error", levels);
#else
		await Task.CompletedTask;
#endif
	}

#if HAS_UNO
	// Reads text, HTML and a bitmap while the browser rejects every clipboard read (or, with failItemReads, every
	// read of the data of a clipboard item) with a DOMException named exceptionName, and returns the console levels
	// the read failures were logged at, comma-separated.
	private static async Task<string> ReadWithFailingBrowserClipboard(string exceptionName, bool failItemReads = false)
	{
		const string Reason = "uno-runtime-test-read-failure";

		// The mock rejects either the read itself, or the reads of the data of the item it returns.
		var mockReads = failItemReads
			? """
				const item = { types: ['text/html', 'image/png'], getType: fail };
				navigator.clipboard.read = () => Promise.resolve([item]);
				navigator.clipboard.readText = () => Promise.resolve('');
				"""
			: """
				navigator.clipboard.read = fail;
				navigator.clipboard.readText = fail;
				""";

		try
		{
			var setup = InvokeJs(
				$$"""
				if (!navigator.clipboard) {
					return 'unavailable';
				}
				const state = window.__unoClipboardReadTest = { levels: [], console: {} };
				for (const level of ['error', 'warn', 'info', 'log', 'debug']) {
					const original = state.console[level] = console[level];
					console[level] = function (...args) {
						if (args.some(a => String(a).includes('{{Reason}}'))) {
							state.levels.push(level);
						}
						return original.apply(console, args);
					};
				}
				const fail = () => Promise.reject(new DOMException('{{Reason}}', '{{exceptionName}}'));
				{{mockReads}}
				return 'ok';
				""");
			Assert.AreEqual("ok", setup, "navigator.clipboard is missing: the page must be a secure context (https or localhost).");

			var view = Clipboard.GetContent();

			// A failed read reads as an empty clipboard.
			Assert.AreEqual("", await view.GetTextAsync());
			Assert.AreEqual("", await view.GetHtmlFormatAsync());

			// The view reports a missing bitmap as null or as a failure; either way there is none.
			try
			{
				Assert.IsNull(await view.GetBitmapAsync(), "bitmap");
			}
			catch (InvalidOperationException)
			{
			}

			return InvokeJs("return window.__unoClipboardReadTest.levels.join(',');");
		}
		finally
		{
			InvokeJs(
				"""
				const state = window.__unoClipboardReadTest;
				if (!state) {
					return 'ok';
				}
				for (const level in state.console) {
					console[level] = state.console[level];
				}
				delete window.__unoClipboardReadTest;
				delete navigator.clipboard.read;
				delete navigator.clipboard.readText;
				return 'ok';
				""");
		}
	}

	private static string InvokeJs(string body)
		=> Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs($"(function() {{ {body} }})()");
#endif

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
}
