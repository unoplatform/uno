#nullable enable

using System;
using System.Threading;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Wayland.Rendering;

/// <summary>
/// Decides whether a frame may be committed: the frame drawn must show the layout of the configure it would
/// acknowledge. All members run on the window's render thread.
/// </summary>
internal interface IWaylandFrameGate
{
	/// <summary>Called before Uno draws its latest recorded frame.</summary>
	void BeginFrame();

	/// <summary>Called right before the commit, with the drawn frame's size and the configured buffer size.</summary>
	bool CanPresent(int frameWidth, int frameHeight, int bufferWidth, int bufferHeight);
}

/// <summary>
/// Drives one window's frames over its negotiated graphics context, paced by wl_surface frame callbacks.
/// Rendering and graphics lifecycle run on the window's render thread.
/// </summary>
internal sealed class WaylandRenderer : IDisposable
{
	private readonly IXamlRootHost _host;
	private readonly IWaylandFrameGate _gate;
	private readonly WaylandFrameScheduler _scheduler;
	private IWaylandGraphicsContext _context;
	private IDrawingFactory _drawingFactory;
	private int _width;
	private int _height;
	private bool _configured;
	private bool _commitAllowed;
	private bool _disposed;

	public WaylandRenderer(IXamlRootHost host, IWaylandFrameGate gate, WaylandRenderScale scale, IWaylandGraphicsContext context, IDrawingFactory drawingFactory, Protocols.WlSurface surface, Action<Action> postToOwner)
	{
		_host = host;
		_gate = gate;
		Scale = scale;
		_context = context;
		_drawingFactory = drawingFactory;
		_scheduler = new WaylandFrameScheduler(surface, Render, ProduceUnpresentedFrame, postToOwner);
		Attach(context);
	}

	public WaylandRenderScale Scale { get; }

	/// <summary>The negotiated backend, read by the host so its CompositionTarget can resolve one.</summary>
	internal IDrawingFactory Factory => Volatile.Read(ref _drawingFactory);

	public bool IsConfigured => _configured;

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

	public void Invalidate()
	{
		if (_configured)
		{
			_scheduler.Invalidate();
		}
		else
		{
			// Uno schedules its next UI frame from the native frame request, so keep consuming frames before the
			// first configure even though nothing can be committed yet.
			ProduceUnpresentedFrame();
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
		if (!_configured || _disposed || !_commitAllowed)
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

	private bool Render() => Draw(commit: true);

	// Compositors withhold frame callbacks from hidden surfaces; Uno keeps getting frames, which are not committed.
	private void ProduceUnpresentedFrame() => Draw(commit: false);

	private bool Draw(bool commit)
	{
		if (_disposed
			|| _context is WaylandEglGraphicsContext { IsLost: true }
			|| _host.RootElement?.Visual.CompositionTarget is not CompositionTarget compositionTarget)
		{
			return false;
		}
		_commitAllowed = commit;
		_gate.BeginFrame();
		compositionTarget.OnNativePlatformFrameRequested(_context);
		return commit && _context.Committed;
	}

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
		try
		{
			// The backend factory owns GPU resources bound to the context, so it goes first, with the context current.
			// A context that cannot be made current is abandoned so the backend issues no GL calls into it.
			if (_context is IWaylandGpuTeardownContext gpu && !gpu.TryMakeCurrentForTeardown())
			{
				(_drawingFactory as IGpuDrawingFactory)?.AbandonContext();
			}
			(_drawingFactory as IDisposable)?.Dispose();
		}
		finally
		{
			_context.Dispose();
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
