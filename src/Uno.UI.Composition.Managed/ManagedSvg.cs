#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A SkiaSharp-free SVG engine (the default <see cref="ISvgRenderer"/>/<see cref="ISvgDocument"/>): parses SVG
/// markup and replays it through the neutral drawing abstraction (<see cref="IPathBuilder"/> geometry,
/// <see cref="IDrawingSession"/> verbs, gradient shaders). It only issues neutral draw verbs into the session the
/// caller supplies (an offscreen to rasterize, or a live session to draw directly); the caller owns the backend, so
/// no Skia and no backend resource is created here.
/// </summary>
/// <remarks>
/// Covers the common icon/illustration subset: svg/g/path/rect/circle/ellipse/line/polyline/polygon/use, fills and
/// strokes (solid + linear/radial gradients), opacity, fill-rule, transforms, viewBox, and inline style. Not yet:
/// text, clipPath/mask/filter/pattern, embedded images, external/CSS-class styling.
/// </remarks>
internal sealed class ManagedSvg : ISvgDocument
{
	private readonly XElement _root;
	private readonly Dictionary<string, XElement> _byId = new();
	// The registered factories, injected at parse — the renderer never reaches a global holder (no IVT into Drawing).
	private readonly IGeometryFactory _geometry;
	private readonly IDrawingFactory _drawing;

	private ManagedSvg(XElement root, IGeometryFactory geometry, IDrawingFactory drawing)
	{
		_root = root;
		_geometry = geometry;
		_drawing = drawing;
		foreach (var el in root.DescendantsAndSelf())
		{
			var id = (string?)el.Attribute("id");
			if (id is not null)
			{
				_byId[id] = el;
			}
		}

		SourceSize = ComputeSourceSize(root, out _viewBox);
	}

	private readonly Rect _viewBox;

	public Size SourceSize { get; }

	// The managed engine renders straight into the session each frame and retains no backend resource; nothing to release.
	public void Dispose() { }

	public static bool TryParse(byte[] svg, IGeometryFactory geometry, IDrawingFactory drawing, out ManagedSvg document)
	{
		document = null!;
		try
		{
			var text = StripBom(svg);
			var xdoc = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
			if (xdoc.Root is null || !string.Equals(xdoc.Root.Name.LocalName, "svg", StringComparison.Ordinal))
			{
				return false;
			}

			document = new ManagedSvg(xdoc.Root, geometry, drawing);
			return document.SourceSize is { Width: > 0, Height: > 0 };
		}
		catch
		{
			return false;
		}
	}

	// Replays the retained SVG into the caller's session. The caller supplies the session (an offscreen to rasterize,
	// or a live session to draw directly) and thus owns any backend resource — the engine only issues neutral draw
	// verbs. Bracketed in Save/Restore so the viewBox transform never leaks into a shared session.
	public void Render(IDrawingSession session, Size targetSize)
	{
		// Map the viewBox into the target (uniform scale, centered — xMidYMid meet).
		var sx = (float)targetSize.Width / (float)_viewBox.Width;
		var sy = (float)targetSize.Height / (float)_viewBox.Height;
		var scale = Math.Min(sx, sy);
		var tx = ((float)targetSize.Width - (float)_viewBox.Width * scale) / 2f - (float)_viewBox.X * scale;
		var ty = ((float)targetSize.Height - (float)_viewBox.Height * scale) / 2f - (float)_viewBox.Y * scale;

		var count = session.Save();
		session.Concat(ToMatrix4x4(new Matrix3x2(scale, 0, 0, scale, tx, ty)));
		// The root <svg>'s own presentation attributes (color, fill, …) are inherited by its descendants.
		RenderChildren(session, _root, SvgStyle.Root.InheritFrom(_root));
		session.RestoreToCount(count);
	}

	private void RenderChildren(IDrawingSession session, XElement parent, SvgStyle inherited)
	{
		foreach (var el in parent.Elements())
		{
			RenderElement(session, el, inherited);
		}
	}

	private void RenderElement(IDrawingSession session, XElement el, SvgStyle inherited)
	{
		var name = el.Name.LocalName;
		if (name is "defs" or "linearGradient" or "radialGradient" or "symbol" or "clipPath" or "mask" or "title" or "desc" or "metadata" or "style")
		{
			return; // definitions / unsupported containers
		}

		var style = inherited.InheritFrom(el);

		var transform = ParseTransform((string?)el.Attribute("transform"));
		var saved = session.SaveCount;
		if (transform is { } t)
		{
			session.Save();
			session.Concat(ToMatrix4x4(t));
		}

		switch (name)
		{
			case "g":
			case "a":
			case "svg":
				RenderChildren(session, el, style);
				break;
			case "use":
				RenderUse(session, el, style);
				break;
			case "path":
				DrawGeometry(session, BuildPath((string?)el.Attribute("d"), style.FillRule), style, el);
				break;
			case "rect":
				DrawGeometry(session, BuildRect(el, style.FillRule), style, el);
				break;
			case "circle":
				DrawGeometry(session, BuildEllipse(Len(el, "cx"), Len(el, "cy"), Len(el, "r"), Len(el, "r"), style.FillRule), style, el);
				break;
			case "ellipse":
				DrawGeometry(session, BuildEllipse(Len(el, "cx"), Len(el, "cy"), Len(el, "rx"), Len(el, "ry"), style.FillRule), style, el);
				break;
			case "line":
				DrawGeometry(session, BuildPolyline(new[] { Len(el, "x1"), Len(el, "y1"), Len(el, "x2"), Len(el, "y2") }, false, style.FillRule), style, el);
				break;
			case "polyline":
				DrawGeometry(session, BuildPolyline(ParseNumbers((string?)el.Attribute("points")), false, style.FillRule), style, el);
				break;
			case "polygon":
				DrawGeometry(session, BuildPolyline(ParseNumbers((string?)el.Attribute("points")), true, style.FillRule), style, el);
				break;
		}

		if (transform is not null)
		{
			session.RestoreToCount(saved);
		}
	}

