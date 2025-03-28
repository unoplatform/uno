#nullable enable

// Mostly inspired from Minsk (https://github.com/terrajobst/minsk/)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Windows.UI.Composition;

internal sealed class ExpressionAnimationParser
{
	private readonly ImmutableArray<ExpressionAnimationToken> _tokens;
	private int _position;

	public ExpressionAnimationParser(string text)
	{
		var tokensBuilder = ImmutableArray.CreateBuilder<ExpressionAnimationToken>();
		var lexer = new ExpressionAnimationLexer(text);

		ExpressionAnimationToken? token;

		while ((token = lexer.EatToken()) is not null)
		{
			tokensBuilder.Add(token.Value);
		}

		_tokens = tokensBuilder.ToImmutable();
	}

	private ExpressionAnimationToken Current => _tokens[_position];

	private bool HasCurrent => _position < _tokens.Length;

	public AnimationExpressionSyntax Parse()
	{
		var expression = ParseExpression();

		if (HasCurrent)
		{
			throw new ArgumentException($"Unexpected token {Current.Kind}.");
		}

		return expression;
	}

	internal static int GetBinaryPrecedence(ExpressionAnimationToken token)
	{
		return token.Kind switch
		{
			ExpressionAnimationTokenKind.QuestionMarkToken => 1,
			ExpressionAnimationTokenKind.LessThanToken or ExpressionAnimationTokenKind.LessThanEqualsToken or ExpressionAnimationTokenKind.GreaterThanToken or ExpressionAnimationTokenKind.GreaterThanEqualsToken => 2,
			ExpressionAnimationTokenKind.PlusToken or ExpressionAnimationTokenKind.MinusToken => 3,
			ExpressionAnimationTokenKind.MultiplyToken or ExpressionAnimationTokenKind.DivisionToken => 4,
			_ => 0
		};
	}

	internal static int GetUnaryPrecedence(ExpressionAnimationToken token)
	{
		return token.Kind switch
		{
			ExpressionAnimationTokenKind.PlusToken or ExpressionAnimationTokenKind.MinusToken => 5,
			_ => 0
		};
	}

	private AnimationExpressionSyntax ParseExpression(int parentPrecedence = 0)
	{
		AnimationExpressionSyntax left;

		var unaryPrecedence = GetUnaryPrecedence(Current);
		if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
		{
			var operatorToken = NextToken();
			var operand = ParseExpression(unaryPrecedence);
			left = new AnimationUnaryExpressionSyntax(operatorToken, operand);
		}
		else
		{
			left = ParsePrimaryExpression();
		}

		while (HasCurrent)
		{
			var precedence = GetBinaryPrecedence(Current);
			if (precedence == 0 || precedence <= parentPrecedence)
			{
				break;
			}

			if (HasCurrent && Current.Kind == ExpressionAnimationTokenKind.QuestionMarkToken)
			{
				_ = NextToken();
				var whenTrue = ParseExpression();
				_ = Match(ExpressionAnimationTokenKind.ColonToken);
				var whenFalse = ParseExpression();
				left = new AnimationTernaryExpressionSyntax(left, whenTrue, whenFalse);
			}
			else
			{
				var operatorToken = NextToken();
				var right = ParseExpression(precedence);
				left = new AnimationBinaryExpressionSyntax(left, operatorToken, right);
			}
		}

		return left;
	}

	private AnimationExpressionSyntax ParsePrimaryExpression()
	{
		if (Current.Kind == ExpressionAnimationTokenKind.OpenParenToken)
		{
			_ = NextToken(); // open
			var expression = ParseExpression();
			Match(ExpressionAnimationTokenKind.CloseParenToken);
			return new AnimationParenthesizedExpressionSyntax(expression);
		}

		if (Current.Kind == ExpressionAnimationTokenKind.NumericLiteralToken)
		{
			return new AnimationNumericExpressionSyntax(NextToken());
		}

		var identifierToken = Match(ExpressionAnimationTokenKind.IdentifierToken);

		AnimationExpressionSyntax identifierOrMemberAccess = new AnimationIdentifierNameSyntax(identifierToken);
		while (NextIsMemberAccess())
		{
			_ = NextToken(); // DotToken.
			var identifier = NextToken();
			identifierOrMemberAccess = new AnimationMemberAccessExpressionSyntax(identifierOrMemberAccess, identifier);
		}

		if (HasCurrent && Current.Kind == ExpressionAnimationTokenKind.OpenParenToken)
		{
			_ = NextToken();

			// Function call.
			var argumentsBuilder = ImmutableArray.CreateBuilder<AnimationExpressionSyntax>();
			if (Current.Kind != ExpressionAnimationTokenKind.CloseParenToken)
			{
				argumentsBuilder.Add(ParseExpression());
				while (Current.Kind == ExpressionAnimationTokenKind.CommaToken)
				{
					_ = NextToken(); // CommaToken
					argumentsBuilder.Add(ParseExpression());
				}
			}

			Match(ExpressionAnimationTokenKind.CloseParenToken);

			return new AnimationFunctionCallSyntax(identifierOrMemberAccess, argumentsBuilder.ToImmutable());
		}

		return identifierOrMemberAccess;
	}

	private ExpressionAnimationToken NextToken()
	{
		var current = Current;
		_position++;
		return current;
	}

	private bool NextIsMemberAccess()
	{
		if (_position + 1 < _tokens.Length)
		{
			return _tokens[_position].Kind == ExpressionAnimationTokenKind.DotToken &&
				_tokens[_position + 1].Kind == ExpressionAnimationTokenKind.IdentifierToken;
		}

		return false;
	}

	private ExpressionAnimationToken Match(ExpressionAnimationTokenKind kind)
	{
		if (Current.Kind == kind)
		{
			return NextToken();
		}

		throw new ArgumentException($"Expected token of kind '{kind}'. Found '{Current.Kind}'.");
	}
}
