#if __SKIA__
#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using static Microsoft.VisualStudio.TestTools.UnitTesting.ConditionMode;
using static Microsoft.VisualStudio.TestTools.UnitTesting.RuntimeTestPlatforms;

namespace Uno.UI.RuntimeTests.Tests;

partial class Given_Clipboard // Win32 contention
{
	private const string ContendedText = "test-string-while-contended";

	/// <summary>
	/// Reading the clipboard must not require winning <c>OpenClipboard</c>.
	/// </summary>
	/// <remarks>
	/// <para><c>OpenClipboard</c> is a global exclusive lock with a single winner. When several
	/// applications are notified of a clipboard change they all read at the same instant, so all but
	/// one are denied — and if a denied read is treated as "the clipboard is empty", paste silently
	/// stops working in every one of them until the next copy.</para>
	/// <para>The lock is owned by a <em>thread</em>, so a second thread of this same process holding it
	/// through a real window handle denies the UI thread exactly as another process would: measured
	/// to fail with <c>ERROR_ACCESS_DENIED</c>, identically to the cross-process case. The holder also
	/// replaces the content while holding, which bumps the clipboard sequence number immediately and
	/// visibly to other threads — so this cannot be satisfied by a cache populated before the
	/// contention started.</para>
	/// </remarks>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(Include, SkiaWin32)]
	public async Task When_Clipboard_Contended_Then_Content_Is_Still_Visible()
	{
		using var held = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		string? holderSetupFailure = null;
		var holderExited = false;

		var holder = new Thread(() =>
		{
			var hwnd = Win32.CreateMessageOnlyWindow();
			var tookClipboard = false;
			try
			{
				// A NULL owner does NOT exclude other threads; a real window handle does.
				tookClipboard = hwnd != IntPtr.Zero && Win32.OpenClipboard(hwnd);
				if (!tookClipboard)
				{
					holderSetupFailure = hwnd == IntPtr.Zero
						? "the owner window could not be created"
						: $"OpenClipboard failed with error {Marshal.GetLastWin32Error()}";
					return;
				}

				if (!Win32.EmptyClipboard())
				{
					holderSetupFailure = $"EmptyClipboard failed with error {Marshal.GetLastWin32Error()}";
					return;
				}

				if (!Win32.TrySetUnicodeText(ContendedText, out var error))
				{
					holderSetupFailure = $"SetClipboardData failed with error {error}";
				}
			}
			finally
			{
				held.Set();

				// Keyed on owning the lock, never on the setup having succeeded: a holder that took the
				// clipboard and then failed still has to release it, or it poisons every later test.
				if (tookClipboard)
				{
					release.Wait(TimeSpan.FromSeconds(30));
					Win32.CloseClipboard();
				}

				// Destroying the owner keeps the content: it was set eagerly, not delay-rendered.
				// This has to run on the thread that created the window, which is this one.
				if (hwnd != IntPtr.Zero)
				{
					Win32.DestroyWindow(hwnd);
				}
			}
		})
		{
			IsBackground = true,
			Name = nameof(When_Clipboard_Contended_Then_Content_Is_Still_Visible),
		};

		holder.Start();

		try
		{
			Assert.IsTrue(held.Wait(TimeSpan.FromSeconds(30)), "the holder thread never reported back");

			// The contention is this test's premise, so a holder that never established it has to say so:
			// letting it fall through would fail the assertions below and read as a product regression.
			Assert.IsNull(holderSetupFailure, $"the holder thread never held the clipboard with text on it, so there is nothing to test: {holderSetupFailure}");

			// Confirm the contention is real rather than assuming it, so this cannot quietly become a
			// test that passes because nothing was ever holding the clipboard.
			if (Win32.OpenClipboard(IntPtr.Zero))
			{
				Win32.CloseClipboard();
				Assert.Fail("expected the clipboard to be contended, but it could be opened");
			}
			else
			{
				Assert.AreEqual(Win32.ERROR_ACCESS_DENIED, Marshal.GetLastWin32Error(), "expected the clipboard to be denied while another thread holds it");
			}

			var view = Clipboard.GetContent();

			Assert.IsNotNull(view, "GetContent returned null while the clipboard was contended");
			Assert.IsTrue(
				view.Contains(StandardDataFormats.Text),
				"the clipboard holds text, so it must be reported as available even while another thread holds the clipboard");
		}
		finally
		{
			release.Set();
			holderExited = holder.Join(TimeSpan.FromSeconds(30));
		}

		// A holder that never exits is still owning the clipboard, which would poison every test after this one.
		Assert.IsTrue(holderExited, "the holder thread did not exit, so it may still be holding the clipboard");

		// And the payload, which does need the lock, must be readable once the contention is over.
		await DelayForClipboard();
		Assert.AreEqual(ContendedText, await Clipboard.GetContent()!.GetTextAsync());
	}
}

