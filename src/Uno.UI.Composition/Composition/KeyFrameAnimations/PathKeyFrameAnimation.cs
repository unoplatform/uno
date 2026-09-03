#nullable enable

#if __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Graphics;

namespace Microsoft.UI.Composition;

public partial class PathKeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<CompositionPath?>> _keyFrames = new();

	// Keyframe CompositionPaths converted to a segment stream once, so per-frame interpolation is cheap.
	private readonly Dictionary<CompositionPath, PathSegments?> _segmentCache = new();

	internal PathKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, CompositionPath path)
		=> InsertKeyFrame(normalizedProgressKey, path, Compositor.GetDefaultEasingFunction());

	public void InsertKeyFrame(float normalizedProgressKey, CompositionPath path, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = path, EasingFunction = easingFunction };

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);

		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = _keyFrames.Values.FirstOrDefault();
		}

		if (!_keyFrames.TryGetValue(1, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		CompositionPath? Lerp(AnimationKeyFrame<CompositionPath?> value1, AnimationKeyFrame<CompositionPath?> value2, float amount)
			=> InterpolatePath(value1.Value, value2.Value, value2.EasingFunction.Ease(amount));

		_keyframeEvaluator = new KeyFrameEvaluator<CompositionPath?>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue.Value;
	}

	private CompositionPath? InterpolatePath(CompositionPath? from, CompositionPath? to, float amount)
	{
		if (from is null || to is null || amount <= 0f || ReferenceEquals(from, to))
		{
			return from ?? to;
		}

		if (amount >= 1f)
		{
			return to;
		}

		var fromSegments = GetSegments(from);
		var toSegments = GetSegments(to);

		if (fromSegments is not null && toSegments is not null
			&& TryMorph(fromSegments, toSegments, amount) is IGeometrySource2D morphed)
		{
			return new CompositionPath(morphed);
		}

		// Different topology -> hold the from-keyframe until the next one (step behaviour).
		return from;
	}

	/// <summary>
	/// Builds a geometry whose points are the per-vertex linear interpolation of two topologically identical
	/// paths. Returns null when the segment streams differ (WinUI likewise only morphs paths of equal
	/// structure). <paramref name="amount"/> runs 0..1 from <paramref name="from"/> to <paramref name="to"/>.
	/// </summary>
	private static IGeometry? TryMorph(PathSegments from, PathSegments to, float amount)
	{
		if (from.Verbs.Count != to.Verbs.Count || from.Points.Count != to.Points.Count)
		{
			return null;
		}

		for (var i = 0; i < from.Verbs.Count; i++)
		{
			if (from.Verbs[i] != to.Verbs[i])
			{
				return null;
			}
		}

		var builder = GeometryFactory.Current.CreatePathBuilder();
		builder.FillRule = from.FillRule;

		var p = 0;
		Vector2 Mix() { var m = Vector2.Lerp(from.Points[p], to.Points[p], amount); p++; return m; }

		foreach (var verb in from.Verbs)
		{
			switch (verb)
			{
				case PathVerb.Move:
					builder.MoveTo(Mix());
					break;
				case PathVerb.Line:
					builder.LineTo(Mix());
					break;
				case PathVerb.Quad:
					builder.QuadraticTo(Mix(), Mix());
					break;
				case PathVerb.Cubic:
					builder.CubicTo(Mix(), Mix(), Mix());
					break;
				case PathVerb.Close:
					builder.Close();
					break;
			}
		}

		return builder.Build();
	}

	private PathSegments? GetSegments(CompositionPath path)
	{
		if (_segmentCache.TryGetValue(path, out var cached))
		{
			return cached;
		}

		var geometry = Compositor.CreatePathGeometry();
		geometry.Path = path;

		PathSegments? segments = null;
		if (geometry.GetBuiltGeometry() is { } built)
		{
			segments = new PathSegments(built.FillRule);
			built.StreamSegments(segments);
		}

		// Only dispose the geometry when it built its own source. A geometry-backed CompositionPath is adopted
		// by reference, so disposing would free the caller's geometry.
		if (path.GeometrySource is not IGeometry)
		{
			geometry.Dispose();
		}

		_segmentCache[path] = segments;
		return segments;
	}

	private enum PathVerb
	{
		Move,
		Line,
		Quad,
		Cubic,
		Close,
	}

	/// <summary>
	/// A path recorded as a verb stream plus the points those verbs consume, which is what makes two paths
	/// comparable for morphing: identical verbs in the same order means the points line up one for one.
	/// </summary>
	private sealed class PathSegments : IGeometrySink
	{
		public PathSegments(GeometryFillRule fillRule) => FillRule = fillRule;

		public GeometryFillRule FillRule { get; }

		public List<PathVerb> Verbs { get; } = new();

		public List<Vector2> Points { get; } = new();

		public void BeginFigure(Vector2 start)
		{
			Verbs.Add(PathVerb.Move);
			Points.Add(start);
		}

		public void LineTo(Vector2 point)
		{
			Verbs.Add(PathVerb.Line);
			Points.Add(point);
		}

		public void QuadTo(Vector2 control, Vector2 point)
		{
			Verbs.Add(PathVerb.Quad);
			Points.Add(control);
			Points.Add(point);
		}

		public void CubicTo(Vector2 control1, Vector2 control2, Vector2 point)
		{
			Verbs.Add(PathVerb.Cubic);
			Points.Add(control1);
			Points.Add(control2);
			Points.Add(point);
		}

		public void EndFigure(bool closed)
		{
			if (closed)
			{
				Verbs.Add(PathVerb.Close);
			}
		}
	}
}
#endif
