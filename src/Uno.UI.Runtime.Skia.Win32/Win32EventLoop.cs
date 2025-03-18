using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.Win32
{
	internal static class Win32EventLoop
	{
		public static readonly uint UnoWin32DispatcherMsg;

		// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private static readonly WNDCLASSEXW _windowClass;
		private static readonly HWND _hwnd;

		private static readonly Queue<Action> _actions = new();

		static unsafe Win32EventLoop()
		{
			UnoWin32DispatcherMsg = PInvoke.RegisterWindowMessage(nameof(UnoWin32DispatcherMsg));
			if (UnoWin32DispatcherMsg == 0)
			{
				throw new InvalidOperationException($"Failed to register Dispatcher Win32 message: {Win32Helper.GetErrorMessage()}");
			}

			using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String("UnoPlatformDispatcherWindow");
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
		}

		[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
		internal static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
		{
			if (msg == UnoWin32DispatcherMsg)
			{
				Action action;
				lock (_actions)
				{
					// It's important not to keep the lock when we invoke the action, as it will deadlock in some cases,
					// specifically when running the Given_ListViewBase tests in a call to GC.WaitForPendingFinalizers.
					action = _actions.Dequeue();
				}
				action.Invoke();
				return new LRESULT(0);
			}
			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public static void Schedule(Action action, NativeDispatcherPriority p)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			lock (_actions)
			{
				_actions.Enqueue(action);
			}
			_ = PInvoke.PostMessage(_hwnd, UnoWin32DispatcherMsg, new WPARAM(), new LPARAM());
		}

		public static bool HasMessages()
			=> PInvoke.PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);

		public static bool RunOnce()
		{
			// We prioritize WM_PAINT messages so that we keep painting as fast
			// as Windows needs us to even if the message queue is full of other
			// messages. Similarly, we need input messages to "skip ahead" in some
			// cases like wheel scrolling where we don't want to wait for the
			// queue to be empty before continuing to scroll.
			if (PInvoke.PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE | PEEK_MESSAGE_REMOVE_TYPE.PM_QS_PAINT | PEEK_MESSAGE_REMOVE_TYPE.PM_QS_INPUT)
				|| PInvoke.PeekMessage(out msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
			{
				PInvoke.TranslateMessage(msg);
				PInvoke.DispatchMessage(msg);
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
