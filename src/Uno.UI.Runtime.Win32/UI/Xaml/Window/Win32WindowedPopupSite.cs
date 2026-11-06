#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Win32;

/// <summary>
/// A popup's top-level window: owned by the main window, so it follows its z-order and stays out of the taskbar, and it
/// never activates, so the keyboard stays on the main window. Everything it shows comes from a frame replayed on a
/// graphics context of its own, on its own render thread.
/// </summary>
/// <remarks>
/// Its per-pixel alpha takes a different path per backend. Each was measured to composite on a real desktop, with a fallback
/// to the last one when the first two can't be set up:
/// <list type="bullet">
/// <item>Vulkan: a swapchain with <c>VK_COMPOSITE_ALPHA_PRE_MULTIPLIED_BIT_KHR</c>.</item>
/// <item>OpenGL: a framebuffer with alpha, on a window with an empty DWM blur-behind region.</item>
/// <item>Software: <c>UpdateLayeredWindow</c>. Only this path is a layered window.</item>
/// </list>
/// </remarks>
internal sealed class Win32WindowedPopupSite : IWindowedPopupSite
{
	private const string WindowClassName = "UnoPlatformPopupWindow";

	private const int MA_NOACTIVATE = 3;
	private const int PA_NOACTIVATE = 3;
	private const int HTCLIENT = 1;
	private const uint WM_NCDESTROY = 0x0082;
	private const int HTTRANSPARENT = -1;

	// lpfnWndProc must stay reachable for as long as the class is registered.
	private static readonly WNDCLASSEXW _windowClass;
	private static readonly Dictionary<HWND, Win32WindowedPopupSite> _hwndToSite = new();

	// The first WndProc calls happen inside CreateWindowEx, before the HWND is known.
	private static Win32WindowedPopupSite? _siteForNextCreateWindow;

	private readonly Win32WindowWrapper _owner;
	private readonly IWindowedPopupContent _content;
	private readonly HWND _hwnd;

	private ISwapChain _context = null!;
	private IDrawingFactory _renderer = null!;
	private Win32WindowWrapper.RenderThread? _renderThread;

	private RectInt32 _bounds;
	private RectInt32 _contentBounds;
	private bool _isLayered;
	private bool _showRequested;
	private bool _isShown;
	private volatile bool _isClosed;

