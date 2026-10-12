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
	// Partial repaint: the compositor clips the clear+replay to the damaged rects before replaying, and the host's
	// target keeps last frame's pixels (WebGpuSwapChainContext.PreservesContents). That clip is tracked here and
	// handed to the frame as a device rect, which scissors the pass and loads instead of clearing. Only a plain
	// rect under a pure scale maps to a scissor; any other clip or transform falls back to repainting whole.
	private Rect? _presentClip;
	private bool _clipNotRect;
	// What this present was told it may skip, fixed when the session opened. Not session state and not a clip: it
	// describes the target, so Save/Restore has no bearing on it.
	private readonly Rect? _damage;
	private readonly Stack<(Vector2 Scale, Rect? Clip, bool NotRect)> _stateStack = new();
	// Immediate-mode drawing on the present session (e.g. the FPS/diagnostics overlay drawn after Replay) records
	// here and joins the replayed frame's pass at Dispose - the present session IS a real drawing session, like the
	// Skia one, not a replay-only sink. State verbs (Save/Scale/clip/...) forward here too so the overlay honours the
	// transform; Scale/Save/Restore additionally drive the frame's root DPI scale (_presentScale).
	private readonly WebGpuCommandRecorder _overlay;
	// Everything the present asked for, in the order it asked: a replayed recording, or a run of immediate-mode
	// drawing done straight on the session. Both are command lists, so both are entries -- which is what keeps
	// drawing done BEFORE a replay underneath it, and lets a present that only draws produce a frame at all.
	private readonly List<WebGpuFrame.ReplayEntry> _entries = new();
	// How much of the recorder's list has already been taken into an entry.
	private int _drawn;

	internal WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s, IDrawingFactory factory, ReadOnlySpan<Rect> damage = default)
	{
		_d = d;
		_s = s;
		_factory = factory;
		_overlay = new WebGpuCommandRecorder(factory);
		if (damage.Length > 0)
		{
			// The bounding box, though the frame can scissor each draw to the region it falls in (one walk, whatever
			// the count). Measured on both: honouring them separately wins only where the regions are many and
			// small, and costs 17-19% on the common two-region scroll, where the box is barely bigger than the
			// regions and a draw reaching both is issued twice. Taking the box also keeps the full-surface scissor
			// widening that collapses SetScissorRect runs, which several regions defeat.
			var bounds = damage[0];
			for (var i = 1; i < damage.Length; i++) { bounds.Union(damage[i]); }
			_damage = bounds;
		}
	}

	public void Replay(IRenderRecord data)
	{
		// During an async backend switch (e.g. the browser's on-canvas WebGPU init) a frame recorded by the
		// previous renderer can reach us; skip it rather than mis-cast — the next frame is recorded by this backend.
		if (data is not WebGpuRenderRecord rd) { return; }
		lock (_d.RenderGate)
		{
			if (_entries.Count == 0)
			{
				// Reclaims last frame's pooled textures/buffers and releases its bind groups, so it belongs to the
				// present and not to the replay -- a second one would free resources this frame is still using.
				_d.BeginFrameResources();
			}

			// Anything drawn on the session before this replay belongs under it.
			TakeDrawn();

			// The frame is recorded in logical coordinates; the root DPI scale is the walk's root matrix, applied at
			// present and never folded into a recording. The render itself waits for Dispose, so a present builds up
			// its whole sequence before any of it is encoded.
			_entries.Add(new WebGpuFrame.ReplayEntry(
				rd.Commands,
				Matrix3x2.CreateScale(_presentScale.X, _presentScale.Y),
				_presentClear ?? rd.ClearColor,
				CurrentDamageClip()));
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
			new WebGpuFrame(_d, _s).Run(rd.Commands, Matrix3x2.Identity, _presentClear ?? rd.ClearColor);
		}
	}

	/// <summary>
	/// Closes off whatever has been drawn on the session since the last entry, as an entry of its own.
	/// </summary>
	/// <remarks>
	/// Every recorded command carries its own matrix and clip (the recorder's state is baked in as it emits), so a
	/// slice of the list replays exactly as it was recorded and needs no root transform of its own.
	/// </remarks>
	private void TakeDrawn()
	{
		if (_overlay.Finish() is not WebGpuRenderRecord recorded || recorded.Commands.Count == _drawn)
		{
			return;
		}

		// Immediate-mode drawing is unbounded -- nothing says where it lands -- so an entry carrying it repaints
		// what it covers rather than claiming a damage rect.
		_entries.Add(new WebGpuFrame.ReplayEntry(
			recorded.Commands.GetRange(_drawn, recorded.Commands.Count - _drawn), Matrix3x2.Identity, null, null));
		_drawn = recorded.Commands.Count;
	}

	// What this replay is confined to, in device pixels: the damage, narrowed by any clip in force. Null repaints
	// everything it covers -- no bound, or a clip the frame cannot turn into a scissor.
	private Vector4? CurrentDamageClip()
	{
		if (_clipNotRect)
		{
			return null;
		}

		// The damage arrived in device pixels; a clip is in the session's own coordinates.
		Vector4? region = _damage is { } damage ? ToVector(damage) : null;
		if (_presentClip is { } clip)
		{
			var scaled = ToDevice(clip);
			region = region is { } bounded
				? new Vector4(MathF.Max(bounded.X, scaled.X), MathF.Max(bounded.Y, scaled.Y), MathF.Min(bounded.Z, scaled.Z), MathF.Min(bounded.W, scaled.W))
				: scaled;
		}

		return region;
	}

	private Vector4 ToDevice(in Rect rect)
		=> new(
			(float)rect.Left * _presentScale.X, (float)rect.Top * _presentScale.Y,
			(float)rect.Right * _presentScale.X, (float)rect.Bottom * _presentScale.Y);

	private static Vector4 ToVector(in Rect rect)
		=> new((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);

	public Matrix4x4 TotalMatrix => _overlay.TotalMatrix;
	// A transform other than the root scale leaves the tracked clip in a space that no longer maps to device pixels,
	// so the frame repaints whole rather than scissoring to a rect it cannot place.
	public void SetMatrix(in Matrix4x4 matrix) { _clipNotRect = true; _overlay.SetMatrix(matrix); }
	public void Concat(in Matrix4x4 matrix) { _clipNotRect = true; _overlay.Concat(matrix); }
	public void Translate(float dx, float dy) { _clipNotRect = true; _overlay.Translate(dx, dy); }
	public void Scale(float sx, float sy) { _presentScale = new Vector2(_presentScale.X * sx, _presentScale.Y * sy); _overlay.Scale(sx, sy); }
	public int Save() { _stateStack.Push((_presentScale, _presentClip, _clipNotRect)); _overlay.Save(); return _stateStack.Count; }
	public int SaveCount => _stateStack.Count;
	public object NativeSurface => null;
	public IDrawingFactory Factory => _factory;
	public void Restore() { if (_stateStack.Count > 0) { (_presentScale, _presentClip, _clipNotRect) = _stateStack.Pop(); } _overlay.Restore(); }
	public void RestoreToCount(int count) { while (_stateStack.Count > count) { Restore(); } }
	public void SaveLayer() => _overlay.SaveLayer();
	public void SaveLayer(IColorFilter colorFilter) => _overlay.SaveLayer(colorFilter);
	public void SaveLayerMask() => _overlay.SaveLayerMask();
	public void SaveLayer(IEffectFilter filter) => _overlay.SaveLayer(filter);
	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect)
	{
		if (operation == ClipOperation.Intersect)
		{
			var clip = rect;
			if (_presentClip is { } current) { clip.Intersect(current); }
			_presentClip = clip;
		}
		else
		{
			_clipNotRect = true;
		}
		_overlay.ClipRect(rect, operation);
	}

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect) { _clipNotRect = true; _overlay.ClipRoundRect(roundRect, operation); }
	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect) { _clipNotRect = true; _overlay.ClipPath(geometry, operation); }
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
			TakeDrawn();
			if (_entries.Count == 0)
			{
				// The present neither replayed nor drew (e.g. a transitional frame during an async backend switch).
				return;
			}
			new WebGpuFrame(_d, _s).Run(_entries);
			_entries.Clear();
			_drawn = 0;
		}
	}
}
