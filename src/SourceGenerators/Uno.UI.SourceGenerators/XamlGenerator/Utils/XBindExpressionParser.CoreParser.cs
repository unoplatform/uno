using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils;

internal partial class XBindExpressionParser
{
	// Internal for testing
	internal sealed class CoreParser
	{
		private readonly string _xBind;
		private int _position;

		public bool IsDone => _position >= _xBind.Length;
		public char Current => IsDone ? '\0' : _xBind[_position];

		public char LookAhead
		{
			get
			{
				if (_position + 1 >= _xBind.Length)
				{
					return '\0';
				}

				return _xBind[_position + 1];
			}
		}

		public CoreParser(string xBind)
		{
			_xBind = xBind;
		}

		public XBindRoot ParseXBind()
		{
			Debug.Assert(_position == 0, "ParseXBind should be called on the whole expression.");
			if (string.IsNullOrWhiteSpace(_xBind))
			{
				return null;
			}

			var bindPath = ParseXBindPath();
			if (IsDone)
			{
				return bindPath;
			}

			if (Current != '(')
			{
				throw new("Expected end of x:Bind expression or start of argument list.");
			}

			_position++;
			if (Current == ')')
			{
				_position++;
				if (IsDone)
				{
					return new XBindInvocation() { Path = bindPath, Arguments = Array.Empty<XBindArgument>() };
				}

				throw new("Expected end of expression after invocation.");
			}


			var arguments = new List<XBindArgument>();
			arguments.Add(ParseXBindArgument());
			while (Current == ',')
			{
				_position++;
				arguments.Add(ParseXBindArgument());
			}

			if (Current != ')')
			{
				throw new("Expected ')' after parsing invocation arguments");
			}

			_position++;
			if (!IsDone)
			{
				throw new("Expected end of expression after invocation.");
			}

			return new XBindInvocation() { Path = bindPath, Arguments = arguments.ToArray() };
		}

		private XBindArgument ParseXBindArgument()
		{
			SkipWhitespaces();
			if (Current == '"')
			{
				// literal string argument
				var oldPosition = _position++;
				while (!IsDone && Current != '"')
				{
					_position++;
				}

				if (Current != '"')
				{
					throw new("Missing closing quote");
				}

				var literal = _xBind.Substring(oldPosition, ++_position - oldPosition);
				return new XBindLiteralArgument() { LiteralArgument = literal };
			}
			else if (char.IsDigit(Current))
			{
				// literal numeric argument
				var oldPosition = _position;
				while (char.IsDigit(Current))
				{
					_position++;
				}

				if (Current == '.')
				{
					_position++;
					if (!char.IsDigit(Current))
					{
						throw new("Expected digits after floating point.");
					}

					while (char.IsDigit(Current))
					{
						_position++;
					}
				}

				var literal = _xBind.Substring(oldPosition, _position - oldPosition);
				return new XBindLiteralArgument() { LiteralArgument = literal };
			}

			return new XBindPathArgument() { Path = ParseXBindPath() };
		}

		private void SkipWhitespaces()
		{
			while (char.IsWhiteSpace(Current))
			{
				_position++;
			}
		}

		private XBindPath ParseXBindPath()
		{
			XBindPath path;
			if (Current == '(')
			{
				_position++;
				var expression = ParseXBindPath();
				if (Current != ')')
				{
					throw new("Missing parenthesis?");
				}

				_position++;

				if (char.IsLetter(Current))
				{
					var expression2 = ParseXBindPath();
					return new XBindCast() { Expression = expression2, Type = expression };
				}

				var isPathlessCast = Current != '.' && Current != '[';
				path = new XBindParenthesizedExpression() { Expression = expression, IsPathlessCast = isPathlessCast };
			}
			else
			{
				path = ParseXBindIdentifier();
			}

			while (true)
			{
				if (Current == '.')
				{
					_position++;
					if (Current == '(')
					{
						_position++;

						var attachedPropertyPath = ParseXBindPath();

						if (attachedPropertyPath is not XBindMemberAccess memberAccess)
						{
							throw new("Expected attached property path to be member access.");
						}

						if (Current != ')')
						{
							throw new("Expected ')' in attached property syntax.");
						}

						path = new XBindAttachedPropertyAccess() { Member = path, PropertyClass = memberAccess.Path, PropertyName = memberAccess.Identifier };

						_position++;
					}
					else
					{
						path = new XBindMemberAccess() { Path = path, Identifier = ParseXBindIdentifier() };
					}
				}
				else if (Current == '[')
				{
					var currentPosition = ++_position;
					while (Current != ']')
					{
						_position++;
					}

					path = new XBindIndexerAccess() { Path = path, Index = _xBind.Substring(currentPosition, _position - currentPosition) };
					_position++;
				}
				else
				{
					break;
				}
			}

			return path;
		}

		private XBindIdentifier ParseXBindIdentifier()
		{
			var identifierStart = _position;
			if (!TryEatValidIdentifier())
			{
				throw new($"Invalid identifier character {Current}");
			}

			if (Current == ':' && LookAhead == ':')
			{
				_position += 2;

				if (!TryEatValidIdentifier())
				{
					throw new($"Invalid identifier character {Current}");
				}
			}

			return new XBindIdentifier() { IdentifierText = _xBind.Substring(identifierStart, _position - identifierStart) };
		}

		private bool TryEatValidIdentifier()
		{
			var foundValid = false;
			while (char.IsLetterOrDigit(Current) || Current == '_')
			{
				_position++;
				foundValid = true;
			}

			return foundValid;
		}
	}
}