/// <summary>
/// The interop is file-local: these entry points only exist on Windows, and the rest of
/// <c>Given_Clipboard</c> is compiled for every Skia head (X11, macOS, wasm, mobile), where
/// calling them would throw <see cref="DllNotFoundException"/>. Keeping them out of the
/// partial class means they cannot be reached from a sibling file by accident.
/// </summary>
file static class Win32
{
	public const int ERROR_ACCESS_DENIED = 5;

	private const uint CF_UNICODETEXT = 13;
	private const uint GMEM_MOVEABLE = 0x0002;
	private const int HWND_MESSAGE = -3;

	/// <summary>
	/// Puts <paramref name="value"/> on the clipboard, which must already be open on this thread.
	/// </summary>
	/// <remarks>
	/// <para>The allocation is freed on every failing path, because <c>SetClipboardData</c> only takes
	/// ownership of the handle on success. Keeping both halves here is the point: a caller that
	/// allocated separately would have to know that rule to avoid leaking.</para>
	/// <para>Each step is checked so that a failure is reported to the test rather than thrown: this
	/// runs on the holder thread, where an exception would take the whole test runner down with it.</para>
	/// </remarks>
	public static bool TrySetUnicodeText(string value, out int error)
	{
		// SetClipboardData requires GMEM_MOVEABLE.
		var handle = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)((value.Length + 1) * sizeof(char)));
		if (handle == IntPtr.Zero)
		{
			error = Marshal.GetLastWin32Error();
			return false;
		}

		var pointer = GlobalLock(handle);
		if (pointer == IntPtr.Zero)
		{
			error = Marshal.GetLastWin32Error();
			GlobalFree(handle);

			return false;
		}

		Marshal.Copy(value.ToCharArray(), 0, pointer, value.Length);
		Marshal.WriteInt16(pointer, value.Length * sizeof(char), 0);
		GlobalUnlock(handle);

		if (SetClipboardData(CF_UNICODETEXT, handle) != IntPtr.Zero)
		{
			error = 0;
			return true;
		}

		error = Marshal.GetLastWin32Error();
		GlobalFree(handle);

		return false;
	}

	/// <summary>
	/// A thread-owned window handle, which is what makes <c>OpenClipboard</c> exclusive.
	/// </summary>
	/// <remarks>
	/// A built-in class already has a native window procedure, so nothing here has to outlive the
	/// window: registering a class would mean handing user32 a pointer into a managed delegate that
	/// the GC is free to collect the moment this returns, and a class that can never be unregistered.
	/// </remarks>
	public static IntPtr CreateMessageOnlyWindow()
		=> CreateWindowEx(0, "STATIC", null, 0, 0, 0, 0, 0, (IntPtr)HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool OpenClipboard(IntPtr hWndNewOwner);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool CloseClipboard();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool EmptyClipboard();

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool DestroyWindow(IntPtr hwnd);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern IntPtr CreateWindowEx(uint exStyle, string className, string? windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GlobalFree(IntPtr handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GlobalLock(IntPtr handle);

	[DllImport("kernel32.dll")]
	private static extern bool GlobalUnlock(IntPtr handle);
}
#endif
