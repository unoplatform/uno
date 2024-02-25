namespace Microsoft.UI.Composition;

internal enum ExpressionAnimationTokenKind
{
	DotToken,
	CommaToken,
	PlusToken,
	MinusToken,
	MultiplyToken,
	DivisionToken,
	OpenParenToken,
	CloseParenToken,
	IdentifierToken,
	NumericLiteralToken,
	// TODO: QuestionMarkToken and ColonToken to support ternary operators.
}
