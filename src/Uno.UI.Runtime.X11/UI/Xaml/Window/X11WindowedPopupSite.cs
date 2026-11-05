#nullable enable

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;

namespace Uno.UI.Runtime.X11;

/// <summary>
/// A popup's top-level window: override-redirect, so no window manager decorates, moves or focuses it, and typed
/// <c>_NET_WM_WINDOW_TYPE_POPUP_MENU</c>, which is what EWMH asks of such a window so compositors treat it as a menu.
/// Everything it shows comes from a frame replayed on a graphics context of its own, on its own render thread.
/// </summary>
/// <remarks>
/// It has its own connection, for three reasons: <c>eglTerminate</c> ends everything on an X display, an X client
/// can't drop one window's GL state alone, and closing the connection leaves nothing behind. Its input is selected
/// through the owner's top connection instead, so events reach the owner's event thread and pointer source.
/// </remarks>
internal sealed class X11WindowedPopupSite : IWindowedPopupSite
{
	private const uint CWSibling = 1 << 5;
	private const uint CWStackMode = 1 << 6;
	private const int XIGrabModeAsync = 1;

	private readonly X11XamlRootHost _owner;
	private readonly IWindowedPopupContent _content;
	private readonly IntPtr _display;
	private readonly int _screen;
	private readonly object _renderGate = new();

	private X11Window? _window;
	private ISwapChain _context = null!;
	private IDrawingFactory _renderer = null!;
	private Thread? _renderThread;
	private bool _renderRequested;

	private RectInt32 _bounds;
	private RectInt32 _contentBounds;
	private bool _hasAlphaChannel;
	private bool _showRequested;
	private bool _isShown;
	private bool _isOwnerMapped = true;
	private int? _capturedDevice; // Guarded by the owner's top connection lock.
	private int _disposed;
	private volatile bool _isClosed;

	private X11WindowedPopupSite(X11XamlRootHost owner, IWindowedPopupContent content, IntPtr display)
	{
		_owner = owner;
		_content = content;
		_display = display;
		_screen = XLib.XDefaultScreen(display);
	}

	internal IntPtr Window => _window!.Value.Window;

	internal static IWindowedPopupSite? TryCreate(X11XamlRootHost owner, IWindowedPopupContent content)
	{
		// A missing library or symbol must leave the popup in its window, not take the app down.
		X11WindowedPopupSite? site = null;
		try
		{
			var display = XLib.XOpenDisplay(IntPtr.Zero);
			if (display == IntPtr.Zero)
			{
				return null;
			}

			site = new X11WindowedPopupSite(owner, content, display);
			site.Initialize();
			return site;
		}
		catch (Exception e)
		{
			typeof(X11WindowedPopupSite).LogWarn()?.Warn($"A popup can't get a window, so it stays in its main window: {e.Message}");
			site?.Dispose();
			return null;
		}
	}

	private unsafe void Initialize()
	{
		// Negotiation walks the app's backend order, this only answers for the owner's kind and the software fallback.
		GraphicsRegistry.ContextFactory = kind => Task.FromResult(CreateWindowAndContext(kind));

		var init = GraphicsRegistry.Initialize();
		_context = init.Context;
		_renderer = init.Renderer;

		if (_context.Kind != _owner.GraphicsContextKind)
		{
			this.LogWarn()?.Warn($"A popup window can't be transparent on {_owner.GraphicsContextKind}, so it is rendered with {_context.Kind}.");
		}

		using (X11Helper.XLock(_display))
		{
			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(_display, Window, ref attributes);
			_hasAlphaChannel = attributes.depth == 32;

			var type = X11Helper.GetAtom(_display, "_NET_WM_WINDOW_TYPE");
			var atom = X11Helper.GetAtom(_display, X11Helper.XA_ATOM);
			_ = XLib.XChangeProperty(_display, Window, type, atom, 32, PropertyMode.Replace, new[] { X11Helper.GetAtom(_display, "_NET_WM_WINDOW_TYPE_POPUP_MENU") }, 1);

			_ = XLib.XChangeProperty(
				_display,
				Window,
				X11Helper.GetAtom(_display, "WM_TRANSIENT_FOR"),
				X11Helper.GetAtom(_display, "WINDOW"),
				32,
				PropertyMode.Replace,
				new[] { _owner.RootX11Window.Window },
				1);

			_ = XLib.XStoreName(_display, Window, "PopupHost");

			// It exists on the server before the owner's connection selects its input.
			_ = XLib.XSync(_display, false);
		}

		this.LogDebug()?.Debug($"Popup window 0x{Window.ToString("X", CultureInfo.InvariantCulture)} renders with {_context.Kind}.");

		_owner.AddPopupSite(this);

		_renderThread = new Thread(RenderLoop)
		{
			IsBackground = true,
			Name = "X11 popup render",
			Priority = ThreadPriority.AboveNormal,
		};
		_renderThread.Start();
	}

