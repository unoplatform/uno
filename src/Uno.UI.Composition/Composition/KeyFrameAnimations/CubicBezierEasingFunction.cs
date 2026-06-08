using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

	internal override float Ease(float t)
	{
		if (t <= 0f)
		{
			return 0f;
		}

		if (t >= 1f)
		{
			return 1f;
		}

		// Coefficient form of the cubic Bezier with P0=(0,0) and P3=(1,1):
		//   B(s) = ((a*s + b)*s + c)*s
		var cx = 3f * ControlPoint1.X;
		var bx = 3f * (ControlPoint2.X - ControlPoint1.X) - cx;
		var ax = 1f - cx - bx;

		var cy = 3f * ControlPoint1.Y;
		var by = 3f * (ControlPoint2.Y - ControlPoint1.Y) - cy;
		var ay = 1f - cy - by;

		// The input t is the X coordinate of the easing curve (time progress),
		// not the Bezier parameter. Solve for the parameter s such that
		// SampleX(s) == t, then return SampleY(s).
		var s = SolveCurveX(t, ax, bx, cx);
		return ((ay * s + by) * s + cy) * s;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float SolveCurveX(float x, float ax, float bx, float cx)
	{
		const float epsilon = 1e-6f;

		// Newton-Raphson — usually converges in a handful of iterations because
		// SampleX is monotonic when control-point X values are in [0, 1].
		var t = x;
		for (var i = 0; i < 8; i++)
		{
			var currentX = ((ax * t + bx) * t + cx) * t - x;
			if (MathF.Abs(currentX) < epsilon)
			{
				return t;
			}

			var currentSlope = (3f * ax * t + 2f * bx) * t + cx;
			if (MathF.Abs(currentSlope) < epsilon)
			{
				break;
			}

			t -= currentX / currentSlope;
		}

		// Bisection fallback in case Newton diverged or stalled (e.g. control
		// points outside [0, 1] that make SampleX non-monotonic).
		var lo = 0f;
		var hi = 1f;
		t = x;
		while (lo < hi)
		{
			var currentX = ((ax * t + bx) * t + cx) * t;
			if (MathF.Abs(currentX - x) < epsilon)
			{
				return t;
			}

			if (x > currentX)
			{
				lo = t;
			}
			else
			{
				hi = t;
			}

			var next = (hi + lo) * 0.5f;
			if (next == t)
			{
				break;
			}

			t = next;
		}

		return t;
	}
}
