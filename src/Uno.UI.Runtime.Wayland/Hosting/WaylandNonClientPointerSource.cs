#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.UI.Input.Internal;
using Uno.UI.Runtime.Wayland.Input;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.Foundation;
using Windows.Graphics;

namespace Uno.UI.Runtime.Wayland;

// Non-client input for windows that extend their content into the title bar. Wayland has no non-client hit
// testing, so presses are resolved after XAML routed them: one left unhandled in a caption region starts
// xdg_toplevel.move (a double-click toggles maximize, a right-click opens the window menu), while border
// regions start a resize. Interactive elements in the title bar handle their presses and keep working.
internal sealed class WaylandNonClientPointerSource : INativeInputNonClientPointerSource, IDisposable
{
	private const uint LeftButton = 0x110; // BTN_LEFT
	private const uint RightButton = 0x111; // BTN_RIGHT
	private const ulong DoubleClickTime = 500_000; // PointerPoint.Timestamp is in microseconds.
	private const double DoubleClickDistance = 4;

	private readonly WaylandConnection _connection;
	private readonly XdgToplevel _toplevel;
	private readonly Window _window;
	private readonly int _windowId;
	private readonly Func<double> _scale;
	private readonly Action<Action> _post;
	private readonly WaylandNonClientRegions _regions = new();
	private readonly PointerEventHandler _pressedHandler;

	private readonly WaylandProtocolInputConnection? _input;
	private readonly WaylandPointerInputSource _pointer;
	private readonly WaylandPointerPress _press;

	// Set on the UI thread when the window closes; read before posting requests to the dispatch thread.
	private volatile bool _disposed;

	// UI thread state.
	private UIElement? _root;
	private ulong _lastClickTime;
	private Point _lastClickPosition;

	internal WaylandNonClientPointerSource(WaylandConnection connection, XdgToplevel toplevel, Window window, int windowId, WaylandXamlRootHost host, Func<double> scale, Action<Action> post)
	{
		_connection = connection;
		_press = new(connection.PostToUI);
		_toplevel = toplevel;
		_window = window;
		_windowId = windowId;
		_scale = scale;
		_post = post;
		_pressedHandler = OnPointerPressed;
		ExtendsContent = window.AppWindow.TitleBar.ExtendsContentIntoTitleBar;
		_input = host.Input;
		_pointer = host.PointerSource;
		if (_input is { } input)
		{
			input.PointerButton += OnPointerButton;
		}
		// The visual tree root may not exist yet; a pointer always enters before it presses.
		_pointer.PointerEntered += OnPointerEntered;
	}

	// UI thread.
	internal bool ExtendsContent { get; set; }

	public void ClearAllRegionRects() => _regions.Clear();

	public void ClearRegionRects(NonClientRegionKind region)
	{
		if (TryMap(region, out var kind))
		{
			_regions.Set(kind, []);
		}
	}

	public RectInt32[] GetRegionRects(NonClientRegionKind region)
		=> TryMap(region, out var kind)
			? Array.ConvertAll(_regions.Get(kind), r => new RectInt32 { X = r.X, Y = r.Y, Width = r.Width, Height = r.Height })
			: [];

	public void SetRegionRects(NonClientRegionKind region, RectInt32[] rects)
	{
		if (TryMap(region, out var kind))
		{
			_regions.Set(kind, Array.ConvertAll(rects, r => new WaylandNonClientRect(r.X, r.Y, r.Width, r.Height)));
		}
	}

	private static bool TryMap(NonClientRegionKind region, out WaylandNonClientRegionKind kind)
	{
		kind = (WaylandNonClientRegionKind)(int)region;
		return Enum.IsDefined(kind);
	}

	private void OnPointerButton(object? sender, WlPointer.ButtonEventArgs args)
	{
		if (args.State == 1)
		{
			_press.Queue(args.Serial, args.Button);
		}
	}

	private void OnPointerEntered(object sender, Windows.UI.Core.PointerEventArgs args) => AttachToRoot();

