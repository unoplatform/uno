#nullable enable

using System;

namespace Microsoft.UI.Composition;

internal class AnimationIdentifierNameSyntax : AnimationExpressionSyntax
{
	private CompositionObject? _result;
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

		if (expressionAnimation.ReferenceParameters.TryGetValue((string)Identifier.Value!, out var value))
		{
			value.AddContext(expressionAnimation, null);
			_result = value;
			return value;
		}

		throw new ArgumentException($"Unrecognized identifier '{Identifier.Value}'.");
	}

	public override void Dispose()
	{
		if (_expressionAnimation is not null && _result is not null)
		{
			_result.RemoveContext(_expressionAnimation, null);
		}

		_result = null;
		_expressionAnimation = null;
	}
}