	private ISwapChain? CreateWindowAndContext(GraphicsContextKind kind)
	{
		if (kind != _owner.GraphicsContextKind && kind != GraphicsContextKind.Software)
		{
			return null;
		}

		DestroyWindow();

		using var lockDisposable = X11Helper.XLock(_display);

		var root = XLib.XRootWindow(_display, _screen);
		Size size = new(1, 1);

		try
		{
			switch (kind)
			{
				case GraphicsContextKind.OpenGL:
					_window = X11XamlRootHost.CreateGLXWindow(_display, _screen, size, root, isPopup: true);
					_ = XLib.XSync(_display, false);
					return new X11OpenGLGraphicsContext(_window.Value);

				case GraphicsContextKind.OpenGLES:
					_window = X11XamlRootHost.CreateSoftwareRenderWindow(_display, _screen, size, root, isPopup: true);
					return new X11EGLGraphicsContext(_window.Value);

				case GraphicsContextKind.Vulkan:
					_window = X11XamlRootHost.CreateSoftwareRenderWindow(_display, _screen, size, root, isPopup: true);
					return new X11VulkanGraphicsContext(_window.Value, transparent: true);

				case GraphicsContextKind.Software:
					_window = X11XamlRootHost.CreateSoftwareRenderWindow(_display, _screen, size, root, isPopup: true);
					return new X11SoftwareGraphicsContext(_window.Value);

				default:
					return null;
			}
		}
		catch (Exception e)
		{
			this.LogInfo()?.Info($"A popup window has no {kind} context ({e.Message}), falling through.");
			DestroyWindow();
			return null;
		}
	}

	private void DestroyWindow()
	{
		if (_window is { } window)
		{
			using var lockDisposable = X11Helper.XLock(_display);

			if (window.glXInfo is { } glx)
			{
				GlxInterface.glXDestroyContext(_display, glx.context);
			}

			_ = XLib.XDestroyWindow(_display, window.Window);
			_ = XLib.XSync(_display, false);
			_window = null;
		}
	}

	private void RenderLoop()
	{
		while (true)
		{
			lock (_renderGate)
			{
				while (!_renderRequested && !_isClosed)
				{
					Monitor.Wait(_renderGate);
				}

				_renderRequested = false;
			}

			if (_isClosed)
			{
				return;
			}

			try
			{
				DrawFrame();
			}
			catch (Exception e)
			{
				this.LogError()?.Error("Failed to render a popup window.", e);
			}
		}
	}

	private void DrawFrame()
	{
		var drawn = _content.DrawFrame(_context, _renderer);

		using (X11Helper.XLock(_display))
		{
			_ = XLib.XFlush(_display);
		}

		if (!drawn)
		{
			return;
		}

		// A just-resized swapchain drops its frame. No other frame may come, so the same one is presented again.
		if (_context is X11VulkanGraphicsContext { PresentedLastFrame: false })
		{
			InvalidateRender();
			return;
		}

		if (!_isShown)
		{
			NativeDispatcher.Main.Enqueue(ShowIfRequested, NativeDispatcherPriority.High);
		}
	}