	// UI thread, when the window closes: requests posted afterwards would target destroyed protocol objects.
	public void Dispose()
	{
		_disposed = true;
		_pointer.PointerEntered -= OnPointerEntered;
		if (_input is { } input)
		{
			input.PointerButton -= OnPointerButton;
		}
		_root?.RemoveHandler(UIElement.PointerPressedEvent, _pressedHandler);
		_root = null;
	}

	private void AttachToRoot()
	{
		if (_disposed)
		{
			return;
		}
		var root = _window.RootElement;
		if (root != _root)
		{
			_root?.RemoveHandler(UIElement.PointerPressedEvent, _pressedHandler);
			_root = root;
			_root?.AddHandler(UIElement.PointerPressedEvent, _pressedHandler, handledEventsToo: true);
		}
	}

	// Runs last in the bubbling route, so args.Handled tells whether an element took the press.
	private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		if (_disposed || !ExtendsContent
			|| args.Pointer.PointerDeviceType != PointerDeviceType.Mouse
			|| _window.AppWindow.Presenter is FullScreenPresenter
			|| _connection.Seat is not { } seat)
		{
			return;
		}
		var point = args.GetCurrentPoint(null);
		var button = point.Properties.PointerUpdateKind switch
		{
			PointerUpdateKind.LeftButtonPressed => LeftButton,
			PointerUpdateKind.RightButtonPressed => RightButton,
			_ => 0u,
		};
		if (button == 0 || _press.Take(button) is not { } serial)
		{
			return;
		}
		var position = point.Position;
		var scale = _scale();
		var (area, edges) = _regions.HitTest(position.X * scale, position.Y * scale, _window.AppWindow.TitleBar.Height);
		var presenter = _window.AppWindow.Presenter as OverlappedPresenter;
		switch (area)
		{
			case WaylandNonClientArea.Resize when button == LeftButton && presenter is not { IsResizable: false }:
				Trace($"resize edges={edges}");
				Post(() => _toplevel.Resize(seat, serial, edges));
				break;
			case WaylandNonClientArea.Icon:
				ShowWindowMenu(seat, serial, position);
				break;
			case WaylandNonClientArea.Caption when button == LeftButton && !args.Handled:
				var isDoubleClick = point.Timestamp - _lastClickTime <= DoubleClickTime
					&& Math.Abs(position.X - _lastClickPosition.X) <= DoubleClickDistance
					&& Math.Abs(position.Y - _lastClickPosition.Y) <= DoubleClickDistance;
				_lastClickTime = isDoubleClick ? 0 : point.Timestamp;
				_lastClickPosition = position;
				if (!isDoubleClick)
				{
					Trace("move");
					Post(() => _toplevel.Move(seat, serial));
				}
				else if (presenter is { IsMaximizable: true, IsResizable: true })
				{
					Trace("toggle maximized");
					if (presenter.State == OverlappedPresenterState.Maximized)
					{
						presenter.Restore();
					}
					else
					{
						presenter.Maximize();
					}
				}
				break;
			case WaylandNonClientArea.Caption when button == RightButton && !args.Handled && !HasContextFlyout(args.OriginalSource as DependencyObject):
				ShowWindowMenu(seat, serial, position);
				break;
		}
	}

	// Window menu coordinates are relative to the window geometry, which starts at the content origin here.
	private void ShowWindowMenu(WlSeat seat, uint serial, Point position)
	{
		Trace("window menu");
		Post(() => _toplevel.ShowWindowMenu(seat, serial, (int)position.X, (int)position.Y));
	}

	private void Post(Action action) => _post(() =>
	{
		if (!_disposed)
		{
			action();
		}
	});

	// A context flyout opens on release, so its element leaves the right press unhandled.
	private static bool HasContextFlyout(DependencyObject? element)
	{
		for (; element is not null; element = VisualTreeHelper.GetParent(element))
		{
			if (element is UIElement { ContextFlyout: not null })
			{
				return true;
			}
		}
		return false;
	}

	private void Trace(string action)
	{
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace($"window{_windowId}: non-client {action}");
		}
	}
}
