#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal static partial class XBindExpressionParser
	{
		internal static (string[] properties, bool hasFunction) ParseProperties(string rawFunction, Func<string, bool> isStaticMethod)
		{
			if (!string.IsNullOrEmpty(rawFunction))
			{
				var expression = ParseExpression(rawFunction);
					var v = new Visitor(isStaticMethod);
				v.Visit(expression);

					return (v.IdentifierNames.ToArray(), v.HasMethodInvocation);
				}

			return (Array.Empty<string>(), false);
		}

		internal static string Rewrite(string contextName, string rawFunction)
		{
			SyntaxNode expression = ParseExpression(rawFunction);

			expression = new Rewriter(contextName).Visit(expression);

			return expression.ToFullString();
		}

		class Rewriter : CSharpSyntaxRewriter
		{
			private readonly string _contextName;

			public Rewriter(string contextName)
			{
				if (string.IsNullOrEmpty(contextName))
				{
					throw new ArgumentException($"'{contextName}' cannot be null or empty", nameof(contextName));
				}

				_contextName = contextName;
			}

			public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				var e = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;

				var methodName = e.Expression.ToFullString();
				if (!methodName.StartsWith("global::", StringComparison.Ordinal) && !Helpers.IsAttachedPropertySyntax(node.Expression).result)
				{
					return e.WithExpression(SyntaxFactory.ParseExpression($"{_contextName}.{methodName}"));
				}

				return e;
			}

			public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				var e = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;
				var isValidParent = !Helpers.IsInsideMethod(node).result && !Helpers.IsInsideMemberAccessExpression(node).result;

				var isParenthesizedExpression = node.Expression is ParenthesizedExpressionSyntax;
				if (isValidParent
					&& !isParenthesizedExpression
					)
				{
					var isPathLessCast = Helpers.IsPathLessCast(node);
					if (isPathLessCast.result)
					{
						return ParseExpression($"({isPathLessCast.expression?.Expression}){_contextName}");
					}

					var isInsideAttachedPropertySyntax = Helpers.IsInsideAttachedPropertySyntax(node);
					if (isInsideAttachedPropertySyntax.result)
					{
						return ParseExpression($"{_contextName}.{isInsideAttachedPropertySyntax.expression?.Expression.ToString().TrimEnd('.')}");
					}
					else
					{
						var expression = e.ToFullString();
						if (expression.StartsWith("global::", StringComparison.Ordinal))
						{
							return e;
						}
						else
						{
							return ParseExpression($"{_contextName}.{expression}");
						}
					}
				}

				var isAttachedPropertySyntax = Helpers.IsAttachedPropertySyntax(node);
				if (isAttachedPropertySyntax.result)
				{
					if (e.Expression is IdentifierNameSyntax)
					{
						if (
							isAttachedPropertySyntax.expression?.ArgumentList.Arguments.FirstOrDefault() is { } property
							&& property.Expression is MemberAccessExpressionSyntax memberAccessExpression
							)
						{
							return ParseExpression($"{memberAccessExpression.Expression}.Get{memberAccessExpression.Name}");
						}
					}
				}

				return e;
			}

			public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
			{
				var isInsideCast = Helpers.IsInsideCast(node);
				var isValidParent =
					(
						!Helpers.IsInsideMethod(node).result
						&& !Helpers.IsInsideMemberAccessExpression(node).result
						&& !isInsideCast.result
					)
					|| Helpers.IsInsideCastWithParentheses(node).result
					|| Helpers.IsInsideCastAsRoot(node).result
					|| Helpers.IsInsideCastAsMethodArgument(node).result;

				if (isValidParent)
				{
					var newIdentifier = node.ToFullString();

					var rawFunction = string.IsNullOrWhiteSpace(newIdentifier)
						? _contextName
						: $"{_contextName}.{newIdentifier}";

					return ParseExpression(rawFunction);

				}

				return base.VisitIdentifierName(node);
			}
		}

		class Visitor : CSharpSyntaxWalker
		{
			private readonly Func<string, bool> _isStaticMethod;

			public bool HasMethodInvocation { get; private set; }

			public List<string> IdentifierNames { get; } = new List<string>();

			public Visitor(Func<string, bool> isStaticMethod)
			{
				_isStaticMethod = isStaticMethod;
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				base.VisitInvocationExpression(node);

				HasMethodInvocation = true;

				if (!_isStaticMethod(node.Expression.ToFullString()) && node.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					IdentifierNames.Add(memberAccess.Expression.ToString());
				}
			}

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				base.VisitMemberAccessExpression(node);

				var isInsideMethod = Helpers.IsInsideMethod(node);
				var isInsideMemberAccess = Helpers.IsInsideMemberAccessExpression(node);
				var isValidParent = !isInsideMethod.result && !isInsideMemberAccess.result;

				if (isValidParent)
				{
					IdentifierNames.Add(node.ToString());
				}
			}

			public override void VisitIdentifierName(IdentifierNameSyntax node)
			{
				base.VisitIdentifierName(node);

				var isInsideMethod = Helpers.IsInsideMethod(node);
				var isInsideMemberAccess = Helpers.IsInsideMemberAccessExpression(node);
				var isValidParent = !isInsideMethod.result && !isInsideMemberAccess.result;

				if (isValidParent)
				{
					IdentifierNames.Add(node.Identifier.Text);
				}
			}
		}

		private static class Helpers
		{
			internal static (bool result, MemberAccessExpressionSyntax? memberAccess) IsInsideMemberAccessExpression(SyntaxNode node)
				=> IsInside(node, n => n as MemberAccessExpressionSyntax);

			internal static (bool result, InvocationExpressionSyntax? expression) IsInsideMethod(SyntaxNode node)
			{
				var currentNode = node.Parent;
				var child = node;

				do
				{
					if (currentNode is InvocationExpressionSyntax invocation
						&& invocation.Expression == child)
					{
						return (true, invocation);
					}

					child = currentNode;
					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCast(SyntaxNode node)
				=> IsInside(node, n => n as CastExpressionSyntax);

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCastWithParentheses(SyntaxNode node)
				=> IsInside(
					node,
					n => n is CastExpressionSyntax cast
						&& cast.Parent is ParenthesizedExpressionSyntax
						&& cast.Expression == node ? cast : null);

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCastAsMethodArgument(SyntaxNode node)
				=> IsInside(
					node,
					n => n is CastExpressionSyntax cast
						&& cast.Parent is ArgumentSyntax
						&& cast.Expression == node ? cast : null);

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCastAsRoot(SyntaxNode node)
				=> IsInside(
					node,
					n => n is CastExpressionSyntax cast
						&& cast.Parent is null
						&& cast.Expression == node ? cast : null);

			internal static (bool result, T? expression) IsInside<T>(SyntaxNode node, Func<SyntaxNode?, T?> predicate) where T : SyntaxNode
			{
				var currentNode = node.Parent;

				do
				{
					if (predicate(currentNode) is { } cast)
					{
						return (true, cast);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}

			internal static (bool result, ParenthesizedExpressionSyntax? expression) IsPathLessCast(SyntaxNode node)
			{
				var currentNode = node.Parent;

				do
				{
					if (currentNode is ArgumentSyntax arg
						&& arg.Expression is ParenthesizedExpressionSyntax expressionSyntax)
					{
						return (true, expressionSyntax);
					}

					if (currentNode is ParenthesizedExpressionSyntax expressionSyntax2
						&& currentNode.Parent is null)
					{
						return (true, expressionSyntax2);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}

			internal static (bool result, InvocationExpressionSyntax? expression) IsAttachedPropertySyntax(SyntaxNode node)
			{
				var currentNode = node.Parent;

				if (node.GetText().ToString().EndsWith(".", StringComparison.Ordinal))
				{
					do
					{
						if (currentNode is InvocationExpressionSyntax arg)
						{
							return (true, arg);
						}

						currentNode = currentNode?.Parent;
					}
					while (currentNode != null);
				}

				return (false, null);
			}

			internal static (bool result, InvocationExpressionSyntax? expression) IsInsideAttachedPropertySyntax(SyntaxNode node)
			{
				var currentNode = node.Parent;

				do
				{
					if (currentNode is InvocationExpressionSyntax arg
						&& arg.Expression is MemberAccessExpressionSyntax memberAccess
						&& memberAccess.ToString().EndsWith(".", StringComparison.Ordinal))
					{
						return (true, arg);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}
		}
	}
}
