using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils;

/// <summary>
/// The base class for all XBind nodes.
/// </summary>
public abstract class XBindNode
{
}

/// <summary>
/// The root of the tree produced when parsing an x:Bind expression
/// </summary>
/// <remarks>
/// <c>xbind_root → xbind_invocation | xbind_path | '\0'</c>
/// </remarks>
public abstract class XBindRoot : XBindNode
{

}

/// <summary>
/// An invocation expression on the form xbind_path(xbind_path_arg1, xbind_path_arg2, ...)
/// </summary>
/// <remarks>
/// <c>xbind_invocation → xbind_path '(' (xbind_argument (',' xbind_argument)*)? ')'</c>
/// </remarks>
public class XBindInvocation : XBindRoot
{
	public XBindPath Path { get; set; }
	public XBindArgument[] Arguments { get; set; }
}

/// <summary>
/// Represents an argument to <see cref="XBindInvocation"/>
/// </summary>
/// <remarks>
/// xbind_argument → xbind_path_argument | xbind_literal_argument
/// </remarks>
public abstract class XBindArgument : XBindNode
{
}

public class XBindPathArgument : XBindArgument
{
	public XBindPath Path { get; set; }
}


public class XBindLiteralArgument : XBindArgument
{
	public string LiteralArgument { get; set; }
}


/// <summary>
/// The base class for different xbind_path subtypes.
/// </summary>
/// <remarks>
/// <c>xbind_path → xbind_member_access | xbind_indexer_access | xbind_identifier | xbind_attached_property_access | xbind_parenthesized_expression | xbind_cast</c>
/// </remarks>
public abstract class XBindPath : XBindRoot
{

}

/// <summary>
/// An XBind member access with '.'
/// </summary>
/// <remarks>
/// <c>xbind_member_access → xbind_path '.' xbind_identifier</c>
/// </remarks>
public class XBindMemberAccess : XBindPath
{
	public XBindPath Path { get; set; }
	public XBindIdentifier Identifier { get; set; }
}

/// <summary>
/// An XBind index access with '[ ]'
/// </summary>
/// <remarks>
/// <c>xbind_indexer_access → xbind_path '[' .+? ']'</c>
/// </remarks>
public class XBindIndexerAccess : XBindPath
{
	public XBindPath Path { get; set; }
	public string Index { get; set; }
}

/// <summary>
/// An XBind identifier. This consists of letters, digits, and underscores.
/// </summary>
public class XBindIdentifier : XBindPath
{
	public string IdentifierText { get; set; }
}

/// <summary>
/// An XBind attached property access
/// </summary>
/// <remarks>
/// <c>xbind_attached_property_access → xbind_path.(xbind_path.xbind_identifier)</c>
/// </remarks>
public class XBindAttachedPropertyAccess : XBindPath
{
	public XBindPath Member { get; set; }
	public XBindPath PropertyClass { get; set; }
	public XBindIdentifier PropertyName { get; set; }
}

/// <summary>
/// An XBind parenthesized expression.
/// </summary>
/// <remarks>
/// <c>xbind_parenthesized_expression → '(' xbind_path ')'</c>
/// </remarks>
public class XBindParenthesizedExpression : XBindPath
{
	public XBindPath Expression { get; set; }
	public bool IsPathlessCast { get; set; }
}

/// <summary>
/// An XBind parenthesized expression.
/// </summary>
/// <remarks>
/// <c>xbind_parenthesized_expression → '(' xbind_path ')'</c>
/// </remarks>
public class XBindCast : XBindPath
{
	public XBindPath Expression { get; set; }
	public XBindPath Type { get; set; }
}

internal partial class XBindExpressionParser
{
	private sealed class CoreParser
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
				throw new Exception("Expected end of x:Bind expression or start of argument list.");
			}

			_position++;
			if (Current == ')')
			{
				_position++;
				if (IsDone)
				{
					return new XBindInvocation() { Path = bindPath, Arguments = Array.Empty<XBindArgument>() };
				}

				throw new Exception("Expected end of expression after invocation.");
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
				throw new Exception("Expected ')' after parsing invocation arguments");
			}

			_position++;
			if (!IsDone)
			{
				throw new Exception("Expected end of expression after invocation.");
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
					throw new Exception("Missing closing quote");
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
						throw new Exception("Expected digits after floating point.");
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
					throw new Exception("Missing parenthesis?");
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
							throw new Exception("Expected attached property path to be member access.");
						}

						if (Current != ')')
						{
							throw new Exception("Expected ')' in attached property syntax.");
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
				throw new Exception($"Invalid identifier character {Current}");
			}

			if (Current == ':' && LookAhead == ':')
			{
				_position += 2;

				if (!TryEatValidIdentifier())
				{
					throw new Exception($"Invalid identifier character {Current}");
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
