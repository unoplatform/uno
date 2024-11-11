#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Point = System.Drawing.Point;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : NativeWindowWrapperBase, IXamlRootHost
{
	private const string WindowClassName = "UnoPlatformRegularWindow";
	private static readonly HINSTANCE _hInstance = new HINSTANCE(Process.GetCurrentProcess().Handle);

	// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private static readonly WNDCLASSEXW _windowClass;

	// This is necessary to be able to direct the very first WndProc call of a window to the correct window wrapper.
	// That first call is inside CreateWindow, so we don't have a HWND yet. The alternative would be to create a new
	// window class (and a new WndProc) per window, but that sounds excessive.
	private static Win32WindowWrapper? _wrapperForNextCreateWindow;
	private static readonly Dictionary<HWND, Win32WindowWrapper> _hwndToWrapper = new();

	private static readonly XamlRootMap<Win32WindowWrapper> _xamlRootMap = new();

	private readonly HWND _hwnd;
	private readonly ApplicationView _applicationView;
	private readonly IRenderer _renderer;

	private IDisposable? _backgroundDisposable;
	private SKColor _background;

	static unsafe Win32WindowWrapper()
	{
		var windowClassPtr = Marshal.StringToHGlobalUni(WindowClassName);
		using var windowClassNameDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, windowClassPtr);
		var lpClassName = new PCWSTR((char*)windowClassPtr);

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = WndProc,
			hInstance = _hInstance,
			lpszClassName = lpClassName,
			style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-class-styles
		};

		var classAtom = PInvoke.RegisterClassEx(_windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	// https://learn.microsoft.com/en-us/windows/win32/learnwin32/creating-a-window
	public Win32WindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_ = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) != 0
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetThreadDpiAwarenessContext)} failed: {Win32Helper.GetErrorMessage()}");

		// this must come before CreateWindow(), which sends a WM_GETMINMAXINFO message that reads from _applicationView
		_applicationView = ApplicationView.GetForWindowId(window.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;

		_hwnd = CreateWindow();

		_xamlRootMap.Register(xamlRoot, this);

		OnWindowSizeOrLocationChanged();
		_ = (RasterizationScale = (float)PInvoke.GetDpiForWindow(_hwnd) / PInvoke.USER_DEFAULT_SCREEN_DPI) != 0
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetDpiForWindow)} failed: {Win32Helper.GetErrorMessage()}");

		UpdateWindowPropertiesFromPackage();

		Win32Host.RegisterWindow(_hwnd);

		_gl = FeatureConfiguration.Rendering.UseOpenGLOnWin32 ?? true ? CreateGlContext() : null;
		_renderer = _gl is null ? new SoftwareRenderer(_hwnd) : new GlRenderer(_hwnd, _gl);

		RegisterForBackgroundColor();


		// TODO: extending into titlebar
		// TODO: NativeOverlappedPresenter and FullScreenPresenter
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(_applicationView.PreferredMinSize))
		{
			if (!PInvoke.GetWindowRect(_hwnd, out var rect))
			{
				this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetWindowRect)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}
			// We are setting the window rect to itself to trigger a WM_GETMINMAXINFO
			_ = PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER)
				|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	private unsafe HWND CreateWindow()
	{
		var title = Marshal.StringToHGlobalUni("Uno Platform");
		using var titleDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, title);

		var preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize.IsEmpty)
		{
			preferredWindowSize = new Size(InitialWidth, InitialHeight);
		}

		var windowClassPtr = Marshal.StringToHGlobalUni(WindowClassName);
		using var windowClassNameDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, windowClassPtr);
		var lpClassName = new PCWSTR((char*)windowClassPtr);

		_wrapperForNextCreateWindow = this;
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
			_hInstance,
			null);
		_wrapperForNextCreateWindow = null;

		if (hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}
		return hwnd;
	}

	private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		if (_wrapperForNextCreateWindow is { } wrapper)
		{
			_hwndToWrapper[hwnd] = _wrapperForNextCreateWindow;
		}
		else if (!_hwndToWrapper.TryGetValue(hwnd, out wrapper))
		{
			throw new Exception($"{nameof(WndProc)} was fired on a {nameof(HWND)} before it was added to, or after it was removed from, {nameof(_hwndToWrapper)}.");
		}
		return wrapper.WndProcInner(hwnd, msg, wParam, lParam);
	}

	private unsafe LRESULT WndProcInner(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		Debug.Assert(_hwnd == HWND.Null || hwnd == _hwnd); // the null check is for when this method gets called inside CreateWindow before setting _hwnd
		switch (msg)
		{
			case PInvoke.WM_ACTIVATE:
				switch (wParam & 0xffff)
				{
					case PInvoke.WA_ACTIVE:
						this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_ACTIVE)}");
						ActivationState = CoreWindowActivationState.CodeActivated;
						break;
					case PInvoke.WA_CLICKACTIVE:
						this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_CLICKACTIVE)}");
						ActivationState = CoreWindowActivationState.PointerActivated;
						break;
					case PInvoke.WA_INACTIVE:
						this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_INACTIVE)}");
						ActivationState = CoreWindowActivationState.Deactivated;
						break;
					default:
						this.Log().Log(LogLevel.Error, wParam, static wParam => $"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message but LOWORD(wParam) is {wParam & 0xffff}, not {nameof(PInvoke.WA_ACTIVE)}, {nameof(PInvoke.WA_CLICKACTIVE)} or {nameof(PInvoke.WA_INACTIVE)}.");
						break;
				}
				break;
			case PInvoke.WM_CLOSE:
				this.Log().Log(LogLevel.Trace, nameof(PInvoke.WM_CLOSE), static messageName => $"WndProc received a {messageName} message.");
				var closingArgs = RaiseClosing();
				if (!closingArgs.Cancel)
				{
					// Closing should continue, perform suspension.
					Application.Current.RaiseSuspending();
				}
				break;
			case PInvoke.WM_DESTROY:
				this.Log().Log(LogLevel.Trace, nameof(PInvoke.WM_DESTROY), static messageName => $"WndProc received a {messageName} message.");
				_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
				Win32Host.UnregisterWindow(_hwnd);
				_renderer.Reset();
				if (_gl is { })
				{
					ReleaseGlContext(_gl.GlContext, _gl.GrGlInterface, _gl.GrContext);
				}
				_backgroundDisposable?.Dispose();
				_xamlRootMap.Unregister(XamlRoot!);
				break;
			case PInvoke.WM_DPICHANGED:
				RasterizationScale = (float)(wParam & 0xffff) / PInvoke.USER_DEFAULT_SCREEN_DPI;
				RECT rect = Unsafe.ReadUnaligned<RECT>(lParam.Value.ToPointer());
				this.Log().Log(LogLevel.Trace, wParam, rect, static (wParam, rect) => $"WndProc received a {nameof(PInvoke.WM_DPICHANGED)} message with LOWORD(wParam) == {wParam & 0xffff} and lParam = RECT {rect.Width}x{rect.Height}@{rect.left}x{rect.top}");
				_ = PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER)
					|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
				break;
			case PInvoke.WM_SIZE:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_SIZE)} message.");
				OnWindowSizeOrLocationChanged();
				break;
			case PInvoke.WM_MOVE:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_MOVE)} message.");
				OnWindowSizeOrLocationChanged();
				break;
			case PInvoke.WM_GETMINMAXINFO:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_GETMINMAXINFO)} message.");
				MINMAXINFO* info = (MINMAXINFO*)lParam.Value;
				info->ptMinTrackSize = new Point((int)_applicationView.PreferredMinSize.Width, (int)_applicationView.PreferredMinSize.Height);
				break;
			case PInvoke.WM_PAINT:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_PAINT)} message.");
				Paint();
				break;
		}

		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	private void OnWindowSizeOrLocationChanged()
	{
		if (!PInvoke.GetClientRect(_hwnd, out RECT clientRect))
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		if (!PInvoke.GetWindowRect(_hwnd, out RECT windowRect))
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetWindowRect)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var scale = RasterizationScale == 0 ? 1 : RasterizationScale;

		this.Log().Log(LogLevel.Trace, windowRect, clientRect, static (windowRect, clientRect) => $"Adjusting window dimensions to {windowRect.Width}x{windowRect.Height}@{windowRect.left}x{windowRect.top} and client area dimensions to {clientRect.Width}x{clientRect.Height}@{clientRect.left}x{clientRect.top}");

		// For things to work correctly with layoutting, Bounds and VisibleBounds need to start at (0,0) regardless of
		// the reported top-left corner by Windows.
		// Bounds = new Rect(windowRect.left / scale, windowRect.top / scale, windowRect.Width / scale, windowRect.Height / scale);
		// VisibleBounds = new Rect(clientRect.left / scale, clientRect.top / scale, clientRect.Width / scale, clientRect.Height / scale);
		Bounds = new Rect(0, 0, windowRect.Width / scale, windowRect.Height / scale);
		VisibleBounds = new Rect(0, 0, clientRect.Width / scale, clientRect.Height / scale);

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
			_ = readChars is not 0 || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetWindowText)} read 0 chars: {Win32Helper.GetErrorMessage()}");
			return Marshal.PtrToStringUni((IntPtr)title, readChars);
		}
		set => _ = PInvoke.SetWindowText(_hwnd, value) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowText)} failed: {Win32Helper.GetErrorMessage()}");
	}

	public override void Activate()
	{
		_ = PInvoke.SetActiveWindow(_hwnd) != HWND.Null
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetActiveWindow)} failed: {Win32Helper.GetErrorMessage()}");
	}

	protected override void ShowCore()
	{
		PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWDEFAULT);
	}

	public override void Close()
	{
		base.Close();

		this.Log().Log(LogLevel.Information, _hwnd, static hwnd => $"Forcibly closing window {hwnd.Value.ToString("X", CultureInfo.InvariantCulture)}");

		_ = PInvoke.DestroyWindow(_hwnd)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.DestroyWindow)} failed: {Win32Helper.GetErrorMessage()}");
	}

	public override void Move(PointInt32 position)
	{
		_ = !PInvoke.SetWindowPos(_hwnd, HWND.Null, position.X, position.Y, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOSIZE)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
	}

	public override void Resize(SizeInt32 size)
	{
		_ = !PInvoke.SetWindowPos(_hwnd, HWND.Null, 0, 0, size.Width, size.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOMOVE)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
	}

	private unsafe void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is { } uri)
		{
			var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
			var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				this.Log().Log(LogLevel.Information, iconPath, static iconPath => $"Loading icon file [{iconPath}] from Package.appxmanifest file");
				SetIcon(iconPath);
			}
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				this.Log().Log(LogLevel.Information, scaledPath, static scaledPath => $"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				SetIcon(scaledPath);
			}
			else
			{
				this.Log().Log(LogLevel.Warning, iconPath, static iconPath => $"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
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
			if (hIcon == HANDLE.Null)
			{
				this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.LoadImage)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}

			_ = PInvoke.PostMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_SMALL, hIcon.Value)
				|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.PostMessage)} failed: {Win32Helper.GetErrorMessage()}");
			_ = PInvoke.PostMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, hIcon.Value)
				|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.PostMessage)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	UIElement? IXamlRootHost.RootElement => Window?.RootElement;

	unsafe void IXamlRootHost.InvalidateRender()
	{
		_ = PInvoke.InvalidateRect(_hwnd, default(RECT*), true)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.InvalidateRect)} failed: {Win32Helper.GetErrorMessage()}");
	}

	private void RegisterForBackgroundColor()
	{
		UpdateRendererBackground();
		_backgroundDisposable = _window?.RegisterBackgroundChangedEvent((_, _) => UpdateRendererBackground());
	}

	private void UpdateRendererBackground()
	{
		if (_window?.Background is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
		{
			_background = new SKColor(brush.Color.AsUInt32());
		}
		else if (_window?.Background is not null)
		{
			this.Log().Log(LogLevel.Error, static () => "This platform only supports SolidColorBrush for the Window background");
		}
		else if (_window is null)
		{
			this.Log().Log(LogLevel.Debug, static () => $"{nameof(UpdateRendererBackground)} is called before {nameof(_window)} is set.");
		}
	}
}
