using System;
using System.Numerics;

namespace Windows.UI.Composition;

internal sealed class AnimationTernaryExpressionSyntax : AnimationExpressionSyntax
{
	private readonly AnimationExpressionSyntax _condition;
	private readonly AnimationExpressionSyntax _whenTrue;
	private readonly AnimationExpressionSyntax _whenFalse;

	public AnimationTernaryExpressionSyntax(AnimationExpressionSyntax condition, AnimationExpressionSyntax whenTrue, AnimationExpressionSyntax whenFalse)
	{
		_condition = condition;
		_whenTrue = whenTrue;
		_whenFalse = whenFalse;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		var value = _condition.Evaluate(expressionAnimation);
		if (value is not bool valueBool)
		{
			throw new Exception($"Ternary expression condition evaluated to '{value}'. It must evaluate to a bool value.");
		}

		if (valueBool)
		{
			return _whenTrue.Evaluate(expressionAnimation);
		}

		return _whenFalse.Evaluate(expressionAnimation);
	}
}
