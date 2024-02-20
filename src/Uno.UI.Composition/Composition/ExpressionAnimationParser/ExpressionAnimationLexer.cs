#nullable enable

// Mostly inspired from Minsk (https://github.com/terrajobst/minsk/)

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.UI.Composition;

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
			bool isFloatingPoint = false;
			while (char.IsDigit(Current))
			{
				_position++;
			}

			if (Current == '.' && char.IsDigit(Peek(1)))
			{
				_position++;
				isFloatingPoint = true;
			}

			while (char.IsDigit(Current))
			{
				_position++;
			}

			string numberText = _text[start.._position];
			object value;
			if (isFloatingPoint)
			{
				// TODO: Support literal suffixes. e.g, "1.5f" (for float) or "500l" (for long)
				value = double.Parse(numberText, CultureInfo.InvariantCulture);
			}
			else
			{
				value = int.Parse(numberText, CultureInfo.InvariantCulture);
			}

			return new ExpressionAnimationToken(ExpressionAnimationTokenKind.NumericLiteralToken, value);
		}

		// TODO: Check if ExpressionAnimation should support postfix/prefix ++ or --.

		if (_knownTokens.TryGetValue(Current, out var kind))
		{
			_position++;
			return new ExpressionAnimationToken(kind, null);
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
