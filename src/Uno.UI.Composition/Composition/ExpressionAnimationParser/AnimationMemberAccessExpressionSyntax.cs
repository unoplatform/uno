using System;
using System.Numerics;

using static Microsoft.UI.Composition.SubPropertyHelpers;

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

	public override object Evaluate(CompositionAnimation expressionAnimation)
	{
		var leftValue = Expression.Evaluate(expressionAnimation);
		var propertyName = (string)Identifier.Value;
		if (leftValue is CompositionAnimation animation)
		{
			if (propertyName.Equals("Target", StringComparison.OrdinalIgnoreCase))
			{
				// 'this.Target' is the object the animation is running on.
				return animation.AnimationTargetObject
					?? throw new ArgumentException("Cannot resolve 'this.Target': the animation has no target object.");
			}

			if (propertyName.Equals("StartingValue", StringComparison.OrdinalIgnoreCase))
			{
				return animation.StartingValue
					?? throw new ArgumentException("Cannot resolve 'this.StartingValue': the animated property has no value.");
			}

			if (propertyName.Equals("FinalValue", StringComparison.OrdinalIgnoreCase))
			{
				return animation.FinalValue
					?? throw new ArgumentException("Cannot resolve 'this.FinalValue': the animated property has no value.");
			}

			if (propertyName.Equals("CurrentValue", StringComparison.OrdinalIgnoreCase))
			{
				return animation.CurrentValue
					?? throw new ArgumentException("Cannot resolve 'this.CurrentValue': the animated property has no value.");
			}
		}

		if (leftValue is CompositionObject leftCompositionObject)
		{
			return leftCompositionObject.GetAnimatableProperty(propertyName, string.Empty);
		}
		else
		{
			return GetSubProperty(propertyName, leftValue);
		}

		throw new ArgumentException($"Cannot find evaluate property '{propertyName}' on object of type '{leftValue?.GetType()}'.");
	}
}
