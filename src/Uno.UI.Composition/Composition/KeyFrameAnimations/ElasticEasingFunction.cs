using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class ElasticEasingFunction
{
	public CompositionEasingFunctionMode Mode { get; }
	public int Oscillations { get; }
	public float Springiness { get; }

	internal ElasticEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, int oscillations, float springiness) : base(owner)
	{
		Mode = mode;
		Oscillations = Math.Max(0, oscillations);
		Springiness = float.IsFinite(springiness) ? springiness : 0.0f;
	}

	internal override float Ease(float t) => Ease(t, Mode);

	internal override float EaseIn(float t)
	{
		var amplitude = CompositionMathHelpers.IsCloseRealZero(Springiness) ? t :
						(MathF.Exp(Springiness * t) - 1.0f) / (MathF.Exp(Springiness) - 1.0f);

		return amplitude * MathF.Sin(t * (2.0f * MathF.PI * Oscillations + MathF.PI / 2.0f));
	}
}
