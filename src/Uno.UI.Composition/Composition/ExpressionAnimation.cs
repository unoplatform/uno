#nullable enable

using System;

namespace Windows.UI.Composition;

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

	// ExpressionAnimation is re-evaluated on property changes, not on every render frame by the compositor.
	internal override bool IsTrackedByCompositor => false;

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		if (_parsedExpression is not null)
		{
			RaiseAnimationFrame();
		}
	}

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);

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