	private void RenderUse(IDrawingSession session, XElement el, SvgStyle style)
	{
		var href = (string?)el.Attribute("href") ?? (string?)el.Attribute(XName.Get("href", "http://www.w3.org/1999/xlink"));
		if (href is null || !href.StartsWith('#') || !_byId.TryGetValue(href[1..], out var target))
		{
			return;
		}

		var x = Len(el, "x");
		var y = Len(el, "y");
		var saved = session.SaveCount;
		if (x != 0 || y != 0)
		{
			session.Save();
			session.Translate(x, y);
		}

		RenderElement(session, target, style);
		if (x != 0 || y != 0)
		{
			session.RestoreToCount(saved);
		}
	}

	private void DrawGeometry(IDrawingSession session, IGeometry? geometry, SvgStyle style, XElement el)
	{
		if (geometry is null)
		{
			return;
		}

		using (geometry)
		{
			// Fill: clip to the shape and fill its own bounds when a gradient is referenced, else solid.
			if (!style.FillNone)
			{
				FillRegion(session, geometry, geometry, style.FillRef, style.ResolvedFill, style.FillAlpha);
			}

			// Stroke.
			if (!style.StrokeNone && style.StrokeWidth > 0)
			{
				StrokeGeometry(session, geometry, style);
			}
		}
	}

	// Fills <paramref name="clip"/> with a gradient (mapped over <paramref name="gradientBounds"/>) when a reference
	// resolves, else with a solid color. Gradient fills clip to the region and paint its bounds with the shader.
	private void FillRegion(IDrawingSession session, IGeometry clip, IGeometry gradientBounds, string? gradientRef, Color solid, float alpha)
	{
		if (gradientRef is { } reference && ResolveGradient(reference, gradientBounds, alpha) is { } shader)
		{
			var save = session.SaveCount;
			session.Save();
			session.ClipPath(clip, ClipOperation.Intersect, antialias: true);
			session.DrawRect(clip.Bounds, shader, antialias: true);
			session.RestoreToCount(save);
		}
		else
		{
			session.DrawPath(clip, solid, antialias: true);
		}
	}

	// Strokes <paramref name="geometry"/>. The plain default stroke (butt cap / miter join, solid color, no dash) uses
	// the fast StrokePath verb; any of a non-default cap/join, a dash pattern, or a gradient paint needs the WinUI
	// stroke-fill region so the neutral fill verbs can carry it (StrokePath carries only color + width).
	private void StrokeGeometry(IDrawingSession session, IGeometry geometry, SvgStyle style)
	{
		var needsStrokeFill = style.StrokeRef is not null
			|| style.LineCap != StrokeCap.Butt
			|| style.LineJoin != StrokeJoin.Miter
			|| style.DashArray is { Length: > 0 };

		if (!needsStrokeFill)
		{
			session.StrokePath(geometry, style.ResolvedStroke, style.StrokeWidth, antialias: true);
			return;
		}

		var strokeStyle = new StrokeStyle
		{
			Thickness = style.StrokeWidth,
			StartCap = style.LineCap,
			EndCap = style.LineCap,
			DashCap = style.LineCap,
			LineJoin = style.LineJoin,
			MiterLimit = style.MiterLimit,
			// StrokeStyle dash intervals are multiples of Thickness; SVG authors them in user units.
			DashArray = ScaleToThickness(style.DashArray, style.StrokeWidth),
			DashOffset = style.StrokeWidth > 0 ? style.DashOffset / style.StrokeWidth : 0f,
		};

		using var strokeFill = geometry.GetStrokeFillGeometry(strokeStyle);
		FillRegion(session, strokeFill, geometry, style.StrokeRef, style.ResolvedStroke, style.StrokeAlpha);
	}

	private static float[]? ScaleToThickness(float[]? dashes, float thickness)
	{
		if (dashes is not { Length: > 0 } || thickness <= 0)
		{
			return null;
		}

		var scaled = new float[dashes.Length];
		for (var i = 0; i < dashes.Length; i++)
		{
			scaled[i] = dashes[i] / thickness;
		}

		return scaled;
	}

