#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The context threaded through every command on replay: the destination session, the destination's transform at
/// the moment replay began (folded into recorded absolute <see cref="IDrawingSession.SetMatrix"/> calls so a
/// recording composes into another coordinate space), and the save-depth offset mapping recording-relative save
/// counts onto the destination's stack.
/// </summary>
internal sealed class ReplayContext
{
	public required IDrawingSession Target;
	public Matrix4x4 Base;
	public int DepthOffset;
}

/// <summary>Opaque retained frame produced by <see cref="CommandListRecorder"/>: the recorded verb list.</summary>
internal sealed class CommandList : IRenderRecord
{
	private List<Action<ReplayContext>>? _commands;
	// References this recording holds on the resources its verbs captured. A closure captures a reference where a
	// native display list would have copied, so the recording must keep them alive until it is itself disposed —
	// the composition may drop its own reference as soon as the draw call returns.
	private List<IDrawingResource>? _retained;

	internal CommandList(List<Action<ReplayContext>> commands, List<IDrawingResource>? retained)
	{
		_commands = commands;
		_retained = retained;
	}

	public void Replay(IDrawingSession target)
	{
		if (_commands is not { } commands)
		{
			return;
		}

		// Guard the destination so a recording's own save/restore can never escape past where it began, and so the
		// transform/clip are restored afterwards. Recorded absolute SetMatrix values are relative to the destination
		// transform captured here; relative ops replay as-is.
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
		if (_retained is { } retained)
		{
			_retained = null;
			foreach (var resource in retained)
			{
				resource.Release();
			}
		}
	}
}

/// <summary>
/// Records the neutral drawing verbs into a replayable <see cref="CommandList"/>. Save-count and current transform
/// are tracked so the getters behave like a real session, and recordings nest. This is the backend-agnostic
/// retained fallback the framework uses when a backend has no native retention.
/// </summary>
internal sealed class CommandListRecorder : ICommandRecorder
{
	private readonly List<Action<ReplayContext>> _commands = new();
	private readonly Stack<Matrix4x4> _stack = new();
	private Matrix4x4 _matrix = Matrix4x4.Identity;
	// A fresh session's save count is 1 (SkiaSharp/SKCanvas convention); Save() returns the count to restore to.
	private int _depth = 1;
	private List<IDrawingResource>? _retained;

	// Replay happens after the caller has dropped its reference (the draw is assumed to have copied, as a native
	// display list would), so take one of our own for every resource a recorded closure captures.
	private T Retain<T>(T resource) where T : IDrawingResource
	{
		resource.AddRef();
		(_retained ??= new()).Add(resource);
		return resource;
	}

	private void Push() => _stack.Push(_matrix);

	private void Pop()
	{
		if (_stack.Count > 0)
		{
			_matrix = _stack.Pop();
		}
	}

	public IRenderRecord Finish() => new CommandList(_commands, _retained);

	public Matrix4x4 TotalMatrix => _matrix;

	public int SaveCount => _depth;
	public object? NativeSurface => null;

	// The backend-agnostic recorder has no device of its own; its recorded verbs replay into a real backend session,
	// so it exposes the ambient (single negotiated) factory — the same backend that recording will replay into.
	public IDrawingFactory Factory => DrawingFactory.Current;

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

	public void SaveLayer()
	{
		_commands.Add(ctx => ctx.Target.SaveLayer());
		Push();
		_depth++;
	}

	public void SaveLayer(IColorFilter colorFilter)
	{
		_commands.Add(ctx => ctx.Target.SaveLayer(colorFilter));
		Push();
		_depth++;
	}

	public void SaveLayerMask()
	{
		_commands.Add(ctx => ctx.Target.SaveLayerMask());
		Push();
		_depth++;
	}

	public void SaveLayer(IEffectFilter filter)
	{
		_commands.Add(ctx => ctx.Target.SaveLayer(filter));
		Push();
		_depth++;
	}

	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.ClipRect(r, operation));
	}

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect)
	{
		var rr = roundRect;
		_commands.Add(ctx => ctx.Target.ClipRoundRect(rr, operation));
	}

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect)
	{
		var g = Retain(geometry);
		_commands.Add(ctx => ctx.Target.ClipPath(g, operation));
	}

	public void Clear(Color color)
		=> _commands.Add(ctx => ctx.Target.Clear(color));

	public void DrawRect(in Rect rect, Color color)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.DrawRect(r, color));
	}

	public void DrawRect(in Rect rect, IShader shader)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.DrawRect(r, shader));
	}

	public void DrawRoundedRect(in Rect rect, Vector4 radii, Color color)
	{
		var r = rect;
		_commands.Add(ctx => ctx.Target.DrawRoundedRect(r, radii, color));
	}

	public void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, Color color)
	{
		var o = outer;
		var i = inner;
		_commands.Add(ctx => ctx.Target.DrawRoundedRectBorder(o, outerRadii, i, innerRadii, color));
	}

	public void DrawPath(IGeometry geometry, Color color)
	{
		var g = Retain(geometry);
		_commands.Add(ctx => ctx.Target.DrawPath(g, color));
	}

	public void DrawPaths(ReadOnlySpan<PathInstance> instances, Color color)
	{
		// The span cannot outlive this call, so copy the placements. The geometries are retained rather than copied,
		// so the instances still share one geometry — which is the whole point of the overload.
		var placed = instances.ToArray();
		foreach (var instance in placed)
		{
			Retain(instance.Geometry);
		}

		_commands.Add(ctx => ctx.Target.DrawPaths(placed, color));
	}

	public void DrawPath(IGeometry geometry, Color color, Vector2 offset)
	{
		var g = Retain(geometry);
		_commands.Add(ctx => ctx.Target.DrawPath(g, color, offset));
	}

	public void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive)
	{
		var g = Retain(silhouette);
		_commands.Add(ctx => ctx.Target.DrawShadow(g, color, sigmaX, sigmaY, additive));
	}

	public void StrokePath(IGeometry geometry, Color color, float strokeWidth)
	{
		var g = Retain(geometry);
		_commands.Add(ctx => ctx.Target.StrokePath(g, color, strokeWidth));
	}

	public void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth)
		=> _commands.Add(ctx => ctx.Target.DrawLine(p0, p1, color, strokeWidth));

	public void DrawImage(ITexture texture, float x, float y, float opacity = 1f)
		=> _commands.Add(ctx => ctx.Target.DrawImage(texture, x, y, opacity));

	public void DrawImage(ITexture texture, float x, float y, IColorFilter colorFilter)
		=> _commands.Add(ctx => ctx.Target.DrawImage(texture, x, y, colorFilter));

	public void DrawImageTiled(ITexture texture, in Rect destination, EdgeExtend extendX, EdgeExtend extendY, float opacity = 1f)
	{
		var d = destination;
		_commands.Add(ctx => ctx.Target.DrawImageTiled(texture, d, extendX, extendY, opacity));
	}

	public void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow)
	{
		var c = centerSlice;
		var d = destination;
		_commands.Add(ctx => ctx.Target.DrawImageNineSlice(texture, c, d, centerHollow));
	}

	public void DrawEffectBackdrop(IEffectFilter filter, float opacity)
		=> _commands.Add(ctx => ctx.Target.DrawEffectBackdrop(filter, opacity));
}
