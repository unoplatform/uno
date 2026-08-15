#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The context threaded through every command on replay: the destination session, the destination's transform
/// at the moment replay began (folded into recorded absolute <see cref="IDrawingSession.SetMatrix"/> calls so a
/// recording made in one coordinate space composes into another — matching SkiaSharp's picture playback), and
/// the save-depth offset that maps recording-relative save counts onto the destination's stack.
/// </summary>
internal sealed class ReplayContext
{
	public required IDrawingSession Target;
	public Matrix4x4 Base;
	public int DepthOffset;
}

/// <summary>Opaque retained frame produced by <see cref="CommandListRecorder"/>: the recorded verb list.</summary>
internal sealed class CommandList : IRenderData
{
	private List<Action<ReplayContext>>? _commands;
	// Geometry snapshots the recording owns (the composition disposes the originals right after the draw call,
	// so — like a native display list that copies path data — the recording holds its own copies and frees them
	// when disposed). Other resources (shaders/filters/images) are brush-owned and outlive the recording.
	private List<IGeometry>? _ownedGeometries;

	internal CommandList(List<Action<ReplayContext>> commands, List<IGeometry>? ownedGeometries)
	{
		_commands = commands;
		_ownedGeometries = ownedGeometries;
	}

	public void Replay(IDrawingSession target)
	{
		if (_commands is not { } commands)
		{
			return;
		}

		// Guard the destination so a recording's own save/restore can never escape past where it began, and so
		// the transform/clip are restored afterwards. The recorded absolute SetMatrix values are relative to the
		// destination transform captured here; relative ops (Concat/Translate/Scale/Save/clips/draws) replay as-is.
		var guard = target.Save();
		var ctx = new ReplayContext { Target = target, Base = target.TotalMatrix, DepthOffset = target.SaveCount - 1 };
		foreach (var command in commands)
		{
			command(ctx);
		}
		target.RestoreToCount(guard);
	}

	public void Dispose()
	{
		_commands = null;
		if (_ownedGeometries is { } owned)
		{
			_ownedGeometries = null;
			foreach (var geometry in owned)
			{
				geometry.Dispose();
			}
		}
	}
}

/// <summary>
/// Records the neutral drawing verbs into a replayable <see cref="CommandList"/>. Save-count and current
/// transform are tracked so the getters behave like a real session, and recordings nest (a sub-recording's
/// <see cref="CommandList.Replay"/> re-issues its verbs into this recorder). This is the backend-agnostic
/// retained fallback the framework uses when a backend has no native retention (and the test seam
/// <c>Visual.ForceFallbackRetainedRendering</c> forces even on a native backend).
/// </summary>
internal sealed class CommandListRecorder : ICommandRecorder
{
	private readonly List<Action<ReplayContext>> _commands = new();
	private readonly Stack<Matrix4x4> _stack = new();
	private Matrix4x4 _matrix = Matrix4x4.Identity;
	// A fresh session's save count is 1 (SkiaSharp/SKCanvas convention); Save() returns the count to restore to.
	private int _depth = 1;
	private List<IGeometry>? _ownedGeometries;

	// Replay happens after the caller has disposed the geometry (it assumes the draw copied it, as a native
	// display list would), so snapshot it into a copy this recording owns and frees on Dispose.
	private IGeometry Own(IGeometry geometry)
	{
		var snapshot = geometry.Transform(Matrix3x2.Identity);
		(_ownedGeometries ??= new()).Add(snapshot);
		return snapshot;
	}

	private void Push() => _stack.Push(_matrix);

	private void Pop()
	{
		if (_stack.Count > 0)
		{
			_matrix = _stack.Pop();
		}
	}

	public IRenderData Finish() => new CommandList(_commands, _ownedGeometries);

	public Matrix4x4 TotalMatrix => _matrix;

	public int SaveCount => _depth;

	public int Save()
	{
		_commands.Add(static ctx => ctx.Target.Save());
		Push();
		return _depth++;
	}

	public void Restore()
	{
		_commands.Add(static ctx => ctx.Target.Restore());
		if (_depth > 1)
		{
			_depth--;
			Pop();
		}
	}

	public void RestoreToCount(int count)
	{
		_commands.Add(ctx => ctx.Target.RestoreToCount(count + ctx.DepthOffset));
		while (_depth > count && _depth > 1)
		{
			_depth--;
			Pop();
		}
	}

