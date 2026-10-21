#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Wayland.Input;
using Uno.UI.Runtime.Wayland.Protocols;
using Uno.UI.Runtime.Wayland.Rendering;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland;

internal sealed class WaylandXamlRootHost : IXamlRootHost, IWaylandFrameGate, IWaylandFrameProducer, IDisposable
{
	private readonly Window _window;
	private readonly XamlRoot _xamlRoot;
	private readonly WaylandConnection _connection;
	private readonly int _id;
	private readonly WlSurface _surface;
	private readonly WaylandRenderThread _renderThread;
	private readonly WaylandProtocolInputConnection? _input;
	private readonly WaylandSeat? _seat;
	private readonly WaylandFrameReadiness _readiness = new();
	private readonly WaylandRenderErrorThrottle _renderErrors = new();
	private readonly Func<long, Func<bool>, bool> _preparePresentation;
	private readonly Action<long, bool> _frameConsumed;
	private volatile bool _disposed;

	// Render thread state.
	private volatile WaylandRenderer? _renderer;
	private IWaylandGraphicsContext? _unownedContext;
	private IDrawingFactory? _unownedDrawingFactory;

	// UI thread state.
	private CompositionTarget? _frameTarget;

	// Dispatch thread state.
	private WaylandCursor? _cursor;
	private WlPointer? _cursorPointer;

	internal WaylandXamlRootHost(WaylandConnection connection, int id, Window window, XamlRoot xamlRoot, WlSurface surface, WpViewport? viewport, Action<bool> keyboardFocusChanged, Func<long, Func<bool>, bool> preparePresentation, Action<long, bool> frameConsumed, Action rollbackProtocolObjects)
	{
		_connection = connection;
		_id = id;
		_window = window;
		_xamlRoot = xamlRoot;
		_surface = surface;
		_preparePresentation = preparePresentation;
		_frameConsumed = frameConsumed;
		try
		{
			_renderThread = new WaylandRenderThread($"Uno Wayland render {id}", OnRenderError);
			var (context, drawingFactory) = InitializeGraphics();
			_unownedContext = context;
			_unownedDrawingFactory = drawingFactory;
			var scale = new WaylandRenderScale(surface, viewport);
			var initialized = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_renderThread.Post(() =>
			{
				try
				{
					_renderer = new WaylandRenderer(this, this, scale, context, drawingFactory, surface, _renderThread.Post);
					_unownedContext = null;
					_unownedDrawingFactory = null;
					initialized.TrySetResult();
				}
				catch (Exception error)
				{
					initialized.TrySetException(error);
				}
			});
			initialized.Task.GetAwaiter().GetResult();

			if (connection.Seat is { } wlSeat)
			{
				_input = new WaylandProtocolInputConnection(wlSeat, s => s == surface, connection.Display.Send, SetCursor);
				_input.PointerReleasing += (_, _) => DisposeCursor();
				_input.InputSerial += (_, serial) => connection.InputSerial = serial;
			}
			var input = (IWaylandInputConnection?)_input ?? NoInputConnection.Instance;
			KeyboardSource = new WaylandKeyboardInputSource(input, _connection.PostToUI, keyboardFocusChanged);
			PointerSource = new WaylandPointerInputSource(input, _connection.PostToUI, () => KeyboardSource.Modifiers);
			if (_input is not null)
			{
				var seat = new WaylandSeat(connection.Seat!, _input, PointerSource, KeyboardSource);
				_seat = seat;
				// Capabilities were announced at bind time; replay them for this window's devices.
				connection.Display.Send(() => seat.Capabilities(connection.SeatCapabilities));
			}

			connection.TextInputReceived += OnTextInputReceived;

			// Input sources must exist before registration: Uno resolves them per IXamlRootHost.
			XamlRootMap.Register(xamlRoot, this);
		}
		catch
		{
			if (_renderThread is null)
			{
				try
				{
					using (connection.Display.Lock())
					{
						rollbackProtocolObjects();
					}
				}
				catch (Exception cleanupError)
				{
					connection.TrackWindowTeardown(Task.FromException(cleanupError));
				}
			}
			else
			{
				connection.TrackWindowTeardown(Completion);
				Dispose(rollbackProtocolObjects);
			}
			throw;
		}
	}

