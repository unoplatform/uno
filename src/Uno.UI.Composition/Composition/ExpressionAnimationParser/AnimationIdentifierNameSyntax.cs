#nullable enable

using System;

namespace Microsoft.UI.Composition;

internal class AnimationIdentifierNameSyntax : AnimationExpressionSyntax
{
	private object? _result;
	private CompositionAnimation? _expressionAnimation;

	public ExpressionAnimationToken Identifier { get; }

	public AnimationIdentifierNameSyntax(ExpressionAnimationToken identifier)
	{
		Identifier = identifier;
	}

	public override object Evaluate(CompositionAnimation expressionAnimation)
	{
		if (_expressionAnimation is not null)
		{
			return _result!;
		}

		_expressionAnimation = expressionAnimation;

		var identifierValue = (string)Identifier.Value!;

		if (expressionAnimation.ReferenceParameters.TryGetValue(identifierValue, out var value))
		{
			// Only ExpressionAnimation receives context propagation; key-frame animations re-evaluate
			// the expression on demand, so we don't need to register for property-changed callbacks.
			if (expressionAnimation is ExpressionAnimation)
			{
				value.AddContext(expressionAnimation, null);
			}
			_result = value;
		}
		else if (expressionAnimation.ScalarParameters.TryGetValue(identifierValue, out var scalarValue))
		{
			_result = scalarValue;
		}
		else if (expressionAnimation.Vector2Parameters.TryGetValue(identifierValue, out var vector2))
		{
			_result = vector2;
		}
		else if (expressionAnimation.Vector3Parameters.TryGetValue(identifierValue, out var vector3))
		{
			_result = vector3;
		}
		else if (identifierValue.Equals("Pi", StringComparison.OrdinalIgnoreCase))
		{
			_result = (float)Math.PI;
		}
		else if (identifierValue.Equals("True", StringComparison.OrdinalIgnoreCase))
		{
			_result = true;
		}
		else if (identifierValue.Equals("False", StringComparison.OrdinalIgnoreCase))
		{
			_result = false;
		}
		else
		{
			throw new ArgumentException($"Unrecognized identifier '{Identifier.Value}'.");
		}

		return _result;
	}

	public override void Dispose()
	{
		if (_expressionAnimation is ExpressionAnimation expressionAnimation && _result is not null)
		{
			(_result as CompositionObject)?.RemoveContext(expressionAnimation, null);
		}

		_result = null;
		_expressionAnimation = null;
	}
}
