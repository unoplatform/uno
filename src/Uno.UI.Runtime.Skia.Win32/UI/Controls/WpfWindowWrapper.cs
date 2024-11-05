#nullable enable

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Win32.UI.Controls;

internal class Win32WindowWrapper : NativeWindowWrapperBase
{
	private readonly HWND _hwnd;

	// https://learn.microsoft.com/en-us/windows/win32/learnwin32/creating-a-window
	public unsafe Win32WindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		const string windowClassName = "UnoPlatformRegularWindow";
		var windowClassPtr = Marshal.StringToHGlobalUni(windowClassName);
		using var windowClassNameDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, windowClassPtr);
		var lpClassName = new PCWSTR((char*)windowClassPtr);

		var hInstance = new HINSTANCE(Marshal.GetHINSTANCE(Assembly.GetEntryAssembly()!.GetModules()[0]));

		WNDCLASSEXW windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = WndProc,
			hInstance = hInstance,
			lpszClassName = lpClassName
		};

		var classAtom = PInvoke.RegisterClassEx(windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		var title = Marshal.StringToHGlobalUni("Uno platfooooooooorm");
		using var titleDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, title);

		_hwnd = PInvoke.CreateWindowEx(
			WINDOW_EX_STYLE.WS_EX_OVERLAPPEDWINDOW,
			lpClassName,
			new PCWSTR((char*)title),
			WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			HWND.Null,
			HMENU.Null,
			hInstance,
			null);

		if (_hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	private LRESULT WndProc(HWND param0, uint param1, WPARAM param2, LPARAM param3)
	{
		return new LRESULT(0);
	}

	public override object NativeWindow => _hwnd;
}
