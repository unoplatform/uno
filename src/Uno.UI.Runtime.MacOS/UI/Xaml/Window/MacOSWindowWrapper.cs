using System.ComponentModel;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.UI.Input.Internal;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;

namespace Uno.UI.Runtime.MacOS;

internal class MacOSWindowWrapper : NativeWindowWrapperBase, INativeInputNonClientPointerSource
{
	private readonly MacOSWindowNative _nativeWindow;
	private readonly NonClientRegionSet _nonClientRegions = new();

	public MacOSWindowWrapper(MacOSWindowNative nativeWindow, Window window, XamlRoot xamlRoot, Size initialSize) : base(window, xamlRoot)
	{
		_nativeWindow = nativeWindow;

		nativeWindow.Host.Closing += OnWindowClosing;
		nativeWindow.Host.RasterizationScaleChanged += Host_RasterizationScaleChanged;
		nativeWindow.Host.ActivationChanged += (_, e) => OnNativeActivationChanged(e.IsActive, e.ByPointer);
		nativeWindow.Host.SizeChanged += (_, s) =>
		{
			OnHostSizeChanged(s);
			// e.g. the traffic lights leave the content in full screen
			UpdateTitleBarInsets();
		};
		nativeWindow.Host.NonClientHitTest = HitTestNonClient;
		window.AppWindow.TitleBar.Changed += (_, _) => UpdateTitleBar();
		OnHostSizeChanged(initialSize);
		nativeWindow.Host.PositionChanged += (_, s) => OnHostPositionChanged(s.X, s.Y);
		// the initial event occurred before the managed side was ready to handle it
		NativeUno.uno_window_get_position(nativeWindow.Handle, out var x, out var y);
		OnHostPositionChanged(x, y);

		RasterizationScale = (float)_nativeWindow.Host.RasterizationScale;
	}

	private void Host_RasterizationScaleChanged(object? sender, EventArgs args)
	{
		RasterizationScale = (float)_nativeWindow.Host.RasterizationScale;
		UpdateTitleBarInsets();
	}

	internal override bool HasNativeCaptionButtons => true;

	protected override void ShowCore() => UpdateTitleBar();

	// The native caption buttons (traffic lights) stay; the content extends under a transparent title bar.
	private void UpdateTitleBar()
	{
		if (_nativeWindow.Handle == nint.Zero || Window?.AppWindow is not { NativeAppWindow: not null } appWindow)
		{
			return;
		}

		var titleBar = appWindow.TitleBar;
		var extend = titleBar.ExtendsContentIntoTitleBar;
		NativeUno.uno_window_set_extends_content_into_title_bar(
			_nativeWindow.Handle,
			extend,
			extend ? titleBar.Height / RasterizationScale : 0,
			titleBar.PreferredHeightOption != TitleBarHeightOption.Collapsed);

		UpdateTitleBarInsets();
	}

	private void UpdateTitleBarInsets()
	{
		if (_nativeWindow.Handle == nint.Zero || Window?.AppWindow.TitleBar is not { } titleBar)
		{
			return;
		}

		var (left, right) = NativeUno.uno_window_get_caption_buttons_bounds(_nativeWindow.Handle, out var minX, out var maxX, out var width)
			? MacOSTitleBarMath.GetInsets(minX, maxX, width, RasterizationScale)
			: (0, 0);
		titleBar.SetInsets(left, right);
	}

	/// <summary>
	/// What a left-button press at the given content point, in logical pixels, does while extended into the title bar.
	/// </summary>
	internal MacOSNonClientPointerAction HitTestNonClient(double x, double y)
		=> MacOSTitleBarMath.GetPointerAction(
			_nonClientRegions.HitTest(new PointInt32((int)(x * RasterizationScale), (int)(y * RasterizationScale))));

	public void ClearAllRegionRects() => _nonClientRegions.ClearAll();

	public void ClearRegionRects(NonClientRegionKind region) => _nonClientRegions.Clear(region);

	public RectInt32[] GetRegionRects(NonClientRegionKind region) => _nonClientRegions.Get(region);

	public void SetRegionRects(NonClientRegionKind region, RectInt32[] rects) => _nonClientRegions.Set(region, rects);

	internal void OnNativeActivationChanged(bool isActive, bool byPointer)
		=> ActivationState = !isActive
			? CoreWindowActivationState.Deactivated
			: byPointer ? CoreWindowActivationState.PointerActivated : CoreWindowActivationState.CodeActivated;

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

	public override PointInt32 ConvertLocalToScreen(Point localPoint)
		=> TryConvertLocalToScreen(localPoint, out var screenPoint)
			? screenPoint
			: base.ConvertLocalToScreen(localPoint);

	public override bool TryConvertLocalToScreen(Point localPoint, out PointInt32 screenPoint)
	{
		screenPoint = default;
		if (_nativeWindow.Handle == nint.Zero)
		{
			return false;
		}

		NativeUno.uno_window_convert_local_to_screen(_nativeWindow.Handle, localPoint.X, localPoint.Y, out var x, out var y);
		var scale = RasterizationScale == 0 ? 1 : RasterizationScale;
		screenPoint = new PointInt32((int)Math.Round(x * scale), (int)Math.Round(y * scale));
		return true;
	}

	public override Point ConvertScreenToLocal(PointInt32 screenPoint)
		=> TryConvertScreenToLocal(screenPoint, out var localPoint)
			? localPoint
			: base.ConvertScreenToLocal(screenPoint);

	public override bool TryConvertScreenToLocal(PointInt32 screenPoint, out Point localPoint)
	{
		localPoint = default;
		if (_nativeWindow.Handle == nint.Zero)
		{
			return false;
		}

		var scale = RasterizationScale == 0 ? 1 : RasterizationScale;
		NativeUno.uno_window_convert_screen_to_local(
			_nativeWindow.Handle,
			screenPoint.X / scale,
			screenPoint.Y / scale,
			out var x,
			out var y);
		localPoint = new Point(x, y);
		return true;
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
		presenter.SetNative(new MacOSNativeOverlappedPresenter(_nativeWindow, UpdateTitleBar));
		return Disposable.Create(() => presenter.SetNative(null));
	}

	public override bool IsSystemBackdropSupported(Microsoft.UI.Xaml.Media.SystemBackdrop backdrop)
		=> backdrop is Microsoft.UI.Xaml.Media.MicaBackdrop or Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop;

	public override void SetSystemBackdrop(Microsoft.UI.Xaml.Media.SystemBackdrop? backdrop)
	{
		int material = 0; // None
		if (backdrop is Microsoft.UI.Xaml.Media.MicaBackdrop micaBackdrop)
		{
			material = micaBackdrop.Kind == Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt ? 2 : 1;
		}
		else if (backdrop is Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop)
		{
			material = 3;
		}

		NativeUno.uno_window_set_system_backdrop(_nativeWindow.Handle, material);
	}
}
