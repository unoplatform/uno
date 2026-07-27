// Copyright (C) 1997-2024 Sam Lantinga <slouken@libsdl.org>
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
//
// 	Permission is granted to anyone to use this software for any purpose,
// 	including commercial applications, and to alter it and redistribute it
// 	freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
// 	claim that you wrote the original software. If you use this software
// 	in a product, an acknowledgment in the product documentation would be
// 	appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
// misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

// https://github.com/libsdl-org/SDL/blob/9f8157f42cc0351833c030febe8a559719c875bd/src/video/windows/SDL_windowsclipboard.c

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using SkiaSharp;
using Uno.ApplicationModel.DataTransfer;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Buffer = System.Buffer;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32ClipboardExtension : IClipboardExtension
{
	private const ulong MaxClipboardFormatBytes = 32ul * 1024 * 1024;
	private const ulong MaxClipboardSnapshotBytes = 64ul * 1024 * 1024;
	private const int MaxClipboardSnapshotFormats = 128;
	private const uint MaxFileDropItems = 4096;
	private const uint MaxFileDropPathCharacters = 32_767;
	private const uint MaxFileDropTotalCharacters = 1_048_576;
	// DROPFILES is a fixed 20-byte Win32 header followed by the path payload.
	private const int DropFilesHeaderSize = 20;

	public static Win32ClipboardExtension Instance { get; } = new();


	private static readonly Dictionary<string, (PointerToString FromPointer, StringToBytes ToBytes)> _knownTextBasedClipboardFormats = new()
	{
		["HTML Format"] = (GetUtf8String, GetUtf8Bytes), // HTML fragment with header metadata
		["Rich Text Format"] = (GetAnsiString, GetAnsiBytes), // RTF document
		["Rich Text & Unicode"] = (GetUnicodeString, GetUnicodeBytes), // RTF with Unicode support
		["Rich Text Format Without Objects"] = (GetAnsiString, GetAnsiBytes), // RTF without embedded objects
		["XML Spreadsheet"] = (GetUtf8String, GetUtf8Bytes), // Excel XML format
		["CSV"] = (GetAnsiString, GetAnsiBytes), // Comma-separated values
		["Csv"] = (GetAnsiString, GetAnsiBytes), // Alternate CSV registration (Excel)
		["MIME:text/plain"] = (GetUtf8String, GetUtf8Bytes), // Plain text via MIME
		["MIME:text/html"] = (GetUtf8String, GetUtf8Bytes), // HTML via MIME
		["text/html"] = (GetUtf8String, GetUtf8Bytes), // Raw HTML (Chromium/browsers)
		["text/plain"] = (GetUtf8String, GetUtf8Bytes), // Raw plain text (Chromium/browsers)
		["text/uri-list"] = (GetUtf8String, GetUtf8Bytes), // Newline-separated URIs
		["UniformResourceLocator"] = (GetAnsiString, GetAnsiBytes), // Single URL
		["UniformResourceLocatorW"] = (GetUnicodeString, GetUnicodeBytes), // Single URL (wide)
		["FileName"] = (GetAnsiString, GetAnsiBytes), // File path
		["FileNameW"] = (GetUnicodeString, GetUnicodeBytes), // File path (wide)
	};
	private static readonly Lazy<Encoding> _ansiEncoding = new(() =>
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		return Encoding.GetEncoding(0);
	});
	private static readonly Lazy<Encoding> _oemEncoding = new(() =>
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		using var codePageKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Nls\CodePage");
		if (codePageKey?.GetValue("OEMCP") is not string value || !int.TryParse(value, out var codePage))
		{
			throw new InvalidOperationException("Unable to determine the Windows OEM code page.");
		}
		return Encoding.GetEncoding(codePage);
	});

	// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private readonly WNDCLASSEXW _windowClass;
	private readonly ThreadLocal<HWND> _clipboardWindows;

	private int _listenerRegistered;
	private bool _observeContentChanged;
	private DataPackage? _currentPackage;

	private unsafe Win32ClipboardExtension()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String("UnoPlatformClipboardWindow");

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = &WndProc,
			hInstance = Win32Helper.GetHInstance(),
			lpszClassName = lpClassName,
		};

		var classAtom = PInvoke.RegisterClassEx(_windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		_clipboardWindows = new ThreadLocal<HWND>(CreateClipboardWindow);
		_ = _clipboardWindows.Value;
	}

	private unsafe HWND CreateClipboardWindow()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String("UnoPlatformClipboardWindow");
		using var windowTitle = new Win32Helper.NativeNulTerminatedUtf16String("");
		var hwnd = PInvoke.CreateWindowEx(
			0,
			lpClassName,
			windowTitle,
			WINDOW_STYLE.WS_OVERLAPPED,
			0,
			0,
			0,
			0,
			HWND.HWND_MESSAGE,
			HMENU.Null,
			Win32Helper.GetHInstance(),
			null);

		if (hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		if (Interlocked.CompareExchange(ref _listenerRegistered, 1, 0) == 0)
		{
			var success = PInvoke.AddClipboardFormatListener(hwnd);
			if (!success)
			{
				Volatile.Write(ref _listenerRegistered, 0);
				this.LogError()?.Error($"{nameof(PInvoke.AddClipboardFormatListener)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}

		return hwnd;
	}

	private HWND GetClipboardWindow() => _clipboardWindows.Value;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	internal static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		try
		{
			if (msg is PInvoke.WM_CLIPBOARDUPDATE)
			{
				Instance._currentPackage = null;
				if (Instance._observeContentChanged)
				{
					Instance.ContentChanged?.Invoke(Instance, EventArgs.Empty);
				}
				return new LRESULT(0);
			}
		}
		catch (Exception e)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"Exception in {nameof(WndProc)}", e);
		}
		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	public event EventHandler<object>? ContentChanged;

	public void StartContentChanged() => _observeContentChanged = true;

	public void StopContentChanged() => _observeContentChanged = false;

	public void Clear()
	{
		using (var clipboardDisposable = new ClipboardDisposable(GetClipboardWindow(), true))
		{
			if (!clipboardDisposable.IsOpen)
			{
				return;
			}
		}

		_currentPackage = new DataPackage();
	}

	public void Flush() { }

	private static string GetClipboardFormatName(CLIPBOARD_FORMAT format) =>
		Enum.GetName(format) ?? // cant call GetClipboardFormatName on these
		GetClipboardFormatNameCore(format) ??
		format.ToString();

	private static unsafe string? GetClipboardFormatNameCore(CLIPBOARD_FORMAT format)
	{
		const int MAX_PATH = 260;
		const int BufferSize = MAX_PATH + 1;

		var buffer = Marshal.AllocHGlobal((IntPtr)(BufferSize * Unsafe.SizeOf<char>()));
		using var bufferDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, buffer);
		var length = PInvoke.GetClipboardFormatName((uint)format, new PWSTR((char*)buffer), BufferSize);
		if (length == 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GetClipboardFormatName)} failed (format={format}): {Win32Helper.GetErrorMessage()} ");
			return null;
		}

		return Marshal.PtrToStringUni(buffer);
	}

	private readonly ref struct ClipboardDisposable
	{
		private readonly bool _shouldClose;
		public bool IsOpen => _shouldClose;

		public ClipboardDisposable(HWND hwnd, bool ownClipboard)
		{
			var opened = false;
			for (var attempt = 0; attempt < 20 && !opened; attempt++)
			{
				opened = PInvoke.OpenClipboard(hwnd);
				if (!opened && attempt < 19)
				{
					Thread.Sleep(10);
				}
			}

			_shouldClose = opened;
			if (!_shouldClose) { typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.OpenClipboard)} failed: {Win32Helper.GetErrorMessage()}"); }
			if (ownClipboard && _shouldClose)
			{
				var success = PInvoke.EmptyClipboard();
				if (!success) { typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.EmptyClipboard)} failed: {Win32Helper.GetErrorMessage()}"); }
			}
		}

		public void Dispose()
		{
			if (_shouldClose)
			{
				var success = PInvoke.CloseClipboard();
				if (!success) { typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.CloseClipboard)} failed: {Win32Helper.GetErrorMessage()}"); }
			}
		}
	}
	private delegate string? PointerToString(nint p, int byteLength);
	private delegate byte[] StringToBytes(string s);

	private static unsafe string? GetAnsiString(nint pointer, int byteLength)
		=> GetNullTerminatedString((byte*)pointer, byteLength, _ansiEncoding.Value);

	private static unsafe string? GetUtf8String(nint pointer, int byteLength)
		=> GetNullTerminatedString((byte*)pointer, byteLength, Encoding.UTF8);

	private static unsafe string? GetUnicodeString(nint pointer, int byteLength)
	{
		if (byteLength < sizeof(char) || (byteLength & 1) != 0)
		{
			return null;
		}

		var bytes = new ReadOnlySpan<byte>((void*)pointer, byteLength);
		for (var index = 0; index < byteLength; index += sizeof(char))
		{
			if (bytes[index] == 0 && bytes[index + 1] == 0)
			{
				return Encoding.Unicode.GetString(bytes[..index]);
			}
		}

		return null;
	}

	private static unsafe string? GetNullTerminatedString(byte* pointer, int byteLength, Encoding encoding)
	{
		if (byteLength <= 0)
		{
			return string.Empty;
		}

		var bytes = new ReadOnlySpan<byte>(pointer, byteLength);
		var terminator = bytes.IndexOf((byte)0);
		return terminator >= 0 ? encoding.GetString(bytes[..terminator]) : null;
	}

	private static byte[] GetAnsiBytes(string value) => _ansiEncoding.Value.GetBytes(value + '\0');

	private static byte[] GetUtf8Bytes(string value) => Encoding.UTF8.GetBytes(value + '\0');

	private static byte[] GetUnicodeBytes(string value) => Encoding.Unicode.GetBytes(value + '\0');
}