	private IShader? ResolveGradient(string reference, IGeometry geometry, float alpha)
	{
		if (!reference.StartsWith('#') || !_byId.TryGetValue(reference[1..], out var grad))
		{
			return null;
		}

		var (colors, positions) = ReadStops(grad, alpha);
		if (colors.Length == 0)
		{
			return null;
		}

		var bounds = geometry.Bounds;
		var objectBoundingBox = ((string?)grad.Attribute("gradientUnits") ?? "objectBoundingBox") != "userSpaceOnUse";
		var localMatrix = ParseTransform((string?)grad.Attribute("gradientTransform")) ?? Matrix3x2.Identity;
		var tileMode = ((string?)grad.Attribute("spreadMethod")) switch
		{
			"reflect" => GradientTileMode.Mirror,
			"repeat" => GradientTileMode.Repeat,
			_ => GradientTileMode.Clamp,
		};

		float MapX(float v) => objectBoundingBox ? (float)(bounds.X + v * bounds.Width) : v;
		float MapY(float v) => objectBoundingBox ? (float)(bounds.Y + v * bounds.Height) : v;
		float Frac(string name, float def) => ParseCoordinate((string?)grad.Attribute(name), def, objectBoundingBox);

		if (grad.Name.LocalName == "radialGradient")
		{
			var cx = MapX(Frac("cx", 0.5f));
			var cy = MapY(Frac("cy", 0.5f));
			var r = Frac("r", 0.5f) * (objectBoundingBox ? (float)Math.Max(bounds.Width, bounds.Height) : 1f);
			return _drawing.CreateRadialGradientShader(
				new Vector2(cx, cy), new Vector2(cx, cy), r, r, colors, positions, tileMode, localMatrix);
		}

		var x1 = MapX(Frac("x1", 0f));
		var y1 = MapY(Frac("y1", 0f));
		var x2 = MapX(Frac("x2", 1f));
		var y2 = MapY(Frac("y2", 0f));
		return _drawing.CreateLinearGradientShader(
			new Vector2(x1, y1), new Vector2(x2, y2), colors, positions, tileMode, localMatrix);
	}

	private (Color[] colors, float[] positions) ReadStops(XElement grad, float alpha)
	{
		// Follow xlink:href to inherit stops if this gradient has none.
		var stopsOwner = grad;
		if (!HasStops(grad))
		{
			var href = (string?)grad.Attribute("href") ?? (string?)grad.Attribute(XName.Get("href", "http://www.w3.org/1999/xlink"));
			if (href is not null && href.StartsWith('#') && _byId.TryGetValue(href[1..], out var inherited))
			{
				stopsOwner = inherited;
			}
		}

		var colors = new List<Color>();
		var positions = new List<float>();
		foreach (var stop in stopsOwner.Elements())
		{
			if (stop.Name.LocalName != "stop")
			{
				continue;
			}

			var offset = ParseCoordinate((string?)stop.Attribute("offset"), 0f, true);
			var style = ParseStyle((string?)stop.Attribute("style"));
			var colorText = style.GetValueOrDefault("stop-color") ?? (string?)stop.Attribute("stop-color") ?? "black";
			var stopOpacityText = style.GetValueOrDefault("stop-opacity") ?? (string?)stop.Attribute("stop-opacity");
			var stopOpacity = stopOpacityText is null ? 1f : ParseFloat(stopOpacityText, 1f);
			var c = ParseColor(colorText, alpha * stopOpacity) ?? Color.FromArgb((byte)(alpha * stopOpacity * 255), 0, 0, 0);
			colors.Add(c);
			positions.Add(Math.Clamp(offset, 0f, 1f));
		}

		return (colors.ToArray(), positions.ToArray());
	}

	private static bool HasStops(XElement grad)
	{
		foreach (var e in grad.Elements())
		{
			if (e.Name.LocalName == "stop")
			{
				return true;
			}
		}

		return false;
	}

	// ---- geometry builders ----

	private IGeometry? BuildRect(XElement el, GeometryFillRule fillRule)
	{
		var w = Len(el, "width");
		var h = Len(el, "height");
		if (w <= 0 || h <= 0)
		{
			return null;
		}

		var x = Len(el, "x");
		var y = Len(el, "y");

		// SVG rx/ry auto rules: a missing radius inherits the other; each clamps to half its side; negatives → sharp.
		var hasRx = el.Attribute("rx") is not null;
		var hasRy = el.Attribute("ry") is not null;
		var rx = hasRx ? Len(el, "rx") : 0f;
		var ry = hasRy ? Len(el, "ry") : 0f;
		if (hasRx && !hasRy)
		{
			ry = rx;
		}
		else if (hasRy && !hasRx)
		{
			rx = ry;
		}

		rx = Math.Clamp(rx, 0f, w / 2f);
		ry = Math.Clamp(ry, 0f, h / 2f);

		if (rx > 0 && ry > 0)
		{
			var rounded = _geometry.CreatePrimitiveGeometryBuilder();
			rounded.FillRule = fillRule;
			rounded.AddRoundedRectangle(new Rect(x, y, w, h), rx, ry);
			return rounded.Build();
		}

		var b = _geometry.CreatePathBuilder();
		b.FillRule = fillRule;
		b.MoveTo(new Vector2(x, y));
		b.LineTo(new Vector2(x + w, y));
		b.LineTo(new Vector2(x + w, y + h));
		b.LineTo(new Vector2(x, y + h));
		b.Close();
		return b.Build();
	}

