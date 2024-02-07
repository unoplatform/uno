using System;

namespace Microsoft.UI.Composition;

internal class AnimationIdentifierNameSyntax : AnimationExpressionSyntax
{
	private bool _wasEvaluated;

	public ExpressionAnimationToken Identifier { get; }

	public AnimationIdentifierNameSyntax(ExpressionAnimationToken identifier)
	{
		Identifier = identifier;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		if (expressionAnimation.ReferenceParameters.TryGetValue((string)Identifier.Value, out var value))
		{
			if (!_wasEvaluated)
			{
				_wasEvaluated = true;
				value.AddContext(expressionAnimation, null);
			}

			return value;
		}

		throw new ArgumentException($"Unrecognized identifier '{Identifier.Value}'.");
	}
}
