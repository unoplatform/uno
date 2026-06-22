#nullable enable

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

	// When set, the keyframe's value is evaluated from this expression instead of Value. Parsed once
	// at insertion so per-frame evaluation never re-tokenizes the string; null for literal keyframes.
	// The expression resolves the same reference parameters as the parent animation.
	public AnimationExpressionSyntax? ParsedExpression;
}
