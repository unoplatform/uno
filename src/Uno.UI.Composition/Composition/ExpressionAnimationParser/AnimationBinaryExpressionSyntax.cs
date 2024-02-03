using System;
using System.Numerics;

namespace Microsoft.UI.Composition;

internal class AnimationBinaryExpressionSyntax : AnimationExpressionSyntax
{
	private readonly AnimationExpressionSyntax _left;
	private readonly ExpressionAnimationToken _operatorToken;
	private readonly AnimationExpressionSyntax _right;

	public AnimationBinaryExpressionSyntax(AnimationExpressionSyntax left, ExpressionAnimationToken operatorToken, AnimationExpressionSyntax right)
	{
		_left = left;
		_operatorToken = operatorToken;
		_right = right;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		var leftValue = _left.Evaluate(expressionAnimation);
		var rightValue = _right.Evaluate(expressionAnimation);
		if (_operatorToken.Kind == ExpressionAnimationTokenKind.PlusToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat + rightFloat,
				(double leftDouble, double rightDouble) => leftDouble + rightDouble,
				(decimal leftDecimal, decimal rightDecimal) => leftDecimal + rightDecimal,
				(byte leftByte, int rightByte) => leftByte + rightByte,
				(short leftShort, short rightShort) => leftShort + rightShort,
				(int leftInt, int rightInt) => leftInt + rightInt,
				(Vector3 leftVector3, Vector3 rightVector3) => leftVector3 + rightVector3,
				(Vector2 leftVector2, Vector2 rightVector2) => leftVector2 + rightVector2,
				_ => throw new ArgumentException($"Cannot evaluate binary + between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.MinusToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat - rightFloat,
				(double leftDouble, double rightDouble) => leftDouble - rightDouble,
				(decimal leftDecimal, decimal rightDecimal) => leftDecimal - rightDecimal,
				(byte leftByte, int rightByte) => leftByte - rightByte,
				(short leftShort, short rightShort) => leftShort - rightShort,
				(int leftInt, int rightInt) => leftInt - rightInt,
				(Vector3 leftVector3, Vector3 rightVector3) => leftVector3 - rightVector3,
				(Vector2 leftVector2, Vector2 rightVector2) => leftVector2 - rightVector2,
				_ => throw new ArgumentException($"Cannot evaluate binary - between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.MultiplyToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat * rightFloat,
				(double leftDouble, double rightDouble) => leftDouble * rightDouble,
				(decimal leftDecimal, decimal rightDecimal) => leftDecimal * rightDecimal,
				(byte leftByte, int rightByte) => leftByte * rightByte,
				(short leftShort, short rightShort) => leftShort * rightShort,
				(int leftInt, int rightInt) => leftInt * rightInt,
				(Vector3 leftVector3, Vector3 rightVector3) => leftVector3 * rightVector3,
				(Vector2 leftVector2, Vector2 rightVector2) => leftVector2 * rightVector2,
				_ => throw new ArgumentException($"Cannot evaluate binary * between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.DivisionToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat / rightFloat,
				(double leftDouble, double rightDouble) => leftDouble / rightDouble,
				(decimal leftDecimal, decimal rightDecimal) => leftDecimal / rightDecimal,
				(byte leftByte, byte rightByte) => leftByte / rightByte,
				(short leftShort, short rightShort) => leftShort / rightShort,
				(int leftInt, int rightInt) => leftInt / rightInt,
				(Vector3 leftVector3, Vector3 rightVector3) => leftVector3 / rightVector3,
				(Vector2 leftVector2, Vector2 rightVector2) => leftVector2 / rightVector2,
				_ => throw new ArgumentException($"Cannot evaluate binary / between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}

		throw new ArgumentException($"Unable to binary expression for operator '{_operatorToken.Kind}'.");
	}
}
