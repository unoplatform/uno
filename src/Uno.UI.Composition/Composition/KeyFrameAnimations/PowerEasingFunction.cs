using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Composition;

public partial class PowerEasingFunction
{
	public CompositionEasingFunctionMode Mode { get; }
	public float Power { get; }

	internal PowerEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, float power) : base(owner)
	{
		Mode = mode;
		Power = float.IsFinite(power) ? power : 0.0f;
	}

	internal override float Ease(float t) => Ease(t, Mode);

	internal override float EaseIn(float t)
		=> MathF.Pow(t, Power);
}
