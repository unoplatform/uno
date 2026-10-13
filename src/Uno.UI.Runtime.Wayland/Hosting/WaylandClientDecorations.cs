#nullable enable

using System;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Text;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Wayland.Input;
using Uno.UI.Runtime.Wayland.Protocols;
using Uno.UI.Runtime.Wayland.Rendering;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;

namespace Uno.UI.Runtime.Wayland;

// Client-side title bar and resize border, used when the compositor lacks xdg-decoration or picks client-side
// mode. A synchronized subsurface below the content surface, so its state is applied with the next content commit.
// Drawn on dispatch, or during presentation while managed dispatch is suspended.
internal sealed class WaylandClientDecorations : IDisposable
{
	private const uint LeftButton = 0x110; // BTN_LEFT
	private const uint RightButton = 0x111; // BTN_RIGHT
	private const uint SeatPointerCapability = 1;
	private const uint DoubleClickTime = 500;
	private const double DoubleClickDistance = 4;
	private const float TitleFontSize = 13;

	private static readonly CoreCursor _arrow = new(CoreCursorType.Arrow, 0);
	private static readonly CoreCursor _sizeNorthSouth = new(CoreCursorType.SizeNorthSouth, 0);
	private static readonly CoreCursor _sizeWestEast = new(CoreCursorType.SizeWestEast, 0);
	private static readonly CoreCursor _sizeNorthwestSoutheast = new(CoreCursorType.SizeNorthwestSoutheast, 0);
	private static readonly CoreCursor _sizeNortheastSouthwest = new(CoreCursorType.SizeNortheastSouthwest, 0);

	private readonly WaylandConnection _connection;
	private readonly XdgToplevel _toplevel;
	private readonly int _windowId;
	private readonly IWaylandDecorationsOwner _owner;
	private readonly WlSurface _surface;
	private readonly WlSubsurface _subsurface;
	private readonly WpViewport? _viewport;
	private readonly WaylandRenderScale _scale;
	private readonly WaylandSoftwareGraphicsContext _context;
	private readonly IDrawingFactory<ISoftwareRenderTarget> _drawingFactory;
	private readonly bool _dark;

	private WaylandDecorationLayout _layout;
	private string _title = string.Empty;
	private readonly WaylandDecorationTitle _titleRun = new(() =>
		FontProvider.Current.GetDefaultFont(FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, TitleFontSize));
	private bool _activated;
	private bool _maximized;
	private bool _shown;
	private readonly WaylandDecorationRedrawState _redraw = new();
	private int _frame;

	private WlPointer? _pointer;
	private WaylandCursor? _cursor;
	private bool _pointerInside;
	private uint _enterSerial;
	private double _pointerX;
	private double _pointerY;
	private WaylandDecorationPart _hovered;
	private uint _hoveredEdges;
	private WaylandDecorationPart _pressed;
	private uint _lastClickTime;
	private double _lastClickX;
	private double _lastClickY;
	private bool _disposed;

	private WaylandClientDecorations(WaylandConnection connection, WlSurface parent, XdgToplevel toplevel, int windowId, bool dark, IWaylandDecorationsOwner owner, IGraphicsProvider<IGraphicsContext> provider)
	{
		_connection = connection;
		_toplevel = toplevel;
		_windowId = windowId;
		_dark = dark;
		_owner = owner;
		IDrawingFactory? drawingFactory = null;
		try
		{
			_surface = connection.Compositor.CreateSurface();
			_subsurface = connection.Subcompositor!.GetSubsurface(_surface, parent);
			_subsurface.PlaceBelow(parent);
			_viewport = connection.Viewporter?.GetViewport(_surface);
			_scale = new WaylandRenderScale(_surface, _viewport);
			_context = new WaylandSoftwareGraphicsContext(_surface, connection.Shm, connection.Display.Post, PrepareCommit);
			drawingFactory = provider.CreateGraphics(_context);
			_drawingFactory = drawingFactory as IDrawingFactory<ISoftwareRenderTarget>
				?? throw new NotSupportedException("The graphics backend cannot present software frames.");
			_context.BufferAvailable += OnBufferAvailable;
			if (connection.Seat is { } seat)
			{
				seat.Capabilities += OnSeatCapabilities;
				UpdatePointer(connection.SeatCapabilities);
			}
		}
		catch
		{
			WaylandCleanup.Run(
				() =>
				{
					if (connection.Seat is { } seat)
					{
						seat.Capabilities -= OnSeatCapabilities;
					}
				},
				() => _cursor?.Dispose(),
				() => _pointer?.Dispose(),
				() => (drawingFactory as IDisposable)?.Dispose(),
				() => _context?.Dispose(),
				() => _viewport?.Dispose(),
				() => _subsurface?.Dispose(),
				() => _surface?.Dispose());
			throw;
		}
	}

