using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Composition;

public partial class SineEasingFunction
{
	public CompositionEasingFunctionMode Mode { get; }

	internal SineEasingFunction(Compositor owner, CompositionEasingFunctionMode mode) : base(owner)
	{
		Mode = mode;
	}

	internal override float Ease(float t) => Ease(t, Mode);

	internal override float EaseIn(float t)
		=> 1.0f - MathF.Sin(MathF.PI * 0.5f * (1.0f - t));
}
