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

	public override object Evaluate(CompositionAnimation expressionAnimation)
	{
		var leftValue = _left.Evaluate(expressionAnimation);
		var rightValue = _right.Evaluate(expressionAnimation);
		if (_operatorToken.Kind == ExpressionAnimationTokenKind.PlusToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat + rightFloat,
				(byte leftByte, byte rightByte) => leftByte + rightByte,
				(int leftInt, int rightInt) => leftInt + rightInt,
				(int leftInt, float rightFloat) => leftInt + rightFloat,
				(float leftFloat, int rightInt) => leftFloat + rightInt,
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
				(byte leftByte, byte rightByte) => leftByte - rightByte,
				(int leftInt, int rightInt) => leftInt - rightInt,
				(int leftInt, float rightFloat) => leftInt - rightFloat,
				(float leftFloat, int rightInt) => leftFloat - rightInt,
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
				(byte leftByte, byte rightByte) => leftByte * rightByte,
				(short leftShort, short rightShort) => leftShort * rightShort,
				(int leftInt, int rightInt) => leftInt * rightInt,
				(int leftInt, float rightFloat) => leftInt * rightFloat,
				(float leftFloat, int rightInt) => leftFloat * rightInt,
				(Vector3 leftVector3, Vector3 rightVector3) => leftVector3 * rightVector3,
				(Vector2 leftVector2, Vector2 rightVector2) => leftVector2 * rightVector2,
				(float leftFloat, Vector2 rightVector2) => leftFloat * rightVector2,
				(Vector2 leftVector2, float rightFloat) => leftVector2 * rightFloat,
				(int leftInt, Vector2 rightVector2) => (float)leftInt * rightVector2,
				(Vector2 leftVector2, int rightInt) => leftVector2 * (float)rightInt,
				(float leftFloat, Vector3 rightVector3) => leftFloat * rightVector3,
				(Vector3 leftVector3, float rightFloat) => leftVector3 * rightFloat,
				_ => throw new ArgumentException($"Cannot evaluate binary * between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.DivisionToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat / rightFloat,
				(byte leftByte, byte rightByte) => leftByte / rightByte,
				(short leftShort, short rightShort) => leftShort / rightShort,
				(int leftInt, int rightInt) => leftInt / rightInt,
				(int leftInt, float rightFloat) => leftInt / rightFloat,
				(float leftFloat, int rightInt) => leftFloat / rightInt,
				(Vector3 leftVector3, Vector3 rightVector3) => leftVector3 / rightVector3,
				(Vector2 leftVector2, Vector2 rightVector2) => leftVector2 / rightVector2,
				_ => throw new ArgumentException($"Cannot evaluate binary / between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.GreaterThanToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat > rightFloat,
				(byte leftByte, byte rightByte) => leftByte > rightByte,
				(short leftShort, short rightShort) => leftShort > rightShort,
				(int leftInt, int rightInt) => leftInt > rightInt,
				(int leftInt, float rightFloat) => leftInt > rightFloat,
				(float leftFloat, int rightInt) => leftFloat > rightInt,
				_ => throw new ArgumentException($"Cannot evaluate binary > between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.GreaterThanEqualsToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat >= rightFloat,
				(byte leftByte, byte rightByte) => leftByte >= rightByte,
				(short leftShort, short rightShort) => leftShort >= rightShort,
				(int leftInt, int rightInt) => leftInt >= rightInt,
				(int leftInt, float rightFloat) => leftInt >= rightFloat,
				(float leftFloat, int rightInt) => leftFloat >= rightInt,
				_ => throw new ArgumentException($"Cannot evaluate binary >= between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.LessThanToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat < rightFloat,
				(byte leftByte, byte rightByte) => leftByte < rightByte,
				(short leftShort, short rightShort) => leftShort < rightShort,
				(int leftInt, int rightInt) => leftInt < rightInt,
				(int leftInt, float rightFloat) => leftInt < rightFloat,
				(float leftFloat, int rightInt) => leftFloat < rightInt,
				_ => throw new ArgumentException($"Cannot evaluate binary < between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}
		else if (_operatorToken.Kind == ExpressionAnimationTokenKind.LessThanEqualsToken)
		{
			return (leftValue, rightValue) switch
			{
				(float leftFloat, float rightFloat) => leftFloat <= rightFloat,
				(byte leftByte, byte rightByte) => leftByte <= rightByte,
				(short leftShort, short rightShort) => leftShort <= rightShort,
				(int leftInt, int rightInt) => leftInt <= rightInt,
				(int leftInt, float rightFloat) => leftInt <= rightFloat,
				(float leftFloat, int rightInt) => leftFloat <= rightInt,
				_ => throw new ArgumentException($"Cannot evaluate binary <= between types '{leftValue.GetType()}' and '{rightValue.GetType()}'.")
			};
		}

		throw new ArgumentException($"Unable to binary expression for operator '{_operatorToken.Kind}'.");
	}
}
