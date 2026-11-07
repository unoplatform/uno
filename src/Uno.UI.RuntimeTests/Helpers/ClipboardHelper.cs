#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.RuntimeTests.Helpers;

public static class ClipboardHelper
{
	/// <summary>
	/// True when the platform has a working clipboard. An <c>IClipboardExtension</c> provides
	/// the clipboard on skia desktop targets, while on the browser, Android, and iOS the
	/// WinRT layer provides the implementation directly.
	/// </summary>
	public static bool IsAvailable =>
#if HAS_UNO
		OperatingSystem.IsBrowser() ||
		OperatingSystem.IsAndroid() ||
		OperatingSystem.IsIOS() ||
		Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<Uno.ApplicationModel.DataTransfer.IClipboardExtension>();
#else
		true;
#endif


	/// <summary>
	/// Reads the clipboard, polling until it returns <paramref name="expected"/> (or attempts run out), and
	/// returns the last-read value for the caller to assert on.
	/// </summary>
	/// <remarks>
	/// On the Win32 backend the clipboard-content cache is invalidated asynchronously (via WM_CLIPBOARDUPDATE)
	/// and OpenClipboard can transiently fail while another process holds the clipboard, so a single-shot read
	/// right after a copy flakes. Each miss pumps the message loop via <see cref="UITestHelper.WaitForIdle"/>.
	/// </remarks>
	public static async Task<string?> WaitForTextAsync(string expected, int attempts = 30)
	{
		string? actual = null;
		for (var i = 0; i < attempts && actual != expected; i++)
		{
			try
			{
				actual = await Clipboard.GetContent()!.GetTextAsync();
			}
			catch
			{
				// transient clipboard failure — retry after pumping the message loop
			}

			if (actual != expected)
			{
				await UITestHelper.WaitForIdle();
			}
		}

		return actual;
	}

	/// <summary>
	/// Puts a unique sentinel on the clipboard and waits for it to read back, so that a copy which
	/// never runs is asserted against a known value instead of whatever the host had on its clipboard.
	/// </summary>
	public static async Task<string> SeedDummyData()
	{
		var seed = $"dummy-data-{DateTime.Now.Ticks}";

		var package = new DataPackage();
		package.SetText(seed);
		Clipboard.SetContent(package);

		Assert.AreEqual(seed, await WaitForTextAsync(seed), "The clipboard seed was never written.");

		return seed;
	}
}