	// Null when the window's graphics backend cannot render in software, which the decorations need.
	internal static WaylandClientDecorations? TryCreate(WaylandConnection connection, WlSurface parent, XdgToplevel toplevel, int windowId, bool dark, IWaylandDecorationsOwner owner, IGraphicsProvider? provider)
	{
		if (provider is not IGraphicsProvider<IGraphicsContext> softwareProvider || !provider.PreferredContexts.Contains(GraphicsContextKind.Software))
		{
			return null;
		}
		try
		{
			return new WaylandClientDecorations(connection, parent, toplevel, windowId, dark, owner, softwareProvider);
		}
		catch (NotSupportedException)
		{
			return null;
		}
	}

	internal bool IsReady => !_redraw.Dirty;

	internal bool HasPendingState => _redraw.PendingParentCommit;

	internal WaylandDecorationLayout PendingLayout { get; private set; }
	internal double PendingScale { get; private set; }

	internal void AdvancePendingState()
	{
		if (_redraw.ParentCommitted())
		{
			_connection.Display.Post(Redraw);
		}
	}

	internal void Update(WaylandDecorationLayout layout, string title, bool activated, bool maximized, uint preferredScale, int integerScale)
	{
		if (_disposed)
		{
			return;
		}
		var previousScale = _scale.EffectiveScale;
		if (preferredScale != 0)
		{
			_scale.SetPreferredScale(preferredScale);
		}
		_scale.SetIntegerScale(integerScale);
		if (layout == _layout && title == _title && activated == _activated && maximized == _maximized && previousScale == _scale.EffectiveScale)
		{
			return;
		}
		var moved = layout.SurfaceX != _layout.SurfaceX || layout.SurfaceY != _layout.SurfaceY;
		_layout = layout;
		_title = title;
		_activated = activated;
		_maximized = maximized;
		if (moved)
		{
			_subsurface.SetPosition(layout.SurfaceX, layout.SurfaceY);
		}
		if (_pointerInside)
		{
			UpdateHover();
		}
		Redraw();
	}

	internal void SetTitle(string title)
	{
		if (!_disposed && title != _title)
		{
			_title = title;
			Redraw();
		}
	}

	private void Redraw()
	{
		if (_disposed)
		{
			return;
		}
		if (!_redraw.BeginRedraw())
		{
			return;
		}
		if (!_layout.IsVisible)
		{
			PendingLayout = _layout;
			PendingScale = _scale.EffectiveScale;
			_redraw.CompleteRedraw(committed: true);
			_ = _redraw.ParentCommitted();
			if (_shown)
			{
				// A null buffer unmaps the subsurface; the content commit applies it.
				_shown = false;
				_surface.Attach(null, 0, 0);
				_surface.Commit();
				_redraw.CompleteRedraw(committed: true);
				_owner.RequestParentCommit();
			}
			return;
		}
		var scale = _scale.EffectiveScale;
		var target = _context.AcquireRenderTarget(WaylandRenderScale.ToBufferPixels(_layout.SurfaceWidth, scale), WaylandRenderScale.ToBufferPixels(_layout.SurfaceHeight, scale));
		using (var session = _drawingFactory.BeginPresent((ISoftwareRenderTarget)target, default))
		{
			Draw(session);
		}
		_context.Present();
		_redraw.CompleteRedraw(_context.Committed);
		if (_context.Committed)
		{
			PendingLayout = _layout;
			PendingScale = scale;
			_shown = true;
			_owner.RequestParentCommit();
		}
	}

	// The scale and viewport change in the same commit as the buffer they describe.
	private bool PrepareCommit(int width, int height, Func<bool> submit)
	{
		lock (_connection.Display.SyncRoot)
		{
			_ = _scale.Apply(_layout.SurfaceWidth, _layout.SurfaceHeight);
			return submit();
		}
	}

