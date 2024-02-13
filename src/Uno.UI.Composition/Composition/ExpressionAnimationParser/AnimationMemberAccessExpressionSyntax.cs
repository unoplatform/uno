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
		var leftValue = Expression.Evaluate(expressionAnimation);
		var identifierName = (string)Identifier.Value;
		var leftType = leftValue.GetType();
		if (leftType.GetProperty(identifierName) is { } property)
		{
			return property.GetValue(leftValue);
		}
		else if (leftType.GetField(identifierName) is { } field)
		{
			return field.GetValue(leftValue);
		}

		throw new ArgumentException($"Cannot find property or field named '{identifierName}' on object of type '{leftType}'.");
	}
}
