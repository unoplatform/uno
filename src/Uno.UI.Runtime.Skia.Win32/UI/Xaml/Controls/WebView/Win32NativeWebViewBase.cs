using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Skia.Win32;

internal abstract class Win32NativeWebViewBase : INativeWebView
{
	private const string WindowClassName = "UnoPlatformWebViewWindow";
	private const uint SC_MASK = 0xFFF0;

	private static readonly WNDCLASSEXW _windowClass;
	private static WeakReference<Win32NativeWebViewBase>? _webViewForNextCreateWindow;
	private static readonly Dictionary<HWND, WeakReference<Win32NativeWebViewBase>> _hwndToWebView = new();

	protected ContentPresenter Presenter { get; }
	protected HWND Hwnd { get; }
	protected HWND ParentHwnd => (Presenter.XamlRoot?.HostWindow?.NativeWindow as Win32NativeWindow)?.Hwnd is IntPtr hwnd ? (HWND)hwnd : HWND.Null;

	static unsafe Win32NativeWebViewBase()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = &WndProc,
			hInstance = Win32Helper.GetHInstance(),
			lpszClassName = lpClassName,
			style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW
		};

		var classAtom = PInvoke.RegisterClassEx(_windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	protected Win32NativeWebViewBase(ContentPresenter presenter)
	{
		Presenter = presenter;

		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_webViewForNextCreateWindow = new WeakReference<Win32NativeWebViewBase>(this);
		unsafe
		{
			Hwnd = PInvoke.CreateWindowEx(
			0,
			lpClassName,
			new PCWSTR(),
			WINDOW_STYLE.WS_CHILDWINDOW | WINDOW_STYLE.WS_VISIBLE,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			ParentHwnd,
			HMENU.Null,
			Win32Helper.GetHInstance(),
			null);
		}
		_webViewForNextCreateWindow = null;

		if (Hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		unsafe
		{
			BOOL fDisable = true;
			var hr = PInvoke.DwmSetWindowAttribute(Hwnd, Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED, &fDisable, (uint)Marshal.SizeOf(fDisable));
			if (hr.Failed && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.DwmSetWindowAttribute)} failed for {Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED}: 0x{hr.Value:X8}");
			}
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("Created web view window.");
		}

		var success = PInvoke.RegisterTouchWindow(Hwnd, 0);
		if (!success && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(PInvoke.RegisterTouchWindow)} failed: {Win32Helper.GetErrorMessage()}");
		}

		presenter.Content = new Win32NativeWindow(Hwnd);
	}

	~Win32NativeWebViewBase()
	{
		Presenter.DispatcherQueue.TryEnqueue(() =>
		{
			var success = PInvoke.DestroyWindow(Hwnd);
			if (!success && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.DestroyWindow)} failed: {Win32Helper.GetErrorMessage()}");
			}

			lock (_hwndToWebView)
			{
				_hwndToWebView.Remove(Hwnd);
			}
		});
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		lock (_hwndToWebView)
		{
			if (_webViewForNextCreateWindow is { } webView)
			{
				_hwndToWebView[hwnd] = webView;
			}
			else if (!_hwndToWebView.TryGetValue(hwnd, out webView))
			{
				var logger = typeof(Win32NativeWebViewBase).Log();
				if (logger.IsEnabled(LogLevel.Error))
				{
					logger.Error($"{nameof(Win32NativeWebViewBase)}.{nameof(WndProc)} received message {msg} for unknown {nameof(HWND)}={hwnd}.");
				}
				return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
			}

			if (webView.TryGetTarget(out var target))
			{
				return target.WndProcInner(hwnd, msg, wParam, lParam);
			}

			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}
	}

	private LRESULT WndProcInner(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		switch (msg)
		{
			case PInvoke.WM_SIZE:
				OnWindowSizeChanged();
				return new LRESULT(0);
			case PInvoke.WM_SYSCOMMAND:
				var syscommand = (uint)wParam.Value & SC_MASK;
				if (syscommand == PInvoke.SC_CLOSE)
				{
					var parentHwnd = ParentHwnd;
					if (parentHwnd != HWND.Null)
					{
						PInvoke.SendMessage(parentHwnd, msg, wParam, lParam);
						return new LRESULT(0);
					}
				}
				break;
			case PInvoke.WM_CLOSE:
				{
					var parentHwnd = ParentHwnd;
					if (parentHwnd != HWND.Null)
					{
						PInvoke.SendMessage(parentHwnd, msg, wParam, lParam);
						return new LRESULT(0);
					}
				}
				break;
			case PInvoke.WM_MOUSEACTIVATE:
				// A WS_CHILD window's DefWindowProc only calls SetFocus, not SetForegroundWindow.
				// Explicitly foreground the Uno top-level so clicking an inactive WebView2 brings the app forward.
				{
					var parentHwnd = ParentHwnd;
					if (parentHwnd != HWND.Null)
					{
						PInvoke.SetForegroundWindow(parentHwnd);
					}
					return new LRESULT((nint)PInvoke.MA_ACTIVATE);
				}
		}

		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	protected abstract void OnWindowSizeChanged();

	public abstract string DocumentTitle { get; }
	public abstract void GoBack();
	public abstract void GoForward();
	public abstract void Stop();
	public abstract void Reload();
	public abstract void ProcessNavigation(Uri uri);
	public abstract void ProcessNavigation(string html);
	public abstract void ProcessNavigation(System.Net.Http.HttpRequestMessage httpRequestMessage);
	public abstract System.Threading.Tasks.Task<string?> ExecuteScriptAsync(string script, System.Threading.CancellationToken token);
	public abstract System.Threading.Tasks.Task<string?> InvokeScriptAsync(string script, string[]? arguments, System.Threading.CancellationToken token);
	public abstract void SetScrollingEnabled(bool isScrollingEnabled);
}
