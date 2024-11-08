#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Point = System.Drawing.Point;

namespace Uno.UI.Runtime.Skia.Win32.UI.Controls;

internal class Win32WindowWrapper : NativeWindowWrapperBase
{
	private readonly HWND _hwnd;
	private readonly ApplicationView _applicationView;

	// https://learn.microsoft.com/en-us/windows/win32/learnwin32/creating-a-window
	public Win32WindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		if (!PInvoke.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.SetProcessDpiAwarenessContext)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}

		_hwnd = CreateWindow();
		OnWindowSizeOrLocationChanged();

		_applicationView = ApplicationView.GetForWindowId(window.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;

		UpdateWindowPropertiesFromPackage();


		// TODO: extending into titlebar
		// TODO: NativeOverlappedPresenter and FullScreenPresenter
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(_applicationView.PreferredMinSize))
		{
			if (!PInvoke.GetWindowRect(_hwnd, out var rect))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(PInvoke.GetWindowRect)} failed: {Win32Helper.GetErrorMessage()}");
				}
				return;
			}
			// We are setting the window rect to itself to trigger a WM_GETMINMAXINFO
			PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
		}
	}

	private unsafe HWND CreateWindow()
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
			lpszClassName = lpClassName,
			style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-class-styles
		};

		var classAtom = PInvoke.RegisterClassEx(windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		var title = Marshal.StringToHGlobalUni("Uno Platform");
		using var titleDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, title);

		var preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize.IsEmpty)
		{
			preferredWindowSize = new Size(InitialWidth, InitialHeight);
		}

		var hwnd = PInvoke.CreateWindowEx(
			0,
			lpClassName,
			new PCWSTR((char*)title),
			WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			(int)preferredWindowSize.Width,
			(int)preferredWindowSize.Height,
			HWND.Null,
			HMENU.Null,
			hInstance,
			null);

		if (hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		return hwnd;
	}

	private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		void TraceMessage(string messageName)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"WndProc received a {messageName} message.");
			}
		}

		switch (msg)
		{
			case PInvoke.WM_ACTIVATE:
				switch ((wParam & 0xffff))
				{
					case PInvoke.WA_ACTIVE:
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_ACTIVE)}");
						}
						ActivationState = CoreWindowActivationState.CodeActivated;
						break;
					case PInvoke.WA_CLICKACTIVE:
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_CLICKACTIVE)}");
						}
						ActivationState = CoreWindowActivationState.PointerActivated;
						break;
					case PInvoke.WA_INACTIVE:
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_INACTIVE)}");
						}
						ActivationState = CoreWindowActivationState.Deactivated;
						break;
					default:
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message but LOWORD(wParam) is {wParam & 0xffff}, not {nameof(PInvoke.WA_ACTIVE)}, {nameof(PInvoke.WA_CLICKACTIVE)} or {nameof(PInvoke.WA_INACTIVE)}.");
						}
						break;
				}
				break;
			case PInvoke.WM_CLOSE:
				TraceMessage(nameof(PInvoke.WM_CLOSE));
				var closingArgs = RaiseClosing();
				if (!closingArgs.Cancel)
				{
					// Closing should continue, perform suspension.
					Application.Current.RaiseSuspending();
				}
				break;
			case PInvoke.WM_DESTROY:
				TraceMessage(nameof(PInvoke.WM_DESTROY));
				_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
				break;
			case PInvoke.WM_DPICHANGED:
				RasterizationScale = (float)(wParam & 0xffff) / PInvoke.USER_DEFAULT_SCREEN_DPI;
				RECT rect = Unsafe.ReadUnaligned<RECT>(lParam.Value.ToPointer());
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"WndProc received a {nameof(PInvoke.WM_DPICHANGED)} message with LOWORD(wParam) == {wParam & 0xffff} and lParam = RECT {rect.Width}x{rect.Height}@{rect.left}x{rect.top}");
				}
				if (!PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
					}
				}
				break;
			case PInvoke.WM_SIZE:
				TraceMessage(nameof(PInvoke.WM_SIZE));
				OnWindowSizeOrLocationChanged();
				break;
			case PInvoke.WM_MOVE:
				TraceMessage(nameof(PInvoke.WM_MOVE));
				OnWindowSizeOrLocationChanged();
				break;
			case PInvoke.WM_GETMINMAXINFO:
				TraceMessage(nameof(PInvoke.WM_GETMINMAXINFO));
				MINMAXINFO* info = (MINMAXINFO*)lParam.Value;
				info->ptMinTrackSize = new Point((int)_applicationView.PreferredMinSize.Width, (int)_applicationView.PreferredMinSize.Height);
				break;
		}

		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	private void OnWindowSizeOrLocationChanged()
	{
		if (!PInvoke.GetClientRect(_hwnd, out RECT clientRect))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
			}

			return;
		}

		if (!PInvoke.GetWindowRect(_hwnd, out RECT windowRect))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.GetWindowRect)} failed: {Win32Helper.GetErrorMessage()}");
			}

			return;
		}

		var scale = RasterizationScale == 0 ? 1 : RasterizationScale;

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Adjusting window dimensions to {windowRect.Width}x{windowRect.Height}@{windowRect.left}x{windowRect.top} and client area dimensions to {clientRect.Width}x{clientRect.Height}@{clientRect.left}x{clientRect.top}");
		}

		Bounds = new Rect(windowRect.left / scale, windowRect.top / scale, windowRect.Width / scale, windowRect.Height / scale);
		VisibleBounds = new Rect(clientRect.left / scale, clientRect.top / scale, clientRect.Width / scale, clientRect.Height / scale);
		Size = new SizeInt32(windowRect.Width, windowRect.Height);
		Position = new PointInt32(windowRect.left, windowRect.top);
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

	public override void Move(PointInt32 position)
	{
		if (!PInvoke.SetWindowPos(_hwnd, HWND.Null, position.X, position.Y, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOSIZE))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}

	public override void Resize(SizeInt32 size)
	{
		if (!PInvoke.SetWindowPos(_hwnd, HWND.Null, 0, 0, size.Width, size.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOMOVE))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}

	private unsafe void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
		{
			var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
			var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				SetIcon(iconPath);
			}
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				SetIcon(scaledPath);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
			}
		}

		if (!string.IsNullOrEmpty(Windows.ApplicationModel.Package.Current.DisplayName))
		{
			Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}

		void SetIcon(string iconPath)
		{
			var iconPtr = Marshal.StringToHGlobalUni(iconPath);
			using var iconDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, iconPtr);
			var hIcon = PInvoke.LoadImage(HINSTANCE.Null, new PCWSTR((char*)iconPtr), GDI_IMAGE_TYPE.IMAGE_ICON, 0, 0, IMAGE_FLAGS.LR_DEFAULTSIZE | IMAGE_FLAGS.LR_LOADFROMFILE);
			if (hIcon != HANDLE.Null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(PInvoke.LoadImage)} failed: {Win32Helper.GetErrorMessage()}");
				}
			}
			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_SMALL, hIcon.Value);
			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, hIcon.Value);
		}
	}
}
