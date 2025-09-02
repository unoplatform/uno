using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls;
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
using Point = System.Drawing.Point;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : NativeWindowWrapperBase, IXamlRootHost
{
	private const string WindowClassName = "UnoPlatformRegularWindow";

	// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private static readonly WNDCLASSEXW _windowClass;

	// This is necessary to be able to direct the very first WndProc call of a window to the correct window wrapper.
	// That first call is inside CreateWindow, so we don't have a HWND yet. The alternative would be to create a new
	// window class (and a new WndProc) per window, but that sounds excessive.
	private static Win32WindowWrapper? _wrapperForNextCreateWindow;
	private static readonly Dictionary<HWND, Win32WindowWrapper> _hwndToWrapper = new();

	private readonly HWND _hwnd;
	private readonly ApplicationView _applicationView;
	private readonly IRenderer _renderer;

	private bool _rendererDisposed;
	private IDisposable? _backgroundDisposable;
	private SKColor _background;
	private bool _isFirstEraseBkgnd = true;

	private SKPicture? _picture;
	private SKPath? _clipPath;

	static unsafe Win32WindowWrapper()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = &WndProc,
			hInstance = Win32Helper.GetHInstance(),
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
		var success = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) != 0;
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetThreadDpiAwarenessContext)} failed: {Win32Helper.GetErrorMessage()}"); }

		// this must come before CreateWindow(), which sends a WM_GETMINMAXINFO message that reads from _applicationView
		_applicationView = ApplicationView.GetForWindowId(window.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;

		_hwnd = CreateWindow();

		XamlRootMap.Register(xamlRoot, this);

		Win32SystemThemeHelperExtension.Instance.SystemThemeChanged += OnSystemThemeChanged;
		OnSystemThemeChanged(Win32SystemThemeHelperExtension.Instance, EventArgs.Empty);

		UpdateWindowPropertiesFromPackage();

		Win32Host.RegisterWindow(_hwnd);

		_renderer = FeatureConfiguration.Rendering.UseOpenGLOnWin32 ?? true
			? (IRenderer?)GlRenderer.TryCreateGlRenderer(_hwnd) ?? new SoftwareRenderer(_hwnd)
			: new SoftwareRenderer(_hwnd);

		RegisterForBackgroundColor();

		PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);

		// synchronously initialize Position and Size here before anyone reads their values
		OnWindowSizeOrLocationChanged();
		UpdateDisplayInfo();
		if (RasterizationScale != 1)
		{
			// https://github.com/unoplatform/uno/issues/20021
			Resize(new SizeInt32((int)(Size.Width * RasterizationScale), (int)(Size.Height * RasterizationScale)));
		}

		// TODO: extending into titlebar
	}

	public static IEnumerable<HWND> GetHwnds() => _hwndToWrapper.Keys;

	private unsafe void OnSystemThemeChanged(object? _, EventArgs __)
	{
		BOOL value = Win32SystemThemeHelperExtension.Instance.GetSystemTheme() is SystemTheme.Dark;
		var hResult = PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &value, (uint)Marshal.SizeOf(value));
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.DwmSetWindowAttribute)} failed: {Win32Helper.GetErrorMessage(hResult)}");
		}
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(_applicationView.PreferredMinSize))
		{
			if (!PInvoke.GetWindowRect(_hwnd, out var rect))
			{
				this.LogError()?.Error($"{nameof(PInvoke.GetWindowRect)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}
			// We are setting the window rect to itself to trigger a WM_GETMINMAXINFO
			var success = PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}"); }
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
			Win32Helper.GetHInstance(),
			null);
		_wrapperForNextCreateWindow = null;

		if (hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		var success = PInvoke.RegisterTouchWindow(hwnd, 0);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.RegisterTouchWindow)} failed: {Win32Helper.GetErrorMessage()}"); }
		var success2 = PInvoke.EnableMouseInPointer(true);
		if (!success2) { this.LogError()?.Error($"{nameof(PInvoke.EnableMouseInPointer)} failed: {Win32Helper.GetErrorMessage()}"); }
		return hwnd;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
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
				OnWmActivate(wParam);
				return new LRESULT(0);
			case PInvoke.WM_CLOSE:
				if (OnWmClose())
				{
					return new LRESULT(0);
				}
				break;
			case PInvoke.WM_DESTROY:
				OnWmDestroy();
				return new LRESULT(0);
			case PInvoke.WM_DPICHANGED:
				OnWmDpiChanged(wParam, lParam);
				return new LRESULT(0);
			case PInvoke.WM_SIZE:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_SIZE)} message.");
				UpdateDisplayInfo();
				OnWindowSizeOrLocationChanged();
				return new LRESULT(0);
			case PInvoke.WM_MOVE:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_MOVE)} message.");
				UpdateDisplayInfo();
				OnWindowSizeOrLocationChanged();
				return new LRESULT(0);
			case PInvoke.WM_GETMINMAXINFO:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_GETMINMAXINFO)} message.");
				MINMAXINFO* info = (MINMAXINFO*)lParam.Value;
				info->ptMinTrackSize = new Point((int)_applicationView.PreferredMinSize.Width, (int)_applicationView.PreferredMinSize.Height);
				return new LRESULT(0);
			case PInvoke.WM_ERASEBKGND:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_ERASEBKGND)} message.");
				if (_isFirstEraseBkgnd)
				{
					// Without painting on the first WM_ERASEBKGND, we get an initial white frame
					_isFirstEraseBkgnd = false;
					Paint();
					return new LRESULT(1);
				}
				else
				{
					// Paiting on WM_ERASEBKGND causes severe flickering in hosted native windows so we
					// only do it the first time when we really need to
					return new LRESULT(0);
				}
			case PInvoke.WM_PAINT:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_PAINT)} message.");
				Paint();
				break;
			case PInvoke.WM_KEYDOWN:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_KEYDOWN)} message.");
				OnKey(wParam, lParam, true);
				break;
			case PInvoke.WM_KEYUP:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_KEYUP)} message.");
				OnKey(wParam, lParam, false);
				break;
			case PInvoke.WM_POINTERDOWN or PInvoke.WM_POINTERUP or PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL
				or PInvoke.WM_POINTERENTER or PInvoke.WM_POINTERLEAVE or PInvoke.WM_POINTERUPDATE:
				OnPointer(msg, wParam, _hwnd);
				return new LRESULT(0);
			case PInvoke.WM_POINTERCAPTURECHANGED:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_POINTERCAPTURECHANGED)} message.");
				OnPointerCaptureChanged(wParam);
				return new LRESULT(0);
			case PInvoke.WM_SETCURSOR:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_SETCURSOR)} message.");
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

	private unsafe void OnWmDpiChanged(WPARAM wParam, LPARAM lParam)
	{
		RECT rect = Unsafe.ReadUnaligned<RECT>(lParam.Value.ToPointer());
		this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_DPICHANGED)} message with LOWORD(wParam) == {Win32Helper.LOWORD(wParam)} and lParam = RECT {rect.ToRect()}");
		// the order of the next lines matters or else the canvas might not be resized correctly
		UpdateDisplayInfo();
		var success = PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	private void OnWmDestroy()
	{
		this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_DESTROY)} message.");
		_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
		Win32SystemThemeHelperExtension.Instance.SystemThemeChanged -= OnSystemThemeChanged;
		Win32Host.UnregisterWindow(_hwnd);
		_renderer.Dispose();
		_rendererDisposed = true;
		_backgroundDisposable?.Dispose();
		XamlRootMap.Unregister(XamlRoot!);
	}

	private bool OnWmClose()
	{
		this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_CLOSE)} message.");
		var closingArgs = RaiseClosing();
		if (closingArgs.Cancel)
		{
			return true;
		}
		// Closing should continue, perform suspension.
		Application.Current.RaiseSuspending();
		return false;
	}

	private void OnWmActivate(WPARAM wParam)
	{
		switch ((uint)Win32Helper.LOWORD(wParam))
		{
			case PInvoke.WA_ACTIVE:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_ACTIVE)}");
				ActivationState = CoreWindowActivationState.CodeActivated;
				break;
			case PInvoke.WA_CLICKACTIVE:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_CLICKACTIVE)}");
				ActivationState = CoreWindowActivationState.PointerActivated;
				break;
			case PInvoke.WA_INACTIVE:
				this.LogTrace()?.Trace($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message with LOWORD(wParam) == {nameof(PInvoke.WA_INACTIVE)}");
				ActivationState = CoreWindowActivationState.Deactivated;
				break;
			default:
				this.LogError()?.Error($"WndProc received a {nameof(PInvoke.WM_ACTIVATE)} message but LOWORD(wParam) is {Win32Helper.LOWORD(wParam)}, not {nameof(PInvoke.WA_ACTIVE)}, {nameof(PInvoke.WA_CLICKACTIVE)} or {nameof(PInvoke.WA_INACTIVE)}.");
				break;
		}
	}

	private void OnWindowSizeOrLocationChanged()
	{
		if (!PInvoke.GetClientRect(_hwnd, out RECT clientRect))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		if (!PInvoke.GetWindowRect(_hwnd, out RECT windowRect))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetWindowRect)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var scale = RasterizationScale == 0 ? 1 : RasterizationScale;

		this.LogTrace()?.Trace($"Adjusting window dimensions to {windowRect.ToRect()} and client area dimensions to {clientRect.ToRect()}");

		// For things to work correctly with layoutting, Bounds and VisibleBounds need to start at (0,0) and have the
		// same size regardless of the reported top-left corner by Windows.
		// Bounds = new Rect(windowRect.left / scale, windowRect.top / scale, windowRect.Width / scale, windowRect.Height / scale);
		// VisibleBounds = new Rect(clientRect.left / scale, clientRect.top / scale, clientRect.Width / scale, clientRect.Height / scale);
		var bounds = new Rect(0, 0, clientRect.Width / scale, clientRect.Height / scale);
		SetBoundsAndVisibleBounds(bounds, bounds);

		Size = new SizeInt32(windowRect.Width, windowRect.Height);
		Position = new PointInt32(windowRect.left, windowRect.top);
	}

	public override object NativeWindow => new Win32NativeWindow(_hwnd);

	public override unsafe string Title
	{
		get
		{
			char* title = stackalloc char[1024];
			var readChars = PInvoke.GetWindowText(_hwnd, new PWSTR(title), 1024);
			if (readChars is 0) { this.LogError()?.Error($"{nameof(PInvoke.GetWindowText)} read 0 chars: {Win32Helper.GetErrorMessage()}"); }
			return Marshal.PtrToStringUni((IntPtr)title, readChars);
		}
		set
		{
			var success = PInvoke.SetWindowText(_hwnd, value);
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowText)} failed: {Win32Helper.GetErrorMessage()}"); }
		}
	}

	protected internal override void Activate()
	{
		var success = PInvoke.SetActiveWindow(_hwnd) != HWND.Null;
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetActiveWindow)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	protected override void ShowCore()
	{
		if (Window?.AppWindow.Presenter is FullScreenPresenter)
		{
			// The window takes a split second to be rerendered with the fullscreen window size but
			// no fix has been found for this yet.
			SetWindowStyle(WINDOW_STYLE.WS_DLGFRAME, false);
			_ = PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_MAXIMIZE);
		}
		else
		{
			PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWDEFAULT);
		}
	}

	protected override void CloseCore()
	{
		this.LogInfo()?.Info($"Forcibly closing window {_hwnd.Value.ToString("X", CultureInfo.InvariantCulture)}");

		var success = PInvoke.DestroyWindow(_hwnd);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.DestroyWindow)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	public override void Move(PointInt32 position)
	{
		var success = PInvoke.SetWindowPos(_hwnd, HWND.Null, position.X, position.Y, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	public override void Resize(SizeInt32 size)
	{
		var success = PInvoke.SetWindowPos(_hwnd, HWND.Null, 0, 0, size.Width, size.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	private unsafe void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is { } uri)
		{
			var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
			var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				this.LogInfo()?.Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				SetIcon(iconPath);
			}
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				this.LogInfo()?.Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				SetIcon(scaledPath);
			}
			else
			{
				this.LogWarn()?.Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
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
				this.LogError()?.Error($"Couldn't find icon file [{iconPath}].");
				return;
			}

			var image = SKImage.FromEncodedData(iconPath);
			if (image is null)
			{
				this.LogError()?.Error($"Couldn't load icon file [{iconPath}].");
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
			bmi->biHeight = image.Height * 2; // the multiplication by 2 is unexplainable, it seems to draw only half the image without the multiplication
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
				this.LogError()?.Error($"{nameof(PInvoke.CreateIconFromResource)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}

			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_SMALL, hIcon.Value);
			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, hIcon.Value);
		}
	}

	UIElement? IXamlRootHost.RootElement => Window?.RootElement;

	unsafe void IXamlRootHost.InvalidateRender()
	{
		if (!SkiaRenderHelper.CanRecordPicture(Window?.RootElement))
		{
			// Try again next tick
			Window?.RootElement?.XamlRoot?.QueueInvalidateRender();
			return;
		}

		var (picture, path) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(int)(Window.Bounds.Width),
			(int)(Window.Bounds.Height),
			Window.RootElement,
			invertPath: false);

		Interlocked.Exchange(ref _picture, picture);
		Interlocked.Exchange(ref _clipPath, path);

		RenderingNegativePathReevaluated?.Invoke(this, path);

		var success = PInvoke.InvalidateRect(_hwnd, default(RECT*), true);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.InvalidateRect)} failed: {Win32Helper.GetErrorMessage()}"); }
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
			this.LogError()?.Error("This platform only supports SolidColorBrush for the Window background");
		}
		else if (_window is null)
		{
			this.LogDebug()?.Debug($"{nameof(UpdateRendererBackground)} is called before {nameof(_window)} is set.");
		}
	}
}
