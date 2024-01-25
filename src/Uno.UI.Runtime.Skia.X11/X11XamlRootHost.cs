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
using Avalonia.X11.Glx;
using Uno.UI;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost : IXamlRootHost
{
	private const int InitialWidth = 900;
	private const int InitialHeight = 800;
	private const IntPtr EventsMask =
		(IntPtr)EventMask.ExposureMask |
		(IntPtr)EventMask.ButtonPressMask |
		(IntPtr)EventMask.ButtonReleaseMask |
		(IntPtr)EventMask.PointerMotionMask |
		(IntPtr)EventMask.KeyPressMask |
		(IntPtr)EventMask.KeyReleaseMask |
		(IntPtr)EventMask.EnterWindowMask |
		(IntPtr)EventMask.LeaveWindowMask |
		(IntPtr)EventMask.StructureNotifyMask |
		(IntPtr)EventMask.FocusChangeMask |
		(IntPtr)EventMask.VisibilityChangeMask |
		(IntPtr)EventMask.NoEventMask;

	private static bool _firstWindowCreated;
	private static object _x11WindowToXamlRootHostMutex = new();
	private static Dictionary<X11Window, X11XamlRootHost> _x11WindowToXamlRootHost = new();

	private readonly TaskCompletionSource _closed;
	private readonly ApplicationView _applicationView;
	private readonly Window _window;

	private X11Window? _x11Window;
	private IX11Renderer? _renderer;

	public X11Window X11Window => _x11Window!.Value;

	public X11XamlRootHost(Window winUIWindow, Action<Size> resizeCallback, Action closeCallback, Action<bool> focusCallback, Action<bool> visibilityCallback)
	{
		_window = winUIWindow;

		_resizeCallback = resizeCallback;
		_closeCallback = closeCallback;
		_focusCallback = focusCallback;
		_visibilityCallback = visibilityCallback;

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;

		_closed = new TaskCompletionSource();
		Closed = _closed.Task;
		Closed.ContinueWith(_ => _applicationView.PropertyChanged -= OnApplicationViewPropertyChanged);

		Initialize();
	}

	public Task Closed { get; }

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// might need to explicitly set _NET_WM_NAME as well?
		using var _ = X11Helper.XLock(X11Window.Display);
		var __ = XLib.XStoreName(X11Window.Display, X11Window.Window, _applicationView.Title);
	}

	public static X11XamlRootHost? GetXamlRootHostFromX11Window(X11Window window)
	{
		lock (_x11WindowToXamlRootHostMutex)
		{
			return _x11WindowToXamlRootHost.TryGetValue(window, out var root) ? root : null;
		}
	}

	public static void CloseAllWindows()
	{
		lock (_x11WindowToXamlRootHostMutex)
		{
			foreach (var host in _x11WindowToXamlRootHost.Values)
			{
				host._closed.SetResult();
			}

			_x11WindowToXamlRootHost.Clear();
		}
	}

	public static bool AllWindowsDone()
	{
		lock (_x11WindowToXamlRootHostMutex)
		{
			return _firstWindowCreated && _x11WindowToXamlRootHost.Count == 0;
		}
	}

	// TODO: return the first window and keep the order somehow
	// This is just a terrible workaround to deal with DisplayInformation being a singleton instead
	// of having a DisplayInformation instance per application view.
	public static X11Window GetWindow()
	{
		lock (_x11WindowToXamlRootHost)
		{
			foreach (var pair in _x11WindowToXamlRootHost)
			{
				if (pair.Value._closed.Task.IsCompleted)
				{
					_x11WindowToXamlRootHost.Remove(pair.Key);
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

	public static void Close(X11Window x11window)
	{
		lock (_x11WindowToXamlRootHostMutex)
		{
			if (_x11WindowToXamlRootHost.Remove(x11window, out var host))
			{
				host._closed.SetResult();
			}
			else
			{
				if (typeof(X11XamlRootHost).Log().IsEnabled(LogLevel.Error))
				{
					typeof(X11XamlRootHost).Log().Error($"{nameof(Close)} could not find X11Window {x11window}");
				}
			}
		}
	}

	private void Initialize()
	{
		IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);

		using var _1 = X11Helper.XLock(display);

		if (display == IntPtr.Zero)
		{
			this.Log().Error("XLIB ERROR: Cannot connect to X server");
		}

		int screen = XLib.XDefaultScreen(display);

		var size = ApplicationView.PreferredLaunchViewSize;
		if (size == Size.Empty)
		{
			size = new Size(InitialWidth, InitialHeight);
		}

		IntPtr window;
		if (FeatureConfiguration.Rendering.UseOpenGLOnX11 ?? IsOpenGLSupported(display))
		{
			_x11Window = CreateGLXWindow(display, screen, size);
			window = _x11Window.Value.Window;
		}
		else
		{
			window = XLib.XCreateSimpleWindow(display, XLib.XRootWindow(display, screen), 0, 0, (int)size.Width, (int)size.Height, 0,
				XLib.XBlackPixel(display, screen), XLib.XWhitePixel(display, screen));
			XLib.XSelectInput(display, window, EventsMask);
			_x11Window = new X11Window(display, window);
		}

		// Tell the WM to send a WM_DELETE_WINDOW message before closing
		IntPtr deleteWindow = X11Helper.GetAtom(display, X11Helper.WM_DELETE_WINDOW);
		var _2 = XLib.XSetWMProtocols(display, window, new[] { deleteWindow }, 1);

		lock (_x11WindowToXamlRootHostMutex)
		{
			_firstWindowCreated = true;
			_x11WindowToXamlRootHost[_x11Window.Value] = this;
		}

		InitializeX11EventsThread();

		// The window must be mapped before DisplayInformationExtension is initialized.
		var _3 = XLib.XMapWindow(display, window);

		if (FeatureConfiguration.Rendering.UseOpenGLOnX11 ?? IsOpenGLSupported(display))
		{
			_renderer = new X11OpenGLRenderer(this, _x11Window.Value);
		}
		else
		{
			_renderer = new X11SoftwareRenderer(this, _x11Window.Value);
		}

		// This is necessary to initialize the lazy-initialized instance as it will be needed before before the first measure cycle,
		// which will need the DisplayInformation to calculate the bounds of the window (otherwise we get NaNxNaN).
		Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
	}

	// https://github.com/gamedevtech/X11OpenGLWindow
	// https://learnopengl.com/Advanced-OpenGL/Framebuffers
	private unsafe X11Window CreateGLXWindow(IntPtr display, int screen, Size size)
	{
		int[] glxAttribs = {
			GlxConsts.GLX_X_RENDERABLE    , /* True */ 1,
			GlxConsts.GLX_DRAWABLE_TYPE   , GlxConsts.GLX_WINDOW_BIT,
			GlxConsts.GLX_RENDER_TYPE     , GlxConsts.GLX_RGBA_BIT,
			GlxConsts.GLX_X_VISUAL_TYPE   , GlxConsts.GLX_TRUE_COLOR,
			GlxConsts.GLX_RED_SIZE        , 8,
			GlxConsts.GLX_GREEN_SIZE      , 8,
			GlxConsts.GLX_BLUE_SIZE       , 8,
			GlxConsts.GLX_ALPHA_SIZE      , 8,
			GlxConsts.GLX_DEPTH_SIZE      , 24,
			GlxConsts.GLX_STENCIL_SIZE    , 8,
			GlxConsts.GLX_DOUBLEBUFFER    , /* True */ 1,
			(int)X11Helper.None
		};

		IntPtr bestFbc = IntPtr.Zero;
		XVisualInfo* visual = null;
		var ptr = GlxInterface.glXChooseFBConfig(display, screen, glxAttribs, out var count);
		if (ptr == null || *ptr == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.glXChooseFBConfig)} failed to retrieve GLX frambuffer configurations.");
		}
		for (var c = 0; c < count; c++)
		{
			XVisualInfo* visual_ = GlxInterface.glXGetVisualFromFBConfig(display, ptr[c]);
			if (visual_->depth == 32) // 24bit color + 8bit stencil as requested above
			{
				bestFbc = ptr[c];
				visual = visual_;
				break;
			}
		}

		if (visual == null)
		{
			throw new InvalidOperationException("Could not create correct visual window.\n");
		}

		IntPtr context = GlxInterface.glXCreateNewContext(display, bestFbc, GlxConsts.GLX_RGBA_TYPE, IntPtr.Zero, /* True */ 1);
		var _1 = XLib.XSync(display, false);

		XSetWindowAttributes attribs = default;
		attribs.border_pixel = XLib.XBlackPixel(display, screen);
		attribs.background_pixel = XLib.XWhitePixel(display, screen);
		attribs.override_redirect = /* True */ 1;
		attribs.colormap = XLib.XCreateColormap(display, XLib.XRootWindow(display, screen), visual->visual, /* AllocNone */ 0);
		attribs.event_mask = EventsMask;
		var window = XLib.XCreateWindow(display, XLib.XRootWindow(display, screen), 0, 0, (int)size.Width, (int)size.Height, 0,
			(int)visual->depth, /* InputOutput */ 1, visual->visual,
			(UIntPtr)(XCreateWindowFlags.CWBackPixel | XCreateWindowFlags.CWColormap | XCreateWindowFlags.CWBorderPixel | XCreateWindowFlags.CWEventMask),
			ref attribs);

		var _2 = GlxInterface.glXGetFBConfigAttrib(display, bestFbc, GlxConsts.GLX_STENCIL_SIZE, out var stencil);
		var _3 = GlxInterface.glXGetFBConfigAttrib(display, bestFbc, GlxConsts.GLX_SAMPLES, out var samples);
		return new X11Window(display, window, (stencil, samples, context));
	}

	private bool IsOpenGLSupported(IntPtr display)
	{
		try
		{
			return GlxInterface.glXQueryExtension(display, out _, out _);
		}
		catch (Exception) // most likely DllNotFoundException, but can be other types
		{
			return false;
		}
	}

	void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

	UIElement? IXamlRootHost.RootElement => _window.RootElement;
}