	public void SetMatrix(in Matrix4x4 matrix)
	{
		var m = matrix;
		// Absolute set: fold in the destination's transform on replay so local-space recordings compose correctly.
		_commands.Add(ctx => ctx.Target.SetMatrix(m * ctx.Base));
		_matrix = m;
	}

	public void Concat(in Matrix4x4 matrix)
	{
		var m = matrix;
		_commands.Add(ctx => ctx.Target.Concat(m));
		_matrix = m * _matrix;
	}

	public void Translate(float dx, float dy)
	{
		_commands.Add(ctx => ctx.Target.Translate(dx, dy));
		_matrix = Matrix4x4.CreateTranslation(dx, dy, 0) * _matrix;
	}

	public void Scale(float sx, float sy)
	{
		_commands.Add(ctx => ctx.Target.Scale(sx, sy));
		_matrix = Matrix4x4.CreateScale(sx, sy, 1) * _matrix;
	}

	public void SaveLayer(bool antialias = false)
	{
		_commands.Add(ctx => ctx.Target.SaveLayer(antialias));
		Push();
		_depth++;
	}

	public void SaveLayer(IColorFilter colorFilter, bool antialias = false)
	{
		_commands.Add(ctx => ctx.Target.SaveLayer(colorFilter, antialias));
		Push();
		_depth++;
	}

	public void SaveLayer(BlendMode blendMode, bool antialias = false)
	{
		_commands.Add(ctx => ctx.Target.SaveLayer(blendMode, antialias));
		Push();
		_depth++;
	}

	public void SaveLayer(IEffectFilter filter)
	{
		_commands.Add(ctx => ctx.Target.SaveLayer(filter));
		Push();
		_depth++;
	}

	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.ClipRect(r, operation, antialias));
	}

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		var rr = roundRect;
		_commands.Add(ctx => ctx.Target.ClipRoundRect(rr, operation, antialias));
	}

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		var g = Own(geometry);
		_commands.Add(ctx => ctx.Target.ClipPath(g, operation, antialias));
	}

	public void Clear(Color color)
		=> _commands.Add(ctx => ctx.Target.Clear(color));

	public void DrawRect(in Rect rect, Color color, bool antialias = false)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.DrawRect(r, color, antialias));
	}

	public void DrawRect(in Rect rect, IShader shader, bool antialias = false)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.DrawRect(r, shader, antialias));
	}

	public void DrawRoundedRect(in Rect rect, Vector4 radii, Color color, bool antialias = false)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.DrawRoundedRect(r, radii, color, antialias));
	}

	public void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, Color color, bool antialias = false)
	{
		var o = outer;
		var i = inner;
		_commands.Add(ctx => ctx.Target.DrawRoundedRectBorder(o, outerRadii, i, innerRadii, color, antialias));
	}

	public void DrawPath(IGeometry geometry, Color color, bool antialias = false)
	{
		var g = Own(geometry);
		_commands.Add(ctx => ctx.Target.DrawPath(g, color, antialias));
	}

	public void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive, bool antialias = false)
	{
		var g = Own(silhouette);
		_commands.Add(ctx => ctx.Target.DrawShadow(g, color, sigmaX, sigmaY, additive, antialias));
	}

	public void StrokePath(IGeometry geometry, Color color, float strokeWidth, bool antialias = false)
	{
		var g = Own(geometry);
		_commands.Add(ctx => ctx.Target.StrokePath(g, color, strokeWidth, antialias));
	}

	public void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth, bool antialias = false)
		=> _commands.Add(ctx => ctx.Target.DrawLine(p0, p1, color, strokeWidth, antialias));

	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false)
		=> _commands.Add(ctx => ctx.Target.DrawImage(texture, x, y, sampling, opacity, antialias));

	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false)
		=> _commands.Add(ctx => ctx.Target.DrawImage(texture, x, y, sampling, colorFilter, antialias));

	public void DrawImageNineSlice(IImageTexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false)
	{
		var c = centerSlice;
		var d = destination;
		_commands.Add(ctx => ctx.Target.DrawImageNineSlice(texture, c, d, centerHollow, antialias));
	}

	public void DrawEffectBackdrop(IEffectFilter filter, float opacity)
		=> _commands.Add(ctx => ctx.Target.DrawEffectBackdrop(filter, opacity));
}
