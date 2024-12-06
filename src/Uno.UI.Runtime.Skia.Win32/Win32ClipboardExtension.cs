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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Ole;
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

		var hInstance = new HINSTANCE(Process.GetCurrentProcess().Handle);

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = WndProc,
			hInstance = hInstance,
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
			hInstance,
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

	public unsafe DataPackageView GetContent()
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
						{
							var handle = PInvoke.GetClipboardData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT);
							if (handle == IntPtr.Zero)
							{
								this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
								break;
							}

							if (PInvoke.GlobalLock((HGLOBAL)handle.Value) is not null
								|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GlobalLock)} failed: {Win32Helper.GetErrorMessage()}"))
							{
								using var lockDisposable = new DisposableStruct<Win32ClipboardExtension, HGLOBAL>(static (@this, h) =>
								{
									_ = PInvoke.GlobalUnlock(h) || @this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GlobalUnlock)} failed: {Win32Helper.GetErrorMessage()}");
								}, this, (HGLOBAL)handle.Value);

								_currentPackage.SetText(Marshal.PtrToStringUni(handle)!);
							}
						}
						break;
					case CLIPBOARD_FORMAT.CF_DIB:
						{
							var handle = PInvoke.GetClipboardData((uint)CLIPBOARD_FORMAT.CF_DIB);
							if (handle == IntPtr.Zero)
							{
								this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
								break;
							}

							var memSize = (uint)PInvoke.GlobalSize((HGLOBAL)handle.Value);
							if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
							{
								this.Log().Log(LogLevel.Error, memSize, static memSize => $"{nameof(PInvoke.GlobalSize)} returned {memSize}: {Win32Helper.GetErrorMessage()}");
								break;
							}

							_currentPackage.SetDataProvider(StandardDataFormats.Bitmap, ct =>
							{
								var dib = PInvoke.GlobalLock((HGLOBAL)handle.Value);
								if (dib is not null
									|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GlobalLock)} failed: {Win32Helper.GetErrorMessage()}"))
								{
									using var lockDisposable = new DisposableStruct<Win32ClipboardExtension, HGLOBAL>(static (@this, h) =>
									{
										_ = PInvoke.GlobalUnlock(h) || @this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GlobalUnlock)} failed: {Win32Helper.GetErrorMessage()}");
									}, this, (HGLOBAL)dib);

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
								}
								else
								{
									return Task.FromException<object>(new InvalidOperationException($"{nameof(PInvoke.GlobalLock)} failed: {Win32Helper.GetErrorMessage()}"));
								}
							});
						}
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

	public void SetContent(DataPackage content)
	{
		using var clipboardDisposable = new ClipboardDisposable(_hwnd, true);

		var view = content.GetView();
		if (content.Contains(StandardDataFormats.Text))
		{
			var task = view.GetTextAsync().AsTask();
			while (!task.IsCompleted)
			{
				Win32Host.RunOnce();
			}

			if (task.IsCompletedSuccessfully)
			{
				var nativeString = Marshal.StringToHGlobalUni(task.Result);
				var success = PInvoke.SetClipboardData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT, new HANDLE(nativeString)) != HANDLE.Null || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
				if (!success)
				{
					// "If SetClipboardData succeeds, the system owns the object identified by the hMem parameter. The application may not write to or free the data once ownership has been transferred to the system"
					Marshal.FreeHGlobal(nativeString);
				}
			}
		}
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
