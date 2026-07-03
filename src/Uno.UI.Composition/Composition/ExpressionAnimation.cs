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

		// Parse the expression once and reuse the tree across every target this animation is
		// started on. Re-parsing per Start would re-register the reference-parameter contexts each
		// time (AnimationIdentifierNameSyntax.Evaluate calls AddContext once per tree), so a single
		// referenced-object change would fan out to O(N^2) re-evaluations when the same animation is
		// shared across N targets, and every extra registration would leak on teardown.
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

	internal override void Stop()
	{
		base.Stop();

		// Only tear down the shared parse tree (whose Dispose removes the reference-parameter
		// contexts) once the animation has been stopped on every target it was started on.
		if (StartedObjectCount == 0)
		{
			_parsedExpression?.Dispose();
			_parsedExpression = null;
			_parsedExpressionText = null;
		}
	}
}
