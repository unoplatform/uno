using System;

namespace Microsoft.UI.Composition;

internal class AnimationIdentifierNameSyntax : AnimationExpressionSyntax
{
	public ExpressionAnimationToken Identifier { get; }

	public AnimationIdentifierNameSyntax(ExpressionAnimationToken identifier)
	{
		Identifier = identifier;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		if (expressionAnimation.ReferenceParameters.TryGetValue((string)Identifier.Value, out var value))
		{
			value.AddContext(expressionAnimation, "HelloMyProperty");
			return value;
		}

		throw new ArgumentException($"Unrecognized identifier '{Identifier.Value}'.");
	}
}
