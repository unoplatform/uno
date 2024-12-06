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
			lpfnWndProc = WndProc,
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
		_ = PInvoke.AddClipboardFormatListener(_hwnd) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.AddClipboardFormatListener)} failed: {Win32Helper.GetErrorMessage()}");
		Win32Host.RegisterWindow(_hwnd);
	}

	internal LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		if (msg is PInvoke.WM_CLIPBOARDUPDATE)
		{
			_currentPackage = null;
			if (_observeContentChanged)
			{
				ContentChanged?.Invoke(this, EventArgs.Empty);
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

			uint lastFormat = 0;
			while ((lastFormat = PInvoke.EnumClipboardFormats(lastFormat)) != 0)
			{
				switch ((CLIPBOARD_FORMAT)lastFormat)
				{
					case CLIPBOARD_FORMAT.CF_UNICODETEXT:
						GetText();
						break;
					case CLIPBOARD_FORMAT.CF_HDROP:
						GetFileDropList();
						break;
					case CLIPBOARD_FORMAT.CF_DIB:
						GetBitmap();
						break;
				}
			}

			if (Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_SUCCESS)
			{
				this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.EnumClipboardFormats)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
		return _currentPackage.GetView();
	}

	private unsafe void GetText()
	{
		var handle = (HGLOBAL)(IntPtr)PInvoke.GetClipboardData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT);
		if (handle == IntPtr.Zero)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var bytes);
		if (lockDisposable is null)
		{
			return;
		}

		_currentPackage!.SetText(Marshal.PtrToStringUni((IntPtr)bytes)!);
	}

	private unsafe void GetBitmap()
	{
		var handle = (HGLOBAL)(IntPtr)PInvoke.GetClipboardData((uint)CLIPBOARD_FORMAT.CF_DIB);
		if (handle == IntPtr.Zero)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var memSize = (uint)PInvoke.GlobalSize(handle);
		if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
		{
			this.Log().Log(LogLevel.Error, memSize, static memSize => $"{nameof(PInvoke.GlobalSize)} returned {memSize}: {Win32Helper.GetErrorMessage()}");
			return;
		}

		_currentPackage!.SetDataProvider(StandardDataFormats.Bitmap, _ =>
		{
			using var lockDisposable = Win32Helper.GlobalLock(handle, out var dib);
			if (lockDisposable is null)
			{
				return Task.FromException<object>(new InvalidOperationException($"{nameof(PInvoke.GlobalLock)} failed: {Win32Helper.GetErrorMessage()}"));
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

	private unsafe void GetFileDropList()
	{
		var handle = (HGLOBAL)(IntPtr)PInvoke.GetClipboardData((uint)CLIPBOARD_FORMAT.CF_HDROP);
		if (handle == IntPtr.Zero)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var firstByte);
		if (lockDisposable is null)
		{
			return;
		}

		var hDrop = new HDROP((IntPtr)firstByte);

		var filesDropped = PInvoke.DragQueryFile(hDrop, 0xFFFFFFFF, new PWSTR(), 0);
		if (filesDropped == 0)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.DragQueryFile)} failed when querying total count: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var files = new List<IStorageItem>((int)filesDropped);
		for (uint i = 0; i < filesDropped; i++)
		{
			var charLength = PInvoke.DragQueryFile(hDrop, i, new PWSTR(), 0);
			if (charLength == 0)
			{
				this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.DragQueryFile)} failed when querying buffer length: {Win32Helper.GetErrorMessage()}");
				break;
			}
			charLength++; // + 1 for \0

			var buffer = Marshal.AllocHGlobal((IntPtr)(charLength * Unsafe.SizeOf<char>()));
			using var bufferDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, buffer);
			var charsWritten = PInvoke.DragQueryFile(hDrop, i, new PWSTR((char*)buffer), charLength);
			if (charsWritten == 0)
			{
				this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.DragQueryFile)} failed when querying file path: {Win32Helper.GetErrorMessage()}");
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
				this.Log().Log(LogLevel.Error, filePath, static filePath => $"HDROP Clipboard: file path '{filePath}' was not a valid file or directory.");
			}
		}

		_currentPackage!.SetStorageItems(files);
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
				Win32Host.RunOnce();
			}

			if (!task.IsCompletedSuccessfully)
			{
				this.Log().Log(LogLevel.Error, task, static task => $"{nameof(view.GetTextAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
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

				var shouldUnlock = true;
				using var lockDisposable = Win32Helper.GlobalLock(
					handle,
					out var dstBytes,
					// ReSharper disable once AccessToModifiedClosure
					() => shouldUnlock);

				Buffer.MemoryCopy(srcBytes, dstBytes, bufferLength, bufferLength);

				var success = PInvoke.SetClipboardData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT, new HANDLE(handle)) != HANDLE.Null || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
				// "If SetClipboardData succeeds, the system owns the object identified by the hMem parameter. The application may not write to or free the data once ownership has been transferred to the system"
				shouldFree = !success;
				shouldUnlock = !success; // The docs aren't very clear on this, but Windows refuses to unlock after SetClipboardData succeeds.
			}
		}
	}

	// Untested
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
			Win32Host.RunOnce();
		}

		if (!task.IsCompletedSuccessfully)
		{
			this.Log().Log(LogLevel.Error, task, static task => $"{nameof(view.GetBitmapAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var task2 = task.Result.OpenReadAsync().AsTask();
		while (!task2.IsCompleted)
		{
			Win32Host.RunOnce();
		}

		if (!task2.IsCompletedSuccessfully)
		{
			this.Log().Log(LogLevel.Error, task2, static task => $"{nameof(RandomAccessStreamReference.OpenReadAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var stream = task2.Result;
		Debug.Assert(stream.CanRead);
		stream.Seek(0);

		// If the hMem parameter identifies a memory object, the object must have been allocated using the function with the GMEM_MOVEABLE flag
		var shouldFree = true;
		using var allocDisposable = Win32Helper.GlobalAlloc(
			GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
			(UIntPtr)(stream.Size - (ulong)sizeof(BITMAPFILEHEADER)),
			out var handle,
			// ReSharper disable once AccessToModifiedClosure
			() => shouldFree);

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var bmp);

		stream.AsStreamForWrite().Write(new ReadOnlySpan<byte>(bmp, (int)stream.Size));
		var isBmp = stream.Size > (ulong)sizeof(BITMAPFILEHEADER) && ((BITMAPFILEHEADER*)bmp)->bfType == /* BM */ 0x4d42;

		if (!isBmp)
		{
			this.Log().Log(LogLevel.Error, static () => "Failed to copy BMP image to clipboard: invalid BMP format.");
			_ = PInvoke.GlobalFree(handle) == IntPtr.Zero || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GlobalFree)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var success = PInvoke.SetClipboardData((uint)CLIPBOARD_FORMAT.CF_DIB, new HANDLE(handle)) != HANDLE.Null || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
		// "If SetClipboardData succeeds, the system owns the object identified by the hMem parameter. The application may not write to or free the data once ownership has been transferred to the system"
		shouldFree = !success;
	}

	private readonly ref struct ClipboardDisposable
	{
		private readonly bool _shouldClose;
		public ClipboardDisposable(HWND hwnd, bool ownClipboard)
		{
			_shouldClose = PInvoke.OpenClipboard(hwnd) || typeof(Win32ClipboardExtension).Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.OpenClipboard)} failed: {Win32Helper.GetErrorMessage()}");
			if (ownClipboard)
			{
				_ = PInvoke.EmptyClipboard() || typeof(Win32ClipboardExtension).Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.EmptyClipboard)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}

		public void Dispose()
		{
			if (_shouldClose)
			{
				_ = PInvoke.CloseClipboard() || typeof(Win32ClipboardExtension).Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.CloseClipboard)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}
}
