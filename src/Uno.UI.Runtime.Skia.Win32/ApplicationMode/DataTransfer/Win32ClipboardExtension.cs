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
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Uno.ApplicationModel.DataTransfer;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Buffer = System.Buffer;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32ClipboardExtension : IClipboardExtension
{
	// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private readonly WNDCLASSEXW _windowClass;
	private readonly HWND _hwnd;

	private bool _observeContentChanged;
	private DataPackage? _currentPackage;

	public static Win32ClipboardExtension Instance { get; } = new();

	private unsafe Win32ClipboardExtension()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String("UnoPlatformClipboardWindow");
		using var windowTitle = new Win32Helper.NativeNulTerminatedUtf16String("");

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

		_hwnd = PInvoke.CreateWindowEx(
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

		if (_hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		// No need to unregister. This class lasts the lifetime on the app.
		var success = PInvoke.AddClipboardFormatListener(_hwnd);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.AddClipboardFormatListener)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	internal static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
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
		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	public event EventHandler<object>? ContentChanged;

	public void StartContentChanged() => _observeContentChanged = true;

	public void StopContentChanged() => _observeContentChanged = false;

	public void Clear()
	{
		using var clipboardDisposable = new ClipboardDisposable(_hwnd, true);
	}

	public void Flush() { }

	public DataPackageView GetContent()
	{
		if (_currentPackage is null)
		{
			_currentPackage = new DataPackage();

			using var clipboardDisposable = new ClipboardDisposable(_hwnd, false);

			var formats = new List<CLIPBOARD_FORMAT>();
			uint lastFormat = 0;
			while ((lastFormat = PInvoke.EnumClipboardFormats(lastFormat)) != 0)
			{
				formats.Add((CLIPBOARD_FORMAT)lastFormat);
			}

			if (Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_SUCCESS)
			{
				this.LogError()?.Error($"{nameof(PInvoke.EnumClipboardFormats)} failed: {Win32Helper.GetErrorMessage()}");
			}
			else
			{
				ReadContentIntoPackage(_currentPackage, formats, static format =>
				{
					var handle = (HGLOBAL)(IntPtr)PInvoke.GetClipboardData((uint)format);
					if (handle == IntPtr.Zero)
					{
						typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
						return null;
					}

					return handle;
				});
			}
		}
		return _currentPackage.GetView();
	}

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
						GetFileDropList(handle, package);
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

	private static unsafe void GetFileDropList(HGLOBAL handle, DataPackage package)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var firstByte);
		if (lockDisposable is null)
		{
			return;
		}

		var hDrop = new HDROP((IntPtr)firstByte);

		var filesDropped = PInvoke.DragQueryFile(hDrop, 0xFFFFFFFF, new PWSTR(), 0);
		if (filesDropped == 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying total count: {Win32Helper.GetErrorMessage()}");
			return;
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

		package.SetStorageItems(files);
	}

	public void SetContent(DataPackage content)
	{
		using var clipboardDisposable = new ClipboardDisposable(_hwnd, true);

		TrySetText(content);
		TrySetImage(content);
	}

	private unsafe void TrySetText(DataPackage content)
	{
		var view = content.GetView();
		if (content.Contains(StandardDataFormats.Text))
		{
			var task = view.GetTextAsync().AsTask();
			while (!task.IsCompleted)
			{
				Win32EventLoop.RunOnce();
			}

			if (!task.IsCompletedSuccessfully)
			{
				this.LogError()?.Error($"{nameof(view.GetTextAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
				return;
			}

			var str = task.Result;
			fixed (void* srcBytes = &str.GetPinnableReference())
			{
				// If the hMem parameter identifies a memory object, the object must have been allocated using the function with the GMEM_MOVEABLE flag
				var bufferLength = (str.Length + 1) * sizeof(char); // + 1 for \0
				var shouldFree = true;
				using var allocDisposable = Win32Helper.GlobalAlloc(
					GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
					(UIntPtr)bufferLength,
					out var handle,
					// ReSharper disable once AccessToModifiedClosure
					() => shouldFree);

				if (allocDisposable is null)
				{
					return;
				}

				using var lockDisposable = Win32Helper.GlobalLock(handle, out var dstBytes);
				Buffer.MemoryCopy(srcBytes, dstBytes, bufferLength, bufferLength);
				var success = PInvoke.SetClipboardData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT, new HANDLE(handle)) != HANDLE.Null;
				if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}"); }

				// "If SetClipboardData succeeds, the system owns the object identified by the hMem parameter. The application may not write to or free the data once ownership has been transferred to the system"
				shouldFree = !success;
			}
		}
	}

	private unsafe void TrySetImage(DataPackage content)
	{
		if (!content.Contains(StandardDataFormats.Bitmap))
		{
			return;
		}

		var view = content.GetView();
		var task = view.GetBitmapAsync().AsTask();
		while (!task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task.IsCompletedSuccessfully)
		{
			this.LogError()?.Error($"{nameof(view.GetBitmapAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var task2 = task.Result.OpenReadAsync().AsTask();
		while (!task2.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task2.IsCompletedSuccessfully)
		{
			this.LogError()?.Error($"{nameof(RandomAccessStreamReference.OpenReadAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var stream = task2.Result;
		Debug.Assert(stream.CanRead);
		stream.Seek(0);

		var readStream = stream.AsStreamForRead();
		if (readStream.ReadByte() != 0x42 || readStream.ReadByte() != 0x4D)
		{
			this.LogError()?.Error("Failed to copy BMP image to clipboard: invalid BMP format.");
			return;
		}

		// If the hMem parameter identifies a memory object, the object must have been allocated using the function with the GMEM_MOVEABLE flag
		var shouldFree = true;
		using var allocDisposable = Win32Helper.GlobalAlloc(
			GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
			(UIntPtr)(stream.Size - (ulong)sizeof(BITMAPFILEHEADER)),
			out var handle,
			// ReSharper disable once AccessToModifiedClosure
			() => shouldFree);

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var bmp);

		readStream.Seek(sizeof(BITMAPFILEHEADER), SeekOrigin.Begin);
		readStream.ReadExactly(new Span<byte>(bmp, (int)stream.Size - sizeof(BITMAPFILEHEADER)));

		var success = PInvoke.SetClipboardData((uint)CLIPBOARD_FORMAT.CF_DIB, new HANDLE(handle)) != HANDLE.Null;
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}"); }
		// "If SetClipboardData succeeds, the system owns the object identified by the hMem parameter. The application may not write to or free the data once ownership has been transferred to the system"
		shouldFree = !success;
	}

	internal static unsafe HGLOBAL WriteStringToHGlobal(string text)
	{
		fixed (void* srcBytes = &text.GetPinnableReference())
		{
			var bufferLength = (text.Length + 1) * sizeof(char); // + 1 for \0
			var handle = PInvoke.GlobalAlloc(
				GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
				(UIntPtr)bufferLength);

			if (handle == IntPtr.Zero)
			{
				return (HGLOBAL)IntPtr.Zero;
			}

			using var lockDisposable = Win32Helper.GlobalLock(handle, out var dstBytes);
			if (lockDisposable is null)
			{
				PInvoke.GlobalFree(handle);
				return (HGLOBAL)IntPtr.Zero;
			}

			Buffer.MemoryCopy(srcBytes, dstBytes, bufferLength, bufferLength);
			return handle;
		}
	}

	internal static unsafe HGLOBAL WriteFileListToHGlobal(IReadOnlyList<string> filePaths)
	{
		// Calculate the size needed for the file list
		// DROPFILES structure + null-terminated file paths + final null terminator
		var dropFilesSize = Marshal.SizeOf<DROPFILES>();
		var totalSize = dropFilesSize;

		foreach (var path in filePaths)
		{
			totalSize += (path.Length + 1) * sizeof(char); // + 1 for null terminator
		}
		totalSize += sizeof(char); // Final null terminator

		var handle = PInvoke.GlobalAlloc(
			GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
			(UIntPtr)totalSize);

		if (handle == IntPtr.Zero)
		{
			return (HGLOBAL)IntPtr.Zero;
		}

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var memory);
		if (lockDisposable is null)
		{
			PInvoke.GlobalFree(handle);
			return (HGLOBAL)IntPtr.Zero;
		}

		// Set up DROPFILES structure
		var dropFiles = (DROPFILES*)memory;
		dropFiles->pFiles = (uint)dropFilesSize;
		dropFiles->pt = default;
		dropFiles->fNC = 0;
		dropFiles->fWide = 1; // Unicode

		// Write file paths
		var currentPos = (byte*)memory + dropFilesSize;
		foreach (var path in filePaths)
		{
			var pathBytes = Encoding.Unicode.GetBytes(path + '\0');
			fixed (byte* pathPtr = pathBytes)
			{
				Buffer.MemoryCopy(pathPtr, currentPos, pathBytes.Length, pathBytes.Length);
			}
			currentPos += pathBytes.Length;
		}

		// Write final null terminator
		*(char*)currentPos = '\0';

		return handle;
	}

	private readonly ref struct ClipboardDisposable
	{
		private readonly bool _shouldClose;
		public ClipboardDisposable(HWND hwnd, bool ownClipboard)
		{
			_shouldClose = PInvoke.OpenClipboard(hwnd);
			if (!_shouldClose) { typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.OpenClipboard)} failed: {Win32Helper.GetErrorMessage()}"); }
			if (ownClipboard)
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
}
