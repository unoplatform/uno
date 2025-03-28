#nullable enable

// Mostly inspired from Minsk (https://github.com/terrajobst/minsk/)

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Windows.UI.Composition;

internal sealed class ExpressionAnimationLexer
{
	private readonly string _text;
	private int _position;

	private static Dictionary<char, ExpressionAnimationTokenKind> _knownTokens = new()
	{
		['+'] = ExpressionAnimationTokenKind.PlusToken,
		['-'] = ExpressionAnimationTokenKind.MinusToken,
		['*'] = ExpressionAnimationTokenKind.MultiplyToken,
		['/'] = ExpressionAnimationTokenKind.DivisionToken,
		['.'] = ExpressionAnimationTokenKind.DotToken,
		[','] = ExpressionAnimationTokenKind.CommaToken,
		['('] = ExpressionAnimationTokenKind.OpenParenToken,
		[')'] = ExpressionAnimationTokenKind.CloseParenToken,
		['?'] = ExpressionAnimationTokenKind.QuestionMarkToken,
		[':'] = ExpressionAnimationTokenKind.ColonToken,
	};

	public ExpressionAnimationLexer(string text)
	{
		_text = text;
	}

	private char Current => Peek(0);

	private char Peek(int i)
		=> _position + i < _text.Length ? _text[_position + i] : '\0';

	public ExpressionAnimationToken? EatToken()
	{
		while (char.IsWhiteSpace(Current))
		{
			_position++;
		}

		if (_position >= _text.Length)
		{
			return null;
		}

		if (char.IsDigit(Current))
		{
			int start = _position;
			while (char.IsDigit(Current))
			{
				_position++;
			}

			if (Current == '.' && char.IsDigit(Peek(1)))
			{
				_position++;
			}

			while (char.IsDigit(Current))
			{
				_position++;
			}

			string numberText = _text[start.._position];
			object value;
			value = float.Parse(numberText, CultureInfo.InvariantCulture);

			if (Current == 'f' || Current == 'F')
			{
				_position++;
			}

			return new ExpressionAnimationToken(ExpressionAnimationTokenKind.NumericLiteralToken, value);
		}

		// TODO: Check if ExpressionAnimation should support postfix/prefix ++ or --.

		if (_knownTokens.TryGetValue(Current, out var kind))
		{
			_position++;
			return new ExpressionAnimationToken(kind, null);
		}

		if (Current == '>')
		{
			_position++;
			if (Current == '=')
			{
				_position++;
				return new ExpressionAnimationToken(ExpressionAnimationTokenKind.GreaterThanEqualsToken, null);
			}

			return new ExpressionAnimationToken(ExpressionAnimationTokenKind.GreaterThanToken, null);
		}

		if (Current == '<')
		{
			_position++;
			if (Current == '=')
			{
				_position++;
				return new ExpressionAnimationToken(ExpressionAnimationTokenKind.LessThanEqualsToken, null);
			}

			return new ExpressionAnimationToken(ExpressionAnimationTokenKind.LessThanToken, null);
		}

		if (char.IsLetter(Current))
		{
			int start = _position;
			while (char.IsLetterOrDigit(Current) || Current == '_')
			{
				_position++;
			}

			string identifierText = _text[start.._position];

			return new ExpressionAnimationToken(ExpressionAnimationTokenKind.IdentifierToken, identifierText);
		}

		throw new ArgumentException($"Unexpected character '{Current}'.");
	}
}
