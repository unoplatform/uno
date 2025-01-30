using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class ExponentialEasingFunction
{
	public CompositionEasingFunctionMode Mode { get; }
	public float Exponent { get; }

	internal ExponentialEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, float exponent) : base(owner)
	{
		Mode = mode;
		Exponent = float.IsFinite(exponent) ? exponent : 0.0f;
	}

	internal override float Ease(float t) => Ease(t, Mode);

	internal override float EaseIn(float t)
	{
		if (CompositionMathHelpers.IsCloseRealZero(Exponent))
		{
			return t;
		}
		else
		{
			return (MathF.Exp(Exponent * t) - 1) / (MathF.Exp(Exponent) - 1);
		}
	}
}
