#nullable enable

namespace Windows.UI.Composition;

internal readonly struct ExpressionAnimationToken
{
	public ExpressionAnimationTokenKind Kind { get; }

	public object? Value { get; }

	public ExpressionAnimationToken(ExpressionAnimationTokenKind kind, object? value)
	{
		Kind = kind;
		Value = value;
	}
}
