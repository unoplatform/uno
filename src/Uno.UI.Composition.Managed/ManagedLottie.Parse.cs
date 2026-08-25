#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

internal sealed partial class ManagedLottie
{
	public static bool TryParse(string json, out ManagedLottie? animation)
	{
		animation = null;
		try
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("layers", out var layersEl) || layersEl.ValueKind != JsonValueKind.Array)
			{
				return false;
			}

			var layers = new List<Layer>();
			var byIndex = new Dictionary<int, Layer>();
			foreach (var le in layersEl.EnumerateArray())
			{
				var layer = ParseLayer(le);
				layers.Add(layer);
				byIndex[layer.Index] = layer;
			}

			animation = new ManagedLottie
			{
				Width = F(root, "w", 0),
				Height = F(root, "h", 0),
				FrameRate = F(root, "fr", 60),
				InPoint = F(root, "ip", 0),
				OutPoint = F(root, "op", 0),
				Layers = layers,
				LayersByIndex = byIndex,
			};
			return animation.Width > 0 && animation.Height > 0;
		}
		catch
		{
			animation = null;
			return false;
		}
	}

	private static Layer ParseLayer(JsonElement le)
	{
		var layer = new Layer
		{
			Type = (int)F(le, "ty", -1),
			Index = (int)F(le, "ind", 0),
			InPoint = F(le, "ip", 0),
			OutPoint = F(le, "op", 0),
		};
		if (le.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.Number)
		{
			layer.ParentIndex = parent.GetInt32();
		}
		if (le.TryGetProperty("ks", out var ks))
		{
			layer.Transform = ParseTransform(ks);
		}
		if (le.TryGetProperty("shapes", out var shapes) && shapes.ValueKind == JsonValueKind.Array)
		{
			layer.Shapes = ParseShapeList(shapes);
		}
		return layer;
	}

	private static Transform ParseTransform(JsonElement ks)
	{
		var t = new Transform();
		if (ks.TryGetProperty("a", out var a)) { t.Anchor = ParseVector(a); }
		if (ks.TryGetProperty("p", out var p)) { t.Position = ParseVector(p); }
		if (ks.TryGetProperty("s", out var s)) { t.Scale = ParseVector(s); }
		if (ks.TryGetProperty("r", out var r)) { t.Rotation = ParseScalar(r); }
		if (ks.TryGetProperty("o", out var o)) { t.Opacity = ParseScalar(o); }
		return t;
	}

	private static IReadOnlyList<ShapeItem> ParseShapeList(JsonElement items)
	{
		var list = new List<ShapeItem>();
		foreach (var it in items.EnumerateArray())
		{
			if (ParseShapeItem(it) is { } item)
			{
				list.Add(item);
			}
		}
		return list;
	}

	private static ShapeItem? ParseShapeItem(JsonElement it)
	{
		var ty = it.TryGetProperty("ty", out var tyEl) && tyEl.ValueKind == JsonValueKind.String ? tyEl.GetString() : null;
		switch (ty)
		{
			case "gr":
				return new GroupShape { Items = it.TryGetProperty("it", out var sub) && sub.ValueKind == JsonValueKind.Array ? ParseShapeList(sub) : Array.Empty<ShapeItem>() };
			case "sh":
				return new PathShape { Path = it.TryGetProperty("ks", out var shk) ? ParsePath(shk) : AnimatedPath.Empty };
			case "rc":
				return new RectShape
				{
					Position = it.TryGetProperty("p", out var rp) ? ParseVector(rp) : AnimatedVector.Constant(Vector2.Zero),
					Size = it.TryGetProperty("s", out var rs) ? ParseVector(rs) : AnimatedVector.Constant(Vector2.Zero),
					Roundness = it.TryGetProperty("r", out var rr) ? ParseScalar(rr) : AnimatedScalar.Constant(0),
				};
			case "el":
				return new EllipseShape
				{
					Position = it.TryGetProperty("p", out var ep) ? ParseVector(ep) : AnimatedVector.Constant(Vector2.Zero),
					Size = it.TryGetProperty("s", out var es) ? ParseVector(es) : AnimatedVector.Constant(Vector2.Zero),
				};
			case "fl":
				return new FillShape
				{
					Color = it.TryGetProperty("c", out var fc) ? ParseColor(fc) : AnimatedColor.Constant(Color.FromArgb(255, 0, 0, 0)),
					Opacity = it.TryGetProperty("o", out var fo) ? ParseScalar(fo) : AnimatedScalar.Constant(100),
					EvenOdd = (int)F(it, "r", 1) == 2,
				};
			case "st":
				return new StrokeShape
				{
					Color = it.TryGetProperty("c", out var sc) ? ParseColor(sc) : AnimatedColor.Constant(Color.FromArgb(255, 0, 0, 0)),
					Opacity = it.TryGetProperty("o", out var so) ? ParseScalar(so) : AnimatedScalar.Constant(100),
					Width = it.TryGetProperty("w", out var sw) ? ParseScalar(sw) : AnimatedScalar.Constant(1),
					Cap = (int)F(it, "lc", 2),
					Join = (int)F(it, "lj", 2),
				};
			case "tr":
				return new TransformShape { Transform = ParseTransform(it) };
			default:
				return null; // gf/gs/tm/rp/mm/sr/… not modelled in v1 — skipped, not fatal
		}
	}

	// ---- property parsers ----

	private static bool IsAnimated(JsonElement prop)
		=> prop.TryGetProperty("k", out var k)
			&& k.ValueKind == JsonValueKind.Array
			&& k.GetArrayLength() > 0
			&& k[0].ValueKind == JsonValueKind.Object
			&& k[0].TryGetProperty("t", out _);

	private static AnimatedScalar ParseScalar(JsonElement prop)
	{
		if (!prop.TryGetProperty("k", out var k))
		{
			return AnimatedScalar.Constant(0);
		}
		if (!IsAnimated(prop))
		{
			return AnimatedScalar.Constant(ReadFloatArray(k) is { Length: > 0 } arr ? arr[0] : 0f);
		}
		return AnimatedScalar.FromTrack(ParseValueKeyframes(k));
	}

	private static AnimatedVector ParseVector(JsonElement prop)
	{
		if (!prop.TryGetProperty("k", out var k))
		{
			return AnimatedVector.Constant(Vector2.Zero);
		}
		if (!IsAnimated(prop))
		{
			var arr = ReadFloatArray(k);
			return AnimatedVector.FromTrack(Track.Const(arr.Length > 0 ? arr : new[] { 0f, 0f }));
		}
		return AnimatedVector.FromTrack(ParseValueKeyframes(k));
	}

	private static AnimatedColor ParseColor(JsonElement prop)
	{
		if (!prop.TryGetProperty("k", out var k))
		{
			return AnimatedColor.Constant(Color.FromArgb(255, 0, 0, 0));
		}
		if (!IsAnimated(prop))
		{
			return AnimatedColor.FromTrack(Track.Const(ReadFloatArray(k)));
		}
		return AnimatedColor.FromTrack(ParseValueKeyframes(k));
	}

	// float[]-valued keyframe track (scalar/vector/color): each element is { t, s, [e], [h], [i], [o] }.
	private static Track ParseValueKeyframes(JsonElement kArray)
	{
		var kfs = new List<Keyframe>();
		foreach (var kf in kArray.EnumerateArray())
		{
			var frame = F(kf, "t", 0);
			var start = kf.TryGetProperty("s", out var s) ? ReadFloatArray(s) : Array.Empty<float>();
			float[]? end = kf.TryGetProperty("e", out var e) ? ReadFloatArray(e) : null;
			var hold = (int)F(kf, "h", 0) == 1;
			ReadEase(kf, out var ox, out var oy, out var ix, out var iy, out var hasEase);
			kfs.Add(new Keyframe(frame, start, end, hold, ox, oy, ix, iy, hasEase));
		}
		return Track.Animated(kfs.ToArray());
	}

	private static AnimatedPath ParsePath(JsonElement prop)
	{
		if (!prop.TryGetProperty("k", out var k))
		{
			return AnimatedPath.Empty;
		}
		// Animated path: array of keyframe objects, each 's' a shape (sometimes wrapped in a 1-element array).
		if (k.ValueKind == JsonValueKind.Array && k.GetArrayLength() > 0 && k[0].ValueKind == JsonValueKind.Object && k[0].TryGetProperty("t", out _))
		{
			var kfs = new List<(float, ShapeData, bool, float, float, float, float, bool)>();
			foreach (var kf in k.EnumerateArray())
			{
				var frame = F(kf, "t", 0);
				var shapeEl = kf.TryGetProperty("s", out var s)
					? (s.ValueKind == JsonValueKind.Array && s.GetArrayLength() > 0 ? s[0] : s)
					: default;
				var shape = ReadShape(shapeEl);
				var hold = (int)F(kf, "h", 0) == 1;
				ReadEase(kf, out var ox, out var oy, out var ix, out var iy, out var hasEase);
				kfs.Add((frame, shape, hold, ox, oy, ix, iy, hasEase));
			}
			return new AnimatedPath(kfs.ToArray());
		}
		// Static path: k is the shape object.
		return new AnimatedPath(ReadShape(k));
	}

	private static ShapeData ReadShape(JsonElement shapeEl)
	{
		if (shapeEl.ValueKind != JsonValueKind.Object)
		{
			return default;
		}
		var v = ReadPointArray(shapeEl, "v");
		var i = ReadPointArray(shapeEl, "i");
		var o = ReadPointArray(shapeEl, "o");
		var closed = shapeEl.TryGetProperty("c", out var c) && c.ValueKind == JsonValueKind.True;
		return new ShapeData(v, i, o, closed);
	}

	// ---- primitives ----

	private static void ReadEase(JsonElement kf, out float ox, out float oy, out float ix, out float iy, out bool hasEase)
	{
		ox = oy = ix = iy = 0f;
		hasEase = false;
		if (kf.TryGetProperty("o", out var o) && kf.TryGetProperty("i", out var i))
		{
			ox = FirstOf(o, "x"); oy = FirstOf(o, "y");
			ix = FirstOf(i, "x"); iy = FirstOf(i, "y");
			hasEase = true;
		}
	}

	// An ease handle component is either a number or a 1+-element array; take the first.
	private static float FirstOf(JsonElement handle, string name)
	{
		if (!handle.TryGetProperty(name, out var el))
		{
			return 0f;
		}
		if (el.ValueKind == JsonValueKind.Array)
		{
			return el.GetArrayLength() > 0 ? (float)el[0].GetDouble() : 0f;
		}
		return el.ValueKind == JsonValueKind.Number ? (float)el.GetDouble() : 0f;
	}

	private static Vector2[] ReadPointArray(JsonElement obj, string name)
	{
		if (!obj.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
		{
			return Array.Empty<Vector2>();
		}
		var pts = new Vector2[arr.GetArrayLength()];
		var idx = 0;
		foreach (var pt in arr.EnumerateArray())
		{
			var x = pt.ValueKind == JsonValueKind.Array && pt.GetArrayLength() > 0 ? (float)pt[0].GetDouble() : 0f;
			var y = pt.ValueKind == JsonValueKind.Array && pt.GetArrayLength() > 1 ? (float)pt[1].GetDouble() : 0f;
			pts[idx++] = new Vector2(x, y);
		}
		return pts;
	}

	// A value is either a scalar number or an array of numbers → normalize to float[].
	private static float[] ReadFloatArray(JsonElement el)
	{
		if (el.ValueKind == JsonValueKind.Number)
		{
			return new[] { (float)el.GetDouble() };
		}
		if (el.ValueKind == JsonValueKind.Array)
		{
			var arr = new float[el.GetArrayLength()];
			var idx = 0;
			foreach (var n in el.EnumerateArray())
			{
				arr[idx++] = n.ValueKind == JsonValueKind.Number ? (float)n.GetDouble() : 0f;
			}
			return arr;
		}
		return Array.Empty<float>();
	}

	private static float F(JsonElement obj, string name, float fallback)
		=> obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? (float)el.GetDouble() : fallback;
}