partial class Win32ClipboardExtension // from clipboard
{
	public DataPackageView GetContent()
	{
		if (_currentPackage is null)
		{
			_currentPackage = GetContentPackage();
		}

		return _currentPackage.GetView();
	}

	private static DataPackage GetContentPackage()
	{
		var package = new DataPackage();

		using var clipboardDisposable = new ClipboardDisposable(Instance.GetClipboardWindow(), false);
		if (!clipboardDisposable.IsOpen)
		{
			return package;
		}

		var formats = new List<CLIPBOARD_FORMAT>();
		var formatLimitReached = false;
		for (uint lastFormat = 0; (lastFormat = PInvoke.EnumClipboardFormats(lastFormat)) != 0;)
		{
			if (formats.Count >= MaxClipboardSnapshotFormats)
			{
				formatLimitReached = true;
				typeof(Win32ClipboardExtension).LogError()?.Error($"Clipboard contains more than {MaxClipboardSnapshotFormats} formats; remaining formats were ignored.");
				break;
			}

			formats.Add((CLIPBOARD_FORMAT)lastFormat);
		}

		if (!formatLimitReached && Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_SUCCESS)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.EnumClipboardFormats)} failed: {Win32Helper.GetErrorMessage()}");
			return package;
		}

		ulong snapshotBytes = 0;
		foreach (var format in formats)
		{
			var loader = (Action<DataPackage, CLIPBOARD_FORMAT, HGLOBAL>?)(format switch
			{
				// https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats#constants
				// https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#synthesized-clipboard-formats

				// synthesized text formats
				CLIPBOARD_FORMAT.CF_TEXT => null,
				CLIPBOARD_FORMAT.CF_LOCALE => GetUnknownData, // 4 bytes CultureInfo.LCID
				CLIPBOARD_FORMAT.CF_UNICODETEXT => GetText,
				CLIPBOARD_FORMAT.CF_OEMTEXT => GetOemText,

				// synthesized image formats
				CLIPBOARD_FORMAT.CF_BITMAP => null, // Windows synthesizes CF_DIB from CF_BITMAP; handled below
				CLIPBOARD_FORMAT.CF_DIB => GetDib,
				CLIPBOARD_FORMAT.CF_DIBV5 => null,
				CLIPBOARD_FORMAT.CF_PALETTE => null,

				// synthesized meta-file formats
				CLIPBOARD_FORMAT.CF_METAFILEPICT => null,
				CLIPBOARD_FORMAT.CF_ENHMETAFILE => null,

				CLIPBOARD_FORMAT.CF_HDROP => null,

				CLIPBOARD_FORMAT.CF_SYLK => null,
				CLIPBOARD_FORMAT.CF_DIF => null,
				CLIPBOARD_FORMAT.CF_TIFF => null,
				CLIPBOARD_FORMAT.CF_PENDATA => null,
				CLIPBOARD_FORMAT.CF_RIFF => null,
				CLIPBOARD_FORMAT.CF_WAVE => null,

				_ => GetUnknownData,
			});
			if (loader is { })
			{
				// GetClipboardData must be called here, and not within async-func of SetDataProvider,
				// or it will throw: Thread does not have a clipboard open
				var handle = PInvoke.GetClipboardData((uint)format);
				if (handle == default)
				{
					typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
					continue;
				}

				var formatBytes = (ulong)PInvoke.GlobalSize((HGLOBAL)(IntPtr)handle);
				if (formatBytes == 0)
				{
					typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalSize)} failed for clipboard format {(uint)format}: {Win32Helper.GetErrorMessage()}");
					continue;
				}

				if (formatBytes > MaxClipboardFormatBytes || snapshotBytes > MaxClipboardSnapshotBytes - formatBytes)
				{
					typeof(Win32ClipboardExtension).LogError()?.Error($"Clipboard format {(uint)format} exceeds the clipboard snapshot budget and was ignored.");
					continue;
				}

				snapshotBytes += formatBytes;
				loader.Invoke(package, format, (HGLOBAL)(IntPtr)handle);
			}
		}

		return package;
	}
	private static unsafe void GetText(DataPackage package, CLIPBOARD_FORMAT format, HGLOBAL handle)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr);
		if (lockDisposable is null) return;

		var byteLength = checked((int)PInvoke.GlobalSize(handle));
		if (GetUnicodeString((IntPtr)ptr, byteLength) is { } text)
		{
			package.SetText(text);
		}
		else
		{
			typeof(Win32ClipboardExtension).LogError()?.Error("Clipboard Unicode text is not null-terminated within its allocation.");
		}
	}
	private static unsafe void GetOemText(DataPackage package, CLIPBOARD_FORMAT format, HGLOBAL handle)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr);
		if (lockDisposable is null) return;

		var byteLength = checked((int)PInvoke.GlobalSize(handle));
		if (GetNullTerminatedString((byte*)ptr, byteLength, _oemEncoding.Value) is { } text)
		{
			package.SetData(GetClipboardFormatName(format), text);
		}
		else
		{
			typeof(Win32ClipboardExtension).LogError()?.Error("Clipboard OEM text is not null-terminated within its allocation.");
		}
	}
