using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Uno.Disposables;
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

	private readonly TaskCompletionSource _closed; // To keep it simple, only SetResult if you have the lock
	private readonly ApplicationView _applicationView;
	private readonly Window _window;

	private X11Window? _x11Window;
	private IX11Renderer? _renderer;

	public X11Window X11Window => _x11Window!.Value;

	public X11XamlRootHost(Window winUIWindow, Action<Size> resizeCallback, Action closingCallback, Action<bool> focusCallback, Action<bool> visibilityCallback)
	{
		_window = winUIWindow;

		_resizeCallback = resizeCallback;
		_closingCallback = closingCallback;
		_focusCallback = focusCallback;
		_visibilityCallback = visibilityCallback;

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;
		var coreApplicationView = CoreApplication.GetCurrentView();
		coreApplicationView.TitleBar.ExtendViewIntoTitleBarChanged += UpdateWindowPropertiesFromCoreApplication;

		_closed = new TaskCompletionSource();
		Closed = _closed.Task;
		Closed.ContinueWith(_ =>
		{
			_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
			coreApplicationView.TitleBar.ExtendViewIntoTitleBarChanged -= UpdateWindowPropertiesFromCoreApplication;
		});

		Initialize();
		UpdateWindowPropertiesFromPackage();
		OnApplicationViewPropertyChanged(this, new PropertyChangedEventArgs(null));
	}

	public Task Closed { get; }

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		var minSize = _applicationView.PreferredMinSize;

		if (minSize != Size.Empty)
		{
			var hints = new XSizeHints
			{
				flags = (int)XSizeHintsFlags.PMinSize,
				min_width = (int)minSize.Width,
				min_height = (int)minSize.Height
			};

			XLib.XSetWMNormalHints(X11Window.Display, X11Window.Window, ref hints);
		}
	}

	internal void UpdateWindowPropertiesFromCoreApplication()
	{
		var coreApplicationView = CoreApplication.GetCurrentView();

		// Sadly, there is no de jure standard for this. It's basically a set of hints used
		// in the old Motif WM. Other WMs started using it and it became a thing.
		var hintsAtom = X11Helper.GetAtom(X11Window.Display, X11Helper._MOTIF_WM_HINTS);
		var _ = XLib.XChangeProperty(
			X11Window.Display,
			X11Window.Window,
			hintsAtom,
			hintsAtom,
			32,
			PropertyMode.Replace,
			new[] { (IntPtr)MotifFlags.Decorations, 0, coreApplicationView.TitleBar.ExtendViewIntoTitleBar ? 0 : (IntPtr)MotifDecorations.All, 0, 0 },
			5);
	}

	private void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is { } uri)
		{
			var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
			var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				SetIconFromFile(iconPath);
			}
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				SetIconFromFile(scaledPath);
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
			_applicationView.Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}

		unsafe void SetIconFromFile(string iconPath)
		{
			using var fileStream = File.OpenRead(iconPath);
			using var codec = SKCodec.Create(fileStream);
			if (codec is null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Unable to create an SKCodec instance for icon file {iconPath}.");
				}
				return;
			}
			using var bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
			if (result != SKCodecResult.Success)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Unable to decode icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
				return;
			}

			var pixels = bitmap.Pixels;
			var data = Marshal.AllocHGlobal((pixels.Length + 2) * sizeof(IntPtr));
			using var _1 = Disposable.Create(() => Marshal.FreeHGlobal(data));

			var ptr = (IntPtr*)data.ToPointer();
			*(ptr++) = bitmap.Width;
			*(ptr++) = bitmap.Height;
			foreach (var pixel in bitmap.Pixels)
			{
				*(ptr++) = pixel.Alpha << 24 | pixel.Red << 16 | pixel.Green << 8 | pixel.Blue << 0;
			}

			var display = _x11Window!.Value.Display;
			using var _2 = X11Helper.XLock(display);

			var wmIconAtom = X11Helper.GetAtom(display, X11Helper._NET_WM_ICON);
			var cardinalAtom = X11Helper.GetAtom(display, X11Helper.XA_CARDINAL);
			var _3 = XLib.XChangeProperty(
				display,
				_x11Window!.Value.Window,
				wmIconAtom,
				cardinalAtom,
				32,
				PropertyMode.Replace,
				data,
				pixels.Length + 2);

			var _4 = XLib.XFlush(display);
			var _5 = XLib.XSync(display, false); // wait until the pixels are actually copied
		}
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
				using (X11Helper.XLock(host.X11Window.Display))
				{
					host._closed.SetResult();
				}
			}

			_x11WindowToXamlRootHost.Clear();
		}
	}

	public static bool AllWindowsDone()
	{
		// This probably doesn't need a lock, since it doesn't modify anything and reading outdated values is fine,
		// but let's be cautious.
		lock (_x11WindowToXamlRootHostMutex)
		{
			return _firstWindowCreated && _x11WindowToXamlRootHost.Count == 0;
		}
	}

	public static void Close(X11Window x11window)
	{
		lock (_x11WindowToXamlRootHostMutex)
		{
			if (_x11WindowToXamlRootHost.Remove(x11window, out var host))
			{
				using (X11Helper.XLock(x11window.Display))
				{
					host._closed.SetResult();
				}
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
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("XLIB ERROR: Cannot connect to X server");
			}
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
			window = XLib.XCreateSimpleWindow(
				display,
				XLib.XRootWindow(display, screen),
				0,
				0,
				(int)size.Width,
				(int)size.Height,
				0,
				XLib.XBlackPixel(display, screen),
				XLib.XWhitePixel(display, screen));
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
	}

	// https://github.com/gamedevtech/X11OpenGLWindow/blob/4a3d55bb7aafd135670947f71bd2a3ee691d3fb3/README.md
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
		// Not sure why this is needed, commented out until further notice
		// attribs.override_redirect = /* True */ 1;
		attribs.colormap = XLib.XCreateColormap(display, XLib.XRootWindow(display, screen), visual->visual, /* AllocNone */ 0);
		attribs.event_mask = EventsMask;
		var window = XLib.XCreateWindow(
			display,
			XLib.XRootWindow(display, screen),
			0,
			0,
			(int)size.Width,
			(int)size.Height,
			0,
			(int)visual->depth,
			/* InputOutput */ 1,
			visual->visual,
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