	internal WlSurface Surface => _surface;
	internal WaylandPointerInputSource PointerSource { get; }
	internal WaylandProtocolInputConnection? Input => _input;
	internal WaylandKeyboardInputSource KeyboardSource { get; }

	// The backend that won negotiation, also asked for the client-side decorations' software rendering.
	internal IGraphicsProvider? GraphicsProvider { get; private set; }

	// Written on the UI thread, read by the dispatch thread for cursor scaling.
	internal double Scale { get; set; } = 1;

	public UIElement? RootElement => _window.RootElement;

	IDrawingFactory? IXamlRootHost.Renderer => _renderer?.Factory;

	public Windows.UI.Color? BackgroundColor
	{
		get
		{
			switch (_window.Background)
			{
				case SolidColorBrush brush:
					return brush.Color;
				case not null:
					this.LogError()?.Error("This platform only supports SolidColorBrush for the Window background");
					return null;
				default:
					return null;
			}
		}
	}

	public void InvalidateRender()
	{
		if (!_disposed)
		{
			_renderThread.Post(() => _renderer?.Invalidate());
		}
	}

	// UI thread. The host turns a negotiated context kind into a context for this surface; the backend is chosen by
	// GraphicsRegistry, honouring the kinds the host builder disabled.
	private (IWaylandGraphicsContext Context, IDrawingFactory DrawingFactory) InitializeGraphics()
	{
		var previousFactory = GraphicsRegistry.ContextFactory;
		GraphicsInitialization init;
		try
		{
			GraphicsRegistry.ContextFactory = kind => Task.FromResult<ISwapChain?>(CreateContext(kind));
			init = GraphicsRegistry.Initialize();
		}
		finally
		{
			GraphicsRegistry.ContextFactory = previousFactory;
		}
		GraphicsProvider = init.Provider;
		Microsoft.UI.Composition.Compositor.GetSharedCompositor().IsSoftwareRenderer = init.Context.Kind == GraphicsContextKind.Software;
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace($"window{_id}: graphics context {init.Context.Kind}");
		}
		var context = (IWaylandGraphicsContext)init.Context;
		if (context is WaylandEglGraphicsContext egl)
		{
			egl.ContextLost += OnContextLost;
		}
		return (context, init.Renderer);
	}

	private IWaylandGraphicsContext? CreateContext(GraphicsContextKind kind)
	{
		bool PrepareCommit(int width, int height, Func<bool> submit)
			=> _connection.Display.SubmitPresentation(() =>
				(_renderer?.PrepareCommit(width, height) ?? false) && _preparePresentation(_readiness.RenderGeneration, () =>
				{
					_renderer!.ApplyScale();
					return submit();
				}));
		switch (kind)
		{
			case GraphicsContextKind.OpenGLES:
				try
				{
					return new WaylandEglGraphicsContext(_connection.Display.NativeHandle, _surface, PrepareCommit);
				}
				catch (Exception error) when (error is InvalidOperationException or NotSupportedException or DllNotFoundException or EntryPointNotFoundException)
				{
					if (this.Log().IsEnabled(LogLevel.Information))
					{
						this.Log().Info($"EGL OpenGL ES context creation failed ({error.Message}); falling through.");
					}
					return null;
				}

			case GraphicsContextKind.Software:
				return new WaylandSoftwareGraphicsContext(_surface, _connection.Shm, _renderThread.Post, PrepareCommit);

			default:
				return null;
		}
	}

	// Render thread. The backend's resources died with the context, so the window negotiates graphics again,
	// which falls back to software rendering when EGL is unusable.
	private void OnContextLost(object? sender, EventArgs args)
	{
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"The EGL context of window {_id} was lost; recreating its graphics.");
		}
		_connection.PostToUI(() =>
		{
			if (_disposed)
			{
				return;
			}
			IWaylandGraphicsContext context;
			IDrawingFactory drawingFactory;
			try
			{
				(context, drawingFactory) = InitializeGraphics();
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Wayland graphics could not be recreated for window {_id}.", error);
				}
				return;
			}
			_renderThread.Post(() =>
			{
				if (_renderer is { } renderer)
				{
					renderer.ReplaceGraphics(context, drawingFactory);
				}
				else
				{
					(drawingFactory as IDisposable)?.Dispose();
					context.Dispose();
				}
			});
		});
	}

	// UI thread, after the configure's bounds and scale were applied. The render target follows new bounds
	// immediately, while Uno records the new layout only in a later frame.
	internal void ConfigureUI(long generation, bool boundsChanged)
	{
		if (RootElement?.Visual.CompositionTarget is not CompositionTarget compositionTarget)
		{
			// Nothing was recorded yet, so no older layout can be presented.
			_readiness.ConfigureUI(generation, boundsChanged: false);
			return;
		}
		var waits = _readiness.ConfigureUI(generation, boundsChanged);
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace(FormattableString.Invariant($"window{_id}: UI configure generation {generation}{(waits ? " waits for layout" : "")}"));
		}
		if (waits)
		{
			if (_frameTarget is null)
			{
				_frameTarget = compositionTarget;
				compositionTarget.FrameRendered += OnFrameRendered;
			}
			// Records a frame even when layout leaves the visuals unchanged.
			((ICompositionTarget)compositionTarget).RequestNewFrame();
		}
	}

	// UI thread.
	private void OnFrameRendered()
	{
		var bounds = _xamlRoot.Bounds;
		// The recorded frame reflects the new bounds only if no layout pass was still pending.
		var arranged = FrameRenderHelper.CanRecordFrame(RootElement)
			&& Math.Abs(RootElement.RenderSize.Width - bounds.Width) * Scale <= 1
			&& Math.Abs(RootElement.RenderSize.Height - bounds.Height) * Scale <= 1;
		if (_readiness.OnFrameRecorded(arranged))
		{
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace(FormattableString.Invariant($"window{_id}: layout recorded at {bounds.Width}x{bounds.Height}{(arranged ? "" : " (gave up waiting)")}"));
			}
			InvalidateRender();
		}
	}

	// Called after the configure was staged on the dispatch thread.
	internal void Configure(int logicalWidth, int logicalHeight, uint preferredScale, int integerScale, long generation)
	{
		_renderThread.Post(() =>
		{
			if (_renderer is not { } renderer)
			{
				return;
			}
			_readiness.ConfigureRender(generation);
			if (preferredScale != 0)
			{
				renderer.Scale.SetPreferredScale(preferredScale);
			}
			renderer.Scale.SetIntegerScale(integerScale);
			renderer.Configure(logicalWidth, logicalHeight);
		});
	}

	// Render thread.
	bool IWaylandFrameProducer.TryRequestFrame(ISwapChain swapChain)
	{
		if (RootElement?.Visual.CompositionTarget is not CompositionTarget compositionTarget)
		{
			return false;
		}
		compositionTarget.OnNativePlatformFrameRequested(swapChain);
		return true;
	}

	// Render thread, before Uno draws its latest recorded frame.
	void IWaylandFrameGate.BeginFrame() => _readiness.BeginFrame();

	bool IWaylandFrameGate.IsFrameReady
	{
		get
		{
			if (_readiness.IsFrameReady)
			{
				return true;
			}
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace(FormattableString.Invariant($"window{_id}: skip stale generation {_readiness.RenderGeneration} before drawing"));
			}
			return false;
		}
	}

	void IWaylandFrameGate.FrameOpportunity() => _frameConsumed(_readiness.RenderGeneration, false);

	void IWaylandFrameGate.EndFrame(bool presented)
	{
		if (_readiness.IsFrameReady)
		{
			_frameConsumed(_readiness.RenderGeneration, presented);
		}
	}

	// Render thread. Keeps the previous buffer while the frame still has the size of an older configure or scale,
	// so a frame drawn for stale geometry never acknowledges the new configure.
	bool IWaylandFrameGate.CanPresent(int frameWidth, int frameHeight, int bufferWidth, int bufferHeight)
	{
		if (!_readiness.CanPresent(frameWidth, frameHeight, bufferWidth, bufferHeight))
		{
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace(FormattableString.Invariant($"window{_id}: skip stale frame {frameWidth}x{frameHeight} generation {_readiness.RenderGeneration} for buffer {bufferWidth}x{bufferHeight}"));
			}
			return false;
		}
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace(FormattableString.Invariant($"window{_id}: present frame {frameWidth}x{frameHeight} generation {_readiness.RenderGeneration}"));
		}
		return true;
	}

	private void OnRenderError(Exception error)
	{
		// The host reports compositor failure once when its dispatch loop stops.
		if (_connection.Display.DispatchError is not null || !this.Log().IsEnabled(LogLevel.Error))
		{
			return;
		}
		if (_renderErrors.ShouldReport(Environment.TickCount64, out var suppressed))
		{
			this.Log().Error(suppressed == 0
				? $"Wayland rendering failed for window {_id}."
				: $"Wayland rendering failed for window {_id} ({suppressed} further failures since the last report).", error);
		}
	}

	// Dispatch thread.
	private void SetCursor(uint serial, CoreCursor? cursor)
	{
		if (_disposed || _input?.Pointer is not { } pointer)
		{
			return;
		}
		if (_cursor is null || _cursorPointer != pointer)
		{
			DisposeCursor();
			_cursor = new WaylandCursor(
				_connection.Shm.Handle,
				new WaylandCursorSurface(pointer, _connection.Compositor.CreateSurface()),
				_connection.CursorShape?.GetPointer(pointer));
			_cursorPointer = pointer;
		}
		_cursor.Set(serial, cursor, Scale);
	}

	private void DisposeCursor()
	{
		_cursor?.Dispose();
		_cursor = null;
		_cursorPointer = null;
	}

	// Completes once Dispose has released the renderer and destroyed the protocol objects.
	internal Task Completion => _renderThread.Completion;

	// UI thread. Protocol objects are destroyed after the renderer has released its buffers.
	internal void Dispose(Action destroyProtocolObjects)
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		var display = _connection.Display;
		Exception? inputError = null;
		try
		{
			WaylandCleanup.Run(
				() =>
				{
					if (_frameTarget is not null)
					{
						_frameTarget.FrameRendered -= OnFrameRendered;
						_frameTarget = null;
					}
				},
				() =>
				{
					XamlRootMap.Unregister(_xamlRoot);
				},
				() =>
				{
					using (display.Lock())
					{
						_connection.TextInputReceived -= OnTextInputReceived;
						WaylandCleanup.Run(
							() => _seat?.Dispose(),
							() =>
							{
								if (_seat is null)
								{
									KeyboardSource?.Dispose();
								}
							},
							DisposeCursor);
					}
				});
		}
		catch (Exception error)
		{
			inputError = error;
		}
		_renderThread.Complete(() =>
		{
			try
			{
				if (_renderer is { } renderer)
				{
					renderer.Dispose();
					_renderer = null;
				}
				else if (_unownedContext is { } context)
				{
					try
					{
						(context as IWaylandGpuTeardownContext)?.TryMakeCurrentForTeardown();
						(_unownedDrawingFactory as IDisposable)?.Dispose();
					}
					finally
					{
						context.Dispose();
						_unownedContext = null;
						_unownedDrawingFactory = null;
					}
				}
			}
			finally
			{
				var destroyed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				if (!display.TrySend(() =>
				{
					try
					{
						destroyProtocolObjects();
						destroyed.TrySetResult();
					}
					catch (Exception error)
					{
						destroyed.TrySetException(error);
					}
				}))
				{
					destroyed.TrySetException(new InvalidOperationException("Wayland dispatch stopped before window destruction."));
				}
				WaylandCleanup.Run(
					() => destroyed.Task.GetAwaiter().GetResult(),
					() =>
					{
						if (inputError is not null)
						{
							throw inputError;
						}
					});
			}
		});
		_renderThread.Dispose();
	}

	void IDisposable.Dispose() => Dispose(() => { });

	// Dispatch thread: text produced by the input method ends any pending dead-key or Compose sequence.
	private void OnTextInputReceived(object? sender, EventArgs e) => KeyboardSource.ResetCompose();

	// Used when the compositor advertises no wl_seat: sources exist but never receive devices.
	private sealed class NoInputConnection : IWaylandInputConnection
	{
		internal static readonly NoInputConnection Instance = new();
		public IDisposable GetPointer(WaylandPointerInputSource sink) => throw new NotSupportedException();
		public IDisposable GetKeyboard(WaylandKeyboardInputSource sink) => throw new NotSupportedException();
		public IDisposable GetTouch(WaylandPointerInputSource sink) => throw new NotSupportedException();
		public void SetCursor(uint enterSerial, CoreCursor? cursor) { }
		public void Send(Action requests) { }
	}
}
