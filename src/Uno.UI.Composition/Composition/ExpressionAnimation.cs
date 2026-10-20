#nullable enable

using System;

namespace Microsoft.UI.Composition;

public partial class ExpressionAnimation : CompositionAnimation
{
	private AnimationExpressionSyntax? _parsedExpression;
	private string? _parsedExpressionText;
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

		// Reuse the parse tree when Start runs again with an unchanged expression: re-parsing would
		// re-register the reference-parameter contexts (Evaluate calls AddContext once per tree) and
		// leak the previous registrations.
		if (_parsedExpression is null || !string.Equals(_parsedExpressionText, Expression, StringComparison.Ordinal))
		{
			_parsedExpression?.Dispose();
			_parsedExpression = new ExpressionAnimationParser(Expression).Parse();
			_parsedExpressionText = Expression;
		}

		return _parsedExpression.Evaluate(this);
	}

	internal override object? Evaluate()
		=> _parsedExpression?.Evaluate(this);

	// Snapshot the expression + reference parameters so each target this animation is started on
	// evaluates independently, even if the source instance is later reconfigured for another target.
	internal override CompositionAnimation CloneAnimation()
	{
		var clone = new ExpressionAnimation(Compositor) { Expression = Expression };
		CopyParametersTo(clone);
		return clone;
	}

	internal override void Stop()
	{
		base.Stop();

		// Disposing the parse tree removes its reference-parameter contexts, so only tear it down
		// once this instance is no longer started on any target.
		if (StartedObjectCount == 0)
		{
			_parsedExpression?.Dispose();
			_parsedExpression = null;
			_parsedExpressionText = null;
		}
	}
}
