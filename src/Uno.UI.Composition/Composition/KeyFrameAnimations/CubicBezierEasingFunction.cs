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
		=> EaseInternal(Vector2.Zero, ControlPoint1, ControlPoint2, Vector2.One, t).Y;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Vector2 EaseInternal(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
	{
		var x = 1 - t;

		return
		x * x * x * p0 +
		3 * x * x * t * p1 +
		3 * x * t * t * p2 +
		t * t * t * p3;
	}
}
