#nullable enable

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.RuntimeTests.Helpers;

public static class ClipboardHelper
{
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
}