	private void ShowIfRequested()
	{
		if (_showRequested && !_isShown && !_isClosed)
		{
			_isShown = true;
			ApplyMapState();
			_owner.RestackPopupSites(this);

			// Not every swapchain keeps what a hidden window was given.
			InvalidateRender();
		}
	}

	private void ApplyMapState()
	{
		using var lockDisposable = X11Helper.XLock(_display);

		if (_isShown && _isOwnerMapped)
		{
			_ = XLib.XMapWindow(_display, Window);
		}
		else
		{
			_ = X11Helper.XUnmapWindow(_display, Window);
		}

		_ = XLib.XFlush(_display);
	}

	public bool IsClosed => _isClosed;

	public bool HasFocus
	{
		get
		{
			if (_isClosed)
			{
				return false;
			}

			using var lockDisposable = X11Helper.XLock(_display);
			_ = X11Helper.XGetInputFocus(_display, out var focus, out _);
			return focus == Window;
		}
	}

	public bool HasPointerCapture
	{
		get
		{
			using var lockDisposable = X11Helper.XLock(_owner.TopX11Window.Display);
			return _capturedDevice is not null;
		}
	}

	public RectInt32 ScreenBounds
	{
		get
		{
			if (_isClosed)
			{
				return default;
			}

			// Override-redirect, so it's never reparented: the position is relative to the screen.
			var attributes = GetAttributes();
			return new RectInt32(attributes.x, attributes.y, attributes.width, attributes.height);
		}
	}

	public bool IsVisible => !_isClosed && GetAttributes().map_state == MapState.IsViewable;

	private XWindowAttributes GetAttributes()
	{
		using var lockDisposable = X11Helper.XLock(_display);
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(_display, Window, ref attributes);
		return attributes;
	}

	public void MoveAndResize(RectInt32 rect, RectInt32 contentBounds)
	{
		if (_isClosed || (rect.Equals(_bounds) && contentBounds.Equals(_contentBounds)))
		{
			return;
		}

		var isResized = rect.Width != _bounds.Width || rect.Height != _bounds.Height;
		_bounds = rect;
		_contentBounds = contentBounds;

		using var lockDisposable = X11Helper.XLock(_display);

		_ = X11Helper.XMoveResizeWindow(_display, Window, rect.X, rect.Y, (uint)Math.Max(rect.Width, 1), (uint)Math.Max(rect.Height, 1));
		UpdateShape();

		// The render thread sizes its buffers to the window, so a new size has to have reached the server.
		if (isResized)
		{
			_ = XLib.XSync(_display, false);
		}
		else
		{
			_ = XLib.XFlush(_display);
		}
	}

	// The gutter never takes input, so a press on the shadow lands on whatever is under it, in any app.
	// Without a compositor nothing blends the window's alpha, so the gutter is cut off and no shadow shows.
	private unsafe void UpdateShape()
	{
		XRectangle content = new()
		{
			X = (short)(_contentBounds.X - _bounds.X),
			Y = (short)(_contentBounds.Y - _bounds.Y),
			W = (short)_contentBounds.Width,
			H = (short)_contentBounds.Height,
		};

		X11Helper.XShapeCombineRectangles(_display, Window, X11Helper.ShapeInput, 0, 0, &content, 1, X11Helper.ShapeSet, X11Helper.Unsorted);

		if (_hasAlphaChannel && HasCompositor())
		{
			X11Helper.XShapeCombineMask(_display, Window, X11Helper.ShapeBounding, 0, 0, IntPtr.Zero, X11Helper.ShapeSet);
		}
		else
		{
			X11Helper.XShapeCombineRectangles(_display, Window, X11Helper.ShapeBounding, 0, 0, &content, 1, X11Helper.ShapeSet, X11Helper.Unsorted);
		}
	}

	private bool HasCompositor()
		=> XLib.XGetSelectionOwner(_display, X11Helper.GetAtom(_display, "_NET_WM_CM_S" + _screen.ToString(CultureInfo.InvariantCulture))) != X11Helper.None;