	private void OnBufferAvailable(object? sender, EventArgs args)
	{
		if (_redraw.Dirty && !_disposed)
		{
			Redraw();
		}
	}

	private void Draw(IDrawingSession session)
	{
		session.Clear(Color.FromArgb(0, 0, 0, 0));
		session.Save();
		var scale = (float)_scale.EffectiveScale;
		session.Scale(scale, scale);
		var layout = _layout;
		var margin = layout.ResizeMargin;
		var geometry = new Rect(margin, margin, layout.ContentWidth, layout.GeometryHeight);
		if (margin > 0)
		{
			using var silhouette = CreateRectangle(geometry);
			session.Save();
			session.ClipRect(geometry, ClipOperation.Difference);
			session.DrawShadow(silhouette, Color.FromArgb((byte)(_activated ? 0x50 : 0x30), 0, 0, 0), margin / 3f, margin / 3f, additive: false);
			session.Restore();
			using var border = CreateRectangle(new Rect(geometry.X - 0.5, geometry.Y - 0.5, geometry.Width + 1, geometry.Height + 1));
			session.StrokePath(border, Color.FromArgb(0x40, 0, 0, 0), 1);
		}
		if (layout.TitleBarHeight > 0)
		{
			DrawTitleBar(session, layout, geometry);
		}
		session.Restore();
		_frame++;
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace(FormattableString.Invariant($"window{_windowId}: decorations frame {_frame} surface {layout.SurfaceWidth}x{layout.SurfaceHeight}@({layout.SurfaceX},{layout.SurfaceY}) title={layout.TitleBarHeight} margin={margin} buttons={layout.Buttons} activated={_activated} hovered={_hovered}"));
		}
	}

	private void DrawTitleBar(IDrawingSession session, WaylandDecorationLayout layout, Rect geometry)
	{
		var bar = new Rect(geometry.Left, geometry.Top, geometry.Width, layout.TitleBarHeight);
		var background = _dark
			? (_activated ? Color.FromArgb(0xFF, 0x20, 0x20, 0x20) : Color.FromArgb(0xFF, 0x2B, 0x2B, 0x2B))
			: (_activated ? Color.FromArgb(0xFF, 0xE8, 0xE8, 0xE8) : Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3));
		var foreground = _dark
			? (_activated ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xFF, 0x9A, 0x9A, 0x9A))
			: (_activated ? Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A) : Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A));
		session.DrawRect(bar, background);

		var (glyphs, positions) = _titleRun.GetRun(_title, (float)(layout.ButtonsLeft - bar.Left - 24));
		if (glyphs.Length > 0)
		{
			var font = _titleRun.Font;
			var baseline = (float)(bar.Top + bar.Height / 2) - (font.Ascent + font.Descent) / 2;
			session.Save();
			session.Translate((float)bar.Left + 12, 0);
			session.DrawGlyphRun(font, glyphs, positions, baseline, foreground);
			session.Restore();
		}

		DrawButton(session, WaylandDecorationPart.Minimize, foreground);
		DrawButton(session, WaylandDecorationPart.Maximize, foreground);
		DrawButton(session, WaylandDecorationPart.Close, foreground);
	}

	private void DrawButton(IDrawingSession session, WaylandDecorationPart part, Color foreground)
	{
		if (!_layout.TryGetButton(part, out var x, out var y, out var width, out var height))
		{
			return;
		}
		var rect = new Rect(x, y, width, height);
		var hovered = _hovered == part && (_pressed == WaylandDecorationPart.None || _pressed == part);
		var pressed = hovered && _pressed == part;
		if (hovered)
		{
			var highlight = part == WaylandDecorationPart.Close
				? Color.FromArgb((byte)(pressed ? 0xCC : 0xFF), 0xC4, 0x2B, 0x1C)
				: _dark ? Color.FromArgb((byte)(pressed ? 0x10 : 0x18), 0xFF, 0xFF, 0xFF) : Color.FromArgb((byte)(pressed ? 0x24 : 0x12), 0, 0, 0);
			session.DrawRect(rect, highlight);
			if (part == WaylandDecorationPart.Close)
			{
				foreground = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
			}
		}
		// Glyph strokes are centered on pixels, like the caption glyphs of other hosts.
		var cx = (float)Math.Round(x + width / 2.0) + 0.5f;
		var cy = (float)Math.Round(y + height / 2.0) + 0.5f;
		const float half = 5;
		switch (part)
		{
			case WaylandDecorationPart.Minimize:
				session.DrawLine(new(cx - half, cy), new(cx + half, cy), foreground, 1);
				break;
			case WaylandDecorationPart.Maximize when _maximized:
				StrokeLines(session, foreground, new(cx - half, cy - half + 2), new(cx + half - 2, cy - half + 2), new(cx + half - 2, cy + half), new(cx - half, cy + half), new(cx - half, cy - half + 2));
				StrokeLines(session, foreground, new(cx - half + 2, cy - half + 2), new(cx - half + 2, cy - half), new(cx + half, cy - half), new(cx + half, cy + half - 2), new(cx + half - 2, cy + half - 2));
				break;
			case WaylandDecorationPart.Maximize:
				StrokeLines(session, foreground, new(cx - half, cy - half), new(cx + half, cy - half), new(cx + half, cy + half), new(cx - half, cy + half), new(cx - half, cy - half));
				break;
			case WaylandDecorationPart.Close:
				session.DrawLine(new(cx - half, cy - half), new(cx + half, cy + half), foreground, 1);
				session.DrawLine(new(cx - half, cy + half), new(cx + half, cy - half), foreground, 1);
				break;
		}
	}

	private static void StrokeLines(IDrawingSession session, Color color, params ReadOnlySpan<Vector2> points)
	{
		for (var i = 1; i < points.Length; i++)
		{
			session.DrawLine(points[i - 1], points[i], color, 1);
		}
	}

	private static IGeometry CreateRectangle(Rect rect)
	{
		// A private builder: the shared cached one belongs to the UI thread's rendering.
		var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
		builder.AddRectangle(rect);
		return builder.Build();
	}

	private void OnSeatCapabilities(object? sender, WlSeat.CapabilitiesEventArgs args) => UpdatePointer(args.Capabilities);

	private void UpdatePointer(uint capabilities)
	{
		var hasPointer = (capabilities & SeatPointerCapability) != 0;
		if (hasPointer && _pointer is null && !_disposed)
		{
			// A separate wl_pointer for the decoration surface; the content surface's input is unaffected.
			_pointer = _connection.Seat!.GetPointer();
			_pointer.Enter += OnPointerEnter;
			_pointer.Leave += OnPointerLeave;
			_pointer.Motion += OnPointerMotion;
			_pointer.Button += OnPointerButton;
		}
		else if ((!hasPointer || _disposed) && _pointer is { } pointer)
		{
			_cursor?.Dispose();
			_cursor = null;
			pointer.Enter -= OnPointerEnter;
			pointer.Leave -= OnPointerLeave;
			pointer.Motion -= OnPointerMotion;
			pointer.Button -= OnPointerButton;
			pointer.Dispose();
			_pointer = null;
			_pointerInside = false;
		}
	}

	private void OnPointerEnter(object? sender, WlPointer.EnterEventArgs args)
	{
		if (args.Surface != _surface)
		{
			return;
		}
		_pointerInside = true;
		_enterSerial = args.Serial;
		_pointerX = args.SurfaceX.ToDouble();
		_pointerY = args.SurfaceY.ToDouble();
		_hoveredEdges = uint.MaxValue; // Force a cursor update for the new enter serial.
		UpdateHover();
	}

	private void OnPointerLeave(object? sender, WlPointer.LeaveEventArgs args)
	{
		if (!_pointerInside || args.Surface != _surface)
		{
			return;
		}
		_pointerInside = false;
		_pressed = WaylandDecorationPart.None;
		SetHovered(WaylandDecorationPart.None);
	}

	private void OnPointerMotion(object? sender, WlPointer.MotionEventArgs args)
	{
		if (_pointerInside)
		{
			_pointerX = args.SurfaceX.ToDouble();
			_pointerY = args.SurfaceY.ToDouble();
			UpdateHover();
		}
	}

	private void UpdateHover()
	{
		var (part, edges) = _layout.HitTest(_pointerX, _pointerY);
		if (edges != _hoveredEdges && _pointer is { } pointer)
		{
			_hoveredEdges = edges;
			_cursor ??= new WaylandCursor(
				_connection.Shm.Handle,
				new WaylandCursorSurface(pointer, _connection.Compositor.CreateSurface()),
				_connection.CursorShape?.GetPointer(pointer));
			_cursor.Set(_enterSerial, CursorFor(edges), _scale.EffectiveScale);
		}
		SetHovered(part);
	}

	private void SetHovered(WaylandDecorationPart part)
	{
		if (_hovered != part)
		{
			var redraw = IsButton(part) || IsButton(_hovered);
			_hovered = part;
			if (redraw)
			{
				Redraw();
			}
		}
	}

	private void OnPointerButton(object? sender, WlPointer.ButtonEventArgs args)
	{
		if (!_pointerInside || _connection.Seat is not { } seat)
		{
			return;
		}
		_connection.InputSerial = args.Serial;
		var (part, edges) = _layout.HitTest(_pointerX, _pointerY);
		var pressed = args.State == 1;
		if (!pressed)
		{
			var target = _pressed;
			_pressed = WaylandDecorationPart.None;
			if (target != WaylandDecorationPart.None)
			{
				Redraw();
				if (target == part)
				{
					Invoke(target);
				}
			}
			return;
		}
		if (args.Button == RightButton && part == WaylandDecorationPart.TitleBar)
		{
			// Window menu coordinates are relative to the window geometry.
			_toplevel.ShowWindowMenu(seat, args.Serial, (int)(_pointerX - _layout.ResizeMargin), (int)(_pointerY - _layout.ResizeMargin));
			return;
		}
		if (args.Button != LeftButton)
		{
			return;
		}
		switch (part)
		{
			case WaylandDecorationPart.ResizeEdge:
				_toplevel.Resize(seat, args.Serial, edges);
				break;
			case WaylandDecorationPart.TitleBar:
				var isDoubleClick = args.Time - _lastClickTime <= DoubleClickTime
					&& Math.Abs(_pointerX - _lastClickX) <= DoubleClickDistance
					&& Math.Abs(_pointerY - _lastClickY) <= DoubleClickDistance;
				_lastClickTime = args.Time;
				_lastClickX = _pointerX;
				_lastClickY = _pointerY;
				if (isDoubleClick)
				{
					_lastClickTime -= DoubleClickTime + 1;
					if ((_layout.Buttons & WaylandCaptionButtons.Maximize) != 0)
					{
						_owner.ToggleMaximized();
					}
				}
				else
				{
					_toplevel.Move(seat, args.Serial);
				}
				break;
			case WaylandDecorationPart.Minimize:
			case WaylandDecorationPart.Maximize:
			case WaylandDecorationPart.Close:
				_pressed = part;
				Redraw();
				break;
		}
	}

	private void Invoke(WaylandDecorationPart part)
	{
		switch (part)
		{
			case WaylandDecorationPart.Minimize:
				_owner.Minimize();
				break;
			case WaylandDecorationPart.Maximize:
				_owner.ToggleMaximized();
				break;
			case WaylandDecorationPart.Close:
				_owner.RequestClose();
				break;
		}
	}

	private static bool IsButton(WaylandDecorationPart part)
		=> part is WaylandDecorationPart.Minimize or WaylandDecorationPart.Maximize or WaylandDecorationPart.Close;

	private static CoreCursor CursorFor(uint edges) => edges switch
	{
		1 or 2 => _sizeNorthSouth,
		4 or 8 => _sizeWestEast,
		5 or 10 => _sizeNorthwestSoutheast,
		6 or 9 => _sizeNortheastSouthwest,
		_ => _arrow,
	};

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (_connection.Seat is { } seat)
		{
			seat.Capabilities -= OnSeatCapabilities;
		}
		_context.BufferAvailable -= OnBufferAvailable;
		WaylandCleanup.Run(
			() => UpdatePointer(0),
			() => (_drawingFactory as IDisposable)?.Dispose(),
			() => _context.Dispose(),
			() => _viewport?.Dispose(),
			() => _subsurface.Dispose(),
			() => _surface.Dispose());
	}
}

// Window actions requested from the decorations, on the dispatch thread.
internal interface IWaylandDecorationsOwner
{
	void RequestParentCommit();
	void ToggleMaximized();
	void Minimize();
	void RequestClose();
}