	private IGeometry? BuildEllipse(float cx, float cy, float rx, float ry, GeometryFillRule fillRule)
	{
		if (rx <= 0 || ry <= 0)
		{
			return null;
		}

		// Four cubic bezier quadrants (kappa).
		const float k = 0.5522847498f;
		var b = _geometry.CreatePathBuilder();
		b.FillRule = fillRule;
		b.MoveTo(new Vector2(cx + rx, cy));
		b.CubicTo(new Vector2(cx + rx, cy + ry * k), new Vector2(cx + rx * k, cy + ry), new Vector2(cx, cy + ry));
		b.CubicTo(new Vector2(cx - rx * k, cy + ry), new Vector2(cx - rx, cy + ry * k), new Vector2(cx - rx, cy));
		b.CubicTo(new Vector2(cx - rx, cy - ry * k), new Vector2(cx - rx * k, cy - ry), new Vector2(cx, cy - ry));
		b.CubicTo(new Vector2(cx + rx * k, cy - ry), new Vector2(cx + rx, cy - ry * k), new Vector2(cx + rx, cy));
		b.Close();
		return b.Build();
	}

	private IGeometry? BuildPolyline(float[] pts, bool close, GeometryFillRule fillRule)
	{
		if (pts.Length < 4)
		{
			return null;
		}

		var b = _geometry.CreatePathBuilder();
		b.FillRule = fillRule;
		b.MoveTo(new Vector2(pts[0], pts[1]));
		for (var i = 2; i + 1 < pts.Length; i += 2)
		{
			b.LineTo(new Vector2(pts[i], pts[i + 1]));
		}

		if (close)
		{
			b.Close();
		}

		return b.Build();
	}

	private IGeometry? BuildPath(string? d, GeometryFillRule fillRule)
	{
		if (string.IsNullOrWhiteSpace(d))
		{
			return null;
		}

		var b = _geometry.CreatePathBuilder();
		b.FillRule = fillRule;
		new SvgPathParser(d, b).Parse();
		return b.Build();
	}

	// ---- attribute helpers ----

	private static float Len(XElement el, string name) => ParseFloat((string?)el.Attribute(name), 0f);

	private static Size ComputeSourceSize(XElement root, out Rect viewBox)
	{
		var vb = (string?)root.Attribute("viewBox");
		if (vb is not null)
		{
			var n = ParseNumbers(vb);
			if (n.Length == 4 && n[2] > 0 && n[3] > 0)
			{
				viewBox = new Rect(n[0], n[1], n[2], n[3]);
				var w = ParseFloat((string?)root.Attribute("width"), n[2]);
				var h = ParseFloat((string?)root.Attribute("height"), n[3]);
				return new Size(w > 0 ? w : n[2], h > 0 ? h : n[3]);
			}
		}

		var width = ParseFloat((string?)root.Attribute("width"), 0f);
		var height = ParseFloat((string?)root.Attribute("height"), 0f);
		viewBox = new Rect(0, 0, width, height);
		return new Size(width, height);
	}

	private static Matrix4x4 ToMatrix4x4(Matrix3x2 m) =>
		new(m.M11, m.M12, 0, 0, m.M21, m.M22, 0, 0, 0, 0, 1, 0, m.M31, m.M32, 0, 1);

	private static string StripBom(byte[] bytes)
	{
		var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
		return System.Text.Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
	}

	internal static float ParseFloat(string? s, float fallback)
	{
		if (string.IsNullOrEmpty(s))
		{
			return fallback;
		}

		var span = s.AsSpan().Trim();
		// Drop a trailing unit (px, pt, %, etc.) — treated as user units in v1.
		var end = 0;
		while (end < span.Length && (char.IsDigit(span[end]) || span[end] is '.' or '-' or '+' or 'e' or 'E'))
		{
			end++;
		}

		return float.TryParse(span[..end], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
	}

	private static float ParseCoordinate(string? s, float def, bool fraction)
	{
		if (string.IsNullOrEmpty(s))
		{
			return def;
		}

		var t = s.Trim();
		if (t.EndsWith('%'))
		{
			return ParseFloat(t[..^1], def * 100f) / 100f;
		}

		return ParseFloat(t, def);
	}

	internal static float[] ParseNumbers(string? s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return Array.Empty<float>();
		}

		var list = new List<float>();
		var i = 0;
		while (i < s.Length)
		{
			while (i < s.Length && (s[i] is ' ' or ',' or '\t' or '\n' or '\r'))
			{
				i++;
			}

			var start = i;
			if (i < s.Length && (s[i] is '-' or '+'))
			{
				i++;
			}

			while (i < s.Length && (char.IsDigit(s[i]) || s[i] is '.' or 'e' or 'E' || ((s[i] is '-' or '+') && i > start && (s[i - 1] is 'e' or 'E'))))
			{
				i++;
			}

			if (i > start && float.TryParse(s.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
			{
				list.Add(v);
			}
			else if (i == start)
			{
				i++; // avoid infinite loop on stray chars
			}
		}

		return list.ToArray();
	}

	private static Matrix3x2? ParseTransform(string? s)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return null;
		}

		var result = Matrix3x2.Identity;
		var i = 0;
		var any = false;
		while (i < s.Length)
		{
			var open = s.IndexOf('(', i);
			if (open < 0)
			{
				break;
			}

			var op = s[i..open].Trim();
			var close = s.IndexOf(')', open);
			if (close < 0)
			{
				break;
			}

			var args = ParseNumbers(s[(open + 1)..close]);
			i = close + 1;

			var m = op switch
			{
				"translate" => Matrix3x2.CreateTranslation(args.Length > 0 ? args[0] : 0, args.Length > 1 ? args[1] : 0),
				"scale" => Matrix3x2.CreateScale(args.Length > 0 ? args[0] : 1, args.Length > 1 ? args[1] : (args.Length > 0 ? args[0] : 1)),
				"rotate" when args.Length >= 3 => Matrix3x2.CreateRotation(Deg(args[0]), new Vector2(args[1], args[2])),
				"rotate" => Matrix3x2.CreateRotation(args.Length > 0 ? Deg(args[0]) : 0),
				"matrix" when args.Length == 6 => new Matrix3x2(args[0], args[1], args[2], args[3], args[4], args[5]),
				"skewX" => new Matrix3x2(1, 0, MathF.Tan(Deg(args.Length > 0 ? args[0] : 0)), 1, 0, 0),
				"skewY" => new Matrix3x2(1, MathF.Tan(Deg(args.Length > 0 ? args[0] : 0)), 0, 1, 0, 0),
				_ => Matrix3x2.Identity,
			};

			result = m * result;
			any = true;
		}

		return any ? result : null;
	}

