#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

internal sealed partial class ManagedLottie
{
	// Ellipse-quadrant bézier control ratio.
	private const float Kappa = 0.5522847498f;

	public Vector2 Size => new(Width, Height);

	public TimeSpan Duration => FrameRate > 0
		? TimeSpan.FromSeconds(Math.Max(0, OutPoint - InPoint) / FrameRate)
		: TimeSpan.Zero;

	public void Render(IDrawingSession session, IGeometryFactory geometry, float progress, Rect area)
	{
		if (Width <= 0 || Height <= 0)
		{
			return;
		}

		var frame = InPoint + Math.Clamp(progress, 0f, 1f) * Math.Max(0, OutPoint - InPoint);

		session.Save();
		session.Translate((float)area.X, (float)area.Y);
		session.Scale((float)(area.Width / Width), (float)(area.Height / Height));

		// Lottie layer order is top-first; paint back-to-front so earlier layers land on top.
		for (var i = Layers.Count - 1; i >= 0; i--)
		{
			var layer = Layers[i];
			if (layer.Type != 4 || frame < layer.InPoint || frame >= layer.OutPoint || layer.Shapes.Count == 0)
			{
				continue; // v1 draws shape layers only; null layers still contribute via parenting (WorldMatrix)
			}

			session.Save();
			session.Concat(new Matrix4x4(WorldMatrix(layer, frame, 0)));
			RenderShapes(session, geometry, layer.Shapes, frame, layer.Transform.Opacity.Evaluate(frame) / 100f);
			session.Restore();
		}

		session.Restore();
	}

	private Matrix3x2 WorldMatrix(Layer layer, float frame, int depth)
	{
		var m = layer.Transform.Matrix(frame);
		if (depth < 32 && layer.ParentIndex is { } pi && LayersByIndex.TryGetValue(pi, out var parent) && parent != layer)
		{
			m *= WorldMatrix(parent, frame, depth + 1);
		}
		return m;
	}

	private static void RenderShapes(IDrawingSession session, IGeometryFactory geometry, IReadOnlyList<ShapeItem> items, float frame, float opacity)
	{
		var localOpacity = opacity;
		TransformShape? tr = null;
		foreach (var it in items)
		{
			if (it is TransformShape t)
			{
				tr = t;
				break;
			}
		}

		session.Save();
		if (tr is not null)
		{
			session.Concat(new Matrix4x4(tr.Transform.Matrix(frame)));
			localOpacity *= tr.Transform.Opacity.Evaluate(frame) / 100f;
		}

		// One geometry for all direct path/rect/ellipse items in this group (fills/strokes paint that union).
		IGeometry? combined = BuildGeometry(geometry, items, frame);

		// Trim paths (tm): keep the [start,end] arc-length fraction of the group's paths (offset-rotated).
		foreach (var it in items)
		{
			if (it is TrimShape trim && combined is not null)
			{
				var trimmed = ApplyTrim(combined, trim, frame);
				if (!ReferenceEquals(trimmed, combined))
				{
					combined.Dispose();
					combined = trimmed;
				}
				break;
			}
		}

		if (combined is not null)
		{
			foreach (var it in items)
			{
				if (it is FillShape fill)
				{
					session.DrawPath(combined, WithOpacity(fill.Color.Evaluate(frame), localOpacity * fill.Opacity.Evaluate(frame) / 100f), antialias: true);
				}
			}
			foreach (var it in items)
			{
				if (it is StrokeShape stroke)
				{
					var w = stroke.Width.Evaluate(frame);
					if (w > 0)
					{
						session.StrokePath(combined, WithOpacity(stroke.Color.Evaluate(frame), localOpacity * stroke.Opacity.Evaluate(frame) / 100f), w, antialias: true);
					}
				}
			}
			combined.Dispose();
		}

		// Nested groups carry their own transform/paints.
		foreach (var it in items)
		{
			if (it is GroupShape group)
			{
				RenderShapes(session, geometry, group.Items, frame, localOpacity);
			}
		}

		session.Restore();
	}

	// Trim [Start,End]% of the concatenated path length, rotated by Offset (deg, 360 = full). Returns the original
	// when the trim is a no-op (full path). Wrap (range crossing the seam) is a best-effort union of the two arcs.
	private static IGeometry? ApplyTrim(IGeometry geom, TrimShape trim, float frame)
	{
		var offset = trim.Offset.Evaluate(frame) / 360f;
		var a = trim.Start.Evaluate(frame) / 100f + offset;
		var b = trim.End.Evaluate(frame) / 100f + offset;
		if (b < a)
		{
			(a, b) = (b, a);
		}
		var len = b - a;
		if (len <= 1e-4f)
		{
			return geom.GetFilledGeometry(0f, 0f) is { } empty ? empty : geom;   // nothing visible
		}
		if (len >= 1f)
		{
			return geom;   // whole path
		}
		// Rotate the seam so a ∈ [0,1).
		var shift = MathF.Floor(a);
		a -= shift;
		b -= shift;
		if (b <= 1f)
		{
			return geom.GetFilledGeometry(a, b);
		}
		// Wrap across the seam: [a,1] ∪ [0,b-1].
		var g1 = geom.GetFilledGeometry(a, 1f);
		var g2 = geom.GetFilledGeometry(0f, b - 1f);
		var union = g1.Combine(g2, GeometryCombineMode.Union);
		g1.Dispose();
		g2.Dispose();
		return union;
	}

