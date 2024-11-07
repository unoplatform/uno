#nullable enable

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
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

		var title = Marshal.StringToHGlobalUni("Uno Platform");
		using var titleDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, title);

		_hwnd = PInvoke.CreateWindowEx(
			0,
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

	private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	public override object NativeWindow => _hwnd;

	public override unsafe string Title
	{
		get
		{
			char* title = stackalloc char[1024];
			var readChars = PInvoke.GetWindowText(_hwnd, new PWSTR(title), 1024);
			if (readChars is 0)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(PInvoke.GetWindowText)} read 0 chars: {Win32Helper.GetErrorMessage()}");
				}
			}
			return Marshal.PtrToStringUni((IntPtr)title, readChars);
		}
		set => PInvoke.SetWindowText(_hwnd, value);
	}

	public override void Activate()
	{
		if (PInvoke.SetActiveWindow(_hwnd) == HWND.Null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.SetActiveWindow)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}

	protected override void ShowCore()
	{
		if (!PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.ShowWindow)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}

	public override void Close()
	{
		base.Close();

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Forcibly closing window {_hwnd.Value.ToString("X", CultureInfo.InvariantCulture)}");
		}

		if (!PInvoke.DestroyWindow(_hwnd))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.DestroyWindow)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}
}