	private static float Deg(float degrees) => degrees * MathF.PI / 180f;

	internal static Dictionary<string, string> ParseStyle(string? style)
	{
		var dict = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(style))
		{
			return dict;
		}

		foreach (var decl in style.Split(';'))
		{
			var colon = decl.IndexOf(':');
			if (colon > 0)
			{
				dict[decl[..colon].Trim()] = decl[(colon + 1)..].Trim();
			}
		}

		return dict;
	}

	internal static Color? ParseColor(string? text, float alpha)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		text = text.Trim();
		if (text is "none" or "transparent")
		{
			return Color.FromArgb(0, 0, 0, 0);
		}

		var a = (byte)Math.Clamp(alpha * 255f, 0, 255);

		if (text.StartsWith('#'))
		{
			var hex = text[1..];
			if (hex.Length == 3)
			{
				var r = Convert.ToInt32($"{hex[0]}{hex[0]}", 16);
				var g = Convert.ToInt32($"{hex[1]}{hex[1]}", 16);
				var b = Convert.ToInt32($"{hex[2]}{hex[2]}", 16);
				return Color.FromArgb(a, (byte)r, (byte)g, (byte)b);
			}

			if (hex.Length is 6 or 8)
			{
				var r = Convert.ToInt32(hex[..2], 16);
				var g = Convert.ToInt32(hex.Substring(2, 2), 16);
				var b = Convert.ToInt32(hex.Substring(4, 2), 16);
				var alphaByte = hex.Length == 8 ? (byte)((Convert.ToInt32(hex.Substring(6, 2), 16) * alpha)) : a;
				return Color.FromArgb(alphaByte, (byte)r, (byte)g, (byte)b);
			}
		}

		if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase) || text.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
		{
			return ParseFunctionalColor(text, alpha);
		}

		if (_namedColors.TryGetValue(text.ToLowerInvariant(), out var named))
		{
			return Color.FromArgb(a, (byte)(named >> 16), (byte)(named >> 8), (byte)named);
		}

		return null;
	}

	// Parses CSS functional colors: rgb()/rgba() (integer or percentage channels) and hsl()/hsla().
	private static Color? ParseFunctionalColor(string text, float alpha)
	{
		var isHsl = text.StartsWith("hsl", StringComparison.OrdinalIgnoreCase);
		var open = text.IndexOf('(');
		var close = text.IndexOf(')');
		if (open < 0 || close <= open)
		{
			return null;
		}

		var components = TokenizeComponents(text[(open + 1)..close]);
		if (components.Count < 3)
		{
			return null;
		}

		var alphaComponent = 1f;
		if (components.Count >= 4)
		{
			var (value, percent) = components[3];
			alphaComponent = percent ? value / 100f : value;
		}

		var a = (byte)Math.Clamp(alphaComponent * alpha * 255f, 0, 255);

		if (isHsl)
		{
			var h = components[0].value; // degrees
			var s = Math.Clamp(components[1].value / 100f, 0f, 1f);
			var l = Math.Clamp(components[2].value / 100f, 0f, 1f);
			var (r, g, b) = HslToRgb(h, s, l);
			return Color.FromArgb(a, r, g, b);
		}

		static byte Channel((float value, bool percent) c) =>
			(byte)Math.Clamp(c.percent ? c.value / 100f * 255f : c.value, 0f, 255f);

		return Color.FromArgb(a, Channel(components[0]), Channel(components[1]), Channel(components[2]));
	}

	// Splits an rgb()/hsl() argument list on commas, whitespace and the modern '/' alpha separator, recording
	// whether each component was written as a percentage.
	private static List<(float value, bool percent)> TokenizeComponents(string inner)
	{
		var result = new List<(float, bool)>();
		foreach (var raw in inner.Split(new[] { ',', ' ', '\t', '\n', '\r', '/' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var token = raw.Trim();
			var percent = token.EndsWith('%');
			if (percent)
			{
				token = token[..^1];
			}

			if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
			{
				result.Add((value, percent));
			}
		}

		return result;
	}

	private static (byte r, byte g, byte b) HslToRgb(float hueDegrees, float s, float l)
	{
		var h = (hueDegrees % 360f + 360f) % 360f / 360f;
		float r, g, b;
		if (s == 0f)
		{
			r = g = b = l;
		}
		else
		{
			var q = l < 0.5f ? l * (1f + s) : l + s - l * s;
			var p = 2f * l - q;
			r = HueToChannel(p, q, h + 1f / 3f);
			g = HueToChannel(p, q, h);
			b = HueToChannel(p, q, h - 1f / 3f);
		}

		return ((byte)Math.Clamp(r * 255f, 0f, 255f), (byte)Math.Clamp(g * 255f, 0f, 255f), (byte)Math.Clamp(b * 255f, 0f, 255f));
	}

	private static float HueToChannel(float p, float q, float t)
	{
		if (t < 0f)
		{
			t += 1f;
		}
		else if (t > 1f)
		{
			t -= 1f;
		}

		if (t < 1f / 6f)
		{
			return p + (q - p) * 6f * t;
		}

		if (t < 1f / 2f)
		{
			return q;
		}

		if (t < 2f / 3f)
		{
			return p + (q - p) * (2f / 3f - t) * 6f;
		}

		return p;
	}

	private static readonly Dictionary<string, int> _namedColors = new(StringComparer.Ordinal)
	{
		["aliceblue"] = 0xF0F8FF, ["antiquewhite"] = 0xFAEBD7, ["aqua"] = 0x00FFFF, ["aquamarine"] = 0x7FFFD4,
		["azure"] = 0xF0FFFF, ["beige"] = 0xF5F5DC, ["bisque"] = 0xFFE4C4, ["black"] = 0x000000,
		["blanchedalmond"] = 0xFFEBCD, ["blue"] = 0x0000FF, ["blueviolet"] = 0x8A2BE2, ["brown"] = 0xA52A2A,
		["burlywood"] = 0xDEB887, ["cadetblue"] = 0x5F9EA0, ["chartreuse"] = 0x7FFF00, ["chocolate"] = 0xD2691E,
		["coral"] = 0xFF7F50, ["cornflowerblue"] = 0x6495ED, ["cornsilk"] = 0xFFF8DC, ["crimson"] = 0xDC143C,
		["cyan"] = 0x00FFFF, ["darkblue"] = 0x00008B, ["darkcyan"] = 0x008B8B, ["darkgoldenrod"] = 0xB8860B,
		["darkgray"] = 0xA9A9A9, ["darkgreen"] = 0x006400, ["darkgrey"] = 0xA9A9A9, ["darkkhaki"] = 0xBDB76B,
		["darkmagenta"] = 0x8B008B, ["darkolivegreen"] = 0x556B2F, ["darkorange"] = 0xFF8C00, ["darkorchid"] = 0x9932CC,
		["darkred"] = 0x8B0000, ["darksalmon"] = 0xE9967A, ["darkseagreen"] = 0x8FBC8F, ["darkslateblue"] = 0x483D8B,
		["darkslategray"] = 0x2F4F4F, ["darkslategrey"] = 0x2F4F4F, ["darkturquoise"] = 0x00CED1, ["darkviolet"] = 0x9400D3,
		["deeppink"] = 0xFF1493, ["deepskyblue"] = 0x00BFFF, ["dimgray"] = 0x696969, ["dimgrey"] = 0x696969,
		["dodgerblue"] = 0x1E90FF, ["firebrick"] = 0xB22222, ["floralwhite"] = 0xFFFAF0, ["forestgreen"] = 0x228B22,
		["fuchsia"] = 0xFF00FF, ["gainsboro"] = 0xDCDCDC, ["ghostwhite"] = 0xF8F8FF, ["gold"] = 0xFFD700,
		["goldenrod"] = 0xDAA520, ["gray"] = 0x808080, ["green"] = 0x008000, ["greenyellow"] = 0xADFF2F,
		["grey"] = 0x808080, ["honeydew"] = 0xF0FFF0, ["hotpink"] = 0xFF69B4, ["indianred"] = 0xCD5C5C,
		["indigo"] = 0x4B0082, ["ivory"] = 0xFFFFF0, ["khaki"] = 0xF0E68C, ["lavender"] = 0xE6E6FA,
		["lavenderblush"] = 0xFFF0F5, ["lawngreen"] = 0x7CFC00, ["lemonchiffon"] = 0xFFFACD, ["lightblue"] = 0xADD8E6,
		["lightcoral"] = 0xF08080, ["lightcyan"] = 0xE0FFFF, ["lightgoldenrodyellow"] = 0xFAFAD2, ["lightgray"] = 0xD3D3D3,
		["lightgreen"] = 0x90EE90, ["lightgrey"] = 0xD3D3D3, ["lightpink"] = 0xFFB6C1, ["lightsalmon"] = 0xFFA07A,
		["lightseagreen"] = 0x20B2AA, ["lightskyblue"] = 0x87CEFA, ["lightslategray"] = 0x778899, ["lightslategrey"] = 0x778899,
		["lightsteelblue"] = 0xB0C4DE, ["lightyellow"] = 0xFFFFE0, ["lime"] = 0x00FF00, ["limegreen"] = 0x32CD32,
		["linen"] = 0xFAF0E6, ["magenta"] = 0xFF00FF, ["maroon"] = 0x800000, ["mediumaquamarine"] = 0x66CDAA,
		["mediumblue"] = 0x0000CD, ["mediumorchid"] = 0xBA55D3, ["mediumpurple"] = 0x9370DB, ["mediumseagreen"] = 0x3CB371,
		["mediumslateblue"] = 0x7B68EE, ["mediumspringgreen"] = 0x00FA9A, ["mediumturquoise"] = 0x48D1CC, ["mediumvioletred"] = 0xC71585,
		["midnightblue"] = 0x191970, ["mintcream"] = 0xF5FFFA, ["mistyrose"] = 0xFFE4E1, ["moccasin"] = 0xFFE4B5,
		["navajowhite"] = 0xFFDEAD, ["navy"] = 0x000080, ["oldlace"] = 0xFDF5E6, ["olive"] = 0x808000,
		["olivedrab"] = 0x6B8E23, ["orange"] = 0xFFA500, ["orangered"] = 0xFF4500, ["orchid"] = 0xDA70D6,
		["palegoldenrod"] = 0xEEE8AA, ["palegreen"] = 0x98FB98, ["paleturquoise"] = 0xAFEEEE, ["palevioletred"] = 0xDB7093,
		["papayawhip"] = 0xFFEFD5, ["peachpuff"] = 0xFFDAB9, ["peru"] = 0xCD853F, ["pink"] = 0xFFC0CB,
		["plum"] = 0xDDA0DD, ["powderblue"] = 0xB0E0E6, ["purple"] = 0x800080, ["rebeccapurple"] = 0x663399,
		["red"] = 0xFF0000, ["rosybrown"] = 0xBC8F8F, ["royalblue"] = 0x4169E1, ["saddlebrown"] = 0x8B4513,
		["salmon"] = 0xFA8072, ["sandybrown"] = 0xF4A460, ["seagreen"] = 0x2E8B57, ["seashell"] = 0xFFF5EE,
		["sienna"] = 0xA0522D, ["silver"] = 0xC0C0C0, ["skyblue"] = 0x87CEEB, ["slateblue"] = 0x6A5ACD,
		["slategray"] = 0x708090, ["slategrey"] = 0x708090, ["snow"] = 0xFFFAFA, ["springgreen"] = 0x00FF7F,
		["steelblue"] = 0x4682B4, ["tan"] = 0xD2B48C, ["teal"] = 0x008080, ["thistle"] = 0xD8BFD8,
		["tomato"] = 0xFF6347, ["turquoise"] = 0x40E0D0, ["violet"] = 0xEE82EE, ["wheat"] = 0xF5DEB3,
		["white"] = 0xFFFFFF, ["whitesmoke"] = 0xF5F5F5, ["yellow"] = 0xFFFF00, ["yellowgreen"] = 0x9ACD32,
	};

	/// <summary>The inheritable presentation state (fill/stroke/color/opacity/fill-rule/stroke geometry).</summary>
	private readonly struct SvgStyle
	{
		public Color ResolvedFill { get; private init; }
		public bool FillNone { get; private init; }
		public string? FillRef { get; private init; }
		public float FillAlpha { get; private init; }
		public GeometryFillRule FillRule { get; private init; }
		public Color ResolvedStroke { get; private init; }
		public bool StrokeNone { get; private init; }
		public string? StrokeRef { get; private init; }
		public float StrokeAlpha { get; private init; }
		public float StrokeWidth { get; private init; }
		public StrokeCap LineCap { get; private init; }
		public StrokeJoin LineJoin { get; private init; }
		public float MiterLimit { get; private init; }
		/// <summary>Dash intervals in user units (converted to thickness multiples at stroke time), or null for solid.</summary>
		public float[]? DashArray { get; private init; }
		public float DashOffset { get; private init; }
		/// <summary>The inherited <c>color</c> value that <c>currentColor</c> resolves to.</summary>
		public Color CurrentColor { get; private init; }

		public static SvgStyle Root => new()
		{
			ResolvedFill = Color.FromArgb(255, 0, 0, 0),
			FillNone = false,
			FillAlpha = 1f,
			FillRule = GeometryFillRule.NonZero,
			ResolvedStroke = Color.FromArgb(255, 0, 0, 0),
			StrokeNone = true,
			StrokeAlpha = 1f,
			StrokeWidth = 1f,
			LineCap = StrokeCap.Butt,
			LineJoin = StrokeJoin.Miter,
			MiterLimit = 4f,
			CurrentColor = Color.FromArgb(255, 0, 0, 0),
		};

		public SvgStyle InheritFrom(XElement el)
		{
			var style = ParseStyle((string?)el.Attribute("style"));
			string? Get(string name) => style.GetValueOrDefault(name) ?? (string?)el.Attribute(name);

			var opacity = Get("opacity") is { } o ? ParseFloat(o, 1f) : 1f;
			var fillOpacity = (Get("fill-opacity") is { } fo ? ParseFloat(fo, 1f) : 1f) * opacity;
			var strokeOpacity = (Get("stroke-opacity") is { } so ? ParseFloat(so, 1f) : 1f) * opacity;

			// `color` feeds currentColor and is itself inheritable.
			var currentColor = CurrentColor;
			if (Get("color") is { } colorText && !IsInherit(colorText))
			{
				currentColor = IsCurrentColor(colorText) ? currentColor : ParseColor(colorText, 1f) ?? currentColor;
			}

			var fillNone = FillNone;
			var fillRef = FillRef;
			var fill = ResolvedFill;
			if (Get("fill") is { } fillText && !IsInherit(fillText))
			{
				(fillNone, fillRef, fill) = ResolvePaint(fillText, fillOpacity, ResolvedFill, currentColor);
			}

			var strokeNone = StrokeNone;
			var strokeRef = StrokeRef;
			var stroke = ResolvedStroke;
			if (Get("stroke") is { } strokeText && !IsInherit(strokeText))
			{
				(strokeNone, strokeRef, stroke) = ResolvePaint(strokeText, strokeOpacity, ResolvedStroke, currentColor);
			}

			var fillRule = Get("fill-rule") is { } fr
				? fr.Trim() switch
				{
					"evenodd" => GeometryFillRule.EvenOdd,
					"nonzero" => GeometryFillRule.NonZero,
					_ => FillRule,
				}
				: FillRule;

			var lineCap = Get("stroke-linecap") is { } lc
				? lc.Trim() switch
				{
					"round" => StrokeCap.Round,
					"square" => StrokeCap.Square,
					"butt" => StrokeCap.Butt,
					_ => LineCap,
				}
				: LineCap;

			var lineJoin = Get("stroke-linejoin") is { } lj
				? lj.Trim() switch
				{
					"round" => StrokeJoin.Round,
					"bevel" => StrokeJoin.Bevel,
					"miter" or "miter-clip" or "arcs" => StrokeJoin.Miter,
					_ => LineJoin,
				}
				: LineJoin;

			var dashArray = DashArray;
			if (Get("stroke-dasharray") is { } da)
			{
				var t = da.Trim();
				dashArray = t is "none" or "" ? null : ParseNumbers(t);
				if (dashArray is { Length: 0 })
				{
					dashArray = null;
				}
			}

			return new SvgStyle
			{
				ResolvedFill = ApplyAlpha(fill, fillOpacity),
				FillNone = fillNone,
				FillRef = fillRef,
				FillAlpha = fillOpacity,
				FillRule = fillRule,
				ResolvedStroke = ApplyAlpha(stroke, strokeOpacity),
				StrokeNone = strokeNone,
				StrokeRef = strokeRef,
				StrokeAlpha = strokeOpacity,
				StrokeWidth = Get("stroke-width") is { } sw ? ParseFloat(sw, StrokeWidth) : StrokeWidth,
				LineCap = lineCap,
				LineJoin = lineJoin,
				MiterLimit = Get("stroke-miterlimit") is { } ml ? ParseFloat(ml, MiterLimit) : MiterLimit,
				DashArray = dashArray,
				DashOffset = Get("stroke-dashoffset") is { } dofs ? ParseFloat(dofs, DashOffset) : DashOffset,
				CurrentColor = currentColor,
			};
		}

		private static (bool none, string? reference, Color color) ResolvePaint(string text, float alpha, Color current, Color currentColor)
		{
			text = text.Trim();
			if (text == "none")
			{
				return (true, null, current);
			}

			if (IsCurrentColor(text))
			{
				return (false, null, currentColor);
			}

			if (text.StartsWith("url(", StringComparison.Ordinal))
			{
				var close = text.IndexOf(')');
				var reference = close > 4 ? text[4..close].Trim().Trim('\'', '"') : null;
				// A color after the reference is the fallback when the reference can't be resolved.
				var rest = close >= 0 && close + 1 < text.Length ? text[(close + 1)..].Trim() : string.Empty;
				var fallback = rest.Length == 0
					? current
					: IsCurrentColor(rest) ? currentColor : ParseColor(rest, alpha) ?? current;
				return (false, reference, fallback);
			}

			return (false, null, ParseColor(text, alpha) ?? current);
		}

		private static bool IsInherit(string text) => string.Equals(text.Trim(), "inherit", StringComparison.Ordinal);

		private static bool IsCurrentColor(string text) => string.Equals(text.Trim(), "currentColor", StringComparison.OrdinalIgnoreCase);

		private static Color ApplyAlpha(Color c, float alpha) => Color.FromArgb((byte)Math.Clamp(c.A * alpha, 0, 255), c.R, c.G, c.B);
	}
}
