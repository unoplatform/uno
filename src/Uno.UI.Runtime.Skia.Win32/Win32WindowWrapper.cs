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
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
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

	public static readonly XamlRootMap<Win32WindowWrapper> XamlRootMap = new();

	private readonly HWND _hwnd;
	private readonly ApplicationView _applicationView;
	private readonly IRenderer _renderer;

	private IDisposable? _backgroundDisposable;
	private SKColor _background;

	static Win32WindowWrapper()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

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

		XamlRootMap.Register(xamlRoot, this);

		Win32SystemThemeHelperExtension.Instance.SystemThemeChanged += OnSystemThemeChanged;
		OnSystemThemeChanged(Win32SystemThemeHelperExtension.Instance, EventArgs.Empty);

		OnWindowSizeOrLocationChanged();
		_ = (RasterizationScale = (float)PInvoke.GetDpiForWindow(_hwnd) / PInvoke.USER_DEFAULT_SCREEN_DPI) != 0
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetDpiForWindow)} failed: {Win32Helper.GetErrorMessage()}");

		UpdateWindowPropertiesFromPackage();

		Win32Host.RegisterWindow(_hwnd);

		_gl = FeatureConfiguration.Rendering.UseOpenGLOnWin32 ?? true ? CreateGlContext() : null;
		_renderer = _gl is null ? new SoftwareRenderer(_hwnd) : new GlRenderer(_hwnd, _gl);

		RegisterForBackgroundColor();

		PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);

		// TODO: extending into titlebar
		// TODO: NativeOverlappedPresenter and FullScreenPresenter
	}

	private unsafe void OnSystemThemeChanged(object? _, EventArgs __)
	{
		BOOL value = Win32SystemThemeHelperExtension.Instance.GetSystemTheme() is SystemTheme.Dark;
		var hresult = PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &value, (uint)Marshal.SizeOf(value));
		if (hresult.Failed)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.DwmSetWindowAttribute)} failed: {Win32Helper.GetErrorMessage()}");
		}
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
		using var title = new Win32Helper.NativeNulTerminatedUtf16String("Uno Platform");

		var preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize.IsEmpty)
		{
			preferredWindowSize = new Size(InitialWidth, InitialHeight);
		}

		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_wrapperForNextCreateWindow = this;
		var hwnd = PInvoke.CreateWindowEx(
			0,
			lpClassName,
			title,
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

		_ = PInvoke.RegisterTouchWindow(hwnd, 0) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
		_ = PInvoke.EnableMouseInPointer(true) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.EnableMouseInPointer)} failed: {Win32Helper.GetErrorMessage()}");
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
				switch ((uint)Win32Helper.LOWORD(wParam))
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
						this.Log().Log(LogLevel.Error, wParam, static wParam => $"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message but LOWORD(wParam) is {Win32Helper.LOWORD(wParam)}, not {nameof(PInvoke.WA_ACTIVE)}, {nameof(PInvoke.WA_CLICKACTIVE)} or {nameof(PInvoke.WA_INACTIVE)}.");
						break;
				}
				return new LRESULT(0);
			case PInvoke.WM_CLOSE:
				this.Log().Log(LogLevel.Trace, nameof(PInvoke.WM_CLOSE), static messageName => $"WndProc received a {messageName} message.");
				var closingArgs = RaiseClosing();
				if (closingArgs.Cancel)
				{
					return new LRESULT(0);
				}
				// Closing should continue, perform suspension.
				Application.Current.RaiseSuspending();
				break;
			case PInvoke.WM_DESTROY:
				this.Log().Log(LogLevel.Trace, nameof(PInvoke.WM_DESTROY), static messageName => $"WndProc received a {messageName} message.");
				_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
				Win32Host.UnregisterWindow(_hwnd);
				_renderer.Reset();
				if (_gl is { })
				{
					ReleaseGlContext(_gl.Hdc, _gl.GlContext, _gl.GrGlInterface, _gl.GrContext);
				}
				_backgroundDisposable?.Dispose();
				XamlRootMap.Unregister(XamlRoot!);
				return new LRESULT(0);
			case PInvoke.WM_DPICHANGED:
				RasterizationScale = (float)(Win32Helper.LOWORD(wParam)) / PInvoke.USER_DEFAULT_SCREEN_DPI;
				RECT rect = Unsafe.ReadUnaligned<RECT>(lParam.Value.ToPointer());
				this.Log().Log(LogLevel.Trace, wParam, rect, static (wParam, rect) => $"WndProc received a {nameof(PInvoke.WM_DPICHANGED)} message with LOWORD(wParam) == {Win32Helper.LOWORD(wParam)} and lParam = RECT {rect.ToRect()}");
				_ = PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER)
					|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
				return new LRESULT(0);
			case PInvoke.WM_SIZE:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_SIZE)} message.");
				OnWindowSizeOrLocationChanged();
				return new LRESULT(0);
			case PInvoke.WM_MOVE:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_MOVE)} message.");
				OnWindowSizeOrLocationChanged();
				return new LRESULT(0);
			case PInvoke.WM_GETMINMAXINFO:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_GETMINMAXINFO)} message.");
				MINMAXINFO* info = (MINMAXINFO*)lParam.Value;
				info->ptMinTrackSize = new Point((int)_applicationView.PreferredMinSize.Width, (int)_applicationView.PreferredMinSize.Height);
				return new LRESULT(0);
			case PInvoke.WM_PAINT:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_PAINT)} message.");
				Paint();
				break;
			case PInvoke.WM_KEYDOWN:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_KEYDOWN)} message.");
				OnKey(wParam, lParam, true);
				break;
			case PInvoke.WM_KEYUP:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_KEYUP)} message.");
				OnKey(wParam, lParam, false);
				break;
			case PInvoke.WM_POINTERDOWN or PInvoke.WM_POINTERUP or PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL
				or PInvoke.WM_POINTERENTER or PInvoke.WM_POINTERLEAVE or PInvoke.WM_POINTERUPDATE:
				OnPointer(msg, wParam);
				return new LRESULT(0);
			case PInvoke.WM_POINTERCAPTURECHANGED:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_POINTERCAPTURECHANGED)} message.");
				OnPointerCaptureChanged(wParam);
				return new LRESULT(0);
			case PInvoke.WM_SETCURSOR:
				this.Log().Log(LogLevel.Trace, static () => $"WndProc received a {nameof(PInvoke.WM_SETCURSOR)} message.");
				if ((uint)Win32Helper.LOWORD(lParam) is not (PInvoke.HTBOTTOM or PInvoke.HTBOTTOMLEFT or PInvoke.HTBOTTOMRIGHT
					or PInvoke.HTLEFT or PInvoke.HTRIGHT or PInvoke.HTTOP or PInvoke.HTTOPLEFT or PInvoke.HTTOPRIGHT))
				{
					SetCursor(PointerCursor);
					return new LRESULT(0);
				}
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

		this.Log().Log(LogLevel.Trace, windowRect, clientRect, static (windowRect, clientRect) => $"Adjusting window dimensions to {windowRect.ToRect()} and client area dimensions to {clientRect.ToRect()}");

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

	protected internal override void Activate()
	{
		_ = PInvoke.SetActiveWindow(_hwnd) != HWND.Null
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetActiveWindow)} failed: {Win32Helper.GetErrorMessage()}");
	}

	protected override void ShowCore() => PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWDEFAULT);

	protected override void CloseCore()
	{
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
			// https://github.com/libsdl-org/SDL/blob/fc12cc6dfd859a4e01376162a58f12208e539ac6/src/video/windows/SDL_windowswindow.c#L827
			// This software is provided 'as-is', without any express or implied
			// warranty.  In no event will the authors be held liable for any damages
			// arising from the use of this software.
			//
			// Permission is granted to anyone to use this software for any purpose,
			// including commercial applications, and to alter it and redistribute it
			// freely, subject to the following restrictions:
			//
			// 1. The origin of this software must not be misrepresented; you must not
			//    claim that you wrote the original software. If you use this software
			//    in a product, an acknowledgment in the product documentation would be
			//    appreciated but is not required.
			// 2. Altered source versions must be plainly marked as such, and must not be
			//    misrepresented as being the original software.
			// 3. This notice may not be removed or altered from any source distribution.

			if (!File.Exists(iconPath))
			{
				this.Log().Log(LogLevel.Error, iconPath, static iconPath => $"Couldn't find icon file [{iconPath}].");
				return;
			}

			var image = SKImage.FromEncodedData(iconPath);
			if (image is null)
			{
				this.Log().Log(LogLevel.Error, iconPath, static iconPath => $"Couldn't load icon file [{iconPath}].");
				return;
			}
			using var imageDisposable = new DisposableStruct<SKImage>(static image => image.Dispose(), image);

			var maskLength = image.Height * (image.Width + 7) / 8;
			var imageSize = image.Height * image.Width * Marshal.SizeOf<uint>();
			var iconLength = Marshal.SizeOf<BITMAPINFOHEADER>() + imageSize + maskLength;
			var presBits = stackalloc byte[iconLength];

			var bmi = (BITMAPINFOHEADER*)presBits;
			bmi->biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
			bmi->biWidth = image.Width;
			bmi->biHeight = image.Height * 2; // the multiplication by 2 is unexplainable, it seems to draw only have the image without the multiplication
			bmi->biPlanes = 1;
			bmi->biBitCount = 32;
			bmi->biCompression = /* BI_RGB */ 0x0000;

			// Write the pixels upside down into the bitmap buffer
			var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888);
			using (var surface = SKSurface.Create(info))
			{
				var canvas = surface.Canvas;
				canvas.Translate(0, image.Height);
				canvas.Scale(1, -1);
				canvas.DrawImage(image, 0, 0);
				surface.Snapshot().ReadPixels(info, (IntPtr)(presBits + Marshal.SizeOf<BITMAPINFOHEADER>()));
			}

			// Write the mask
			new Span<byte>(presBits + iconLength - maskLength, maskLength).Fill(0xFF);

			// No need to destroy icons created with CreateIconFromResource
			var hIcon = PInvoke.CreateIconFromResource(presBits, (uint)iconLength, true, 0x00030000);
			if (hIcon == HICON.Null)
			{
				this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.CreateIconFromResource)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}

			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_SMALL, hIcon.Value);
			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, hIcon.Value);
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
