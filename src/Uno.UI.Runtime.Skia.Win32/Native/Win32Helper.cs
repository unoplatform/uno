using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.OpenGL;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.Memory;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class Win32Helper
{
	public static HINSTANCE GetHInstance() => new(Process.GetCurrentProcess().Handle);

	public static string GetErrorMessage() => GetErrorMessage((uint)Marshal.GetLastWin32Error());

	public static unsafe string GetErrorMessage(uint errorCode)
	{
		IntPtr* messagePtr = stackalloc IntPtr[1];
		var messageLength = PInvoke.FormatMessage(
			FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_IGNORE_INSERTS,
			default,
			errorCode,
			0,
			new PWSTR((char*)messagePtr),
			0);
		var message = *messagePtr;
		using var messageDisposable = new DisposableStruct<IntPtr>(static m => PInvoke.LocalFree(new HLOCAL(m.ToPointer())), message);
		return Marshal.PtrToStringUni(message, (int)messageLength);
	}

	public static VirtualKeyModifiers GetKeyModifiers()
	{
		var modifiers = VirtualKeyModifiers.None;
		if (PInvoke.GetKeyState((int)VirtualKey.LeftMenu) < 0 || PInvoke.GetKeyState((int)VirtualKey.RightMenu) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.Menu) < 0)
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}

		if (PInvoke.GetKeyState((int)VirtualKey.LeftControl) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.RightControl) < 0 || PInvoke.GetKeyState((int)VirtualKey.Control) < 0)
		{
			modifiers |= VirtualKeyModifiers.Control;
		}

		if (PInvoke.GetKeyState((int)VirtualKey.LeftShift) < 0 || PInvoke.GetKeyState((int)VirtualKey.RightShift) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.Shift) < 0)
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}

		if (PInvoke.GetKeyState((int)VirtualKey.LeftWindows) < 0 ||
			PInvoke.GetKeyState((int)VirtualKey.RightWindows) < 0)
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}

		return modifiers;
	}

	public readonly struct NativeNulTerminatedUtf16String(string? str) : IDisposable
	{
		public IntPtr Handle { get; } = Marshal.StringToHGlobalUni(str);
		public static unsafe implicit operator PCWSTR(NativeNulTerminatedUtf16String value) => new((char*)value.Handle);
		public void Dispose() => Marshal.FreeHGlobal(Handle);
	}

	public static unsafe GlobalLockDisposable? GlobalLock(HGLOBAL handle, out void* firstByte)
	{
		firstByte = PInvoke.GlobalLock(handle);
		if (firstByte is null)
		{
			typeof(Win32Helper).LogError()?.Error($"{nameof(PInvoke.GlobalLock)} failed: {GetErrorMessage()}");
			return null;
		}

		return new GlobalLockDisposable(handle);
	}

	public readonly struct GlobalLockDisposable(HGLOBAL handle) : IDisposable
	{
		public void Dispose()
		{
			var success = PInvoke.GlobalUnlock(handle) != IntPtr.Zero || Marshal.GetLastWin32Error() == (int)WIN32_ERROR.NO_ERROR;
			if (!success) { typeof(GlobalLockDisposable).LogError()?.Error($"{nameof(PInvoke.GlobalUnlock)} failed: {GetErrorMessage()}"); }
		}
	}

	public static GlobalAllocDisposable? GlobalAlloc(GLOBAL_ALLOC_FLAGS uFlags, UIntPtr dwBytes, out HGLOBAL handle, Func<bool>? shouldFree = null)
	{
		handle = PInvoke.GlobalAlloc(uFlags, dwBytes);
		if (handle == IntPtr.Zero)
		{
			typeof(Win32Helper).LogError()?.Error($"{nameof(PInvoke.GlobalAlloc)} failed: {GetErrorMessage()}");
			return null;
		}

		return new GlobalAllocDisposable(handle, shouldFree);
	}

	public readonly struct GlobalAllocDisposable(HGLOBAL handle, Func<bool>? shouldFree = null) : IDisposable
	{
		public void Dispose()
		{
			if (shouldFree?.Invoke() ?? true)
			{
				var success = PInvoke.GlobalFree(handle) != IntPtr.Zero;
				if (!success) { typeof(GlobalLockDisposable).LogError()?.Error($"{nameof(PInvoke.GlobalFree)} failed: {GetErrorMessage()}"); }
			}
		}
	}

	public readonly struct WglCurrentContextDisposable : IDisposable
	{
		private readonly HDC _oldDc;
		private readonly HGLRC _oldContext;
		public WglCurrentContextDisposable(HDC dc, HGLRC context)
		{
			_oldContext = PInvoke.wglGetCurrentContext();
			_oldDc = PInvoke.wglGetCurrentDC();
			if (!PInvoke.wglMakeCurrent(dc, context))
			{
				this.LogError()?.Error($"{nameof(PInvoke.wglMakeCurrent)} failed: {GetErrorMessage()}");
			}
		}

		public void Dispose()
		{
			if (_oldDc != IntPtr.Zero && _oldContext != IntPtr.Zero && !PInvoke.wglMakeCurrent(_oldDc, _oldContext))
			{
				this.LogError()?.Error($"{nameof(PInvoke.wglMakeCurrent)} failed: {GetErrorMessage()}");
			}
		}
	}

	public static ushort LOWORD(IntPtr a) => unchecked((ushort)(a & 0xffff));
	public static ushort LOWORD(WPARAM wParam) => LOWORD((IntPtr)wParam.Value);
	public static ushort HIWORD(IntPtr a) => unchecked((ushort)((a >> 16) & 0xffff));
	public static ushort HIWORD(WPARAM wParam) => HIWORD((IntPtr)wParam.Value);
	public static ushort GET_POINTERID_WPARAM(WPARAM wParam) => LOWORD(wParam);
	public static bool IS_POINTER_FLAG_SET_WPARAM(WPARAM wParam, uint flag) => (HIWORD(wParam) & flag) == flag;
	public static bool IS_POINTER_INRANGE_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_INRANGE);
	public static bool IS_POINTER_INCONTACT_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_INCONTACT);
	public static bool IS_POINTER_FIRSTBUTTON_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_FIRSTBUTTON);
	public static bool IS_POINTER_SECONDBUTTON_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_SECONDBUTTON);
	public static bool IS_POINTER_THIRDBUTTON_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_THIRDBUTTON);
	public static bool IS_POINTER_FOURTHBUTTON_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_FOURTHBUTTON);
	public static bool IS_POINTER_FIFTHBUTTON_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_FIFTHBUTTON);
	public static bool IS_POINTER_PRIMARY_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_PRIMARY);
	public static bool HAS_POINTER_CONFIDENCE_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_CONFIDENCE);
	public static bool IS_POINTER_CANCELED_WPARAM(WPARAM wParam) => IS_POINTER_FLAG_SET_WPARAM(wParam, PInvoke.POINTER_MESSAGE_FLAG_CANCELED);
	public static short GET_WHEEL_DELTA_WPARAM(WPARAM wParam) => unchecked((short)((wParam >> 16) & 0xffff));
	public static short GET_X_LPARAM(LPARAM lParam) => unchecked((short)(lParam & 0xffff));
	public static short GET_Y_LPARAM(LPARAM lParam) => unchecked((short)((lParam >> 16) & 0xffff));

	public static int TOUCH_COORD_TO_PIXEL(int a) => a / 100; // the fractional loss is intentional to match windows.h

	public static Rect ToRect(this RECT rect) => new Rect(rect.X, rect.Y, rect.Width, rect.Height);
}
