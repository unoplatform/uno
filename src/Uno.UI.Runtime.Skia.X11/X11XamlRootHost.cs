using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Microsoft.UI.Xaml;
using Avalonia.X11;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost : ISkiaApplicationHost, IXamlRootHost
{
	private const int INITIAL_WIDTH = 900;
	private const int INITIAL_HEIGHT = 800;

	private static bool _firstWindowCreated;
	private static object _windowToTCSMutex = new();
	private static Dictionary<X11Window, TaskCompletionSource> _windowToTCS = new();

	private X11Window? _x11Window;

	private X11Renderer? _renderer;

	private Window _window;
	private readonly ApplicationView _applicationView;

	public X11Window X11Window => _x11Window!.Value;

	public X11XamlRootHost(Window winUIWindow, Action<Size> resizeCallback, Action closeCallback, Action<bool> focusCallback, Action<bool> visibilityCallback)
	{
		_window = winUIWindow;

		_resizeCallback = resizeCallback;
		_closeCallback = closeCallback;
		_focusCallback = focusCallback;
		_visibilityCallback = visibilityCallback;

		Initialize();

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;
		var tcs = GetTCSFromX11Window(X11Window);
		tcs?.Task.ContinueWith(_ => _applicationView.PropertyChanged -= OnApplicationViewPropertyChanged);
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// might need to explicitly set _NET_WM_NAME as well?
		XLib.XStoreName(X11Window.Display, X11Window.Window, _applicationView.Title);
	}

	public static TaskCompletionSource? GetTCSFromX11Window(X11Window window)
	{
		lock (_windowToTCSMutex)
		{
			return _windowToTCS.TryGetValue(window, out var tcs) ? tcs : null;
		}
	}

	public static void CompleteWindowTasks()
	{
		lock (_windowToTCSMutex)
		{
			foreach (var taskCompletionSource in _windowToTCS.Values)
			{
				taskCompletionSource.SetResult();
			}

			_windowToTCS.Clear();
		}
	}

	public static bool AllWindowsDone()
	{
		lock (_windowToTCSMutex)
		{
			return _firstWindowCreated && _windowToTCS.Count == 0;
		}
	}

	// TODO: return the first window and keep the order somehow
	// This is just a terrible workaround to deal with DisplayInformation being a singleton instead
	// of having a DisplayInformation instance per application view.
	public static X11Window GetWindow()
	{
		lock (_windowToTCS)
		{
			foreach (var pair in _windowToTCS)
			{
				if (pair.Value.Task.IsCompleted)
				{
					_windowToTCS.Remove(pair.Key);
				}
				else
				{
					return pair.Key;
				}
			}
		}

		typeof(X11XamlRootHost).Log().Error($"{nameof(GetWindow)} didn't find any window.");

		return default;
	}

	public static void TryCompleteWindowCompletionSource(X11Window x11window)
	{
		lock (_windowToTCSMutex)
		{
			if (_windowToTCS.Remove(x11window, out var source))
			{
				source.SetResult();
			}
			else
			{
				if (typeof(X11XamlRootHost).Log().IsEnabled(LogLevel.Error))
				{
					typeof(X11XamlRootHost).Log().Error($"{nameof(TryCompleteWindowCompletionSource)} could not find X11Window {x11window}");
				}
			}
		}
	}

	private void Initialize()
	{
		IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);
		if (display == IntPtr.Zero)
		{
			this.Log().Error("XLIB ERROR: Cannot connect to X server");
		}

		int screen = XLib.XDefaultScreen(display);

		var size = ApplicationView.PreferredLaunchViewSize;
		if (size == Size.Empty)
		{
			size = new Size(INITIAL_WIDTH, INITIAL_HEIGHT);
		}
		IntPtr window = XLib.XCreateSimpleWindow(display, XLib.XRootWindow(display, screen), 0, 0, (int)size.Width, (int)size.Height, 0,
			XLib.XBlackPixel(display, screen), XLib.XWhitePixel(display, screen));

		XLib.XFlush(display); // unnecessary on most Xlib implementations

		_x11Window = new X11Window(display, window);

		// Tell the WM to send a WM_DELETE_WINDOW message before closing
		IntPtr deleteWindow = X11Helper.GetAtom(display, X11Helper.WM_DELETE_WINDOW);
		XLib.XSetWMProtocols(display, window, new[] { deleteWindow }, 1);

		lock (_windowToTCSMutex)
		{
			_firstWindowCreated = true;
			_windowToTCS[_x11Window.Value] = new TaskCompletionSource();
		}

		InitializeX11EventsThread();

		// The window must be mapped before DisplayExtensionExtension is initialized.
		XLib.XMapWindow(display, window);

		_renderer = new X11Renderer(this, _x11Window.Value);
	}

	void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

	UIElement? IXamlRootHost.RootElement => _window.RootElement;
}
