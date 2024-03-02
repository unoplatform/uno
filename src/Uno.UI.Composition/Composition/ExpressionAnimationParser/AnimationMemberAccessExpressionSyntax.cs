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

		var propertySet = leftValue as CompositionPropertySet;
		propertySet ??= (leftValue as CompositionObject).Properties;

		if (propertySet is not null)
		{
			if (propertySet.TryGetBoolean(identifierName, out var @bool) == CompositionGetValueStatus.Succeeded)
			{
				return @bool;
			}
			else if (propertySet.TryGetColor(identifierName, out var color) == CompositionGetValueStatus.Succeeded)
			{
				return color;
			}
			else if (propertySet.TryGetMatrix3x2(identifierName, out var matrix3x2) == CompositionGetValueStatus.Succeeded)
			{
				return matrix3x2;
			}
			else if (propertySet.TryGetMatrix4x4(identifierName, out var matrix4x4) == CompositionGetValueStatus.Succeeded)
			{
				return matrix4x4;
			}
			else if (propertySet.TryGetQuaternion(identifierName, out var quaternion) == CompositionGetValueStatus.Succeeded)
			{
				return quaternion;
			}
			else if (propertySet.TryGetScalar(identifierName, out var @float) == CompositionGetValueStatus.Succeeded)
			{
				return @float;
			}
			else if (propertySet.TryGetVector2(identifierName, out var vector2) == CompositionGetValueStatus.Succeeded)
			{
				return vector2;
			}
			else if (propertySet.TryGetVector3(identifierName, out var vector3) == CompositionGetValueStatus.Succeeded)
			{
				return vector3;
			}
			else if (propertySet.TryGetVector4(identifierName, out var vector4) == CompositionGetValueStatus.Succeeded)
			{
				return vector4;
			}
		}

		throw new ArgumentException($"Cannot find property or field named '{identifierName}' on object of type '{leftType}'.");
	}
}
