using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost : IXamlRootHost
{
	private const int DefaultColorDepth = 32;
	private const int FallbackColorDepth = 24;

	// Note For KeyPress/KeyRelease: subscribing on the top window prevents key inputs from hitting when the pointer
	// is outside the window. https://github.com/unoplatform/uno/issues/19310
	private const IntPtr RootEventsMask =
		(IntPtr)EventMask.ExposureMask |
		(IntPtr)EventMask.StructureNotifyMask |
		(IntPtr)EventMask.VisibilityChangeMask |
		(IntPtr)EventMask.KeyPressMask |
		(IntPtr)EventMask.KeyReleaseMask |
		(IntPtr)EventMask.NoEventMask;
	private const IntPtr TopEventsMask =
		(IntPtr)EventMask.ExposureMask |
		(IntPtr)EventMask.ButtonPressMask |
		(IntPtr)EventMask.ButtonReleaseMask |
		(IntPtr)EventMask.PointerMotionMask |
		(IntPtr)EventMask.EnterWindowMask |
		(IntPtr)EventMask.LeaveWindowMask |
		(IntPtr)EventMask.FocusChangeMask |
		(IntPtr)EventMask.NoEventMask;
	// We only use XI2 for pointer stuff. We use the core protocol events for everything else.
	private const IntPtr EventsHandledByXI2Mask =
		(IntPtr)EventMask.ButtonPressMask |
		(IntPtr)EventMask.PointerMotionMask |
		(IntPtr)EventMask.EnterWindowMask |
		(IntPtr)EventMask.LeaveWindowMask |
		(IntPtr)EventMask.NoEventMask;

	private static readonly int[] _glxAttribs = {
		GlxConsts.GLX_X_RENDERABLE    , /* True */ 1,
		GlxConsts.GLX_DRAWABLE_TYPE   , GlxConsts.GLX_WINDOW_BIT,
		GlxConsts.GLX_RENDER_TYPE     , GlxConsts.GLX_RGBA_BIT,
		GlxConsts.GLX_X_VISUAL_TYPE   , GlxConsts.GLX_TRUE_COLOR,
		GlxConsts.GLX_RED_SIZE        , 8,
		GlxConsts.GLX_GREEN_SIZE      , 8,
		GlxConsts.GLX_BLUE_SIZE       , 8,
		GlxConsts.GLX_ALPHA_SIZE      , 8,
		GlxConsts.GLX_DEPTH_SIZE      , 8,
		GlxConsts.GLX_STENCIL_SIZE    , 8,
		GlxConsts.GLX_DOUBLEBUFFER    , /* True */ 1,
		(int)X11Helper.None
	};

	private static bool _firstWindowCreated;
	private static readonly object _x11WindowToXamlRootHostMutex = new();
	private static readonly Dictionary<X11Window, X11XamlRootHost> _x11WindowToXamlRootHost = new();
	private static readonly ConcurrentDictionary<Window, X11XamlRootHost> _windowToHost = new();

	private readonly TaskCompletionSource _closed; // To keep it simple, only SetResult if you have the lock
	private readonly ApplicationView _applicationView;
	private readonly X11WindowWrapper _wrapper;
	private readonly Window _window;

	private int _synchronizedShutDownTopWindowIdleCounter;

	private X11Window? _x11Window;
	private X11Window? _x11TopWindow;
	private X11Renderer? _renderer;
	private readonly SKPictureRecorder _recorder = new SKPictureRecorder();

	private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private readonly DispatcherTimer _configureTimer = new DispatcherTimer();
	private long _lastConfigureTime;
	private int _configureScheduled;

	public X11Window RootX11Window => _x11Window!.Value;
	public X11Window TopX11Window => _x11TopWindow!.Value;

	public X11XamlRootHost(X11WindowWrapper wrapper, Window winUIWindow, XamlRoot xamlRoot, Action configureCallback, Action closingCallback, Action<bool> focusCallback, Action<bool> visibilityCallback)
	{
		_wrapper = wrapper;
		_window = winUIWindow;

		_closingCallback = closingCallback;
		_focusCallback = focusCallback;
		_visibilityCallback = visibilityCallback;
		_configureCallback = configureCallback;

		_closed = new TaskCompletionSource();
		Closed = _closed.Task;

		_configureTimer.Tick += (_, _) =>
		{
			_configureTimer.Stop();
			_configureScheduled = 0;
			_configureCallback();
		};

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += UpdateWindowPropertiesFromCoreApplication;
		winUIWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged += ExtendContentIntoTitleBar;

		Initialize();

		// Note: the timing of XamlRootMap.Register is very fragile. It needs to be early enough
		// so things like UpdateWindowPropertiesFromPackage can read the DPI, but also late enough so that
		// the X11Window is "initialized".
		_windowToHost[winUIWindow] = this;
		X11Manager.XamlRootMap.Register(xamlRoot, this);

		UpdateWindowPropertiesFromPackage();
		OnApplicationViewPropertyChanged(this, new PropertyChangedEventArgs(null));

		// only start listening to events after we're done setting everything up
		InitializeX11EventsThread();
		_renderingEventLoop = new(start => new Thread(start)
		{
			Name = $"Uno X11 Rendering thread {_id}",
			IsBackground = true
		});

		var windowBackgroundDisposable = _window.RegisterBackgroundChangedEvent((_, _) => UpdateRendererBackground());
		UpdateRendererBackground();

		Closed.ContinueWith(_ =>
		{
			using (X11Helper.XLock(RootX11Window.Display))
			{
				X11Manager.XamlRootMap.Unregister(xamlRoot);
				_windowToHost.Remove(winUIWindow, out var _);
				_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
				CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged -= UpdateWindowPropertiesFromCoreApplication;
				winUIWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged -= ExtendContentIntoTitleBar;
				windowBackgroundDisposable.Dispose();
				_renderingEventLoop.Dispose();
			}
		});
	}

	public static X11XamlRootHost? GetHostFromWindow(Window window)
		=> _windowToHost.TryGetValue(window, out var host) ? host : null;

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

			XLib.XSetWMNormalHints(RootX11Window.Display, RootX11Window.Window, ref hints);
		}
	}

	internal void UpdateWindowPropertiesFromCoreApplication()
	{
		var coreApplicationView = CoreApplication.GetCurrentView();

		ExtendContentIntoTitleBar(coreApplicationView.TitleBar.ExtendViewIntoTitleBar);
	}

	internal void ExtendContentIntoTitleBar(bool extend) => X11Helper.SetMotifWMDecorations(RootX11Window, !extend, 0xFF);

	private void UpdateWindowPropertiesFromPackage()
	{
		Task.Run(SetWindowIcon);

		if (!string.IsNullOrEmpty(Windows.ApplicationModel.Package.Current.DisplayName))
		{
			_applicationView.Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}
	}

	private void SetWindowIcon()
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
			using var _freeDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, data);

			var ptr = (IntPtr*)data.ToPointer();
			*(ptr++) = bitmap.Width;
			*(ptr++) = bitmap.Height;
			foreach (var pixel in bitmap.Pixels)
			{
				*(ptr++) = pixel.Alpha << 24 | pixel.Red << 16 | pixel.Green << 8 | pixel.Blue << 0;
			}

			var display = RootX11Window.Display;
			using var lockDisposable = X11Helper.XLock(display);

			var wmIconAtom = X11Helper.GetAtom(display, X11Helper._NET_WM_ICON);
			var cardinalAtom = X11Helper.GetAtom(display, X11Helper.XA_CARDINAL);
			_ = XLib.XChangeProperty(
				display,
				RootX11Window.Window,
				wmIconAtom,
				cardinalAtom,
				32,
				PropertyMode.Replace,
				data,
				pixels.Length + 2);

			_ = XLib.XFlush(display);
			_ = XLib.XSync(display, false); // wait until the pixels are actually copied
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
				using (X11Helper.XLock(host.RootX11Window.Display))
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
		using var _mutexDisposable = Disposable.Create(() =>
		{
			// set _firstWindowCreated even if we crash. This prevents the Main thread from being
			// kept alive forever even if the main window creation crashed.
			lock (_x11WindowToXamlRootHostMutex)
			{
				_firstWindowCreated = true;
			}
		});

		IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);

		using var lockDisposable = X11Helper.XLock(display);

		if (display == IntPtr.Zero)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("XLIB ERROR: Cannot connect to X server");
			}
			throw new InvalidOperationException("XLIB ERROR: Cannot connect to X server");
		}

		int screen = XLib.XDefaultScreen(display);

		var size = ApplicationView.PreferredLaunchViewSize;
		if (size == Size.Empty)
		{
			size = new Size(NativeWindowWrapperBase.InitialWidth, NativeWindowWrapperBase.InitialHeight);
		}

		// For the root window (that does nothing but act as an anchor for children,
		// we don't bother with OpenGL, since we don't render on this window anyway.
		IntPtr rootXWindow = XLib.XRootWindow(display, screen);
		_x11Window = CreateSoftwareRenderWindow(display, screen, size, rootXWindow);
		var topWindowDisplay = XLib.XOpenDisplay(IntPtr.Zero);
		_x11TopWindow = FeatureConfiguration.Rendering.UseOpenGLOnX11 ?? IsOpenGLSupported(display)
			? CreateGLXWindow(topWindowDisplay, screen, size, RootX11Window.Window)
			: CreateSoftwareRenderWindow(topWindowDisplay, screen, size, RootX11Window.Window);

		// Only XI2.2 has touch events, and that's pretty much the only reason we're using XI2,
		// so to make our assumptions simpler, we assume XI >= 2.2 or no XI at all.
		var usingXi2 = GetXI2Details(display).version >= XIVersion.XI2_2;
		if (usingXi2)
		{
			var mask = XI2Mask;
			if (GetXI2Details(display).version >= XIVersion.XI2_2)
			{
				mask |= XI2_2Mask;
			}
			SetXIEventMask(TopX11Window.Display, TopX11Window.Window, mask);
		}

		XLib.XSelectInput(RootX11Window.Display, RootX11Window.Window, RootEventsMask);
		// to update dpi when X resources change
		XLib.XSelectInput(RootX11Window.Display, rootXWindow, (IntPtr)EventMask.PropertyChangeMask);
		// We make sure not to select events that will be handled by a corresponding XI2 event
		XLib.XSelectInput(TopX11Window.Display, TopX11Window.Window, usingXi2 ? TopEventsMask & ~EventsHandledByXI2Mask : TopEventsMask);

		// Tell the WM to send a WM_DELETE_WINDOW message before closing
		IntPtr deleteWindow = X11Helper.GetAtom(display, X11Helper.WM_DELETE_WINDOW);
		_ = XLib.XSetWMProtocols(RootX11Window.Display, RootX11Window.Window, new[] { deleteWindow }, 1);

		lock (_x11WindowToXamlRootHostMutex)
		{
			_firstWindowCreated = true;
			_x11WindowToXamlRootHost[RootX11Window] = this;
		}

		_ = X11Helper.XClearWindow(RootX11Window.Display, RootX11Window.Window); // the root window is never drawn, just always blank

		if (FeatureConfiguration.Rendering.UseOpenGLOnX11 ?? IsOpenGLSupported(TopX11Window.Display))
		{
			_renderer = new X11OpenGLRenderer(this, TopX11Window);
		}
		else
		{
			_renderer = new X11SoftwareRenderer(this, TopX11Window);
		}
	}

	// https://github.com/gamedevtech/X11OpenGLWindow/blob/4a3d55bb7aafd135670947f71bd2a3ee691d3fb3/README.md
	// https://learnopengl.com/Advanced-OpenGL/Framebuffers
	private unsafe static X11Window CreateGLXWindow(IntPtr display, int screen, Size size, IntPtr parent)
	{
		IntPtr bestFbc = IntPtr.Zero;
		XVisualInfo* visual = null;
		var ptr = GlxInterface.glXChooseFBConfig(display, screen, _glxAttribs, out var count);
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
		_ = XLib.XSync(display, false);

		XSetWindowAttributes attribs = default;
		// Setting the colormap here is necessary, otherwise we get a GLX error on some environments. cf. https://github.com/unoplatform/uno/issues/21285
		attribs.colormap = XLib.XCreateColormap(display, parent, visual->visual, /* AllocNone */ 0);
		var window = XLib.XCreateWindow(
			display,
			parent,
			0,
			0,
			(int)size.Width,
			(int)size.Height,
			0,
			(int)visual->depth,
			/* InputOutput */ 1,
			visual->visual,
			(UIntPtr)XCreateWindowFlags.CWColormap,
			ref attribs);

		_ = GlxInterface.glXGetFBConfigAttrib(display, bestFbc, GlxConsts.GLX_STENCIL_SIZE, out var stencil);
		_ = GlxInterface.glXGetFBConfigAttrib(display, bestFbc, GlxConsts.GLX_SAMPLES, out var samples);
		return new X11Window(display, window, (stencil, samples, context));
	}

	private static X11Window CreateSoftwareRenderWindow(IntPtr display, int screen, Size size, IntPtr parent)
	{
		var matchVisualInfoResult = XLib.XMatchVisualInfo(display, screen, DefaultColorDepth, 4, out var info);
		var success = matchVisualInfoResult != 0;
		if (!success)
		{
			matchVisualInfoResult = XLib.XMatchVisualInfo(display, screen, FallbackColorDepth, 4, out info);

			success = matchVisualInfoResult != 0;
			if (!success)
			{
				if (typeof(X11XamlRootHost).Log().IsEnabled(LogLevel.Error))
				{
					typeof(X11XamlRootHost).Log().Error("XLIB ERROR: Cannot match visual info");
				}
				throw new InvalidOperationException("XLIB ERROR: Cannot match visual info");
			}
		}

		var visual = info.visual;
		var depth = info.depth;

		var xSetWindowAttributes = new XSetWindowAttributes()
		{
			// Settings to true when WindowStyle is None
			//override_redirect = true,
			colormap = XLib.XCreateColormap(display, parent, visual, /* AllocNone */ 0),
			border_pixel = 0,
		};
		var valueMask =
				0
				| SetWindowValuemask.BorderPixel
				| SetWindowValuemask.ColorMap
			//| SetWindowValuemask.OverrideRedirect
			;
		var window = XLib.XCreateWindow(display, parent, 0, 0, (int)size.Width,
			(int)size.Height, 0, (int)depth, /* InputOutput */ 1, visual,
			(UIntPtr)(valueMask), ref xSetWindowAttributes);
		return new X11Window(display, window);
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

	UIElement? IXamlRootHost.RootElement => _window.RootElement;

	private void RaiseConfigureCallback()
	{
		if (Interlocked.Exchange(ref _configureScheduled, 1) == 0)
		{
			// Don't use ticks, which seem to mess things up for some reason
			var now = _stopwatch.ElapsedMilliseconds;
			var delta = now - Interlocked.Exchange(ref _lastConfigureTime, now);
			if (delta > TimeSpan.FromSeconds(1.0 / X11ApplicationHost.RenderFrameRate).TotalMilliseconds)
			{
				QueueAction(this, () =>
				{
					_configureScheduled = 0;
					_configureCallback();
				});
			}
			else
			{
				_configureTimer.Interval = TimeSpan.FromTicks(delta);
				_configureTimer.Start();
			}
		}
	}

	public unsafe void AttachSubWindow(IntPtr window)
	{
		using var lockDisposable = X11Helper.XLock(RootX11Window.Display);
		// this seems to be necessary or else the WM will keep detaching the subwindow
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(RootX11Window.Display, window, ref attributes);
		attributes.override_direct = /* True */ 1;

		IntPtr attr = Marshal.AllocHGlobal(Marshal.SizeOf(attributes));
		Marshal.StructureToPtr(attributes, attr, false);
		_ = X11Helper.XChangeWindowAttributes(RootX11Window.Display, window, (IntPtr)XCreateWindowFlags.CWOverrideRedirect, (XSetWindowAttributes*)attr.ToPointer());
		Marshal.FreeHGlobal(attr);

		_ = X11Helper.XReparentWindow(RootX11Window.Display, window, RootX11Window.Window, 0, 0);
		_ = XLib.XFlush(RootX11Window.Display);
		XLib.XSync(RootX11Window.Display, false); // XSync is necessary after XReparent for unknown reasons
	}

	private void SynchronizedShutDown(X11Window x11Window)
	{
		// This is extremely extremely delicate. You want to prevent any

		if (x11Window == TopX11Window)
		{
			var waitForIdle = () =>
			{
				using var lockDiposable = X11Helper.XLock(TopX11Window.Display);
				_ = XLib.XFlush(TopX11Window.Display);
				_ = XLib.XSync(TopX11Window.Display, false);
				_synchronizedShutDownTopWindowIdleCounter++;
			};
			for (int i = 0; i < 10; i++)
			{
				QueueAction(this, waitForIdle);
			}
		}
		else // RootX11Window
		{
			Debug.Assert(x11Window == RootX11Window);

			SpinWait.SpinUntil(() => _synchronizedShutDownTopWindowIdleCounter == 10);
			if (x11Window == RootX11Window)
			{
				// Be very cautious about making any changes here.
				using var rootLockDiposable = X11Helper.XLock(RootX11Window.Display);
				using var topLockDiposable = X11Helper.XLock(TopX11Window.Display);
				_ = XLib.XFlush(TopX11Window.Display);
				_ = XLib.XFlush(RootX11Window.Display);
				_ = XLib.XDestroyWindow(TopX11Window.Display, TopX11Window.Window);
				_ = XLib.XDestroyWindow(RootX11Window.Display, RootX11Window.Window);
				_ = XLib.XFlush(RootX11Window.Display);
			}
		}
	}

	private void UpdateRendererBackground()
	{
		if (_window.Background is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
		{
			if (_renderer is not null)
			{
				_renderer.SetBackgroundColor(brush.Color);
			}
		}
		else if (_window.Background is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"This platform only supports SolidColorBrush for the Window background");
			}
		}
	}
}
