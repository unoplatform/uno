using System;

namespace Microsoft.UI.Composition;

internal class AnimationMemberAccessExpressionSyntax : AnimationExpressionSyntax
{
	private readonly AnimationExpressionSyntax _expression;
	private readonly ExpressionAnimationToken _identifier;

	public AnimationMemberAccessExpressionSyntax(AnimationExpressionSyntax expression, ExpressionAnimationToken identifier)
	{
		_expression = expression;
		_identifier = identifier;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		var leftValue = _expression.Evaluate(expressionAnimation);
		var identifierName = (string)_identifier.Value;
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
