#nullable enable

using System;

namespace Microsoft.UI.Composition;

public partial class ExpressionAnimation : CompositionAnimation
{
	private AnimationExpressionSyntax? _parsedExpression;
	private string _expression = string.Empty;

	internal ExpressionAnimation(Compositor compositor) : base(compositor)
	{
	}

	public string Expression
	{
		get => _expression;
		set => _expression = value ?? throw new ArgumentException();
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		if (_parsedExpression is not null)
		{
			RaisePropertyChanged();
		}
	}

	internal override object? Start()
	{
		if (Expression.Length == 0)
		{
			throw new InvalidOperationException("Property 'Expression' should not be empty when starting an ExpressionAnimation");
		}

		// TODO: Check what to do if this is a second Start call and we already have non-null _parsedExpression;
		_parsedExpression = new ExpressionAnimationParser(Expression).Parse();

		return _parsedExpression.Evaluate(this);
	}

	internal override object? Evaluate()
		=> _parsedExpression?.Evaluate(this);

	internal override void Stop()
	{
		_parsedExpression?.Dispose();
		_parsedExpression = null;
	}
}
