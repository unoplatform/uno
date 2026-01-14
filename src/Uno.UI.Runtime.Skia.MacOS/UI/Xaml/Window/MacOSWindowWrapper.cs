using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.UI.Input.Internal;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowWrapper : NativeWindowWrapperBase, INativeInputNonClientPointerSource
{
	private readonly MacOSWindowNative _nativeWindow;
	private readonly Dictionary<NonClientRegionKind, RectInt32[]> _regionRects = new();

	public MacOSWindowWrapper(MacOSWindowNative nativeWindow, Window window, XamlRoot xamlRoot, Size initialSize) : base(window, xamlRoot)
	{
		_nativeWindow = nativeWindow;

		nativeWindow.Host.Closing += OnWindowClosing;
		nativeWindow.Host.RasterizationScaleChanged += Host_RasterizationScaleChanged;
		nativeWindow.Host.SizeChanged += (_, s) => OnHostSizeChanged(s);
		OnHostSizeChanged(initialSize);
		nativeWindow.Host.PositionChanged += (_, s) => OnHostPositionChanged(s.X, s.Y);
		// the initial event occurred before the managed side was ready to handle it
		NativeUno.uno_window_get_position(nativeWindow.Handle, out var x, out var y);
		OnHostPositionChanged(x, y);

		RasterizationScale = (float)_nativeWindow.Host.RasterizationScale;

		// Subscribe to title bar events
		window.AppWindow.TitleBar.Changed += OnTitleBarChanged;
		window.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged += OnExtendsContentIntoTitleBarChanged;
	}

	private void Host_RasterizationScaleChanged(object? sender, EventArgs args)
	{
		RasterizationScale = (float)_nativeWindow.Host.RasterizationScale;
	}

	public override object NativeWindow => _nativeWindow;

	public override string Title
	{
		get => NativeUno.uno_window_get_title(_nativeWindow.Handle);
		set => NativeUno.uno_window_set_title(_nativeWindow.Handle, value);
	}

	internal protected override void Activate()
	{
		NativeUno.uno_window_activate(_nativeWindow.Handle);
	}

	protected override void CloseCore()
	{
		NativeUno.uno_window_close(_nativeWindow.Handle);
	}

	public override void Move(PointInt32 position)
	{
		// user input in physical pixels transformed into logical pixels
		var x = position.X / RasterizationScale;
		var y = position.Y / RasterizationScale;
		NativeUno.uno_window_move(_nativeWindow.Handle, x, y);
	}

	public override void Resize(SizeInt32 size)
	{
		// user input in physical pixels transformed into logical pixels
		var w = size.Width / RasterizationScale;
		var h = size.Height / RasterizationScale;
		NativeUno.uno_window_resize(_nativeWindow.Handle, w, h);
	}

	private void OnHostPositionChanged(double x, double y)
	{
		// in physical pixels
		var sx = (int)(x * RasterizationScale);
		var sy = (int)(y * RasterizationScale);
		Position = new PointInt32(sx, sy);
	}

	private void OnHostSizeChanged(Size size)
	{
		var bounds = new Rect(default, size);
		// in logical pixels
		SetBoundsAndVisibleBounds(bounds, bounds);
		// in physical pixels
		int w = (int)(size.Width * RasterizationScale);
		int h = (int)(size.Height * RasterizationScale);
		var fullSize = new SizeInt32(w, h);
		SetSizes(fullSize, fullSize);
	}

	private void OnWindowClosing(object? sender, CancelEventArgs e)
	{
		var closingArgs = RaiseClosing();
		e.Cancel = closingArgs.Cancel;
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		NativeUno.uno_window_enter_full_screen(_nativeWindow.Handle);
		return Disposable.Create(() => NativeUno.uno_window_exit_full_screen(_nativeWindow.Handle));
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new MacOSNativeOverlappedPresenter(_nativeWindow));
		return Disposable.Create(() => presenter.SetNative(null));
	}

	#region Title Bar Support

	private void OnTitleBarChanged(object? sender, EventArgs e)
	{
		UpdateDragRectangles();
	}

	private void OnExtendsContentIntoTitleBarChanged(bool extends)
	{
		NativeUno.uno_window_set_extends_content_into_titlebar(_nativeWindow.Handle, extends);
		UpdateDragRectangles();
	}

	private unsafe void UpdateDragRectangles()
	{
		// Combine Caption regions from WindowChrome with AppWindowTitleBar.DragRectangles
		var captionRects = GetRegionRects(NonClientRegionKind.Caption);
		var allRects = captionRects;

		if (allRects.Length == 0)
		{
			NativeUno.uno_window_set_drag_rectangles(_nativeWindow.Handle, 0, IntPtr.Zero);
			return;
		}

		// Convert RectInt32 (physical pixels) to logical pixels for native layer
		// Each rectangle is 4 doubles: x, y, width, height
		var nativeRects = stackalloc double[allRects.Length * 4];
		for (var i = 0; i < allRects.Length; i++)
		{
			var rect = allRects[i];
			nativeRects[i * 4 + 0] = rect.X / RasterizationScale;
			nativeRects[i * 4 + 1] = rect.Y / RasterizationScale;
			nativeRects[i * 4 + 2] = rect.Width / RasterizationScale;
			nativeRects[i * 4 + 3] = rect.Height / RasterizationScale;
		}

		NativeUno.uno_window_set_drag_rectangles(_nativeWindow.Handle, allRects.Length, (nint)nativeRects);
	}

	#endregion

	#region INativeInputNonClientPointerSource

	public void ClearAllRegionRects()
	{
		_regionRects.Clear();
		UpdateDragRectangles();
	}

	public void ClearRegionRects(NonClientRegionKind region)
	{
		_regionRects.Remove(region);
		if (region == NonClientRegionKind.Caption)
		{
			UpdateDragRectangles();
		}
	}

	public RectInt32[] GetRegionRects(NonClientRegionKind region)
		=> _regionRects.TryGetValue(region, out var list) ? list : Array.Empty<RectInt32>();

	public void SetRegionRects(NonClientRegionKind region, RectInt32[] rects)
	{
		if (rects.Length == 0)
		{
			_regionRects.Remove(region);
		}
		else
		{
			_regionRects[region] = rects;
		}

		// Update native drag rectangles when Caption region changes
		if (region == NonClientRegionKind.Caption)
		{
			UpdateDragRectangles();
		}
	}

	#endregion
}
