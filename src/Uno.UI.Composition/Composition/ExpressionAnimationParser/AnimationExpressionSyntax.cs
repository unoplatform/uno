#nullable enable

using System;

namespace Windows.UI.Composition;

internal abstract class AnimationExpressionSyntax : IDisposable
{
	public virtual void Dispose() { }
	public abstract object Evaluate(ExpressionAnimation expressionAnimation);
}
