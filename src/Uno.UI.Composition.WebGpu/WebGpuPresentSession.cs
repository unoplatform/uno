// The WebGPU present session: the IDrawingSession the compositor draws a frame into. It hands the replayed recording
// and the immediate-mode overlay to one WebGpuFrame at Dispose; WebGpuCommandRecorder records, WebGpuFrame renders.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

public sealed class WebGpuPresentSession : IPresentSession
{
	private readonly WebGpuDevice _d;
	private readonly WebGpuRenderSurface _s;
	private readonly IDrawingFactory _factory;
	private WColor? _presentClear;
	// The composition records in LOGICAL coordinates; the root DPI scale is the walk's root matrix.
	private Vector2 _presentScale = Vector2.One;
	private readonly Stack<Vector2> _presentScaleStack = new();
	// Immediate-mode drawing on the present session (e.g. the FPS/diagnostics overlay drawn after Replay) records
	// here and joins the replayed frame's pass at Dispose - the present session IS a real drawing session, like the
	// Skia one, not a replay-only sink. State verbs (Save/Scale/clip/...) forward here too so the overlay honours the
	// transform; Scale/Save/Restore additionally drive the frame's root DPI scale (_presentScale).
	private readonly WebGpuCommandRecorder _overlay;
	private List<WebGpuCommand> _pendingCmds;
	private Vector2 _pendingScale = Vector2.One;
	private WColor? _pendingClear;

	internal WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s, IDrawingFactory factory)
	{
		_d = d;
		_s = s;
		_factory = factory;
		_overlay = new WebGpuCommandRecorder(factory);
	}

	public void Replay(IRenderRecord data)
	{
		// During an async backend switch (e.g. the browser's on-canvas WebGPU init) a frame recorded by the
		// previous renderer can reach us; skip it rather than mis-cast — the next frame is recorded by this backend.
		if (data is not WebGpuRenderRecord rd) { return; }
		lock (_d.RenderGate)
		{
			_d.BeginFrameResources();   // reclaim last frame's pooled textures/buffers + release its bind groups
										// The frame is recorded in logical coordinates; the root DPI scale is the walk's root matrix, applied at
										// present and never folded into a recording. The render itself waits for Dispose so the immediate-mode
										// overlay joins the same pass.
			_pendingCmds = rd.Commands;
			_pendingScale = _presentScale;
			_pendingClear = _presentClear ?? rd.ClearColor;
		}
	}

	// Renders WITHOUT the per-frame reset — for a nested offscreen render (RenderOffscreen) that may run inside an
	// enclosing frame; resetting the shared pools mid-frame would free the enclosing frame's in-flight resources.
	// The gate is reentrant, so a nested call inside an enclosing Replay is safe; an independent call is serialized.
	public void ReplayNested(IRenderRecord data)
	{
		if (data is not WebGpuRenderRecord rd) { return; }
		lock (_d.RenderGate)
		{
			new WebGpuFrame(_d, _s).Run(rd.Commands, Matrix3x2.Identity, null, _presentClear ?? rd.ClearColor);
		}
	}

	public Matrix4x4 TotalMatrix => _overlay.TotalMatrix;
	public void SetMatrix(in Matrix4x4 matrix) => _overlay.SetMatrix(matrix);
	public void Concat(in Matrix4x4 matrix) => _overlay.Concat(matrix);
	public void Translate(float dx, float dy) => _overlay.Translate(dx, dy);
	public void Scale(float sx, float sy) { _presentScale = new Vector2(_presentScale.X * sx, _presentScale.Y * sy); _overlay.Scale(sx, sy); }
	public int Save() { _presentScaleStack.Push(_presentScale); _overlay.Save(); return _presentScaleStack.Count; }
	public int SaveCount => _presentScaleStack.Count;
	public object NativeSurface => null;
	public IDrawingFactory Factory => _factory;
	public void Restore() { if (_presentScaleStack.Count > 0) { _presentScale = _presentScaleStack.Pop(); } _overlay.Restore(); }
	public void RestoreToCount(int count) { while (_presentScaleStack.Count > count) { Restore(); } }
	public void SaveLayer() => _overlay.SaveLayer();
	public void SaveLayer(IColorFilter colorFilter) => _overlay.SaveLayer(colorFilter);
	public void SaveLayerMask() => _overlay.SaveLayerMask();
	public void SaveLayer(IEffectFilter filter) => _overlay.SaveLayer(filter);
	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect) => _overlay.ClipRect(rect, operation);
	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect) => _overlay.ClipRoundRect(roundRect, operation);
	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect) => _overlay.ClipPath(geometry, operation);
	public void Clear(WColor color) => _presentClear = color;
	public void DrawRect(in Rect rect, WColor color) => _overlay.DrawRect(rect, color);
	public void DrawRect(in Rect rect, IShader shader) => _overlay.DrawRect(rect, shader);
	public void DrawRoundedRect(in RoundRectangle roundRect, WColor color) => _overlay.DrawRoundedRect(roundRect, color);
	public void DrawRoundedRectBorder(in RoundRectangle outer, in RoundRectangle inner, WColor color) => _overlay.DrawRoundedRectBorder(outer, inner, color);
	public void DrawPath(IGeometry geometry, WColor color) => _overlay.DrawPath(geometry, color);
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive) => _overlay.DrawShadow(silhouette, color, sigmaX, sigmaY, additive);
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, StrokeJoin join = StrokeJoin.Miter) => _overlay.StrokePath(geometry, color, strokeWidth, join);
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth) => _overlay.DrawLine(p0, p1, color, strokeWidth);
	public void DrawImage(ITexture texture, float x, float y, float opacity = 1f) => _overlay.DrawImage(texture, x, y, opacity);
	public void DrawImage(ITexture texture, float x, float y, IColorFilter colorFilter) => _overlay.DrawImage(texture, x, y, colorFilter);
	public void DrawImageTiled(ITexture texture, in Rect destination, EdgeExtend extendX, EdgeExtend extendY, float opacity = 1f) => _overlay.DrawImageTiled(texture, destination, extendX, extendY, opacity);
	public void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow) => _overlay.DrawImageNineSlice(texture, centerSlice, destination, centerHollow);
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) => _overlay.DrawEffectBackdrop(filter, opacity);

	// Renders the deferred frame under its root DPI scale with the immediate-mode overlay (e.g. the diagnostics FPS
	// counter drawn after Replay, already in device pixels) on top, in one pass.
	public void Dispose()
	{
		lock (_d.RenderGate)
		{
			if (_pendingCmds is not { } main)
			{
				// No frame was replayed this present (e.g. a transitional frame during an async backend switch).
				return;
			}
			var overlay = _overlay.Finish() is WebGpuRenderRecord od && od.Commands.Count > 0 ? od.Commands : null;
			new WebGpuFrame(_d, _s).Run(main, Matrix3x2.CreateScale(_pendingScale.X, _pendingScale.Y), overlay, _pendingClear);
			_pendingCmds = null;
		}
	}
}
