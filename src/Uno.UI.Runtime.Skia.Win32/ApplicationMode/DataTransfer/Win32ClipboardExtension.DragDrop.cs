using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;
using Buffer = System.Buffer;

namespace Uno.UI.Runtime.Skia.Win32;

// Clipboard-format readers consumed by Win32DragDropExtension, which feeds them HGLOBALs
// coming from an IDataObject instead of the clipboard.
internal partial class Win32ClipboardExtension
{
	internal static void ReadContentIntoPackage(DataPackage package, IEnumerable<CLIPBOARD_FORMAT> formats, Func<CLIPBOARD_FORMAT, HGLOBAL?> dataGetter)
	{
		foreach (var format in formats)
		{
			if (Enum.IsDefined((CLIPBOARD_FORMAT)format) && dataGetter(format) is { } handle)
			{
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
	}
	private static unsafe void GetText(HGLOBAL handle, DataPackage package)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var bytes);
		if (lockDisposable is null)
		{
			return;
		}

		package.SetText(Marshal.PtrToStringUni((IntPtr)bytes)!);
	}
	private static unsafe void GetBitmap(HGLOBAL handle, DataPackage package)
	{
		package.SetDataProvider(StandardDataFormats.Bitmap, _ =>
		{
			using var lockDisposable = Win32Helper.GlobalLock(handle, out var dib);
			if (lockDisposable is null)
			{
				return Task.FromException<object>(new InvalidOperationException($"{nameof(PInvoke.GlobalLock)} failed: {Win32Helper.GetErrorMessage()}"));
			}

			var memSize = (uint)PInvoke.GlobalSize(handle);
			if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
			{
				return Task.FromException<object>(new InvalidOperationException($"{nameof(PInvoke.GlobalSize)} returned {memSize}: {Win32Helper.GetErrorMessage()}"));
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

			return Task.FromResult<object>(RandomAccessStreamReference.CreateFromStream(new MemoryStream(arr).AsRandomAccessStream()));
		});
	}
	internal static unsafe List<IStorageItem>? GetFileDropList(HGLOBAL handle)
	{
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

		var files = new List<IStorageItem>((int)filesDropped);
		for (uint i = 0; i < filesDropped; i++)
		{
			var charLength = PInvoke.DragQueryFile(hDrop, i, new PWSTR(), 0);
			if (charLength == 0)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying buffer length: {Win32Helper.GetErrorMessage()}");
				continue;
			}
			charLength++; // + 1 for \0

			var buffer = Marshal.AllocHGlobal((IntPtr)(charLength * Unsafe.SizeOf<char>()));
			using var bufferDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, buffer);
			var charsWritten = PInvoke.DragQueryFile(hDrop, i, new PWSTR((char*)buffer), charLength);
			if (charsWritten == 0)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying file path: {Win32Helper.GetErrorMessage()}");
				break;
			}
			var filePath = Marshal.PtrToStringUni(buffer, (int)charsWritten);
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
				typeof(Win32ClipboardExtension).LogError()?.Error($"HDROP Clipboard: file path '{filePath}' was not a valid file or directory.");
			}
		}

		return files;
	}
}
