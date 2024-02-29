#nullable enable

using System;

namespace Microsoft.UI.Composition;

internal class AnimationIdentifierNameSyntax : AnimationExpressionSyntax
{
	private object? _result;
	private ExpressionAnimation? _expressionAnimation;

	public ExpressionAnimationToken Identifier { get; }

	public AnimationIdentifierNameSyntax(ExpressionAnimationToken identifier)
	{
		Identifier = identifier;
	}

	public override object Evaluate(ExpressionAnimation expressionAnimation)
	{
		if (_expressionAnimation is not null)
		{
			return _result!;
		}

		_expressionAnimation = expressionAnimation;

		var identifierValue = (string)Identifier.Value!;

		if (expressionAnimation.ReferenceParameters.TryGetValue(identifierValue, out var value))
		{
			value.AddContext(expressionAnimation, null);
			_result = value;
			return value;
		}

		if (expressionAnimation.ScalarParameters.TryGetValue(identifierValue, out var scalarValue))
		{
			_result = scalarValue;
			return scalarValue;
		}

		if (identifierValue.Equals("Pi", StringComparison.Ordinal))
		{
			_result = Math.PI;
			return Math.PI;
		}

		throw new ArgumentException($"Unrecognized identifier '{Identifier.Value}'.");
	}

	public override void Dispose()
	{
		if (_expressionAnimation is not null && _result is not null)
		{
			(_result as CompositionObject)?.RemoveContext(_expressionAnimation, null);
		}

		_result = null;
		_expressionAnimation = null;
	}
}
