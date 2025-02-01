using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class BackEasingFunction
{
	public CompositionEasingFunctionMode Mode { get; }
	public float Amplitude { get; }

	internal BackEasingFunction(Compositor owner, CompositionEasingFunctionMode mode, float amplitude) : base(owner)
	{
		Mode = mode;
		Amplitude = float.IsFinite(amplitude) && amplitude >= 0 ? amplitude : 0.0f;
	}

	internal override float Ease(float t) => Ease(t, Mode);

	internal override float EaseIn(float t)
		=> MathF.Pow(t, 3) - t * Amplitude * MathF.Sin(t * MathF.PI);
}
