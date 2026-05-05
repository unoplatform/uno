using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Composition;

public partial class CubicBezierEasingFunction
{
	public Vector2 ControlPoint1 { get; }
	public Vector2 ControlPoint2 { get; }

	internal CubicBezierEasingFunction(Compositor owner, Vector2 controlPoint1, Vector2 controlPoint2) : base(owner)
	{
		ControlPoint1 = controlPoint1;
		ControlPoint2 = controlPoint2;
	}

	// CSS / WinUI Composition cubic-bezier easing semantics: the curve goes from (0, 0) to
	// (1, 1) with the two supplied points as control points. The input `t` is the elapsed
	// fraction along the X axis, and the output is the curve's Y at that X. To compute it we
	// have to invert the bezier's X function — find the parameter `s` such that bezier_x(s) = t —
	// then evaluate bezier_y(s). Newton–Raphson converges in ~3 iterations for monotonic curves;
	// we fall back to bisection to stay correct for the rare cases where derivative collapses.
	internal override float Ease(float t)
	{
		if (t <= 0f) return 0f;
		if (t >= 1f) return 1f;

		var s = SolveCurveX(t, ControlPoint1.X, ControlPoint2.X);
		return BezierEval(0f, ControlPoint1.Y, ControlPoint2.Y, 1f, s);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float BezierEval(float p0, float p1, float p2, float p3, float t)
	{
		var u = 1f - t;
		return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float BezierDerivative(float p0, float p1, float p2, float p3, float t)
	{
		var u = 1f - t;
		return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
	}

	private static float SolveCurveX(float x, float cp1x, float cp2x)
	{
		const float epsilon = 1e-6f;

		// Newton–Raphson: starts at s = x, converges quickly when X(s) is monotonic.
		var s = x;
		for (int i = 0; i < 8; i++)
		{
			var diff = BezierEval(0f, cp1x, cp2x, 1f, s) - x;
			if (MathF.Abs(diff) < epsilon)
			{
				return s;
			}
			var derivative = BezierDerivative(0f, cp1x, cp2x, 1f, s);
			if (MathF.Abs(derivative) < epsilon)
			{
				break;
			}
			s -= diff / derivative;
		}

		// Bisection fallback for pathological cases (collapsed derivative, divergent step).
		float lo = 0f, hi = 1f;
		s = x;
		for (int i = 0; i < 32; i++)
		{
			var diff = BezierEval(0f, cp1x, cp2x, 1f, s) - x;
			if (MathF.Abs(diff) < epsilon)
			{
				return s;
			}
			if (diff > 0f)
			{
				hi = s;
			}
			else
			{
				lo = s;
			}
			s = (lo + hi) * 0.5f;
		}
		return s;
	}
}
