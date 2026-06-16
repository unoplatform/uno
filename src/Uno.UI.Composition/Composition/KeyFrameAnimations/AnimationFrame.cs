using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Composition;

internal struct AnimationKeyFrame<T>
{
	public T Value;
	public CompositionEasingFunction EasingFunction;

	// When set, the keyframe's value is computed at evaluation time from ParsedExpression instead
	// of Value. The expression has access to the same reference parameters as the parent animation.
	public string Expression;

	// The parsed form of Expression, built once at insertion so per-frame evaluation never
	// re-tokenizes the string. Null for literal keyframes.
	public AnimationExpressionSyntax ParsedExpression;
}