	private static IGeometry? BuildGeometry(IGeometryFactory geometry, IReadOnlyList<ShapeItem> items, float frame)
	{
		IPathBuilder? builder = null;
		foreach (var it in items)
		{
			switch (it)
			{
				case PathShape p:
					AppendPath(builder ??= geometry.CreatePathBuilder(), p.Path.Evaluate(frame));
					break;
				case RectShape r:
					AppendRect(builder ??= geometry.CreatePathBuilder(), r.Position.Evaluate(frame), r.Size.Evaluate(frame), r.Roundness.Evaluate(frame));
					break;
				case EllipseShape e:
					AppendEllipse(builder ??= geometry.CreatePathBuilder(), e.Position.Evaluate(frame), e.Size.Evaluate(frame));
					break;
			}
		}
		return builder?.Build();
	}

	private static void AppendPath(IPathBuilder builder, ShapeData shape)
	{
		if (shape.IsEmpty)
		{
			return;
		}
		var v = shape.Vertices;
		var inT = shape.InTangents;
		var outT = shape.OutTangents;
		var n = v.Length;
		builder.MoveTo(v[0]);
		for (var i = 0; i < n - 1; i++)
		{
			builder.CubicTo(v[i] + outT[i], v[i + 1] + inT[i + 1], v[i + 1]);
		}
		if (shape.Closed && n > 1)
		{
			builder.CubicTo(v[n - 1] + outT[n - 1], v[0] + inT[0], v[0]);
			builder.Close();
		}
	}

	private static void AppendRect(IPathBuilder builder, Vector2 center, Vector2 size, float roundness)
	{
		var hw = size.X / 2f;
		var hh = size.Y / 2f;
		var left = center.X - hw;
		var top = center.Y - hh;
		var right = center.X + hw;
		var bottom = center.Y + hh;
		var r = Math.Clamp(roundness, 0f, Math.Min(hw, hh));

		if (r <= 0f)
		{
			builder.MoveTo(new Vector2(left, top));
			builder.LineTo(new Vector2(right, top));
			builder.LineTo(new Vector2(right, bottom));
			builder.LineTo(new Vector2(left, bottom));
			builder.Close();
			return;
		}

		var k = r * Kappa;
		builder.MoveTo(new Vector2(left + r, top));
		builder.LineTo(new Vector2(right - r, top));
		builder.CubicTo(new Vector2(right - r + k, top), new Vector2(right, top + r - k), new Vector2(right, top + r));
		builder.LineTo(new Vector2(right, bottom - r));
		builder.CubicTo(new Vector2(right, bottom - r + k), new Vector2(right - r + k, bottom), new Vector2(right - r, bottom));
		builder.LineTo(new Vector2(left + r, bottom));
		builder.CubicTo(new Vector2(left + r - k, bottom), new Vector2(left, bottom - r + k), new Vector2(left, bottom - r));
		builder.LineTo(new Vector2(left, top + r));
		builder.CubicTo(new Vector2(left, top + r - k), new Vector2(left + r - k, top), new Vector2(left + r, top));
		builder.Close();
	}

	private static void AppendEllipse(IPathBuilder builder, Vector2 center, Vector2 size)
	{
		var rx = size.X / 2f;
		var ry = size.Y / 2f;
		var kx = rx * Kappa;
		var ky = ry * Kappa;
		var cx = center.X;
		var cy = center.Y;

		builder.MoveTo(new Vector2(cx, cy - ry));
		builder.CubicTo(new Vector2(cx + kx, cy - ry), new Vector2(cx + rx, cy - ky), new Vector2(cx + rx, cy));
		builder.CubicTo(new Vector2(cx + rx, cy + ky), new Vector2(cx + kx, cy + ry), new Vector2(cx, cy + ry));
		builder.CubicTo(new Vector2(cx - kx, cy + ry), new Vector2(cx - rx, cy + ky), new Vector2(cx - rx, cy));
		builder.CubicTo(new Vector2(cx - rx, cy - ky), new Vector2(cx - kx, cy - ry), new Vector2(cx, cy - ry));
		builder.Close();
	}

	private static Color WithOpacity(Color color, float opacity)
		=> Color.FromArgb((byte)Math.Clamp((float)Math.Round(color.A * Math.Clamp(opacity, 0f, 1f)), 0, 255), color.R, color.G, color.B);
}
