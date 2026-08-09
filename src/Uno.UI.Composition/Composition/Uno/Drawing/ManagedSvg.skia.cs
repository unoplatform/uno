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
/// A SkiaSharp-free SVG engine: parses SVG markup and renders it through the neutral drawing abstraction
/// (<see cref="IPathBuilder"/> geometry, <see cref="IDrawingSession"/> verbs, backend gradient shaders), so the
/// core framework's SVG support no longer depends on Skia. Produces an <see cref="IImage"/> at a target size via
/// <see cref="IDrawingFactory.RenderOffscreen"/>.
/// </summary>
/// <remarks>
/// Covers the common icon/illustration subset: svg/g/path/rect/circle/ellipse/line/polyline/polygon/use, fills and
/// strokes (solid + linear/radial gradients), opacity, fill-rule, transforms, viewBox, and inline style. Not yet:
/// text, clipPath/mask/filter/pattern, embedded images, external/CSS-class styling.
/// </remarks>
internal sealed class ManagedSvg
{
	private readonly XElement _root;
	private readonly Dictionary<string, XElement> _byId = new();

	private ManagedSvg(XElement root)
	{
		_root = root;
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

	public static bool TryParse(byte[] svg, out ManagedSvg document)
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

			document = new ManagedSvg(xdoc.Root);
			return document.SourceSize is { Width: > 0, Height: > 0 };
		}
		catch
		{
			return false;
		}
	}

	public IImageTexture Render(int pixelWidth, int pixelHeight)
	{
		return DrawingFactory.Current.RenderOffscreen(pixelWidth, pixelHeight, session =>
		{
			// Map the viewBox into the pixel target (uniform scale, centered — xMidYMid meet).
			var sx = pixelWidth / (float)_viewBox.Width;
			var sy = pixelHeight / (float)_viewBox.Height;
			var scale = Math.Min(sx, sy);
			var tx = (pixelWidth - (float)_viewBox.Width * scale) / 2f - (float)_viewBox.X * scale;
			var ty = (pixelHeight - (float)_viewBox.Height * scale) / 2f - (float)_viewBox.Y * scale;

			session.Concat(ToMatrix4x4(new Matrix3x2(scale, 0, 0, scale, tx, ty)));
			RenderChildren(session, _root, SvgStyle.Root);
		});
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
				DrawGeometry(session, BuildPath((string?)el.Attribute("d")), style, el);
				break;
			case "rect":
				DrawGeometry(session, BuildRect(el), style, el);
				break;
			case "circle":
				DrawGeometry(session, BuildEllipse(Len(el, "cx"), Len(el, "cy"), Len(el, "r"), Len(el, "r")), style, el);
				break;
			case "ellipse":
				DrawGeometry(session, BuildEllipse(Len(el, "cx"), Len(el, "cy"), Len(el, "rx"), Len(el, "ry")), style, el);
				break;
			case "line":
				DrawGeometry(session, BuildPolyline(new[] { Len(el, "x1"), Len(el, "y1"), Len(el, "x2"), Len(el, "y2") }, false), style, el);
				break;
			case "polyline":
				DrawGeometry(session, BuildPolyline(ParseNumbers((string?)el.Attribute("points")), false), style, el);
				break;
			case "polygon":
				DrawGeometry(session, BuildPolyline(ParseNumbers((string?)el.Attribute("points")), true), style, el);
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
			// Fill.
			if (!style.FillNone)
			{
				if (style.FillRef is { } fillRef && ResolveGradient(fillRef, geometry, style.FillAlpha) is { } shader)
				{
					var save = session.SaveCount;
					session.Save();
					session.ClipPath(geometry, ClipOperation.Intersect, antialias: true);
					session.DrawRect(geometry.Bounds, shader, antialias: true);
					session.RestoreToCount(save);
				}
				else
				{
					session.DrawPath(geometry, style.ResolvedFill, antialias: true);
				}
			}

			// Stroke.
			if (!style.StrokeNone && style.StrokeWidth > 0)
			{
				session.StrokePath(geometry, style.ResolvedStroke, style.StrokeWidth, antialias: true);
			}
		}
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
			return DrawingFactory.Current.CreateRadialGradientShader(
				new Vector2(cx, cy), new Vector2(cx, cy), r, r, colors, positions, tileMode, localMatrix);
		}

		var x1 = MapX(Frac("x1", 0f));
		var y1 = MapY(Frac("y1", 0f));
		var x2 = MapX(Frac("x2", 1f));
		var y2 = MapY(Frac("y2", 0f));
		return DrawingFactory.Current.CreateLinearGradientShader(
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

	private IGeometry? BuildRect(XElement el)
	{
		var w = Len(el, "width");
		var h = Len(el, "height");
		if (w <= 0 || h <= 0)
		{
			return null;
		}

		var x = Len(el, "x");
		var y = Len(el, "y");
		var b = DrawingFactory.Current.CreatePathBuilder();
		// (Rounded rx/ry omitted in v1 — sharp corners.)
		b.MoveTo(new Vector2(x, y));
		b.LineTo(new Vector2(x + w, y));
		b.LineTo(new Vector2(x + w, y + h));
		b.LineTo(new Vector2(x, y + h));
		b.Close();
		return b.Build();
	}

	private IGeometry? BuildEllipse(float cx, float cy, float rx, float ry)
	{
		if (rx <= 0 || ry <= 0)
		{
			return null;
		}

		// Four cubic bezier quadrants (kappa).
		const float k = 0.5522847498f;
		var b = DrawingFactory.Current.CreatePathBuilder();
		b.MoveTo(new Vector2(cx + rx, cy));
		b.CubicTo(new Vector2(cx + rx, cy + ry * k), new Vector2(cx + rx * k, cy + ry), new Vector2(cx, cy + ry));
		b.CubicTo(new Vector2(cx - rx * k, cy + ry), new Vector2(cx - rx, cy + ry * k), new Vector2(cx - rx, cy));
		b.CubicTo(new Vector2(cx - rx, cy - ry * k), new Vector2(cx - rx * k, cy - ry), new Vector2(cx, cy - ry));
		b.CubicTo(new Vector2(cx + rx * k, cy - ry), new Vector2(cx + rx, cy - ry * k), new Vector2(cx + rx, cy));
		b.Close();
		return b.Build();
	}

	private IGeometry? BuildPolyline(float[] pts, bool close)
	{
		if (pts.Length < 4)
		{
			return null;
		}

		var b = DrawingFactory.Current.CreatePathBuilder();
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

	private IGeometry? BuildPath(string? d)
	{
		if (string.IsNullOrWhiteSpace(d))
		{
			return null;
		}

		var b = DrawingFactory.Current.CreatePathBuilder();
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

		if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
		{
			var open = text.IndexOf('(');
			var close = text.IndexOf(')');
			if (open > 0 && close > open)
			{
				var parts = ParseNumbers(text[(open + 1)..close]);
				if (parts.Length >= 3)
				{
					var alphaByte = parts.Length >= 4 ? (byte)Math.Clamp(parts[3] * alpha * 255f, 0, 255) : a;
					return Color.FromArgb(alphaByte, (byte)parts[0], (byte)parts[1], (byte)parts[2]);
				}
			}
		}

		if (_namedColors.TryGetValue(text.ToLowerInvariant(), out var named))
		{
			return Color.FromArgb(a, (byte)(named >> 16), (byte)(named >> 8), (byte)named);
		}

		return null;
	}

	private static readonly Dictionary<string, int> _namedColors = new(StringComparer.Ordinal)
	{
		["black"] = 0x000000, ["white"] = 0xFFFFFF, ["red"] = 0xFF0000, ["green"] = 0x008000, ["blue"] = 0x0000FF,
		["lime"] = 0x00FF00, ["yellow"] = 0xFFFF00, ["cyan"] = 0x00FFFF, ["aqua"] = 0x00FFFF, ["magenta"] = 0xFF00FF,
		["fuchsia"] = 0xFF00FF, ["gray"] = 0x808080, ["grey"] = 0x808080, ["silver"] = 0xC0C0C0, ["maroon"] = 0x800000,
		["olive"] = 0x808000, ["navy"] = 0x000080, ["purple"] = 0x800080, ["teal"] = 0x008080, ["orange"] = 0xFFA500,
		["pink"] = 0xFFC0CB, ["brown"] = 0xA52A2A, ["gold"] = 0xFFD700, ["indigo"] = 0x4B0082, ["violet"] = 0xEE82EE,
		["darkgray"] = 0xA9A9A9, ["darkgrey"] = 0xA9A9A9, ["lightgray"] = 0xD3D3D3, ["lightgrey"] = 0xD3D3D3,
		["currentcolor"] = 0x000000,
	};

	/// <summary>The inheritable presentation state (fill/stroke/opacity/fill-rule).</summary>
	private readonly struct SvgStyle
	{
		public Color ResolvedFill { get; private init; }
		public bool FillNone { get; private init; }
		public string? FillRef { get; private init; }
		public float FillAlpha { get; private init; }
		public Color ResolvedStroke { get; private init; }
		public bool StrokeNone { get; private init; }
		public float StrokeWidth { get; private init; }

		public static SvgStyle Root => new()
		{
			ResolvedFill = Color.FromArgb(255, 0, 0, 0),
			FillNone = false,
			FillAlpha = 1f,
			ResolvedStroke = Color.FromArgb(255, 0, 0, 0),
			StrokeNone = true,
			StrokeWidth = 1f,
		};

		public SvgStyle InheritFrom(XElement el)
		{
			var style = ParseStyle((string?)el.Attribute("style"));
			string? Get(string name) => style.GetValueOrDefault(name) ?? (string?)el.Attribute(name);

			var opacity = Get("opacity") is { } o ? ParseFloat(o, 1f) : 1f;
			var fillOpacity = (Get("fill-opacity") is { } fo ? ParseFloat(fo, 1f) : 1f) * opacity;
			var strokeOpacity = (Get("stroke-opacity") is { } so ? ParseFloat(so, 1f) : 1f) * opacity;

			var fillNone = FillNone;
			var fillRef = FillRef;
			var fill = ResolvedFill;
			if (Get("fill") is { } fillText)
			{
				(fillNone, fillRef, fill) = ResolvePaint(fillText, fillOpacity, ResolvedFill);
			}

			var strokeNone = StrokeNone;
			var stroke = ResolvedStroke;
			if (Get("stroke") is { } strokeText)
			{
				var (sn, _, sc) = ResolvePaint(strokeText, strokeOpacity, ResolvedStroke);
				strokeNone = sn;
				stroke = sc;
			}

			return new SvgStyle
			{
				ResolvedFill = ApplyAlpha(fill, fillOpacity),
				FillNone = fillNone,
				FillRef = fillRef,
				FillAlpha = fillOpacity,
				ResolvedStroke = ApplyAlpha(stroke, strokeOpacity),
				StrokeNone = strokeNone,
				StrokeWidth = Get("stroke-width") is { } sw ? ParseFloat(sw, StrokeWidth) : StrokeWidth,
			};
		}

		private static (bool none, string? reference, Color color) ResolvePaint(string text, float alpha, Color current)
		{
			text = text.Trim();
			if (text == "none")
			{
				return (true, null, current);
			}

			if (text.StartsWith("url(", StringComparison.Ordinal))
			{
				var close = text.IndexOf(')');
				var reference = close > 4 ? text[4..close].Trim().Trim('\'', '"') : null;
				return (false, reference, current);
			}

			return (false, null, ParseColor(text, alpha) ?? current);
		}

		private static Color ApplyAlpha(Color c, float alpha) => Color.FromArgb((byte)Math.Clamp(c.A * alpha, 0, 255), c.R, c.G, c.B);
	}
}
