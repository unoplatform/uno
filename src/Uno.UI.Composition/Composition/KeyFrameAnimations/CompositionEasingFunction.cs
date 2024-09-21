using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Composition;

public partial class CompositionEasingFunction
{
	internal CompositionEasingFunction() { }

	internal CompositionEasingFunction(Compositor owner) : base(owner) { }

	public static CubicBezierEasingFunction CreateCubicBezierEasingFunction(Compositor owner, Vector2 controlPoint1, Vector2 controlPoint2)
		=> new(owner, controlPoint1, controlPoint2);

	public static LinearEasingFunction CreateLinearEasingFunction(Compositor owner)
		=> new(owner);

	public static StepEasingFunction CreateStepEasingFunction(Compositor owner, int stepCount)
		=> new(owner, stepCount);

	public static StepEasingFunction CreateStepEasingFunction(Compositor owner)
		=> new(owner);

	public static BackEasingFunction CreateBackEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, float amplitude)
		=> new(owner, mode, amplitude);

	public static BounceEasingFunction CreateBounceEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, int bounces, float bounciness)
		=> new(owner, mode, bounces, bounciness);

	public static CircleEasingFunction CreateCircleEasingFunction(Compositor owner, CompositionEasingFunctionMode mode)
		=> new(owner, mode);

	public static ElasticEasingFunction CreateElasticEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, int oscillations, float springiness)
		=> new(owner, mode, oscillations, springiness);

	public static ExponentialEasingFunction CreateExponentialEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, float exponent)
		=> new(owner, mode, exponent);

	public static PowerEasingFunction CreatePowerEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, float power)
		=> new(owner, mode, power);

	public static SineEasingFunction CreateSineEasingFunction(Compositor owner, CompositionEasingFunctionMode mode)
		=> new(owner, mode);

	internal virtual float Ease(float t) => t;

	internal virtual float EaseIn(float t) => t;

	internal virtual float Ease(float t, CompositionEasingFunctionMode mode) => mode switch
	{
		CompositionEasingFunctionMode.In => EaseIn(t),
		CompositionEasingFunctionMode.Out => 1.0f - EaseIn(1.0f - t),
		CompositionEasingFunctionMode.InOut => (t < 0.5f) ?
											EaseIn(t * 2.0f) * 0.5f :
											(1.0f - EaseIn((1.0f - t) * 2.0f)) * 0.5f + 0.5f,
		_ => t,
	};
}
