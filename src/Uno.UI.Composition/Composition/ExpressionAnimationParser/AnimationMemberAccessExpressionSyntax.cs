using System;

namespace Microsoft.UI.Composition;

internal class AnimationMemberAccessExpressionSyntax : AnimationExpressionSyntax
{
	public AnimationMemberAccessExpressionSyntax(AnimationExpressionSyntax expression, ExpressionAnimationToken identifier)
	{
		Expression = expression;
		Identifier = identifier;
	}

	public AnimationExpressionSyntax Expression { get; }
	public ExpressionAnimationToken Identifier { get; }

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		if (Expression is AnimationMemberAccessExpressionSyntax expression)
		{
			var mostLeftValue = expression.Expression.Evaluate(expressionAnimation);
			var mainPropertyName = (string)expression.Identifier.Value;
			var subPropertyName = (string)Identifier.Value;
			if (mostLeftValue is CompositionObject mostLeftCompositionObject)
			{
				return mostLeftCompositionObject.GetAnimatableProperty(mainPropertyName, subPropertyName);
			}

			throw new Exception($"Cannot evaluate '{mostLeftValue?.GetType()}.{mainPropertyName}.{subPropertyName}'");
		}

		var leftValue = Expression.Evaluate(expressionAnimation);
		var propertyName = (string)Identifier.Value;
		if (leftValue is CompositionObject leftCompositionObject)
		{
			return leftCompositionObject.GetAnimatableProperty(propertyName, string.Empty);
		}

		throw new ArgumentException($"Cannot find evaluate property '{propertyName}' on object of type '{leftValue?.GetType()}'.");
	}
}
