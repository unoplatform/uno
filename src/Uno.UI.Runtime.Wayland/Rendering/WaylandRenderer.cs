#nullable enable

using System;
using System.Threading;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Wayland.Rendering;

/// <summary>
/// Decides whether a frame may be committed: the frame drawn must show the layout of the configure it would
/// acknowledge. All members run on the window's render thread.
/// </summary>
internal interface IWaylandFrameGate
{
	/// <summary>Called before Uno draws its latest recorded frame.</summary>
	void BeginFrame();

	/// <summary>
	/// After <see cref="BeginFrame"/>: whether Uno's latest frame was recorded for the configured generation. When it
	/// was not, the swap chain gives Uno no render target, so the frame is skipped and kept.
	/// </summary>
	bool IsFrameReady { get; }

	/// <summary>Called right before the commit, with the drawn frame's size and the configured buffer size.</summary>
	bool CanPresent(int frameWidth, int frameHeight, int bufferWidth, int bufferHeight);

	/// <summary>Called after a frame was committed, or when the window had no content to draw.</summary>
	void EndFrame(bool presented);

	/// <summary>Called at a display frame callback, independently of layout readiness.</summary>
	void FrameOpportunity();
}

/// <summary>
/// The window's Uno frame producer (its CompositionTarget once content exists). All members run on the
/// window's render thread.
/// </summary>
internal interface IWaylandFrameProducer
{
	/// <summary>Replays Uno's latest frame into the swap chain; false when the window has no content yet.</summary>
	bool TryRequestFrame(ISwapChain swapChain);
}

/// <summary>
/// Drives one window's frames over its negotiated graphics context, paced by wl_surface frame callbacks.
/// Rendering and graphics lifecycle run on the window's render thread.
/// </summary>
internal sealed class WaylandRenderer : IDisposable, ISwapChain
{
	private readonly IWaylandFrameProducer _frames;
	private readonly IWaylandFrameGate _gate;
	private readonly WaylandFrameScheduler _scheduler;
	private IWaylandGraphicsContext _context;
	private IDrawingFactory _drawingFactory;
	private int _width;
	private int _height;
	private bool _configured;
	private bool _disposed;

	public WaylandRenderer(IWaylandFrameProducer frames, IWaylandFrameGate gate, WaylandRenderScale scale, IWaylandGraphicsContext context, IDrawingFactory drawingFactory, Protocols.WlSurface surface, Action<Action> postToOwner)
	{
		_frames = frames;
		_gate = gate;
		Scale = scale;
		_context = context;
		_drawingFactory = drawingFactory;
		_scheduler = new WaylandFrameScheduler(surface, Render, postToOwner, _gate.FrameOpportunity);
		Attach(context);
	}

	public WaylandRenderScale Scale { get; }

	/// <summary>The negotiated backend, read by the host so its CompositionTarget can resolve one.</summary>
	internal IDrawingFactory Factory => Volatile.Read(ref _drawingFactory);

	// Stages the size; the configure is acknowledged right before the first matching commit.
	public void Configure(int logicalWidth, int logicalHeight)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (logicalWidth <= 0 || logicalHeight <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(logicalWidth));
		}
		_width = logicalWidth;
		_height = logicalHeight;
		_configured = true;
		_scheduler.Invalidate();
	}

	// Nothing can be committed before the first configure, which invalidates again.
	public void Invalidate()
	{
		if (_configured)
		{
			_scheduler.Invalidate();
		}
	}

	// Replaces a lost context with a newly negotiated one; the backend factory is bound to its context.
	public void ReplaceGraphics(IWaylandGraphicsContext context, IDrawingFactory drawingFactory)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ReleaseGraphics();
		_context = context;
		Volatile.Write(ref _drawingFactory, drawingFactory);
		Attach(context);
		Invalidate();
	}

	// Called by the graphics context right before it commits a buffer: the configure acknowledgement, scale and
	// viewport must change in the same commit as the buffer they describe.
	public bool PrepareCommit(int width, int height)
	{
		if (!_configured || _disposed)
		{
			return false;
		}
		var scale = Scale.EffectiveScale;
		if (!_gate.CanPresent(width, height, WaylandRenderScale.ToBufferPixels(_width, scale), WaylandRenderScale.ToBufferPixels(_height, scale)))
		{
			return false;
		}
		return true;
	}

	internal void ApplyScale() => Scale.Apply(_width, _height);

	private bool Render()
	{
		if (_disposed || _context is WaylandEglGraphicsContext { IsLost: true })
		{
			return false;
		}
		_gate.BeginFrame();
		if (!_frames.TryRequestFrame(this))
		{
			_gate.EndFrame(false);
			return false;
		}
		if (!_context.Committed)
		{
			return false;
		}
		_gate.EndFrame(true);
		return true;
	}

	GraphicsContextKind IGraphicsContext.Kind => _context.Kind;
	bool ISwapChain.PreservesContents => _context.PreservesContents;

	IRenderTarget? ISwapChain.AcquireRenderTarget(int width, int height)
	{
		// The UI is still laying out a newer configure: its frame would be rejected at the commit, so it is not drawn.
		// Requesting it still lets Uno record the next UI frame.
		if (!_gate.IsFrameReady)
		{
			return null;
		}
		var size = WaylandRenderTargetSize.GetSize(_configured, _width, _height, Scale.EffectiveScale, width, height);
		return _context.AcquireRenderTarget(size.Width, size.Height);
	}

	void ISwapChain.SetPresentDamage(Windows.Foundation.Rect[]? damage) => _context.SetPresentDamage(damage);

	void ISwapChain.Present() => _context.Present();

	private void Attach(IWaylandGraphicsContext context)
	{
		if (context is WaylandSoftwareGraphicsContext software)
		{
			software.BufferAvailable += OnBufferAvailable;
		}
	}

	private void OnBufferAvailable(object? sender, EventArgs args) => _scheduler.BufferAvailable();

	private void ReleaseGraphics()
	{
		if (_context is WaylandSoftwareGraphicsContext software)
		{
			software.BufferAvailable -= OnBufferAvailable;
		}
		ReleaseGraphics(_context, _drawingFactory);
	}

	internal static void ReleaseGraphics(IWaylandGraphicsContext context, IDrawingFactory drawingFactory)
	{
		try
		{
			// The backend factory owns GPU resources bound to the context, so it goes first, with the context current.
			(context as IWaylandGpuTeardownContext)?.TryMakeCurrentForTeardown();
			(drawingFactory as IDisposable)?.Dispose();
		}
		finally
		{
			context.Dispose();
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_scheduler.Dispose();
		ReleaseGraphics();
	}
}
