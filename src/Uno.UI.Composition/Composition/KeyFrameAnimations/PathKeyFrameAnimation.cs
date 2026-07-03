#nullable enable

#if __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Microsoft.UI.Composition;

public partial class PathKeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<CompositionPath?>> _keyFrames = new();

	// Keyframe CompositionPaths converted to SKPath once, so per-frame interpolation is cheap. The
	// cached paths are independent copies so a temporary CompositionPathGeometry being collected can't
	// dispose the SKPath out from under us.
	private readonly Dictionary<CompositionPath, SKPath?> _skPathCache = new();

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

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
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

		var fromPath = GetSKPath(from);
		var toPath = GetSKPath(to);

		if (fromPath is not null && toPath is not null && TryMorph(fromPath, toPath, amount) is { } morphed)
		{
			return new CompositionPath(new SkiaGeometrySource2D(morphed));
		}

		// Different topology -> hold the from-keyframe until the next one (step behaviour).
		return from;
	}

	// Builds a new path whose points are the per-vertex linear interpolation of two topologically
	// identical paths. Returns null when the verb streams differ (WinUI likewise only morphs paths of
	// equal structure). 'amount' runs 0..1 from 'from' to 'to'.
	private static SKPath? TryMorph(SKPath from, SKPath to, float amount)
	{
		var result = new SKPath { FillType = from.FillType };
		using var fromIt = from.CreateRawIterator();
		using var toIt = to.CreateRawIterator();
		var fp = new SKPoint[4];
		var tp = new SKPoint[4];

		while (true)
		{
			var vf = fromIt.Next(fp);
			var vt = toIt.Next(tp);
			if (vf != vt)
			{
				result.Dispose();
				return null;
			}

			switch (vf)
			{
				case SKPathVerb.Move:
					result.MoveTo(Mix(fp[0], tp[0], amount));
					break;
				case SKPathVerb.Line:
					result.LineTo(Mix(fp[1], tp[1], amount));
					break;
				case SKPathVerb.Quad:
					result.QuadTo(Mix(fp[1], tp[1], amount), Mix(fp[2], tp[2], amount));
					break;
				case SKPathVerb.Conic:
					result.ConicTo(Mix(fp[1], tp[1], amount), Mix(fp[2], tp[2], amount), fromIt.ConicWeight());
					break;
				case SKPathVerb.Cubic:
					result.CubicTo(Mix(fp[1], tp[1], amount), Mix(fp[2], tp[2], amount), Mix(fp[3], tp[3], amount));
					break;
				case SKPathVerb.Close:
					result.Close();
					break;
				case SKPathVerb.Done:
					return result;
			}
		}
	}

	private static SKPoint Mix(SKPoint from, SKPoint to, float amount)
		=> new(from.X + (to.X - from.X) * amount, from.Y + (to.Y - from.Y) * amount);

	private SKPath? GetSKPath(CompositionPath path)
	{
		if (_skPathCache.TryGetValue(path, out var cached))
		{
			return cached;
		}

		var geometry = Compositor.CreatePathGeometry();
		geometry.Path = path;
		var built = geometry.GetSKPath();
		var copy = built is null ? null : new SKPath(built);
		_skPathCache[path] = copy;
		return copy;
	}
}
#endif