#if false // this would require System.Drawing.Common
	private static void GetBitmap(DataPackage package, CLIPBOARD_FORMAT format, HGLOBAL handle) => package
		.SetDataProvider(StandardDataFormats.Bitmap, _ =>
		{
			// CF_BITMAP handle is an HBITMAP, not an HGLOBAL — GlobalLock must not be called on it.

			var image = Image.FromHbitmap(handle);

			var ras = new InMemoryRandomAccessStream();
			var stream = ras.AsStreamForWrite(); // dont dispose
			{
				image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
				stream.Flush(); // without this, only the file header is written
				stream.Position = 0;
			}

			return Task.FromResult<object>(RandomAccessStreamReference.CreateFromStream(ras));
		});
#endif
	private static unsafe void GetDib(DataPackage package, CLIPBOARD_FORMAT format, HGLOBAL handle)
	{
		// we are remapping CF_DIB to bitmap, since there is no good way to load from CF_BITMAP which contains an HBITMAP
		// normally, this would've been mapped to "DeviceIndependentBitmap"
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr, logLastError: false);
		if (lockDisposable is null)
		{
			return;
		}

		var memSize = (uint)PInvoke.GlobalSize((HGLOBAL)(IntPtr)handle);
		if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalSize)} returned {memSize}: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var srcBitmapInfo = (BITMAPINFO*)ptr;

		// https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader#color-tables
		int colorTableSize = srcBitmapInfo->bmiHeader.biCompression switch
		{
			// BI_RGB
			0 when srcBitmapInfo->bmiHeader.biBitCount <= 8 => Marshal.SizeOf<RGBQUAD>() * (srcBitmapInfo->bmiHeader.biClrUsed == 0 ? 1 << srcBitmapInfo->bmiHeader.biBitCount : (int)srcBitmapInfo->bmiHeader.biClrUsed),
			0 => 0,
			// BI_BITFIELDS
			3 => 3 * Marshal.SizeOf<uint>(),
			// FOURCC
			_ => Marshal.SizeOf<RGBQUAD>() * (int)srcBitmapInfo->bmiHeader.biClrUsed
		};

		BITMAPFILEHEADER bitmapfileheader = new BITMAPFILEHEADER
		{
			bfType = /* BM */ 0x4d42,
			bfSize = (uint)(Marshal.SizeOf<BITMAPFILEHEADER>() + memSize),
			bfOffBits = (uint)(Marshal.SizeOf<BITMAPFILEHEADER>() + Marshal.SizeOf<BITMAPINFOHEADER>() + colorTableSize)
		};

		var bmpSize = Marshal.SizeOf<BITMAPFILEHEADER>() + (int)memSize;
		var arr = new byte[bmpSize];
		fixed (byte* bmp = arr)
		{
			Buffer.MemoryCopy(&bitmapfileheader, bmp, bmpSize, Marshal.SizeOf<BITMAPFILEHEADER>());
			Buffer.MemoryCopy(ptr, bmp + Marshal.SizeOf<BITMAPFILEHEADER>(), memSize, memSize);
		}

		package.SetBitmap(RandomAccessStreamReference.CreateFromStream(new MemoryStream(arr).AsRandomAccessStream()));
	}
	private static unsafe void GetUnknownData(DataPackage package, CLIPBOARD_FORMAT format, HGLOBAL handle)
	{
		var name = GetClipboardFormatName(format);

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr, logLastError: false);
		if (lockDisposable is null)
		{
			return;
		}

		var size = PInvoke.GlobalSize((HGLOBAL)(IntPtr)handle);
		if (size == 0 || size > int.MaxValue)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalSize)} returned {size}: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var bufferLength = checked((int)size);

		// WinUI detects certain named formats as strings. HGLOBAL itself carries no type metadata,
		// so use the same allowlist while the clipboard-owned handle is still valid.
		if (_knownTextBasedClipboardFormats.TryGetValue(name, out var marshaler))
		{
			if (marshaler.FromPointer.Invoke((IntPtr)ptr, bufferLength) is { } text)
			{
				package.SetData(name, text);
			}
			else
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"Clipboard text format '{name}' is not null-terminated within its allocation.");
			}
			return;
		}

		var buffer = new byte[bufferLength];
		fixed (byte* pBuffer = buffer)
		{
			System.Buffer.MemoryCopy(ptr, pBuffer, bufferLength, bufferLength);
		}

		package.SetData(name, new MemoryStream(buffer).AsRandomAccessStream());
	}

	internal static void ReadContentIntoPackage(DataPackage package, IEnumerable<CLIPBOARD_FORMAT> formats, Func<CLIPBOARD_FORMAT, HGLOBAL?> dataGetter)
	{
		ulong snapshotBytes = 0;
		var formatCount = 0;
		foreach (var format in formats)
		{
			if (++formatCount > MaxClipboardSnapshotFormats)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"Data transfer contains more than {MaxClipboardSnapshotFormats} formats; remaining formats were ignored.");
				break;
			}

			if (!Enum.IsDefined((CLIPBOARD_FORMAT)format) || dataGetter(format) is not { } handle)
			{
				continue;
			}

			var formatBytes = (ulong)PInvoke.GlobalSize(handle);
			if (formatBytes == 0 || formatBytes > MaxClipboardFormatBytes || snapshotBytes > MaxClipboardSnapshotBytes - formatBytes)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"Data transfer format {(uint)format} exceeds the snapshot budget and was ignored.");
				continue;
			}

			snapshotBytes += formatBytes;
			switch (format)
			{
				case CLIPBOARD_FORMAT.CF_UNICODETEXT:
					GetText(handle, package);
					break;
				case CLIPBOARD_FORMAT.CF_HDROP:
					var files = GetFileDropList(handle);
					if (files is not null)
					{
						package.SetStorageItems(files);
					}
					break;
				case CLIPBOARD_FORMAT.CF_DIB:
					GetBitmap(handle, package);
					break;
			}
		}
	}
	private static unsafe void GetText(HGLOBAL handle, DataPackage package)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var bytes);
		if (lockDisposable is null)
		{
			return;
		}

		var byteLength = checked((int)PInvoke.GlobalSize(handle));
		if (GetUnicodeString((IntPtr)bytes, byteLength) is { } text)
		{
			package.SetText(text);
		}
		else
		{
			typeof(Win32ClipboardExtension).LogError()?.Error("Transferred Unicode text is not null-terminated within its allocation.");
		}
	}
	private static unsafe void GetBitmap(HGLOBAL handle, DataPackage package)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var dib);
		if (lockDisposable is null)
		{
			return;
		}

		var memSize = (uint)PInvoke.GlobalSize(handle);
		if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalSize)} returned {memSize}: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var srcBitmapInfo = (BITMAPINFO*)dib;

		// https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader#color-tables
		int colorTableSize = srcBitmapInfo->bmiHeader.biCompression switch
		{
			// BI_RGB
			0 when srcBitmapInfo->bmiHeader.biBitCount <= 8 => Marshal.SizeOf<RGBQUAD>() * (srcBitmapInfo->bmiHeader.biClrUsed == 0 ? 1 << srcBitmapInfo->bmiHeader.biBitCount : (int)srcBitmapInfo->bmiHeader.biClrUsed),
			0 => 0,
			// BI_BITFIELDS
			3 => 3 * Marshal.SizeOf<uint>(),
			// FOURCC
			_ => Marshal.SizeOf<RGBQUAD>() * (int)srcBitmapInfo->bmiHeader.biClrUsed
		};

		BITMAPFILEHEADER bitmapfileheader = new BITMAPFILEHEADER
		{
			bfType = /* BM */ 0x4d42,
			bfSize = (uint)(Marshal.SizeOf<BITMAPFILEHEADER>() + memSize),
			bfOffBits = (uint)(Marshal.SizeOf<BITMAPFILEHEADER>() + Marshal.SizeOf<BITMAPINFOHEADER>() + colorTableSize)
		};

		var bmpSize = (uint)(Marshal.SizeOf<BITMAPFILEHEADER>() + memSize);
		var arr = new byte[bmpSize];
		fixed (byte* bmp = arr)
		{
			Buffer.MemoryCopy(&bitmapfileheader, bmp, bmpSize, Marshal.SizeOf<BITMAPFILEHEADER>());
			Buffer.MemoryCopy(dib, bmp + Marshal.SizeOf<BITMAPFILEHEADER>(), bmpSize - Marshal.SizeOf<BITMAPFILEHEADER>(), bmpSize - Marshal.SizeOf<BITMAPFILEHEADER>());
		}

		package.SetBitmap(RandomAccessStreamReference.CreateFromStream(new MemoryStream(arr).AsRandomAccessStream()));
	}
	internal static unsafe List<string>? GetFileDropPaths(HGLOBAL handle)
	{
		var allocationSize = (ulong)PInvoke.GlobalSize(handle);
		if (allocationSize < DropFilesHeaderSize || allocationSize > MaxClipboardFormatBytes)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"The HDROP allocation size {allocationSize} is invalid.");
			return null;
		}

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var firstByte);
		if (lockDisposable is null)
		{
			return null;
		}

		var hDrop = new HDROP((IntPtr)firstByte);

		var filesDropped = PInvoke.DragQueryFile(hDrop, 0xFFFFFFFF, new PWSTR(), 0);
		if (filesDropped == 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying total count: {Win32Helper.GetErrorMessage()}");
			return null;
		}

		if (filesDropped > MaxFileDropItems)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"HDROP contains more than {MaxFileDropItems} items.");
			return null;
		}

		var paths = new List<string>((int)filesDropped);
		uint totalCharacters = 0;
		for (uint i = 0; i < filesDropped; i++)
		{
			var charLength = PInvoke.DragQueryFile(hDrop, i, new PWSTR(), 0);
			if (charLength == 0)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying buffer length: {Win32Helper.GetErrorMessage()}");
				continue;
			}

			if (charLength > MaxFileDropPathCharacters || totalCharacters > MaxFileDropTotalCharacters - charLength)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error("HDROP exceeds the file-path character budget.");
				return null;
			}
			totalCharacters += charLength;

			var bufferLength = charLength + 1; // + 1 for \0
			var buffer = Marshal.AllocHGlobal((IntPtr)(bufferLength * Unsafe.SizeOf<char>()));
			using var bufferDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, buffer);
			var charsWritten = PInvoke.DragQueryFile(hDrop, i, new PWSTR((char*)buffer), bufferLength);
			if (charsWritten == 0)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying file path: {Win32Helper.GetErrorMessage()}");
				return null;
			}

			paths.Add(Marshal.PtrToStringUni(buffer, (int)charsWritten));
		}

		return paths;
	}

	internal static List<IStorageItem>? GetFileDropList(HGLOBAL handle)
	{
		var paths = GetFileDropPaths(handle);
		if (paths is null)
		{
			return null;
		}

		var files = new List<IStorageItem>(paths.Count);
		var invalidPathCount = 0;
		foreach (var filePath in paths)
		{
			if (Directory.Exists(filePath))
			{
				files.Add(new StorageFolder(filePath));
			}
			else if (File.Exists(filePath))
			{
				files.Add(StorageFile.GetFileFromPath(filePath));
			}
			else
			{
				invalidPathCount++;
			}
		}

		if (invalidPathCount > 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"HDROP contained {invalidPathCount} invalid file or directory paths.");
		}

		return files;
	}
}