	static unsafe Win32WindowedPopupSite()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = &WndProc,
			hInstance = Win32Helper.GetHInstance(),
			lpszClassName = lpClassName,
		};

		if (PInvoke.RegisterClassEx(_windowClass) is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	private unsafe Win32WindowedPopupSite(Win32WindowWrapper owner, IWindowedPopupContent content)
	{
		_owner = owner;
		_content = content;

		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		// Ensure that the window text is correct for UIA.
		// TODO Uno: GetLocalizedResourceString(UIA_WINDOWED_POPUP_HOST), which has no localized variants here.
		using var title = new Win32Helper.NativeNulTerminatedUtf16String("PopupHost");

		_siteForNextCreateWindow = this;
		_hwnd = PInvoke.CreateWindowEx(
			WINDOW_EX_STYLE.WS_EX_NOACTIVATE | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW,
			lpClassName,
			title,
			WINDOW_STYLE.WS_POPUP,
			0,
			0,
			1,
			1,
			owner.Hwnd,
			HMENU.Null,
			Win32Helper.GetHInstance(),
			null);
		_siteForNextCreateWindow = null;

		if (_hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		if (!PInvoke.RegisterTouchWindow(_hwnd, 0))
		{
			this.LogError()?.Error($"{nameof(PInvoke.RegisterTouchWindow)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	internal static IWindowedPopupSite? TryCreate(Win32WindowWrapper owner, IWindowedPopupContent content)
	{
		Win32WindowedPopupSite site;
		try
		{
			site = new Win32WindowedPopupSite(owner, content);
		}
		catch (Exception e)
		{
			typeof(Win32WindowedPopupSite).LogError()?.Error("Failed to create a window for a popup.", e);
			return null;
		}

		if (!site.TryInitializeGraphics())
		{
			site.Dispose();
			return null;
		}

		owner.AddPopupSite(site);
		return site;
	}

	private bool TryInitializeGraphics()
	{
		// Negotiation walks the app's backend order, this only answers for the owner's kind and the software fallback.
		try
		{
			var init = GraphicsRegistry.Initialize(kind => Task.FromResult(CreateContext(kind)));
			_context = init.Context;
			_renderer = init.Renderer;
		}
		catch (Exception e)
		{
			this.LogWarn()?.Warn($"No transparent surface is available for a popup window, the popup stays in its main window: {e.Message}");
			return false;
		}

		if (_context.Kind != _owner.GraphicsContextKind)
		{
			this.LogWarn()?.Warn($"A popup window can't be transparent on {_owner.GraphicsContextKind}, so it is rendered with {_context.Kind}.");
		}
		else
		{
			this.LogDebug()?.Debug($"Popup window 0x{_hwnd.Value:X} renders with {_context.Kind}.");
		}

		_renderThread = new Win32WindowWrapper.RenderThread(DrawFrame, OnFramePresented);
		return true;
	}

	private ISwapChain? CreateContext(GraphicsContextKind kind)
	{
		var ownerKind = _owner.GraphicsContextKind;

		if (kind == ownerKind)
		{
			switch (kind)
			{
				case GraphicsContextKind.Vulkan:
					try
					{
						return new Win32VulkanGraphicsContext(_hwnd, transparent: true);
					}
					catch (Exception e)
					{
						this.LogWarn()?.Warn($"A transparent Vulkan swapchain isn't available: {e.Message}");
						return null;
					}

				case GraphicsContextKind.OpenGL:
					if (!TryEnableDwmAlpha())
					{
						return null;
					}

					return Win32OpenGLGraphicsContext.TryCreate(_hwnd, transparent: true);
			}
		}

		if (kind == GraphicsContextKind.Software)
		{
			var style = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
			_ = PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style | (int)WINDOW_EX_STYLE.WS_EX_LAYERED);
			_isLayered = true;
			return new Win32SoftwareGraphicsContext(_hwnd, layered: true);
		}

		return null;
	}

	private unsafe bool TryEnableDwmAlpha()
	{
		// An empty blur region turns the alpha of the framebuffer on without blurring anything behind it.
		var region = PInvoke.CreateRectRgn(0, 0, -1, -1);
		try
		{
			var blurBehind = new DWM_BLURBEHIND
			{
				dwFlags = /* DWM_BB_ENABLE | DWM_BB_BLURREGION */ 1 | 2,
				fEnable = true,
				hRgnBlur = region,
			};

			var result = PInvoke.DwmEnableBlurBehindWindow(_hwnd, blurBehind);
			if (result.Failed)
			{
				this.LogWarn()?.Warn($"{nameof(PInvoke.DwmEnableBlurBehindWindow)} failed: {Win32Helper.GetErrorMessage(result)}");
				return false;
			}

			return true;
		}
		finally
		{
			_ = PInvoke.DeleteObject(region);
		}
	}

	// Called on the render thread.
	private IGeometry? DrawFrame()
	{
		if (_isClosed || !_content.DrawFrame(_context, _renderer))
		{
			return null;
		}

		// A just-resized swapchain drops its frame. No other frame may come, so the same one is presented again.
		if (_context is IWin32PresentReporting { PresentedLastFrame: false })
		{
			_renderThread?.SignalNewFrame();
			return null;
		}

		return FrameRenderHelper.EmptyClipPath;
	}

	// Called on the render thread.
	private void OnFramePresented(IGeometry _)
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

			var success = PInvoke.SetWindowPos(
				_hwnd,
				HWND.Null, // HWND_TOP
				0,
				0,
				0,
				0,
				SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}"); }

			// Not every swapchain keeps what a hidden window was given.
			_renderThread?.SignalNewFrame();
		}
	}

	public bool IsClosed => _isClosed;

	public bool HasFocus => PInvoke.GetFocus() == _hwnd;

	public bool HasPointerCapture => PInvoke.GetCapture() == _hwnd;

	public RectInt32 ScreenBounds
		=> !_isClosed && PInvoke.GetWindowRect(_hwnd, out var rect)
			? new RectInt32(rect.X, rect.Y, rect.Width, rect.Height)
			: default;

	public bool IsVisible => !_isClosed && PInvoke.IsWindowVisible(_hwnd);

	public void MoveAndResize(RectInt32 rect, RectInt32 contentBounds)
	{
		_contentBounds = contentBounds;

		if (_isClosed || rect.Equals(_bounds))
		{
			return;
		}

		_bounds = rect;

		var success = PInvoke.SetWindowPos(
			_hwnd,
			HWND.Null,
			rect.X,
			rect.Y,
			Math.Max(rect.Width, 1),
			Math.Max(rect.Height, 1),
			SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}"); }
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

		if (!_isClosed)
		{
			_ = PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
		}
	}

	public void SetPointerCapture() => PInvoke.SetCapture(_hwnd);

	public void ReleasePointerCapture()
	{
		if (HasPointerCapture && !PInvoke.ReleaseCapture())
		{
			this.LogError()?.Error($"{nameof(PInvoke.ReleaseCapture)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	public void InvalidateRender() => _renderThread?.SignalNewFrame();

	public void Dispose()
	{
		if (!_isClosed && !PInvoke.DestroyWindow(_hwnd))
		{
			this.LogError()?.Error($"{nameof(PInvoke.DestroyWindow)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	internal void OnOwnerBoundsChanged() => _content.OnOwnerBoundsChanged();

	// Also reached when the owner is destroyed, which takes its owned windows along.
	private void OnDestroyed()
	{
		_isClosed = true;
		_owner.RemovePopupSite(this);

		// The thread is the sole user of the context, so it has to stop first.
		_renderThread?.Dispose();
		_renderThread = null;

		// Every Vulkan window shares one device and queue, and the GRContext's teardown waits on that queue.
		using (_context is Win32VulkanGraphicsContext vulkan ? vulkan.LockDevice() : null)
		{
			(_renderer as IDisposable)?.Dispose();
		}

		_context?.Dispose();

		// Not from inside the destruction of this window and its owner: closing a popup runs app code.
		NativeDispatcher.Main.Enqueue(_content.OnSiteClosed, NativeDispatcherPriority.High);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		try
		{
			if (_siteForNextCreateWindow is { } site)
			{
				_hwndToSite[hwnd] = site;
			}
			else if (!_hwndToSite.TryGetValue(hwnd, out site))
			{
				return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
			}

			return site.WndProcInner(hwnd, msg, wParam, lParam);
		}
		catch (Exception e)
		{
			typeof(Win32WindowedPopupSite).LogError()?.Error($"An unhandled exception was thrown while processing win32 message 0x{msg:X4}.", e);
			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}
	}

	private LRESULT WndProcInner(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		if (msg == Win32UIAutomationInterop.WM_GETOBJECT)
		{
			return Win32UIAutomationInterop.HandleGetObject(hwnd, wParam, lParam, _owner.GetAccessibilityProvider(_content.AutomationRoot));
		}

		switch (msg)
		{
			case PInvoke.WM_MOUSEACTIVATE:
				return new LRESULT(MA_NOACTIVATE);
			case PInvoke.WM_POINTERACTIVATE:
				return new LRESULT(PA_NOACTIVATE);
			case PInvoke.WM_ERASEBKGND:
				return new LRESULT(1);
			case PInvoke.WM_NCHITTEST when !_isLayered:
				// A layered window lets pixels with zero alpha through by itself, other windows have to be told.
				// This only reaches windows of this thread, so a gutter over another app's window still blocks it.
				return new LRESULT(IsInContent(lParam) ? HTCLIENT : HTTRANSPARENT);
			case PInvoke.WM_POINTERDOWN or PInvoke.WM_POINTERUP or PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL
				or PInvoke.WM_POINTERENTER or PInvoke.WM_POINTERLEAVE or PInvoke.WM_POINTERUPDATE:
				// The position is read back as a screen position and made relative to the owner, so the owner's
				// hit testing runs as if the pointer was over its own client area.
				if (msg == PInvoke.WM_POINTERDOWN)
				{
					// Clicking popup content doesn't take Win32 focus from a hosted native child of the owner by itself.
					PInvoke.SetFocus(_owner.Hwnd);
				}

				_owner.OnPointer(msg, wParam, _owner.Hwnd);
				return new LRESULT(0);
			case PInvoke.WM_POINTERCAPTURECHANGED:
				_owner.OnPointerCaptureChanged(wParam);
				return new LRESULT(0);
			case PInvoke.WM_SETCURSOR:
				_owner.ApplyPointerCursor();
				return new LRESULT(1);
			case PInvoke.WM_DPICHANGED:
				// The size follows the owner's scale, not this window's monitor: MUX Reference Popup.cpp,
				// GetWindowedPopupRootVisualScaleCompensation.
				return new LRESULT(0);
			case PInvoke.WM_PAINT:
				_renderThread?.SignalNewFrame();
				break;
			case PInvoke.WM_DESTROY:
				OnDestroyed();
				return new LRESULT(0);
			case WM_NCDESTROY:
				_hwndToSite.Remove(hwnd);
				break;
		}

		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	private bool IsInContent(LPARAM lParam)
	{
		var x = (short)(lParam.Value & 0xFFFF);
		var y = (short)((lParam.Value >> 16) & 0xFFFF);
		return x >= _contentBounds.X && x < _contentBounds.X + _contentBounds.Width
			&& y >= _contentBounds.Y && y < _contentBounds.Y + _contentBounds.Height;
	}
}