	public void Show()
	{
		_showRequested = true;
		_isShown = false;
	}

	public void Hide()
	{
		_showRequested = false;
		_isShown = false;

		if (!_isClosed)
		{
			ReleasePointerCapture();
			ApplyMapState();
		}
	}

	/// <remarks>
	/// Only while a mouse button is held, which the owner's event thread ends without needing the UI thread. A press
	/// already has an implicit grab, this keeps it when the capturing element isn't where the press landed.
	/// </remarks>
	public unsafe void SetPointerCapture()
	{
		var display = _owner.TopX11Window.Display;
		using var lockDisposable = X11Helper.XLock(display);

		if (_isClosed || _capturedDevice is not null || _owner.PressedMouseDevice is not { } device)
		{
			return;
		}

		var maskBits = X11XamlRootHost.PopupGrabXI2Mask;
		XIEventMask mask = new()
		{
			Deviceid = device,
			MaskLen = sizeof(int),
			Mask = &maskBits,
		};

		// Asynchronous, so nothing freezes on the server. Owner events, so the main window still gets what's over it.
		var status = X11Helper.XIGrabDevice(display, device, Window, X11Helper.CurrentTime, X11Helper.None, XIGrabModeAsync, XIGrabModeAsync, owner_events: 1, &mask);
		if (status == X11Helper.Success)
		{
			_capturedDevice = device;
			_ = XLib.XFlush(display);
		}
		else
		{
			this.LogDebug()?.Debug($"{nameof(X11Helper.XIGrabDevice)} failed: {status}");
		}
	}

	public void ReleasePointerCapture()
	{
		var display = _owner.TopX11Window.Display;
		using var lockDisposable = X11Helper.XLock(display);

		if (_capturedDevice is { } device)
		{
			_capturedDevice = null;
			_ = X11Helper.XIUngrabDevice(display, device, X11Helper.CurrentTime);
			_ = XLib.XFlush(display);
		}
	}

	public void InvalidateRender()
	{
		lock (_renderGate)
		{
			_renderRequested = true;
			Monitor.PulseAll(_renderGate);
		}
	}

	internal void OnOwnerBoundsChanged() => _content.OnOwnerBoundsChanged();

	internal void SetOwnerMapped(bool isMapped)
	{
		_isOwnerMapped = isMapped;

		if (!_isClosed && _isShown)
		{
			ApplyMapState();
		}
	}

	/// <summary>Puts the window right above <paramref name="sibling"/>, so what covers the owner covers it as well.</summary>
	internal unsafe void StackAbove(IntPtr sibling)
	{
		if (_isClosed)
		{
			return;
		}

		using var lockDisposable = X11Helper.XLock(_display);

		XWindowChanges changes = new()
		{
			sibling = sibling,
			stack_mode = StackMode.Above,
		};
		_ = X11Helper.XConfigureWindow(_display, Window, CWSibling | CWStackMode, &changes);
		_ = XLib.XFlush(_display);
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		if (_window is not null)
		{
			ReleasePointerCapture();
		}

		_isClosed = true;
		_owner.RemovePopupSite(this);

		lock (_renderGate)
		{
			Monitor.PulseAll(_renderGate);
		}

		// The thread is the sole user of the context, so it has to stop first.
		if (_renderThread is { } thread && !thread.Join(TimeSpan.FromSeconds(5)))
		{
			this.LogError()?.Error("A popup window's render thread didn't stop, so its graphics are left behind.");
			return;
		}

		if (_renderer is not null)
		{
			// The backend's GPU resources can only go while their context is current.
			(_context as IX11GpuTeardownContext)?.MakeCurrentForTeardown();
			(_renderer as IDisposable)?.Dispose();
			_context.Dispose();

			NativeDispatcher.Main.Enqueue(_content.OnSiteClosed, NativeDispatcherPriority.High);
		}

		DestroyWindow();

		using (X11Helper.XLock(_display))
		{
			_ = XLib.XCloseDisplay(_display);
		}
	}
}
