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
		var holderTookClipboard = false;
		var holderExited = false;

		var holder = new Thread(() =>
		{
			var hwnd = Win32.CreateMessageOnlyWindow();
			try
			{
				// A NULL owner does NOT exclude other threads; a real window handle does.
				holderTookClipboard = hwnd != IntPtr.Zero && Win32.OpenClipboard(hwnd);
				if (!holderTookClipboard)
				{
					return;
				}

				Win32.EmptyClipboard();
				Win32.SetClipboardData(Win32.CF_UNICODETEXT, Win32.AllocUnicodeText(ContendedText));
			}
			finally
			{
				held.Set();
				if (holderTookClipboard)
				{
					release.Wait(TimeSpan.FromSeconds(30));
					Win32.CloseClipboard();
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
			Assert.IsTrue(holderTookClipboard, "the holder thread could not take the clipboard, so there is nothing to test");

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
	public const uint CF_UNICODETEXT = 13;
	public const int ERROR_ACCESS_DENIED = 5;

	private const uint GMEM_MOVEABLE = 0x0002;
	private const int HWND_MESSAGE = -3;

	public static IntPtr AllocUnicodeText(string value)
	{
		// SetClipboardData requires GMEM_MOVEABLE, and takes ownership on success.
		var handle = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)((value.Length + 1) * sizeof(char)));
		var pointer = GlobalLock(handle);
		Marshal.Copy(value.ToCharArray(), 0, pointer, value.Length);
		Marshal.WriteInt16(pointer, value.Length * sizeof(char), 0);
		GlobalUnlock(handle);

		return handle;
	}

	public static IntPtr CreateMessageOnlyWindow()
	{
		var className = $"UnoClipboardContentionTest{Environment.CurrentManagedThreadId}";
		WndProcDelegate wndProc = DefWindowProc;
		var windowClass = new WNDCLASSEX
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
			lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
			hInstance = GetModuleHandle(null!),
			lpszClassName = className,
		};

		try
		{
			return RegisterClassEx(ref windowClass) is 0
				? IntPtr.Zero
				: CreateWindowEx(0, className, className, 0, 0, 0, 0, 0, (IntPtr)HWND_MESSAGE, IntPtr.Zero, GetModuleHandle(null!), IntPtr.Zero);
		}
		finally
		{
			// The window only has to outlive the clipboard hold, but the delegate must not be collected
			// while the class is registered.
			GC.KeepAlive(wndProc);
		}
	}

	private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WNDCLASSEX
	{
		public uint cbSize;
		public uint style;
		public IntPtr lpfnWndProc;
		public int cbClsExtra;
		public int cbWndExtra;
		public IntPtr hInstance;
		public IntPtr hIcon;
		public IntPtr hCursor;
		public IntPtr hbrBackground;
		[MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
		[MarshalAs(UnmanagedType.LPWStr)] public string? lpszClassName;
		public IntPtr hIconSm;
	}

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool OpenClipboard(IntPtr hWndNewOwner);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool CloseClipboard();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool EmptyClipboard();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEX windowClass);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern IntPtr CreateWindowEx(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr GetModuleHandle(string moduleName);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GlobalLock(IntPtr handle);

	[DllImport("kernel32.dll")]
	private static extern bool GlobalUnlock(IntPtr handle);
}
#endif
