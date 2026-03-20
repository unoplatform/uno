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
	[PlatformCondition(Include, NativeIOS | NativeAndroid | SkiaWin32)]
	public async Task When_GetSet_Clipboard_Text()
	{
		var package = new DataPackage();
		package.SetText(TestString);

		Clipboard.SetContent(package);

		await DelayForClipboard();

		var view = Clipboard.GetContent();
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

		await DelayForClipboard();

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

		SkiaImageAssert.ArePixelsEqual(bytes, results);
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

		SkiaImageAssert.ArePixelsEqual(bytes, results);
	}
#endif

	private static async Task DelayForClipboard()
	{
		// On some platforms, clipboard operations are not immediately available.
		// On native Android/iOS, SetContent dispatches the write asynchronously
		// via CoreDispatcher.Main.RunAsync. On Wasm, clipboard access is also async.
		if (RuntimeTestsPlatformHelper.CurrentPlatform is
			RuntimeTestPlatforms.SkiaWasm or
			RuntimeTestPlatforms.NativeAndroid or
			RuntimeTestPlatforms.NativeIOS)
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