partial class Win32ClipboardExtension // to clipboard
{
	public void SetContent(DataPackage content)
	{
		using (var clipboardDisposable = new ClipboardDisposable(GetClipboardWindow(), true))
		{
			if (!clipboardDisposable.IsOpen)
			{
				return;
			}

			var view = content.GetView();
			foreach (var format in view.AvailableFormats)
			{
				var setter = (Action<DataPackageView, string>?)(format switch
				{
					_ when format == StandardDataFormats.Text => SetText,
					_ when format == StandardDataFormats.Bitmap => SetBitmap,
					_ => SetUnknownData,
				});
				setter?.Invoke(view, format);
			}
		}

		_currentPackage = content;
	}
	private static void SetText(DataPackageView view, string format)
	{
		var task = view.GetTextAsync().AsTask();
		while (!task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task.IsCompletedSuccessfully)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(view.GetTextAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var str = task.Result;
		var bytes = new byte[(str.Length + 1) * sizeof(char)]; // +1 char: last 2 bytes remain 0 as null terminator
		MemoryMarshal.Cast<char, byte>(str.AsSpan()).CopyTo(bytes);
		SetClipboardData(CLIPBOARD_FORMAT.CF_UNICODETEXT, bytes);
	}
	private static unsafe void SetBitmap(DataPackageView view, string format)
	{
		var task = view.GetBitmapAsync().AsTask();
		while (!task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task.IsCompletedSuccessfully)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(view.GetBitmapAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var task2 = task.Result.OpenReadAsync().AsTask();
		while (!task2.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task2.IsCompletedSuccessfully)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(RandomAccessStreamReference.OpenReadAsync)} failed to fetch data to be copied to the clipboard: {task2.Status}", task2.Exception);
			return;
		}

		var stream = task2.Result;
		Debug.Assert(stream.CanRead);
		stream.Seek(0);

#if false
		// this would require System.Drawing.Common
		var readStream = stream.AsStreamForRead();

		var bitmap = new Bitmap(readStream);
		var handle = (HBITMAP)bitmap.GetHbitmap();

		SetClipboardHBitmapData(CLIPBOARD_FORMAT.CF_BITMAP, handle);
#else
		// since we couldn't create a HBITMAP here, we will just write CF_DIB data directly to the clipboard,
		// which Windows will synthesize CF_BITMAP for us.

		var size = stream.Size;
		if (size > int.MaxValue)
		{
			throw new InvalidOperationException("Clipboard bitmap data is too large to be processed.");
		}

		var bytes = new byte[(int)size];
		stream.AsStreamForRead().ReadExactly(bytes);

		// check for 'BM' file signature, if we got a bitmap image, we can just strip the header and send the pixel data (in DIB format)
		if (bytes.Length > Marshal.SizeOf<BITMAPFILEHEADER>() &&
			bytes[0] == 'B' && bytes[1] == 'M')
		{
			SetClipboardData(CLIPBOARD_FORMAT.CF_DIB, bytes.AsSpan(/* start after: */ Marshal.SizeOf<BITMAPFILEHEADER>()));
		}
		else
		{
			// Unknown image format — decode via SkiaSharp and convert to CF_DIB
			using var skBitmap = SKBitmap.Decode(bytes);
			if (skBitmap is null)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error("SetBitmap: SkiaSharp failed to decode image.");
				return;
			}

			// Ensure BGRA8888 so pixel layout matches what CF_DIB BI_RGB 32bpp expects (BGRX, alpha ignored)
			using var bgra = skBitmap.ColorType == SKColorType.Bgra8888
				? null
				: skBitmap.Copy(SKColorType.Bgra8888);
			var src = bgra ?? skBitmap;

			var width = src.Width;
			var height = src.Height;
			var stride = width * 4;
			var headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
			var pixelDataSize = stride * height;
			var dib = new byte[headerSize + pixelDataSize];

			fixed (byte* pDib = dib)
			{
				var header = (BITMAPINFOHEADER*)pDib;
				header->biSize = (uint)headerSize;
				header->biWidth = width;
				header->biHeight = height; // positive = bottom-up storage
				header->biPlanes = 1;
				header->biBitCount = 32;
				header->biCompression = 0; // BI_RGB
				header->biSizeImage = (uint)pixelDataSize;

				// SkiaSharp rows are top-down; CF_DIB with positive biHeight expects bottom-up
				var pixelSrc = (byte*)src.GetPixels();
				var pixelDst = pDib + headerSize;
				for (var row = 0; row < height; row++)
				{
					Buffer.MemoryCopy(
						pixelSrc + (long)(height - 1 - row) * stride,
						pixelDst + (long)row * stride,
						stride, stride);
				}
			}

			SetClipboardData(CLIPBOARD_FORMAT.CF_DIB, dib);
		}
#endif
	}
	private static void SetUnknownData(DataPackageView view, string format)
	{
		if (!WaitForAsyncOperation(view.GetDataAsync(format), out var task))
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(view.GetDataAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception!);
			return;
		}

		var cfid = (CLIPBOARD_FORMAT)PInvoke.RegisterClipboardFormat(format);
		if (cfid == 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.RegisterClipboardFormat)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		if (task.Result is IRandomAccessStream ras)
		{
			ras.Seek(0);
			var size = ras.Size;
			if (size > int.MaxValue)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"Clipboard data for format '{format}' is too large to copy (size={size} bytes).");
				return;
			}

			var bytes = new byte[checked((int)size)];
			ras.AsStreamForRead().ReadExactly(bytes);

			SetClipboardData(cfid, bytes);
		}
		else if (task.Result is string str)
		{
			var bytes = _knownTextBasedClipboardFormats.TryGetValue(format, out var marshaler)
				? marshaler.ToBytes(str)
				: GetUnicodeBytes(str);
			SetClipboardData(cfid, bytes);
		}
	}

	private static unsafe void SetClipboardData(CLIPBOARD_FORMAT format, ReadOnlySpan<byte> data)
	{
		// If the hMem parameter identifies a memory object, the object must have been allocated using the function with the GMEM_MOVEABLE flag
		var handle = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (UIntPtr)data.Length);
		if (handle == IntPtr.Zero)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalAlloc)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var transferred = false;
		try
		{
			using (var lockDisposable = Win32Helper.GlobalLock(handle, out var dst))
			{
				if (lockDisposable is null)
				{
					return;
				}

				fixed (byte* src = &MemoryMarshal.GetReference(data))
				{
					Buffer.MemoryCopy(src, dst, data.Length, data.Length);
				}
			}

			var result = PInvoke.SetClipboardData((uint)format, new HANDLE(handle));
			if (result == HANDLE.Null)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
			}
			else
			{
				// If SetClipboardData succeeds, the system owns the object identified by the hMem parameter.
				// The application may not write to or free the data once ownership has been transferred to the system.
				transferred = true;
			}
		}
		finally
		{
			if (!transferred && PInvoke.GlobalFree(handle) != IntPtr.Zero)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalFree)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}
	private static void SetClipboardHBitmapData(CLIPBOARD_FORMAT format, HBITMAP hbitmap)
	{
		var result = PInvoke.SetClipboardData((uint)format, new HANDLE(hbitmap));
		if (result == HANDLE.Null)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");

			// System did not take ownership — free the HBITMAP ourselves
			PInvoke.DeleteObject(hbitmap);
		}
		else
		{
			// On success the system owns the HBITMAP; do not delete it
		}
	}
	private static bool WaitForAsyncOperation<T>(IAsyncOperation<T> operation, out Task<T> task)
	{
		task = operation.AsTask();
		while (!task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		return task.IsCompletedSuccessfully;
	}
}
