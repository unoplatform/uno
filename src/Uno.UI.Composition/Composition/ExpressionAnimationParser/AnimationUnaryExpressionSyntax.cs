using System;
using System.Numerics;

namespace Windows.UI.Composition;

internal sealed class AnimationUnaryExpressionSyntax : AnimationExpressionSyntax
{
	private readonly ExpressionAnimationToken _operatorToken;
	private readonly AnimationExpressionSyntax _operand;

	public AnimationUnaryExpressionSyntax(ExpressionAnimationToken operatorToken, AnimationExpressionSyntax operand)
	{
		_operatorToken = operatorToken;
		_operand = operand;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		var value = _operand.Evaluate(expressionAnimation);
		if (_operatorToken.Kind == ExpressionAnimationTokenKind.PlusToken)
		{
			return value;
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.MinusToken)
		{
			return value switch
			{
				decimal valueDecimal => -valueDecimal,
				float valueFloat => -valueFloat,
				double valueDouble => -valueDouble,
				long valueLong => -valueLong,
				int valueInt => -valueInt,
				short valueShort => -valueShort,
				byte valueByte => -valueByte,
				Vector2 valueVector2 => -valueVector2,
				Vector3 valueVector3 => -valueVector3,
				_ => throw new ArgumentException($"Unable to evaluate unary minus expression for type '{value.GetType()}'."),
			};
		}

		throw new ArgumentException($"Unable to evaluate unary expression for operator: '{_operatorToken.Kind}'.");
	}
}
