#nullable enable

using System;

namespace Microsoft.UI.Composition;

internal abstract class AnimationExpressionSyntax : IDisposable
{
	public virtual void Dispose() { }
	public abstract object Evaluate(ExpressionAnimation expressionAnimation);
}
