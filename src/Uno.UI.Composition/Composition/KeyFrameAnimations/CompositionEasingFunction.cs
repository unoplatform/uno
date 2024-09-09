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

	internal virtual float Ease(float t) => t;
}
