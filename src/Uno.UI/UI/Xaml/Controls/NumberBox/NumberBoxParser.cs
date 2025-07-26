// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\NumberBox\NumberBoxParser.cpp, tag winui3/release/1.7.1, commit 5f27a786ac9

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation.Metadata;
using Windows.Globalization.NumberFormatting;

namespace Microsoft.UI.Xaml.Controls;

internal partial class NumberBoxParser
{
	static string c_numberBoxOperators = "+-*/^";

	// Returns list of MathTokens from expression input string. If there are any parsing errors, it returns an empty vector.
	private static List<MathToken> GetTokens(string input, INumberParser numberParser)
	{
		var tokens = new List<MathToken>();

		var expectNumber = true;
		while (input.Length > 0)
		{
			// Skip spaces
			var nextChar = input[0];
			if (nextChar != ' ')
			{
				if (expectNumber)
				{
					if (nextChar == '(')
					{
						// Open parens are also acceptable, but don't change the next expected token type.
						tokens.Add(new MathToken(MathTokenType.Parenthesis, nextChar));
					}
					else
					{
						var (value, charLength) = GetNextNumber(input, numberParser);

						if (charLength > 0)
						{
							tokens.Add(new MathToken(MathTokenType.Numeric, value));

							// UNO TODO: Was pointer manipulation, may be better off with Span<char>.
							input = input.Substring(charLength - 1);// advance the end of the token

							expectNumber = false; // next token should be an operator
						}
						else
						{
							// Error case -- next token is not a number
							return new List<MathToken>();
						}
					}
				}
				else
				{
					if (c_numberBoxOperators.IndexOf(nextChar) != -1)
					{
						tokens.Add(new MathToken(MathTokenType.Operator, nextChar));
						expectNumber = true; // next token should be a number
					}
					else if (nextChar == ')')
					{
						// Closed parens are also acceptable, but don't change the next expected token type.
						tokens.Add(new MathToken(MathTokenType.Parenthesis, nextChar));
					}
					else
					{
						// Error case -- could not evaluate part of the expression
						return new List<MathToken>();
					}
				}
			}

			// UNO TODO: Was pointer manipulation, may be better off with Span<char>.
			input = input.Substring(1);
		}

		return tokens;
	}

	// Attempts to parse a number from the beginning of the given input string. Returns the character size of the matched string.
	static (double, int) GetNextNumber(string input, INumberParser numberParser)
	{
		// Attempt to parse anything before an operator or space as a number
		var match = NextNumberParsing().Match(input);
		if (match.Success)
		{
			// Might be a number
			var matchLength = match.Groups[0].Length;
			var parsedNum = ApiInformation.IsTypePresent(numberParser?.GetType().FullName)
				? numberParser.ParseDouble(input.Substring(0, matchLength))
				: double.TryParse(input.AsSpan().Slice(0, matchLength), out var d)
					? (double?)d
					: null;

			if (parsedNum != null)
			{
				// Parsing was successful
				return (parsedNum.Value, matchLength);
			}
		}

		return (double.NaN, 0);
	}

	static int GetPrecedenceValue(char c)
	{
		int opPrecedence = 0;
		if (c == '*' || c == '/')
		{
			opPrecedence = 1;
		}
		else if (c == '^')
		{
			opPrecedence = 2;
		}

		return opPrecedence;
	}

	// Converts a list of tokens from infix format (e.g. "3 + 5") to postfix (e.g. "3 5 +")
	static List<MathToken> ConvertInfixToPostfix(List<MathToken> infixTokens)
	{
		List<MathToken> postfixTokens = new List<MathToken>();
		Stack<MathToken> operatorStack = new Stack<MathToken>();

		foreach (var token in infixTokens)
		{
			if (token.Type == MathTokenType.Numeric)
			{
				postfixTokens.Add(token);
			}
			else if (token.Type == MathTokenType.Operator)
			{
				while (operatorStack.Count != 0)
				{
					var top = operatorStack.Peek();
					if (top.Type != MathTokenType.Parenthesis && (GetPrecedenceValue(top.Char) >= GetPrecedenceValue(token.Char)))
					{
						postfixTokens.Add(operatorStack.Peek());
						operatorStack.Pop();
					}
					else
					{
						break;
					}
				}
				operatorStack.Push(token);
			}
			else if (token.Type == MathTokenType.Parenthesis)
			{
				if (token.Char == '(')
				{
					operatorStack.Push(token);
				}
				else
				{
					while (operatorStack.Count != 0 && operatorStack.Peek().Char != '(')
					{
						// Pop operators onto output until we reach a left paren
						postfixTokens.Add(operatorStack.Peek());
						operatorStack.Pop();
					}

					if (operatorStack.Count == 0)
					{
						// Broken parenthesis
						return new List<MathToken>();
					}

					// Pop left paren and discard
					operatorStack.Pop();
				}
			}
		}

		// Pop all remaining operators.
		while (operatorStack.Count != 0)
		{
			if (operatorStack.Peek().Type == MathTokenType.Parenthesis)
			{
				// Broken parenthesis
				return new List<MathToken>();
			}

			postfixTokens.Add(operatorStack.Peek());
			operatorStack.Pop();
		}

		return postfixTokens;
	}

	static double? ComputePostfixExpression(List<MathToken> tokens)
	{
		Stack<double> stack = new Stack<double>();

		foreach (var token in tokens)
		{
			if (token.Type == MathTokenType.Operator)
			{
				// There has to be at least two values on the stack to apply
				if (stack.Count < 2)
				{
					return null;
				}

				var op1 = stack.Peek();
				stack.Pop();

				var op2 = stack.Peek();
				stack.Pop();

				double result;

				switch (token.Char)
				{
					case '-':
						result = op2 - op1;
						break;

					case '+':
						result = op1 + op2;
						break;

					case '*':
						result = op1 * op2;
						break;

					case '/':
						if (op1 == 0)
						{
							// divide by zero
							return double.NaN;
						}
						else
						{
							result = op2 / op1;
						}
						break;

					case '^':
						result = Math.Pow(op2, op1);
						break;

					default:
						return null;
				}

				stack.Push(result);
			}
			else if (token.Type == MathTokenType.Numeric)
			{
				stack.Push(token.Value);
			}
		}

		// If there is more than one number on the stack, we didn't have enough operations, which is also an error.
		if (stack.Count != 1)
		{
			return null;
		}

		return stack.Peek();
	}

	public static double? Compute(string expr, INumberParser numberParser)
	{
		// Tokenize the input string
		var tokens = GetTokens(expr, numberParser);
		if (tokens.Count > 0)
		{
			// Rearrange to postfix notation
			var postfixTokens = ConvertInfixToPostfix(tokens);
			if (postfixTokens.Count > 0)
			{
				// Compute expression
				return ComputePostfixExpression(postfixTokens);
			}
		}

		return null;
	}

	[GeneratedRegex("^-?([^-+/*\\(\\)\\^\\s]+)")]
	private static partial Regex NextNumberParsing();
}
