#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;

namespace Uno.UI.Runtime.MacOS;

/// <summary>
/// A popup's window: a borderless, clear child window of the main one, which it follows in position and z-order. It can't
/// become key or main, so the keyboard stays on the main window. Everything it shows comes from a frame replayed on a
/// graphics context of its own: on its own render thread for Metal, from the view's draw callback for software.
/// </summary>
internal sealed class MacOSWindowedPopupSite : IWindowedPopupSite
{
	private readonly MacOSWindowHost _owner;
	private readonly IWindowedPopupContent _content;
	private readonly nint _window;
	private readonly object _renderGate = new();

	private ISwapChain _context = null!;
	private IDrawingFactory _renderer = null!;
	private Thread? _renderThread;
	private bool _renderRequested;
	private RectInt32 _bounds;
	private bool _showRequested;
	private bool _isShown;
	private bool _hasPointerCapture;
	private volatile bool _isClosed;

	private MacOSWindowedPopupSite(MacOSWindowHost owner, IWindowedPopupContent content, nint window)
	{
		_owner = owner;
		_content = content;
		_window = window;
	}

	internal nint Handle => _window;

	internal MacOSWindowHost Owner => _owner;

	internal static IWindowedPopupSite? TryCreate(MacOSWindowHost owner, IWindowedPopupContent content)
	{
		MacOSWindowedPopupSite? site = null;
		try
		{
			// An older libUnoNativeMac has no such export: the popup stays in its main window.
			var window = NativeUno.uno_popup_window_create(owner.NativeHandle);
			if (window == 0)
			{
				typeof(MacOSWindowedPopupSite).LogWarn()?.Warn("A popup can't get a window, so it stays in its main window: the native window wasn't created.");
				return null;
			}

			site = new MacOSWindowedPopupSite(owner, content, window);
			site.Initialize();
			return site;
		}
		catch (Exception e)
		{
			typeof(MacOSWindowedPopupSite).LogWarn()?.Warn($"A popup can't get a window, so it stays in its main window: {e.Message}");
			site?.Dispose();
			return null;
		}
	}

	private void Initialize()
	{
		var init = GraphicsRegistry.Initialize(kind => Task.FromResult(CreateContext(kind)));
		_context = init.Context;
		_renderer = init.Renderer;

		_owner.AddPopupSite(this);

		if (_context is MacOSMetalGraphicsContext)
		{
			NativeUno.uno_window_set_external_present(_window, true);

			// Acquiring a drawable can block for a second, which must not happen on the main thread.
			_renderThread = new Thread(RenderLoop)
			{
				IsBackground = true,
				Name = "macOS popup render",
			};
			_renderThread.Start();
		}
	}

	private ISwapChain? CreateContext(GraphicsContextKind kind)
	{
		if (kind != _owner.GraphicsContextKind)
		{
			return null;
		}

		switch (kind)
		{
			case GraphicsContextKind.Metal:
				NativeUno.uno_window_get_metal_handles(_window, out var device, out var queue);
				return new MacOSMetalGraphicsContext(_window, device, queue);
			case GraphicsContextKind.Software:
				return new MacOSSoftwareGraphicsContext();
			default:
				return null;
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
				if (_content.DrawFrame(_context, _renderer))
				{
					OnFramePresented();
				}
			}
			catch (Exception e)
			{
				this.LogError()?.Error("Failed to render a popup window.", e);
			}
		}
	}

	internal unsafe void SoftDraw(double width, double height, nint* data, int* rowBytes, int* size)
	{
		if (_isClosed || _context is not MacOSSoftwareGraphicsContext context)
		{
			return;
		}

		var hasFrame = _content.DrawFrame(context, _renderer);

		// The view only maps its whole size onto the buffer, which is behind for as long as the window was just resized.
		var scale = _owner.RasterizationScale;
		if (context.CurrentTarget is { } target
			&& target.Width == (int)Math.Round(width * scale)
			&& target.Height == (int)Math.Round(height * scale))
		{
			*data = target.Pixels;
			*size = target.Height * target.RowBytes;
			*rowBytes = target.RowBytes;
		}

		if (hasFrame)
		{
			OnFramePresented();
		}
	}

	private void OnFramePresented()
	{
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
			NativeUno.uno_popup_window_show(_window);
			InvalidateRender();
		}
	}

	public bool IsClosed => _isClosed;

	public bool HasFocus => false;

	// Cocoa keeps sending a drag to the window which took the mouse down, so there's nothing to set up.
	public bool HasPointerCapture => _hasPointerCapture;

	public RectInt32 ScreenBounds
	{
		get
		{
			if (_isClosed)
			{
				return default;
			}

			NativeUno.uno_popup_window_get_frame(_window, out var x, out var y, out var width, out var height);
			var scale = _owner.RasterizationScale;
			return new RectInt32((int)Math.Round(x * scale), (int)Math.Round(y * scale), (int)Math.Round(width * scale), (int)Math.Round(height * scale));
		}
	}

	public bool IsVisible => !_isClosed && NativeUno.uno_popup_window_is_visible(_window);

	public bool IsShown => _isShown;

	public void MoveAndResize(RectInt32 rect, RectInt32 contentBounds)
	{
		if (_isClosed || rect.Equals(_bounds))
		{
			return;
		}

		_bounds = rect;

		var scale = _owner.RasterizationScale;
		NativeUno.uno_popup_window_set_frame(_window, rect.X / scale, rect.Y / scale, Math.Max(rect.Width, 1) / scale, Math.Max(rect.Height, 1) / scale);
	}

	public void Show()
	{
		_showRequested = true;
		_isShown = false;
	}

	public void Hide()
	{
		_showRequested = false;
		_isShown = false;
		_hasPointerCapture = false;

		if (!_isClosed)
		{
			NativeUno.uno_popup_window_hide(_window);
		}
	}

	public void SetPointerCapture() => _hasPointerCapture = true;

	public void ReleasePointerCapture() => _hasPointerCapture = false;

	public void InvalidateRender()
	{
		if (_isClosed)
		{
			return;
		}

		if (_renderThread is null)
		{
			NativeUno.uno_window_invalidate(_window);
			return;
		}

		lock (_renderGate)
		{
			_renderRequested = true;
			Monitor.PulseAll(_renderGate);
		}
	}

	internal void OnOwnerBoundsChanged() => _content.OnOwnerBoundsChanged();

	public void Dispose()
	{
		if (_isClosed)
		{
			return;
		}

		_isClosed = true;
		_owner.RemovePopupSite(this);

		lock (_renderGate)
		{
			Monitor.PulseAll(_renderGate);
		}

		try
		{
			// The thread is the sole user of the context, so it has to stop first.
			if (_renderThread is { } thread && !thread.Join(TimeSpan.FromSeconds(5)))
			{
				// It may still be drawing on the window, which can only go out of sight.
				this.LogError()?.Error("A popup window's render thread didn't stop, so its graphics are left behind.");
				NativeUno.uno_popup_window_hide(_window);
				return;
			}

			try
			{
				try
				{
					(_renderer as IDisposable)?.Dispose();
				}
				finally
				{
					_context?.Dispose();
				}
			}
			finally
			{
				NativeUno.uno_popup_window_destroy(_window);
			}
		}
		finally
		{
			NativeDispatcher.Main.Enqueue(_content.OnSiteClosed, NativeDispatcherPriority.High);
		}
	}
}
