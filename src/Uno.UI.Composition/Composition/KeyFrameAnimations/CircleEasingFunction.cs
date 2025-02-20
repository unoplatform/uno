using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Composition;

public partial class CircleEasingFunction
{
	public CompositionEasingFunctionMode Mode { get; }

	internal CircleEasingFunction(Compositor owner, CompositionEasingFunctionMode mode) : base(owner)
	{
		Mode = mode;
	}

	internal override float Ease(float t) => Ease(t, Mode);

	internal override float EaseIn(float t) => 1.0f - MathF.Sqrt(1.0f - t * t);
}
